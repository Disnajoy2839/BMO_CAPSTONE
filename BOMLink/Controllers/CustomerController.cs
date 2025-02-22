using BOMLink.Data;
using BOMLink.Models;
using BOMLink.ViewModels.CustomerViewModel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;
using CsvHelper;
using CsvHelper.Configuration;
using System.Globalization;
using System.Text;
using BOMLink.ViewModels.CustomerViewModels;

namespace BOMLink.Controllers {
    [Authorize]
    public class CustomerController : Controller {
        private readonly BOMLinkContext _context;

        public CustomerController(BOMLinkContext context) {
            _context = context;
        }

        // GET: Customer
        public async Task<IActionResult> Index(string searchTerm, string sortBy, string sortOrder) {
            var customers = _context.Customers.AsQueryable();

            // Filtering by search term
            if (!string.IsNullOrEmpty(searchTerm)) {
                searchTerm = searchTerm.ToLower();
                customers = customers.Where(c => c.Name.ToLower().Contains(searchTerm) || c.CustomerCode.ToLower().Contains(searchTerm));
            }

            // Sorting logic
            customers = sortBy switch {
                "name" => sortOrder == "desc" ? customers.OrderByDescending(c => c.Name) : customers.OrderBy(c => c.Name),
                "code" => sortOrder == "desc" ? customers.OrderByDescending(c => c.CustomerCode) : customers.OrderBy(c => c.CustomerCode),
                _ => customers.OrderBy(c => c.Name)
            };

            var viewModel = new CustomerViewModel {
                Customers = await customers.ToListAsync(),
                SearchTerm = searchTerm,
                SortBy = sortBy,
                SortOrder = sortOrder
            };

            return View(viewModel);
        }

        // GET: Customer/CreateOrEdit
        [HttpGet]
        public async Task<IActionResult> CreateOrEdit(int? id) {
            if (id == null) {
                // Creating a new customer
                return View(new CreateOrEditCustomerViewModel());
            }

            var customer = await _context.Customers.FindAsync(id);
            if (customer == null) return NotFound();

            var viewModel = new CreateOrEditCustomerViewModel {
                Id = customer.Id,
                Name = customer.Name,
                CustomerCode = customer.CustomerCode,
                Address = customer.Address,
                City = customer.City,
                Province = customer.Province,
                ContactName = customer.ContactName,
                ContactPhone = customer.ContactPhone,
                ContactEmail = customer.ContactEmail
            };

            return View(viewModel);
        }

        // POST: Customer/CreateOrEdit
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateOrEdit(CreateOrEditCustomerViewModel viewModel) {
            if (_context.Customers.Any(c => (c.Name == viewModel.Name || c.CustomerCode == viewModel.CustomerCode) && c.Id != viewModel.Id)) {
                TempData["Error"] = "Customer name and code must be unique.";
                return View(viewModel);
            }

            if (!ModelState.IsValid) return View(viewModel);

            if (viewModel.Id == null) {
                // Create new customer
                var newCustomer = new Customer {
                    Name = viewModel.Name,
                    CustomerCode = viewModel.CustomerCode,
                    Address = viewModel.Address,
                    City = viewModel.City,
                    Province = viewModel.Province,
                    ContactName = viewModel.ContactName,
                    ContactPhone = viewModel.ContactPhone,
                    ContactEmail = viewModel.ContactEmail,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                _context.Customers.Add(newCustomer);
                TempData["Success"] = "Customer added successfully.";
            } else {
                // Edit existing customer
                var customer = await _context.Customers.FindAsync(viewModel.Id);
                if (customer == null) return NotFound();

                customer.Name = viewModel.Name;
                customer.CustomerCode = viewModel.CustomerCode;
                customer.Address = viewModel.Address;
                customer.City = viewModel.City;
                customer.Province = viewModel.Province;
                customer.ContactName = viewModel.ContactName;
                customer.ContactPhone = viewModel.ContactPhone;
                customer.ContactEmail = viewModel.ContactEmail;
                customer.UpdatedAt = DateTime.UtcNow;

                _context.Customers.Update(customer);
                TempData["Success"] = "Customer updated successfully.";
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        // POST: Customer/Delete
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id) {
            var customer = await _context.Customers
                        .Include(c => c.Jobs)
                        .FirstOrDefaultAsync(c => c.Id == id);
            if (customer == null) {
                TempData["Error"] = "Customer not found.";
                return RedirectToAction(nameof(Index));
            }

            if (customer.Jobs.Any()) {
                TempData["Error"] = "This customer cannot be deleted because it has associated job numbers.";
                return RedirectToAction(nameof(Index));
            }

            try {
                _context.Customers.Remove(customer);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Customer deleted successfully.";
            } catch (Exception ex) {
                TempData["Error"] = "Error deleting customer: " + ex.Message;
            }

            return RedirectToAction(nameof(Index));
        }

        // GET: Customer/Details/{id}
        [HttpGet]
        public async Task<IActionResult> Details(int id) {
            var customer = await _context.Customers.FindAsync(id);
            if (customer == null) return NotFound();

            var viewModel = new CustomerDetailsViewModel {
                Id = customer.Id,
                Name = customer.Name,
                CustomerCode = customer.CustomerCode,
                Address = customer.Address,
                City = customer.City,
                Province = customer.Province,
                ContactName = customer.ContactName,
                ContactPhone = customer.ContactPhone,
                ContactEmail = customer.ContactEmail,
                CreatedAt = customer.CreatedAt,
                UpdatedAt = customer.UpdatedAt
            };

            return View(viewModel);
        }

        // Export to CSV
        public IActionResult ExportToCSV() {
            var customers = _context.Customers.ToList();
            var csvBuilder = new StringBuilder();
            csvBuilder.AppendLine("Id,Name,CustomerCode,Address,City,Province,ContactName,ContactPhone,ContactEmail");

            foreach (var customer in customers) {
                csvBuilder.AppendLine($"{customer.Id},{customer.Name},{customer.CustomerCode},{customer.Address},{customer.City},{customer.Province},{customer.ContactName},{customer.ContactPhone},{customer.ContactEmail}");
            }

            return File(Encoding.UTF8.GetBytes(csvBuilder.ToString()), "text/csv", "Customers.csv");
        }

        // Export to Excel
        public IActionResult ExportToExcel() {
            var customers = _context.Customers.ToList();
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

            using var package = new ExcelPackage();
            var worksheet = package.Workbook.Worksheets.Add("Customers");
            worksheet.Cells.LoadFromCollection(customers, true);

            using var stream = new MemoryStream();
            package.SaveAs(stream);
            stream.Position = 0;

            return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "Customers.xlsx");
        }

        // Import Customers
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Import(IFormFile file) {
            if (file == null || file.Length == 0) {
                TempData["Error"] = "Please select a file to upload.";
                return RedirectToAction(nameof(Index));
            }

            var fileExtension = Path.GetExtension(file.FileName).ToLower();
            int importedCount = 0;

            try {
                importedCount = fileExtension switch {
                    ".csv" => await ImportCsv(file),
                    ".xlsx" => await ImportExcel(file),
                    _ => throw new Exception("Invalid file format. Please upload a CSV or Excel file.")
                };

                if (TempData["Error"] == null) {
                    TempData[importedCount > 0 ? "Success" : "Info"] = importedCount > 0
                    ? $"{importedCount} customer(s) imported successfully."
                    : "No new customers were imported (duplicates may have been skipped).";
                }
            } catch (Exception ex) {
                TempData["Error"] = "Error importing file: " + ex.Message;
            }

            return RedirectToAction(nameof(Index));
        }

        // CSV Import Method
        private async Task<int> ImportCsv(IFormFile file) {
            int count = 0;

            using var reader = new StreamReader(file.OpenReadStream());
            using var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture) {
                HeaderValidated = null,
                MissingFieldFound = null
            });

