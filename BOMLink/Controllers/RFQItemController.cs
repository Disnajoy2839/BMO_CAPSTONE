using BOMLink.Data;
using BOMLink.Models;
using BOMLink.ViewModels.RFQItemViewModel;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;
using System.Text;

namespace BOMLink.Controllers {
    public class RFQItemController : Controller {
        private readonly BOMLinkContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public RFQItemController(BOMLinkContext context, UserManager<ApplicationUser> userManager) {
            _context = context;
            _userManager = userManager;
        }

        // GET: RFQItem
        public async Task<IActionResult> Index(int rfqId) {
            var rfq = await _context.RFQs
                .Include(r => r.Supplier)
                .FirstOrDefaultAsync(r => r.Id == rfqId);

            if (rfq == null) {
                return NotFound();
            }

            var items = await _context.RFQItems
                .Where(ri => ri.RFQId == rfqId)
                .Include(ri => ri.BOMItem)
                .ThenInclude(bi => bi.Part)
                .Select(ri => new RFQItemViewModel {
                    Id = ri.Id,
                    RFQId = rfqId,
                    BOMItemId = ri.BOMItemId,
                    PartNumber = ri.BOMItem.Part.PartNumber,
                    Description = ri.BOMItem.Part.Description,
                    Quantity = ri.Quantity,
                    UOM = ri.UOM ?? string.Empty,
                    Price = ri.Price,
                    ETA = ri.ETA ?? string.Empty
                }).ToListAsync();

            var viewModel = new RFQItemListViewModel {
                RFQId = rfqId,
                RFQNumber = $"RFQ-{rfqId:D6}",
                SupplierName = rfq.Supplier.Name,
                Items = items,
                CanEdit = rfq.Status == RFQ.RFQStatus.Draft,
            };

            return View(viewModel);
        }

