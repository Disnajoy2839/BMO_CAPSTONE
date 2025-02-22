using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using BOMLink.Data;
using BOMLink.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using MailKit.Net.Smtp;
using MimeKit;
using OfficeOpenXml;
using MailKit.Security;

namespace BOMLink.Services {
    public class RFQService {
        private readonly BOMLinkContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public RFQService(BOMLinkContext context, UserManager<ApplicationUser> userManager) {
            _context = context;
            _userManager = userManager;
        }

        // Generate Excel attachment for RFQ email
        private byte[] GenerateRFQExcel(RFQ rfq) {
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
            using var package = new ExcelPackage();
            var worksheet = package.Workbook.Worksheets.Add($"RFQ-{rfq.Id:D6}");

            // Headers
            var headers = new[] { "Part Number", "Description", "Quantity", "Manufacturer", "Price", "UOM", "ETA"};
            for (int i = 0; i < headers.Length; i++) {
                worksheet.Cells[1, i + 1].Value = headers[i];
                worksheet.Cells[1, i + 1].Style.Font.Bold = true;
                worksheet.Cells[1, i + 1].AutoFitColumns();
            }

            int row = 2;
            foreach (var item in rfq.RFQItems) {
                worksheet.Cells[row, 1].Value = item.BOMItem.Part.PartNumber;
                worksheet.Cells[row, 2].Value = item.BOMItem.Part.Description;
                worksheet.Cells[row, 3].Value = item.Quantity;
                worksheet.Cells[row, 4].Value = item.BOMItem.Part.Manufacturer?.Name ?? "";
                worksheet.Cells[row, 5].Value = item.Price.HasValue ? item.Price.Value : (double?)null;
                worksheet.Cells[row, 5].Style.Numberformat.Format = "$#,##0.00";
                worksheet.Cells[row, 6].Value = string.IsNullOrEmpty(item.UOM) ? "" : item.UOM;
                worksheet.Cells[row, 7].Value = string.IsNullOrEmpty(item.ETA) ? "" : item.ETA;

                row++;
            }

            using var stream = new MemoryStream();
            package.SaveAs(stream);
            return stream.ToArray();
        }

        // Generate email body for RFQ email
        public async Task<bool> SendRFQEmail(int rfqId) {
            var rfq = await _context.RFQs
                .Include(r => r.Supplier)
                .Include(r => r.BOM)
                .Include(r => r.CreatedBy)
                .Include(r => r.RFQItems)
                .ThenInclude(ri => ri.BOMItem)
                .ThenInclude(bi => bi.Part)
                .ThenInclude(p => p.Manufacturer)
                .FirstOrDefaultAsync(r => r.Id == rfqId);

            if (rfq == null) {
                throw new Exception($"RFQ {rfqId} not found.");
            }

            // Use ApplicationUser.Email as sender email
            string senderEmail = rfq.CreatedBy.Email ?? throw new Exception("Sender email not found.");
            string subject = $"RFQ-{rfq.Id:D6} - {rfq.CreatedBy.UserName} - {rfq.BOM.Description}";
            string recipientEmail = rfq.Supplier.ContactEmail ?? throw new Exception("Supplier email not found.");

            // HTML Email Body
            var emailBody = new StringBuilder();
            emailBody.AppendLine($"<p>Dear {rfq.Supplier.Name},</p>");
            emailBody.AppendLine("<p>Please find attached the RFQ details.</p>");
            emailBody.AppendLine($"<p><strong>RFQ Number:</strong> RFQ-{rfq.Id:D6}</p>");
            emailBody.AppendLine($"<p><strong>Created By:</strong> {rfq.CreatedBy.UserName}</p>");
            emailBody.AppendLine($"<p><strong>Supplier:</strong> {rfq.Supplier.Name}</p>");
            emailBody.AppendLine($"<p><strong>Due Date:</strong> {rfq.DueDate:yyyy-MM-dd}</p>");
            emailBody.AppendLine("<p><strong>Requested Items:</strong></p>");

            // Table Formatting
            emailBody.AppendLine(@"
            <table border='1' cellpadding='5' cellspacing='0' style='border-collapse: collapse; width: 100%; text-align: left; font-family: Arial, sans-serif;'>
                <tr style='background-color: #f2f2f2; font-weight: bold;'>
                    <th style='padding: 8px;'>Part Number</th>
                    <th style='padding: 8px;'>Description</th>
                    <th style='padding: 8px;'>Quantity</th>
                    <th style='padding: 8px;'>Manufacturer</th>
                    <th style='padding: 8px;'>Price</th>
                    <th style='padding: 8px;'>UOM</th>
                    <th style='padding: 8px;'>ETA</th>

                </tr>");

            foreach (var item in rfq.RFQItems) {
                emailBody.AppendLine($@"
                <tr>
                    <td style='padding: 8px;'>{item.BOMItem.Part.PartNumber}</td>
                    <td style='padding: 8px;'>{item.BOMItem.Part.Description}</td>
                    <td style='padding: 8px;'>{item.Quantity}</td>
                    <td style='padding: 8px;'>{item.BOMItem.Part.Manufacturer?.Name ?? ""}</td>
                    <td style='padding: 8px;'>{(item.Price.HasValue ? item.Price.Value.ToString("0.00") : "")}</td>
                    <td style='padding: 8px;'>{(string.IsNullOrEmpty(item.UOM) ? "" : item.UOM)}</td>
                    <td style='padding: 8px;'>{(string.IsNullOrEmpty(item.ETA) ? "" : item.ETA)}</td>
                </tr>");
            }

            emailBody.AppendLine("</table>");
            emailBody.AppendLine($"<p>Kind Regards,</p>");
            emailBody.AppendLine($"<p>{rfq.CreatedBy.FullName}</p>");

            // Generate Excel File
            var excelFile = GenerateRFQExcel(rfq);

            return await SendEmailWithAttachment(senderEmail, recipientEmail, subject, emailBody.ToString(), excelFile);
        }

        // Send RFQ email to supplier
        private async Task<bool> SendEmailWithAttachment(string fromEmail, string toEmail, string subject, string body, byte[] attachmentBytes) {
            try {
                var email = new MimeMessage();
                email.From.Add(new MailboxAddress("BOMLink System", fromEmail));
                email.To.Add(new MailboxAddress("", toEmail));
                email.Subject = subject;

                var builder = new BodyBuilder { HtmlBody = body };
                builder.Attachments.Add($"{subject}.xlsx", attachmentBytes, ContentType.Parse("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"));
                email.Body = builder.ToMessageBody();

                using var smtp = new SmtpClient();
                await smtp.ConnectAsync("smtp.mail.yahoo.com", 465, SecureSocketOptions.SslOnConnect);
                await smtp.AuthenticateAsync(fromEmail, "sgvxjqjypdivqahd"); // App password
                await smtp.SendAsync(email);
                await smtp.DisconnectAsync(true);

                return true;
            } catch (Exception ex) {
                Console.WriteLine($"Email send failed: {ex.Message}");
                return false;
            }
        }
    }
}