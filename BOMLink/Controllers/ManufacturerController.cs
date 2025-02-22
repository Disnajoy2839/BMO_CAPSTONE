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
using BOMLink.ViewModels.ManufacturerViewModel;

namespace BOMLink.Controllers {
    [Authorize]
    public class ManufacturerController : Controller {
        private readonly BOMLinkContext _context;

        public ManufacturerController(BOMLinkContext context) {
            _context = context;
        }

        // GET: Manufacturer
        public async Task<IActionResult> Index(string searchTerm, string sortBy, string sortOrder) {
            var manufacturers = _context.Manufacturers.AsQueryable();

            // Filtering by search term
            if (!string.IsNullOrEmpty(searchTerm)) {
                searchTerm = searchTerm.ToLower();
                manufacturers = manufacturers.Where(m => m.Name.ToLower().Contains(searchTerm));
            }

            // Sorting logic
            manufacturers = sortBy switch {
                "name" => sortOrder == "desc" ? manufacturers.OrderByDescending(m => m.Name) : manufacturers.OrderBy(m => m.Name),
                _ => manufacturers.OrderBy(m => m.Name)
            };

            var viewModel = new ManufacturerViewModel {
                Manufacturers = await manufacturers.ToListAsync(),
                SearchTerm = searchTerm,
                SortBy = sortBy,
                SortOrder = sortOrder
            };

            return View(viewModel);
        }

        // GET: Manufacturer/CreateOrEdit
        public async Task<IActionResult> CreateOrEdit(int? id) {
            if (id == null) {
                // Creating a new manufacturer
                return View(new Manufacturer());
            }

            // Editing an existing manufacturer
            var manufacturer = await _context.Manufacturers.FindAsync(id);
            if (manufacturer == null) {
                TempData["Error"] = "Manufacturer not found.";
                return RedirectToAction("Index");
            }

            return View(manufacturer);
        }

        // POST: Manufacturer/CreateOrEdit
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateOrEdit(int? id, Manufacturer manufacturer) {
            if (_context.Manufacturers.Any(m => m.Name == manufacturer.Name && m.Id != manufacturer.Id)) {
                TempData["Error"] = "Manufacturer name must be unique.";
                return View(manufacturer);
            }

            if (!ModelState.IsValid) {
                return View(manufacturer);
            }

            if (id == null || id == 0) {
                // Creating a new manufacturer
                _context.Add(manufacturer);
                TempData["Success"] = "Manufacturer added successfully.";
            } else {
                // Editing an existing manufacturer
                _context.Update(manufacturer);
                TempData["Success"] = "Manufacturer updated successfully.";
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        // POST: Manufacturer/Delete
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id) {
            var manufacturer = await _context.Manufacturers
                .Include(m => m.Parts)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (manufacturer == null) {
                TempData["Error"] = "Manufacturer not found.";
                return RedirectToAction(nameof(Index));
            }

            if (manufacturer.Parts.Count > 0) {
                TempData["Error"] = "Cannot delete manufacturer with associated parts.";
                return RedirectToAction(nameof(Index));
            }

            try {
                _context.Manufacturers.Remove(manufacturer);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Manufacturer deleted successfully.";
            } catch (Exception ex) {
                TempData["Error"] = "Error deleting manufacturer: " + ex.Message;
            }

            return RedirectToAction(nameof(Index));
        }

        // GET: Manufacturer/Details
        public async Task<IActionResult> Details(int? id) {
            if (id == null) {
                TempData["Error"] = "Invalid manufacturer ID.";
                return RedirectToAction(nameof(Index));
            }

            var manufacturer = await _context.Manufacturers.FindAsync(id);
            if (manufacturer == null) {
                TempData["Error"] = "Manufacturer not found.";
                return RedirectToAction(nameof(Index));
            }

            return View(manufacturer);
        }

        // Export to CSV
        public IActionResult ExportToCSV() {
            var manufacturers = _context.Manufacturers.ToList();
            var csvBuilder = new StringBuilder();
            csvBuilder.AppendLine("ManufacturerId,Name");

            foreach (var manufacturer in manufacturers) {
                csvBuilder.AppendLine($"{manufacturer.Id},{manufacturer.Name}");
            }

            return File(Encoding.UTF8.GetBytes(csvBuilder.ToString()), "text/csv", "Manufacturers.csv");
        }

        // Export to Excel
        public IActionResult ExportToExcel() {
            var manufacturers = _context.Manufacturers.ToList();
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

            using var package = new ExcelPackage();
            var worksheet = package.Workbook.Worksheets.Add("Manufacturers");
            worksheet.Cells.LoadFromCollection(manufacturers, true);

            using var stream = new MemoryStream();
            package.SaveAs(stream);
            stream.Position = 0;

            return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "Manufacturers.xlsx");
        }

        // Import Manufacturers
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
                    ? $"{importedCount} manufacturer(s) imported successfully."
                    : "No new manufacturers were imported (duplicates may have been skipped).";
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
            if (csv.HeaderRecord == null || !csv.HeaderRecord.Contains("ManufacturerId") || !csv.HeaderRecord.Contains("Name")) {
                TempData["Error"] = "Invalid format: Column headers must be 'ManufacturerId' and 'Name'.";
                return 0;
            }

            // Process each row
            while (csv.Read()) {
                string name = csv.GetField("Name")?.Trim();

                if (!string.IsNullOrEmpty(name) && !_context.Manufacturers.Any(m => m.Name == name)) {
                    _context.Manufacturers.Add(new Manufacturer { Name = name });
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
                TempData["Error"] = "Invalid format: The file must have two columns: 'ManufacturerId' and 'Name'.";
                return 0;
            }

            // Validate headers
            string idHeader = worksheet.Cells[1, 1].Text.Trim().ToLower();
            string nameHeader = worksheet.Cells[1, 2].Text.Trim().ToLower();

            if (idHeader != "manufacturerid" || nameHeader != "name") {
                TempData["Error"] = "Invalid format: Column headers must be 'ManufacturerId' and 'Name'.";
                return 0; 
            }

            // Process each row
            for (int row = 2; row <= worksheet.Dimension.Rows; row++) {
                string name = worksheet.Cells[row, 2].Text.Trim();

                if (!string.IsNullOrEmpty(name) && !_context.Manufacturers.Any(m => m.Name == name)) {
                    _context.Manufacturers.Add(new Manufacturer { Name = name });
                    count++;
                }
            }

            await _context.SaveChangesAsync();
            return count;
        }
    }
}