            // Read header row
            csv.Read();
            csv.ReadHeader();

            // Validate required columns
            if (csv.HeaderRecord == null || !csv.HeaderRecord.Contains("Name") || !csv.HeaderRecord.Contains("CustomerCode")) {
                TempData["Error"] = "Invalid format: Column headers must include 'Name' and 'CustomerCode'.";
                return 0;
            }

            // Process each row
            while (csv.Read()) {
                string name = csv.GetField("Name")?.Trim();
                string customerCode = csv.GetField("CustomerCode")?.Trim();
                string address = csv.GetField("Address")?.Trim();
                string city = csv.GetField("City")?.Trim();
                string province = csv.GetField("Province")?.Trim();
                string contactName = csv.GetField("ContactName")?.Trim();
                string contactPhone = csv.GetField("ContactPhone")?.Trim();
                string contactEmail = csv.GetField("ContactEmail")?.Trim();

                if (!string.IsNullOrEmpty(name) && !string.IsNullOrEmpty(customerCode) && !_context.Customers.Any(c => c.CustomerCode == customerCode)) {
                    _context.Customers.Add(new Customer {
                        Name = name,
                        CustomerCode = customerCode,
                        Address = address,
                        City = city,
                        Province = province,
                        ContactName = contactName,
                        ContactPhone = contactPhone,
                        ContactEmail = contactEmail
                    });
                    count++;
                }
            }

            await _context.SaveChangesAsync();
            return count;
        }

        // Excel Import Method
        private async Task<int> ImportExcel(IFormFile file) {
            int count = 0;
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

            using var stream = new MemoryStream();
            await file.CopyToAsync(stream);
            using var package = new ExcelPackage(stream);
            var worksheet = package.Workbook.Worksheets[0];

            // Ensure the file has at least 2 columns
            if (worksheet.Dimension.Columns < 2) {
                TempData["Error"] = "Invalid format: The file must have at least 'Name' and 'CustomerCode'.";
                return 0;
            }

            // Validate headers
            string nameHeader = worksheet.Cells[1, 2].Text.Trim().ToLower();
            string codeHeader = worksheet.Cells[1, 3].Text.Trim().ToLower();

            if (nameHeader != "name" || codeHeader != "customercode") {
                TempData["Error"] = "Invalid format: Column headers must include 'Name' and 'CustomerCode'.";
                return 0;
            }

            // Process each row
            for (int row = 2; row <= worksheet.Dimension.Rows; row++) {
                string name = worksheet.Cells[row, 2].Text.Trim();
                string customerCode = worksheet.Cells[row, 3].Text.Trim();
                string address = worksheet.Cells[row, 4].Text.Trim();
                string city = worksheet.Cells[row, 5].Text.Trim();
                string province = worksheet.Cells[row, 6].Text.Trim();
                string contactName = worksheet.Cells[row, 7].Text.Trim();
                string contactPhone = worksheet.Cells[row, 8].Text.Trim();
                string contactEmail = worksheet.Cells[row, 9].Text.Trim();

                if (!string.IsNullOrEmpty(name) && !string.IsNullOrEmpty(customerCode) && !_context.Customers.Any(c => c.CustomerCode == customerCode)) {
                    _context.Customers.Add(new Customer {
                        Name = name,
                        CustomerCode = customerCode,
                        Address = address,
                        City = city,
                        Province = province,
                        ContactName = contactName,
                        ContactPhone = contactPhone,
                        ContactEmail = contactEmail
                    });
                    count++;
                }
            }

            await _context.SaveChangesAsync();
            return count;
        }
    }
}
