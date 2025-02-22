using BOMLink.Data;
using BOMLink.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;
using CsvHelper;
using CsvHelper.Configuration;
using System.Globalization;
using System.Text;
using BOMLink.ViewModels.SupplierViewModels;

namespace BOMLink.Controllers {
    [Authorize]
    public class SupplierController : Controller {
        private readonly BOMLinkContext _context;

        public SupplierController(BOMLinkContext context) {
            _context = context;
        }

        public async Task<IActionResult> Index(string searchTerm, string sortBy, string sortOrder) {
            var suppliers = _context.Suppliers.AsQueryable();

            // Filtering by search term
            if (!string.IsNullOrEmpty(searchTerm)) {
                searchTerm = searchTerm.ToLower();
                suppliers = suppliers.Where(s =>
                    s.Name.ToLower().Contains(searchTerm) ||
                    s.SupplierCode.ToLower().Contains(searchTerm) ||
                    s.ContactEmail.ToLower().Contains(searchTerm));
            }

            // Sorting logic
            suppliers = sortBy switch {
                "name" => sortOrder == "desc" ? suppliers.OrderByDescending(s => s.Name) : suppliers.OrderBy(s => s.Name),
                "code" => sortOrder == "desc" ? suppliers.OrderByDescending(s => s.SupplierCode) : suppliers.OrderBy(s => s.SupplierCode),
                _ => suppliers.OrderBy(s => s.Name)
            };

            var viewModel = new SupplierViewModel {
                Suppliers = await suppliers.ToListAsync(),
                SearchTerm = searchTerm,
                SortBy = sortBy,
                SortOrder = sortOrder
            };

            return View(viewModel);
        }

        //// GET: Supplier/Create
        //public IActionResult Create() => View();

        //// POST: Supplier/Create
        //[HttpPost]
        //[ValidateAntiForgeryToken]
        //public async Task<IActionResult> Create([Bind("Name, ContactName, ContactPhone, ContactEmail, Address, City, Province, SupplierCode")] Supplier supplier) {
        //    if (_context.Suppliers.Any(s => s.Name == supplier.Name || s.SupplierCode == supplier.SupplierCode)) {
        //        TempData["Error"] = "Supplier name and code must be unique.";
        //        return View(supplier);
        //    }

        //    if (ModelState.IsValid) {
        //        _context.Add(supplier);
        //        await _context.SaveChangesAsync();
        //        TempData["Success"] = "Supplier added successfully.";
        //        return RedirectToAction(nameof(Index));
        //    }

        //    return View(supplier);
        //}

        //// GET: Supplier/Edit
        //public async Task<IActionResult> Edit(int? id) {
        //    if (id == null) {
        //        TempData["Error"] = "Invalid supplier ID.";
        //        return RedirectToAction(nameof(Index));
        //    }

        //    var supplier = await _context.Suppliers.FindAsync(id);
        //    if (supplier == null) {
        //        TempData["Error"] = "Supplier not found.";
        //        return RedirectToAction(nameof(Index));
        //    }

        //    return View(supplier);
        //}

        //// POST: Supplier/Edit
        //[HttpPost]
        //[ValidateAntiForgeryToken]
        //public async Task<IActionResult> Edit(int id, [Bind("Id, Name, ContactName, ContactPhone, ContactEmail, Address, City, Province, SupplierCode")] Supplier supplier) {
        //    if (id != supplier.Id) return NotFound();

        //    if (_context.Suppliers.Any(s => (s.Name == supplier.Name || s.SupplierCode == supplier.SupplierCode) && s.Id != supplier.Id)) {
        //        TempData["Error"] = "Supplier name and code must be unique.";
        //        return View(supplier);
        //    }

        //    if (ModelState.IsValid) {
        //        try {
        //            _context.Update(supplier);
        //            await _context.SaveChangesAsync();
        //            TempData["Success"] = "Supplier edited successfully.";
        //        } catch (DbUpdateConcurrencyException) {
        //            if (!_context.Suppliers.Any(e => e.Id == id))
        //                return NotFound();
        //            else
        //                throw;
        //        }
        //        return RedirectToAction(nameof(Index));
        //    }
        //    return View(supplier);
        //}