        // GET: RFQItem/BulkUpdate
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> BulkUpdate(int rfqId, List<RFQItemUpdateViewModel> rfqItems, List<int> deleteIds) {
            if (rfqItems == null && (deleteIds == null || deleteIds.Count == 0)) {
                TempData["Error"] = "No changes detected.";
                return RedirectToAction("Index", new { rfqId });
            }

            using var transaction = await _context.Database.BeginTransactionAsync();

            try {
                if (deleteIds != null && deleteIds.Any()) {
                    var itemsToDelete = _context.RFQItems.Where(ri => deleteIds.Contains(ri.Id));
                    _context.RFQItems.RemoveRange(itemsToDelete);
                }

                if (rfqItems != null && rfqItems.Any()) {
                    var existingItems = await _context.RFQItems
                        .Where(ri => ri.RFQId == rfqId)
                        .ToListAsync();

                    foreach (var item in rfqItems) {
                        var rfqItem = existingItems.FirstOrDefault(ri => ri.Id == item.Id);
                        if (rfqItem != null) {
                            rfqItem.Quantity = item.Quantity;
                            rfqItem.UOM = item.UOM;
                            rfqItem.Price = item.Price;
                            rfqItem.ETA = item.ETA;
                            rfqItem.UpdatedAt = DateTime.UtcNow;
                        }
                    }
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                TempData["Success"] = "RFQ Items updated successfully.";
                return RedirectToAction("Index", new { rfqId });
            } catch (Exception) {
                await transaction.RollbackAsync();
                TempData["Error"] = "Error updating RFQ items.";
                return RedirectToAction("Index", new { rfqId });
            }
        }

        [HttpPost]
        public async Task<IActionResult> ProcessSupplierSelection(int bomId, Dictionary<int, int> selectedSuppliers) {
            if (selectedSuppliers == null || !selectedSuppliers.Any()) {
                TempData["Error"] = "You must select at least one supplier.";
                return RedirectToAction("SelectSuppliers", new { bomId });
            }

            return await ProcessRFQGeneration(bomId, selectedSuppliers);
        }

        private async Task<IActionResult> ProcessRFQGeneration(int bomId, Dictionary<int, int> selectedSuppliers) {
            using var transaction = await _context.Database.BeginTransactionAsync();

            try {
                var supplierGroups = selectedSuppliers
                    .GroupBy(kvp => kvp.Value)
                    .ToDictionary(g => g.Key, g => g.Select(kvp => kvp.Key).ToList());

                foreach (var supplierGroup in supplierGroups) {
                    int supplierId = supplierGroup.Key;
                    var manufacturerIdsForSupplier = supplierGroup.Value;

                    var existingRFQ = await _context.RFQs
                        .FirstOrDefaultAsync(r => r.BOMId == bomId && r.SupplierId == supplierId);

                    if (existingRFQ == null) {
                        existingRFQ = new RFQ {
                            SupplierId = supplierId,
                            BOMId = bomId,
                            UserId = _userManager.GetUserId(User),
                            Status = RFQ.RFQStatus.Draft,
                            CreatedAt = DateTime.UtcNow,
                            UpdatedAt = DateTime.UtcNow
                        };

                        _context.RFQs.Add(existingRFQ);
                        await _context.SaveChangesAsync();
                    }

                    var bomItemsForSupplier = await _context.BOMItems
                        .Where(bi => manufacturerIdsForSupplier.Contains(bi.Part.ManufacturerId))
                        .ToListAsync();

                    foreach (var bomItem in bomItemsForSupplier) {
                        if (!_context.RFQItems.Any(ri => ri.RFQId == existingRFQ.Id && ri.BOMItemId == bomItem.Id)) {
                            _context.RFQItems.Add(new RFQItem {
                                RFQId = existingRFQ.Id,
                                BOMItemId = bomItem.Id,
                                Quantity = bomItem.Quantity
                            });
                        }
                    }
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return RedirectToAction("Index", "RFQ");
            } catch {
                await transaction.RollbackAsync();
                return RedirectToAction("Index", "BOM");
            }
        }

        // POST: RFQItem/Delete
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id) {
            var rfqItem = await _context.RFQItems
                .Include(ri => ri.RFQ)
                .FirstOrDefaultAsync(ri => ri.Id == id);

            if (rfqItem == null) {
                TempData["Error"] = "RFQ Item not found.";
                return RedirectToAction("Index", new { rfqId = rfqItem?.RFQId });
            }

            // Prevent deletion if RFQ is already sent
            if (rfqItem.RFQ.Status != RFQ.RFQStatus.Draft) {
                TempData["Error"] = "You cannot delete RFQ items after sending the RFQ.";
                return RedirectToAction("Index", new { rfqId = rfqItem.RFQId });
            }

            try {
                _context.RFQItems.Remove(rfqItem);
                await _context.SaveChangesAsync();

                TempData["Success"] = "RFQ Item deleted successfully.";
            } catch (Exception ex) {
                TempData["Error"] = "Error deleting RFQ Item: " + ex.Message;
            }

            return RedirectToAction("Index", new { rfqId = rfqItem.RFQId });
        }

        // GET: RFQ/ExportToCSV
        public IActionResult ExportToCSV(int rfqId) {
            var rfq = _context.RFQs
                .Include(r => r.Supplier)
                .Include(r => r.RFQItems)
                .ThenInclude(ri => ri.BOMItem)
                .ThenInclude(bi => bi.Part)
                .ThenInclude(p => p.Manufacturer)
                .FirstOrDefault(r => r.Id == rfqId);

            if (rfq == null) return NotFound();

            var csvBuilder = new StringBuilder();
            csvBuilder.AppendLine("PartNumber,Description,Quantity,UOM,Manufacturer,Supplier,Price,ETA");

            foreach (var item in rfq.RFQItems) {
                csvBuilder.AppendLine($"{item.BOMItem.Part.PartNumber}," +
                                      $"{item.BOMItem.Part.Description}," +
                                      $"{item.Quantity}," +
                                      $"{(string.IsNullOrEmpty(item.UOM) ? String.Empty : item.UOM)}," +
                                      $"{item.BOMItem.Part.Manufacturer?.Name ?? String.Empty}," +
                                      $"{rfq.Supplier.Name}," +
                                      $"{(item.Price.HasValue ? item.Price.Value.ToString("0.00") : String.Empty)}," +
                                      $"{(string.IsNullOrEmpty(item.ETA) ? String.Empty : item.ETA)}");
            }

            string fileName = $"{rfq.RFQNumber}.csv"; // File named as RFQNumber

            return File(Encoding.UTF8.GetBytes(csvBuilder.ToString()), "text/csv", fileName);
        }

        // GET: RFQ/ExportToExcel
        public IActionResult ExportToExcel(int rfqId) {
            var rfq = _context.RFQs
                .Include(r => r.Supplier)
                .Include(r => r.RFQItems)
                .ThenInclude(ri => ri.BOMItem)
                .ThenInclude(bi => bi.Part)
                .ThenInclude(p => p.Manufacturer)
                .FirstOrDefault(r => r.Id == rfqId);

            if (rfq == null) return NotFound();

            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
            using var package = new ExcelPackage();
            var worksheet = package.Workbook.Worksheets.Add(rfq.RFQNumber);

            // Headers
            worksheet.Cells[1, 1].Value = "Part Number";
            worksheet.Cells[1, 2].Value = "Description";
            worksheet.Cells[1, 3].Value = "Quantity";
            worksheet.Cells[1, 4].Value = "UOM";
            worksheet.Cells[1, 5].Value = "Manufacturer";
            worksheet.Cells[1, 6].Value = "Supplier";
            worksheet.Cells[1, 7].Value = "Price";
            worksheet.Cells[1, 8].Value = "ETA";

            int row = 2;
            foreach (var item in rfq.RFQItems) {
                worksheet.Cells[row, 1].Value = item.BOMItem.Part.PartNumber;
                worksheet.Cells[row, 2].Value = item.BOMItem.Part.Description;
                worksheet.Cells[row, 3].Value = item.Quantity;
                worksheet.Cells[row, 4].Value = string.IsNullOrEmpty(item.UOM) ? String.Empty : item.UOM;
                worksheet.Cells[row, 5].Value = item.BOMItem.Part.Manufacturer?.Name ?? String.Empty;
                worksheet.Cells[row, 6].Value = rfq.Supplier.Name;
                worksheet.Cells[row, 7].Value = item.Price.HasValue ? item.Price.Value.ToString("0.00") : String.Empty;
                worksheet.Cells[row, 8].Value = string.IsNullOrEmpty(item.ETA) ? String.Empty : item.ETA;
                row++;
            }

            using var stream = new MemoryStream();
            package.SaveAs(stream);
            stream.Position = 0;

            string fileName = $"{rfq.RFQNumber}.xlsx"; // File named as RFQNumber

            return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        }
    }
}