        // GET: Supplier/CreateOrEdit
        [HttpGet]
        public async Task<IActionResult> CreateOrEdit(int? id) {
            if (id == null) {
                // Creating a new supplier
                return View(new CreateOrEditSupplierViewModel());
            }

            var supplier = await _context.Suppliers.FindAsync(id);
            if (supplier == null) return NotFound();

            var viewModel = new CreateOrEditSupplierViewModel {
                Id = supplier.Id,
                Name = supplier.Name,
                ContactName = supplier.ContactName,
                ContactPhone = supplier.ContactPhone,
                ContactEmail = supplier.ContactEmail,
                Address = supplier.Address,
                City = supplier.City,
                Province = supplier.Province,
                SupplierCode = supplier.SupplierCode
            };

            return View(viewModel);
        }

        // POST: Supplier/CreateOrEdit
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateOrEdit(CreateOrEditSupplierViewModel viewModel) {
            if (_context.Suppliers.Any(s => (s.Name == viewModel.Name || s.SupplierCode == viewModel.SupplierCode) && s.Id != viewModel.Id)) {
                TempData["Error"] = "Supplier name and code must be unique.";
                return View(viewModel);
            }

            if (!ModelState.IsValid) return View(viewModel);

            if (viewModel.Id == null) {
                // Create new supplier
                var newSupplier = new Supplier {
                    Name = viewModel.Name,
                    ContactName = viewModel.ContactName,
                    ContactPhone = viewModel.ContactPhone,
                    ContactEmail = viewModel.ContactEmail,
                    Address = viewModel.Address,
                    City = viewModel.City,
                    Province = viewModel.Province,
                    SupplierCode = viewModel.SupplierCode,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                _context.Suppliers.Add(newSupplier);
                TempData["Success"] = "Supplier added successfully.";
            } else {
                // Edit existing supplier
                var supplier = await _context.Suppliers.FindAsync(viewModel.Id);
                if (supplier == null) return NotFound();

                supplier.Name = viewModel.Name;
                supplier.ContactName = viewModel.ContactName;
                supplier.ContactPhone = viewModel.ContactPhone;
                supplier.ContactEmail = viewModel.ContactEmail;
                supplier.Address = viewModel.Address;
                supplier.City = viewModel.City;
                supplier.Province = viewModel.Province;
                supplier.SupplierCode = viewModel.SupplierCode;
                supplier.UpdatedAt = DateTime.UtcNow;

                _context.Suppliers.Update(supplier);
                TempData["Success"] = "Supplier updated successfully.";
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        // POST: Supplier/Delete
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id) {
            var supplier = await _context.Suppliers.FindAsync(id);
            if (supplier == null) {
                TempData["Error"] = "Supplier not found.";
                return RedirectToAction(nameof(Index));
            }

            if (_context.RFQs.Any(r => r.SupplierId == id)) {
                TempData["Error"] = "Supplier cannot be deleted because it is associated with one or more RFQs.";
                return RedirectToAction(nameof(Index));
            }

            try {
                _context.Suppliers.Remove(supplier);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Supplier deleted successfully.";
            } catch (Exception ex) {
                TempData["Error"] = "Error deleting supplier: " + ex.Message;
            }

            return RedirectToAction(nameof(Index));
        }

        // GET: Supplier/Details/{id}
        [HttpGet]
        public async Task<IActionResult> Details(int id) {
            var supplier = await _context.Suppliers.FindAsync(id);
            if (supplier == null) return NotFound();

            var viewModel = new SupplierDetailsViewModel {
                Id = supplier.Id,
                Name = supplier.Name,
                ContactName = supplier.ContactName,
                ContactPhone = supplier.ContactPhone,
                ContactEmail = supplier.ContactEmail,
                Address = supplier.Address,
                City = supplier.City,
                Province = supplier.Province,
                SupplierCode = supplier.SupplierCode,
                CreatedAt = supplier.CreatedAt,
                UpdatedAt = supplier.UpdatedAt
            };

            return View(viewModel);
        }

        // Export to CSV
        public IActionResult ExportToCSV() {
            var suppliers = _context.Suppliers.ToList();
            var csvBuilder = new StringBuilder();
            csvBuilder.AppendLine("Id,Name,ContactName,ContactPhone,ContactEmail,Address,City,Province,SupplierCode");

            foreach (var supplier in suppliers) {
                csvBuilder.AppendLine($"{supplier.Id},{supplier.Name},{supplier.ContactName},{supplier.ContactPhone},{supplier.ContactEmail},{supplier.Address},{supplier.City},{supplier.Province},{supplier.SupplierCode}");
            }

            return File(Encoding.UTF8.GetBytes(csvBuilder.ToString()), "text/csv", "Suppliers.csv");
        }

        // Export to Excel
        public IActionResult ExportToExcel() {
            var suppliers = _context.Suppliers.ToList();
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

            using var package = new ExcelPackage();
            var worksheet = package.Workbook.Worksheets.Add("Suppliers");
            worksheet.Cells.LoadFromCollection(suppliers, true);

            using var stream = new MemoryStream();
            package.SaveAs(stream);
            stream.Position = 0;

            return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "Suppliers.xlsx");
        }

        // Import Suppliers
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
                    ? $"{importedCount} supplier(s) imported successfully."
                    : "No new suppliers were imported (duplicates may have been skipped).";
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
            if (csv.HeaderRecord == null || !csv.HeaderRecord.Contains("Name") || !csv.HeaderRecord.Contains("ContactEmail") || !csv.HeaderRecord.Contains("SupplierCode")) {
                TempData["Error"] = "Invalid format: Column headers must include 'Name', 'ContactEmail' and 'SupplierCode'.";
                return 0;
            }

            // Process each row
            while (csv.Read()) {
                string name = csv.GetField("Name")?.Trim();
                string contactEmail = csv.GetField("ContactEmail")?.Trim();
                string contactName = csv.GetField("ContactName")?.Trim();
                string contactPhone = csv.GetField("ContactPhone")?.Trim();
                string address = csv.GetField("Address")?.Trim();
                string city = csv.GetField("City")?.Trim();
                string province = csv.GetField("Province")?.Trim();
                string supplierCode = csv.GetField("SupplierCode")?.Trim();

                if (!string.IsNullOrEmpty(name) && !string.IsNullOrEmpty(contactEmail) && !string.IsNullOrEmpty(supplierCode) && !_context.Suppliers.Any(s => s.Name == name) && !_context.Suppliers.Any(s => s.SupplierCode == supplierCode)) {
                    _context.Suppliers.Add(new Supplier {
                        Name = name,
                        ContactEmail = contactEmail,
                        ContactName = contactName,
                        ContactPhone = contactPhone,
                        Address = address,
                        City = city,
                        Province = province,
                        SupplierCode = supplierCode
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

            // Ensure the file has at least 3 columns
            if (worksheet.Dimension.Columns < 3) {
                TempData["Error"] = "Invalid format: The file must have at least two columns: 'Name', 'ContactEmail' and 'SupplierCode'.";
                return 0;
            }

            // Validate headers
            string nameHeader = worksheet.Cells[1, 2].Text.Trim().ToLower();
            string emailHeader = worksheet.Cells[1, 5].Text.Trim().ToLower();
            string codeHeader = worksheet.Cells[1, 9].Text.Trim().ToLower();

            if (nameHeader != "name" || emailHeader != "contactemail" || codeHeader != "suppliercode") {
                TempData["Error"] = "Invalid format: Column headers must include 'Name', 'ContactEmail' and 'SupplierCode'.";
                return 0;
            }

            // Process each row
            for (int row = 2; row <= worksheet.Dimension.Rows; row++) {
                string name = worksheet.Cells[row, 2].Text.Trim();
                string contactName = worksheet.Cells[row, 3].Text.Trim();
                string contactPhone = worksheet.Cells[row, 4].Text.Trim();
                string contactEmail = worksheet.Cells[row, 5].Text.Trim();
                string address = worksheet.Cells[row, 6].Text.Trim();
                string city = worksheet.Cells[row, 7].Text.Trim();
                string province = worksheet.Cells[row, 8].Text.Trim();
                string supplierCode = worksheet.Cells[row, 9].Text.Trim();

                if (!string.IsNullOrEmpty(name) && !string.IsNullOrEmpty(contactEmail) && !string.IsNullOrEmpty(supplierCode) && !_context.Suppliers.Any(s => s.Name == name) && !_context.Suppliers.Any(s => s.SupplierCode == supplierCode)) {
                    _context.Suppliers.Add(new Supplier {
                        Name = name,
                        ContactEmail = contactEmail,
                        ContactName = contactName,
                        ContactPhone = contactPhone,
                        Address = address,
                        City = city,
                        Province = province,
                        SupplierCode = supplierCode
                    });
                    count++;
                }
            }

            await _context.SaveChangesAsync();
            return count;
        }

    }
}