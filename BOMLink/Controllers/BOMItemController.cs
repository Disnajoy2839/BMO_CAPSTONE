

using System.Globalization;
using System.Text;
using BOMLink.Data;
using BOMLink.Models;
using BOMLink.ViewModels.BOMItemViewModels;
using CsvHelper.Configuration;
using CsvHelper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;
using Microsoft.Data.SqlClient;
using BOMLink.Services;

namespace BOMLink.Controllers {
    [Authorize]
    public class BOMItemController : Controller {
        private readonly BOMLinkContext _context;
        private readonly AzureOCRService _ocrService;

        public BOMItemController(BOMLinkContext context, AzureOCRService ocrService) {
            _context = context;
            _ocrService = ocrService;
        }

        // GET: BOMItem/Index
        public async Task<IActionResult> Index(int bomId, string searchTerm, string manufacturerFilter, string sortBy, string sortOrder) {
            var bom = await _context.BOMs
                .Include(b => b.BOMItems)
                .ThenInclude(bi => bi.Part)
                .ThenInclude(bi => bi.Manufacturer)
                .FirstOrDefaultAsync(b => b.Id == bomId);

            if (bom == null) return NotFound();

            var query = bom.BOMItems.AsQueryable();

            // Search Functionality
            if (!string.IsNullOrEmpty(searchTerm)) {
                query = query.Where(bi =>
                    (bi.Part.PartNumber != null && bi.Part.PartNumber.ToLower().Contains(searchTerm.ToLower())) ||
                    (bi.Part.Description != null && bi.Part.Description.ToLower().Contains(searchTerm.ToLower())) ||
                    (bi.Part.Manufacturer != null && bi.Part.Manufacturer.Name.ToLower().Contains(searchTerm.ToLower()))
                );
            }

            // Filter by Manufacturer
            if (!string.IsNullOrEmpty(manufacturerFilter)) {
                query = query.Where(bi => bi.Part.Manufacturer != null && bi.Part.Manufacturer.Name == manufacturerFilter);
            }

            // Sorting Functionality
            sortOrder = string.IsNullOrEmpty(sortOrder) ? "asc" : sortOrder.ToLower();
            query = sortBy switch {
                "partNumber" => sortOrder == "asc" ? query.OrderBy(bi => bi.Part.PartNumber) : query.OrderByDescending(bi => bi.Part.PartNumber),
                "description" => sortOrder == "asc" ? query.OrderBy(bi => bi.Part.Description) : query.OrderByDescending(bi => bi.Part.Description),
                "quantity" => sortOrder == "asc" ? query.OrderBy(bi => bi.Quantity) : query.OrderByDescending(bi => bi.Quantity),
                "manufacturer" => sortOrder == "asc" ? query.OrderBy(bi => bi.Part.Manufacturer.Name) : query.OrderByDescending(bi => bi.Part.Manufacturer.Name),
                _ => query.OrderBy(bi => bi.Part.PartNumber)
            };

            // Get available manufacturers for filter dropdown
            var availableManufacturers = bom.BOMItems
                .Where(bi => bi.Part.Manufacturer != null)
                .Select(bi => bi.Part.Manufacturer.Name)
                .Distinct()
                .OrderBy(m => m)
                .ToList();

            var viewModel = new BOMItemViewModel {
                BOMId = bom.Id,
                BOMNumber = $"BOM-{bom.Id:D6}",
                Description = bom.Description,
                Status = bom.Status.ToString(),
                SearchTerm = searchTerm,
                ManufacturerFilter = manufacturerFilter,
                AvailableManufacturers = availableManufacturers,
                SortBy = sortBy,
                SortOrder = sortOrder,
                BOMItems = query.Select(bi => new BOMItemViewModel {
                    Id = bi.Id,
                    PartNumber = bi.Part.PartNumber,
                    PartDescription = bi.Part.Description,
                    Quantity = bi.Quantity,
                    Manufacturer = bi.Part.Manufacturer.Name ?? "N/A"
                }).ToList()
            };

            return View(viewModel);
        }

        // GET: Create or Edit BOM Item
        [HttpGet]
        public async Task<IActionResult> CreateOrEdit(int? id, int bomId) {
            var bom = await _context.BOMs
                .Include(b => b.BOMItems)
                .FirstOrDefaultAsync(b => b.Id == bomId);

            if (bom == null) return NotFound();

            var existingPartIds = bom.BOMItems.Select(bi => bi.PartId).ToList();

            var viewModel = new CreateOrEditBOMItemViewModel {
                BOMId = bom.Id,
                BOMNumber = $"BOM-{bom.Id:D6}",
                ExistingPartIds = existingPartIds,
                AvailableParts = await _context.Parts
                    .OrderBy(p => p.PartNumber)
                    .Select(p => new SelectListItem { Value = p.Id.ToString(), Text = $"{p.PartNumber} - {p.Description}" })
                    .ToListAsync()
            };

            if (id.HasValue) {
                var bomItem = await _context.BOMItems.FirstOrDefaultAsync(bi => bi.Id == id);
                if (bomItem == null) {
                    TempData["Error"] = "BOM Item not found!";
                    return RedirectToAction("Index", new { bomId });
                }

                viewModel.Id = bomItem.Id;
                viewModel.PartId = bomItem.PartId;
                viewModel.Quantity = bomItem.Quantity;
                viewModel.Notes = bomItem.Notes;
            }

            return View(viewModel);
        }

        // POST: Create or Edit BOM Item
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateOrEdit(CreateOrEditBOMItemViewModel model) {
            ModelState.Remove("BOMNumber");

            bool partExists = await _context.BOMItems
                .AnyAsync(bi => bi.BOMId == model.BOMId && bi.PartId == model.PartId && bi.Id != model.Id);

            if (partExists) {
                ModelState.AddModelError("PartId", "This part has already been added to the BOM.");
            }

            if (!ModelState.IsValid) {
                model.AvailableParts = await _context.Parts
                    .OrderBy(p => p.PartNumber)
                    .Select(p => new SelectListItem { Value = p.Id.ToString(), Text = $"{p.PartNumber} - {p.Description}" })
                    .ToListAsync();
                return View(model);
            }

            if (model.Id == 0) // Create new BOMItem
            {
                var newItem = new BOMItem {
                    BOMId = model.BOMId,
                    PartId = model.PartId,
                    Quantity = model.Quantity,
                    Notes = string.IsNullOrWhiteSpace(model.Notes) ? null : model.Notes
                };

                _context.BOMItems.Add(newItem);
            } else // Edit existing BOMItem
              {
                var existingBOMItem = await _context.BOMItems.FindAsync(model.Id);
                if (existingBOMItem == null) return NotFound();

                existingBOMItem.PartId = model.PartId;
                existingBOMItem.Quantity = model.Quantity;
                existingBOMItem.Notes = model.Notes;
            }

            await _context.SaveChangesAsync();

            var bom = await _context.BOMs.FindAsync(model.BOMId);
            if (bom != null) {
                bom.UpdateStatus();
                bom.IncrementVersion();
                bom.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }

            TempData["Success"] = "BOM item saved successfully.";
            return RedirectToAction("Index", new { bomId = model.BOMId });
        }

        // POST: Delete BOM Item
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id) {
            var bomItem = await _context.BOMItems.Include(bi => bi.BOM).FirstOrDefaultAsync(bi => bi.Id == id);
            if (bomItem == null) return NotFound();

            // Prevent deleting items in Approved BOMs
            if (bomItem.BOM.Status == BOMStatus.Locked) {
                TempData["Error"] = "Cannot delete items from an Approved BOM.";
                return RedirectToAction("Index", new { bomId = bomItem.BOMId });
            }

            _context.BOMItems.Remove(bomItem);

            await _context.SaveChangesAsync();

            var bom = await _context.BOMs.FindAsync(bomItem.BOMId);
            if (bom != null) {
                bom.UpdateStatus();
                bom.IncrementVersion();
                bom.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }

            TempData["Success"] = "BOM Item deleted successfully.";
            return RedirectToAction("Index", new { bomId = bomItem.BOMId });
        }

        // GET: BOMItem/ExportToCSV
        public IActionResult ExportToCSV(int bomId) {
            var bom = _context.BOMs
                .Include(b => b.BOMItems)
                .ThenInclude(bi => bi.Part)
                .ThenInclude(p => p.Manufacturer)
                .FirstOrDefault(b => b.Id == bomId);

            if (bom == null) return NotFound();

            var csvBuilder = new StringBuilder();
            csvBuilder.AppendLine("PartNumber,Description,Quantity,Manufacturer");

            foreach (var item in bom.BOMItems) {
                csvBuilder.AppendLine($"{item.Part.PartNumber},{item.Part.Description},{item.Quantity},{item.Part.Manufacturer?.Name}");
            }

            string fileName = $"{bom.BOMNumber}.csv"; // File named as BOMNumber

            return File(Encoding.UTF8.GetBytes(csvBuilder.ToString()), "text/csv", fileName);
        }

        // GET: BOMItem/ExportToExcel
        public IActionResult ExportToExcel(int bomId) {
            var bom = _context.BOMs
                .Include(b => b.BOMItems)
                .ThenInclude(bi => bi.Part)
                .ThenInclude(p => p.Manufacturer)
                .FirstOrDefault(b => b.Id == bomId);

            if (bom == null) return NotFound();

            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
            using var package = new ExcelPackage();
            var worksheet = package.Workbook.Worksheets.Add(bom.BOMNumber);

            // Headers
            worksheet.Cells[1, 1].Value = "Part Number";
            worksheet.Cells[1, 2].Value = "Description";
            worksheet.Cells[1, 3].Value = "Quantity";
            worksheet.Cells[1, 4].Value = "Manufacturer";

            int row = 2;
            foreach (var item in bom.BOMItems) {
                worksheet.Cells[row, 1].Value = item.Part.PartNumber;
                worksheet.Cells[row, 2].Value = item.Part.Description;
                worksheet.Cells[row, 3].Value = item.Quantity;
                worksheet.Cells[row, 4].Value = item.Part.Manufacturer?.Name;
                row++;
            }

            using var stream = new MemoryStream();
            package.SaveAs(stream);
            stream.Position = 0;

            string fileName = $"{bom.BOMNumber}.xlsx"; // File named as BOMNumber

            return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        }

        // GET: BOMItem/Import
        public IActionResult Import(int bomId) {
            var viewModel = new ImportBOMItemsViewModel {
                BOMId = bomId
            };
            return View(viewModel);
        }

        // POST: BOMItem/Import
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Import(IFormFile file, int bomId) {
            if (file == null || file.Length == 0) {
                TempData["Error"] = "Please select a valid file.";
                return RedirectToAction("Index", new { bomId });
            }

            var fileExtension = Path.GetExtension(file.FileName).ToLower();

            return fileExtension switch {
                ".csv" => await HandleCSVImport(file, bomId),
                ".xlsx" => await HandleExcelImport(file, bomId),
                ".jpg" or ".jpeg" or ".png" or ".pdf" => await HandleOCRImport(file, bomId),
                _ => HandleInvalidFileType(bomId)
            };
        }

        private IActionResult HandleInvalidFileType(int bomId) {
            TempData["Error"] = "Invalid file format. Please upload a CSV, Excel, or Image/PDF file.";
            return RedirectToAction("Index", new { bomId });
        }

        // Process Import
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ProcessImport(ImportBOMItemsViewModel model) {
            if (string.IsNullOrEmpty(model.FilePath) && model.ExtractedParts == null) {
                TempData["Error"] = "No data found for import.";
                return RedirectToAction("Index", new { bomId = model.BOMId });
            }

            int importedCount = 0;

            try {
                if (!string.IsNullOrEmpty(model.FilePath)) {
                    var fileExtension = Path.GetExtension(model.FilePath).ToLower();

                    importedCount = fileExtension switch {
                        ".csv" => await ImportCsv(model),
                        ".xlsx" => await ImportExcel(model),
                        ".jpg" or ".jpeg" or ".png" or ".pdf" => await ImportOCR(model), // 🔹 Added OCR handling
                        _ => throw new Exception($"Invalid file format. {fileExtension}")
                    };
                } else if (model.ExtractedParts != null) {
                    importedCount = await ImportOCR(model);
                }

                TempData["Success"] = importedCount > 0
                    ? $"{importedCount} BOM item(s) imported successfully."
                    : "No new items were imported. Please check your data.";

            } catch (Exception ex) {
                TempData["Error"] = "Error importing file: " + ex.Message;
            }

            return RedirectToAction("Index", new { bomId = model.BOMId });
        }


        // Handle CSV Import
        private async Task<IActionResult> HandleCSVImport(IFormFile file, int bomId) {
            // Save temporary file
            var tempFilePath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.csv");
            using (var stream = new FileStream(tempFilePath, FileMode.Create)) {
                await file.CopyToAsync(stream);
            }

            var viewModel = new ImportBOMItemsViewModel {
                BOMId = bomId,
                FilePath = tempFilePath
            };

            return View("SelectColumns", viewModel);
        }


        // Import BOM Items from CSV file
        private async Task<int> ImportCsv(ImportBOMItemsViewModel model) {
            int count = 0;
            var partQuantities = new Dictionary<string, int>(); // Store cumulative part quantities

            var bom = await _context.BOMs
                .Include(b => b.BOMItems)
                .Include(b => b.DraftBOMItems)
                .FirstOrDefaultAsync(b => b.Id == model.BOMId);

            if (bom == null) return 0;

            using var reader = new StreamReader(model.FilePath);
            using var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture) {
                HeaderValidated = null,
                MissingFieldFound = null
            });

            csv.Read();
            csv.ReadHeader();

            while (csv.Read()) {
                string partNumberAux = csv.GetField(model.PartNumberColumn)?.Trim();
                string partNumber = new string(partNumberAux.Where(char.IsLetterOrDigit).ToArray()).ToUpper();
                string quantityText = csv.GetField(model.QuantityColumn - 1)?.Trim();

                if (string.IsNullOrEmpty(partNumber) || string.IsNullOrEmpty(quantityText)) continue;
                if (!int.TryParse(quantityText, out int quantity) || quantity < 1) continue;

                if (partQuantities.ContainsKey(partNumber)) {
                    partQuantities[partNumber] += quantity; // Accumulate quantity
                } else {
                    partQuantities[partNumber] = quantity;
                }
            }

            count = await AddPartsToBOM(model.BOMId, partQuantities);

            return count;
        }

        // Handle Excel Import
        private async Task<IActionResult> HandleExcelImport(IFormFile file, int bomId) {
            // Save temporary file
            var tempFilePath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.xlsx");
            using (var stream = new FileStream(tempFilePath, FileMode.Create)) {
                await file.CopyToAsync(stream);
            }

            var viewModel = new ImportBOMItemsViewModel {
                BOMId = bomId,
                FilePath = tempFilePath
            };

            return View("SelectColumns", viewModel);
        }

        // Import BOM Items from Excel file
        private async Task<int> ImportExcel(ImportBOMItemsViewModel model) {
            int count = 0;
            var partQuantities = new Dictionary<string, int>(); // Store cumulative part quantities
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

            var bom = await _context.BOMs
                .Include(b => b.BOMItems)
                .Include(b => b.DraftBOMItems)
                .FirstOrDefaultAsync(b => b.Id == model.BOMId);

            if (bom == null) return 0;

            using var stream = new MemoryStream(System.IO.File.ReadAllBytes(model.FilePath));
            using var package = new ExcelPackage(stream);
            var worksheet = package.Workbook.Worksheets[0];

            for (int row = 2; row <= worksheet.Dimension.Rows; row++) {
                string partNumberAux = worksheet.Cells[row, model.PartNumberColumn].Text.Trim();
                string partNumber = new string(partNumberAux.Where(char.IsLetterOrDigit).ToArray()).ToUpper();
                string quantityText = worksheet.Cells[row, model.QuantityColumn].Text.Trim();

                if (string.IsNullOrEmpty(partNumber) || string.IsNullOrEmpty(quantityText)) continue;
                if (!int.TryParse(quantityText, out int quantity) || quantity < 1) continue;

                if (partQuantities.ContainsKey(partNumber)) {
                    partQuantities[partNumber] += quantity; // Accumulate quantity
                } else {
                    partQuantities[partNumber] = quantity;
                }
            }

            count = await AddPartsToBOM(model.BOMId, partQuantities);

            return count;
        }

        // Handle OCR Import
        private async Task<IActionResult> HandleOCRImport(IFormFile file, int bomId) {
            using var stream = file.OpenReadStream();
            var extractedText = await _ocrService.ExtractTextFromImageAsync(stream);

            // Convert extracted text into a structured list of OCRModel
            var extractedParts = ParsePartsAndQuantities(extractedText);

            // If the extracted data contains only two columns, proceed to import immediately
            if (extractedParts.Count > 0 && extractedParts.All(p => p.PartNumber != null && p.Quantity > 0)) {
                var model = new ImportBOMItemsViewModel {
                    BOMId = bomId,
                    ExtractedParts = extractedParts
                };
                return View("SelectColumns", model); // Let the user choose columns
            }

            // If OCR result is empty or not formatted properly, show an error
            TempData["Error"] = "OCR extraction failed or the file format is incorrect.";
            return RedirectToAction("Index", new { bomId });
        }


        // Import BOM Items from OCR
        private async Task<int> ImportOCR(ImportBOMItemsViewModel model) {
            int count = 0;
            var partQuantities = new Dictionary<string, int>(); // Store cumulative part quantities

            var bom = await _context.BOMs
                .Include(b => b.BOMItems)
                .Include(b => b.DraftBOMItems)
                .FirstOrDefaultAsync(b => b.Id == model.BOMId);

            if (bom == null) return 0;

            foreach (var entry in model.ExtractedParts) {
                string partNumberAux = entry.PartNumber.Trim();
                string partNumber = new string(partNumberAux.Where(char.IsLetterOrDigit).ToArray()).ToUpper();
                int quantity = entry.Quantity;

                if (string.IsNullOrEmpty(partNumber) || quantity <= 0) continue;

                // If part already exists in dictionary, accumulate quantity
                if (partQuantities.ContainsKey(partNumber)) {
                    partQuantities[partNumber] += quantity;
                } else {
                    partQuantities[partNumber] = quantity;
                }
            }

            count = await AddPartsToBOM(model.BOMId, partQuantities);

            return count;
        }

        // Add Parts to BOM
        private async Task<int> AddPartsToBOM(int bomId, Dictionary<string, int> partQuantities) {
            int count = 0;
            List<string> missingParts = new List<string>();

            var bom = await _context.BOMs
                .Include(b => b.BOMItems)
                .Include(b => b.DraftBOMItems)
                .FirstOrDefaultAsync(b => b.Id == bomId);

            if (bom == null) return 0;

            foreach (var entry in partQuantities) {
                string partNumber = entry.Key;
                int totalQuantity = entry.Value;

                var part = await _context.Parts.FirstOrDefaultAsync(p => p.PartNumber == partNumber);
                if (part == null) {
                    var existingDraft = await _context.DraftBOMItems.FirstOrDefaultAsync(d => d.BOMId == bomId && d.PartNumber == partNumber);
                    if (existingDraft != null) {
                        existingDraft.Quantity += totalQuantity; // Accumulate in Draft
                    } else {
                        _context.DraftBOMItems.Add(new DraftBOMItem {
                            BOMId = bomId,
                            PartNumber = partNumber,
                            Quantity = totalQuantity,
                            IsResolved = false
                        });
                    }

                    missingParts.Add(partNumber);
                    continue;
                }

                var existingBOMItem = await _context.BOMItems
                    .FirstOrDefaultAsync(bi => bi.BOMId == bomId && bi.PartId == part.Id);

                if (existingBOMItem != null) {
                    existingBOMItem.Quantity += totalQuantity; // Accumulate in BOM
                } else {
                    _context.BOMItems.Add(new BOMItem {
                        BOMId = bomId,
                        PartId = part.Id,
                        Quantity = totalQuantity
                    });
                }

                count++;
            }

            await _context.SaveChangesAsync();

            if (bom != null) {
                bom.UpdateStatus();
                bom.IncrementVersion();
                bom.UpdatedAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();

            if (missingParts.Count > 0) {
                TempData["Info"] = "Some parts were not found and need review.";
            }

            return count;
        }

        // GET: Review Draft Items
        public async Task<IActionResult> ReviewDraftItems(int bomId) {
            var draftItems = await _context.DraftBOMItems
                .Where(d => d.BOMId == bomId && !d.IsResolved)
                .ToListAsync();

            ViewData["BOMId"] = bomId;

            var existingParts = await _context.Parts.Select(p => p.PartNumber).ToListAsync();
            ViewData["ExistingParts"] = existingParts;

            return View(draftItems);
        }

        // GET: Confirm Part
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ConfirmPart(int id) {
            var draftItem = await _context.DraftBOMItems.FindAsync(id);
            if (draftItem == null) return NotFound();

            var part = await _context.Parts.FirstOrDefaultAsync(p => p.PartNumber == draftItem.PartNumber);
            if (part == null) {
                TempData["Error"] = "This part still does not exist in the database.";
                return RedirectToAction(nameof(ReviewDraftItems), new { bomId = draftItem.BOMId });
            }

            // Move to BOM Items
            _context.BOMItems.Add(new BOMItem {
                BOMId = draftItem.BOMId,
                PartId = part.Id,
                Quantity = draftItem.Quantity
            });

            // Mark as resolved
            draftItem.IsResolved = true;
            var bom = await _context.BOMs
                .Include(b => b.BOMItems)
                .Include(b => b.DraftBOMItems)
                .FirstOrDefaultAsync(b => b.Id == draftItem.BOMId);

            if (draftItem.IsResolved) {
                _context.DraftBOMItems.Remove(draftItem);
            }

            await _context.SaveChangesAsync();

            if (bom != null) {
                bom.UpdateStatus();
                bom.IncrementVersion();
                bom.UpdatedAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();

            TempData["Success"] = "Part added to BOM successfully.";
            return RedirectToAction(nameof(ReviewDraftItems), new { bomId = draftItem.BOMId });
        }

        // Post: Delete Draft Item
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteDraftItem(int id) {
            var draftItem = await _context.DraftBOMItems.FindAsync(id);
            if (draftItem == null) return NotFound();

            _context.DraftBOMItems.Remove(draftItem);

            var bom = await _context.BOMs
                .Include(b => b.BOMItems)
                .Include(b => b.DraftBOMItems)
                .FirstOrDefaultAsync(b => b.Id == draftItem.BOMId);

            await _context.SaveChangesAsync();

            if (bom != null) {
                bom.UpdateStatus();
                bom.IncrementVersion();
                bom.UpdatedAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();

            TempData["Success"] = "Draft BOM item removed.";
            return RedirectToAction(nameof(ReviewDraftItems), new { bomId = draftItem.BOMId });
        }

        // GET: Search Parts for AutoComplete
        [HttpGet]
        public async Task<IActionResult> SearchParts(string term) {
            var parts = await _context.Parts
                .Where(p => p.PartNumber.Contains(term) || p.Description.Contains(term))
                .OrderBy(p => p.PartNumber)
                .Select(p => new {
                    id = p.Id,
                    partNumber = p.PartNumber,
                    description = p.Description
                })
                .Take(10) // Limit results for better performance
                .ToListAsync();

            return Json(parts);
        }

        // GET: BOMItem/GetPartDetails
        public async Task<IActionResult> GetPartDetails(int partId) {
            var part = await _context.Parts
                .Where(p => p.Id == partId)
                .Select(p => new {
                    p.PartNumber,
                    p.Description
                })
                .FirstOrDefaultAsync();

            if (part == null) return NotFound();

            return Json(part);
        }

        /// <summary>
        /// Parses extracted text and identifies Part Numbers and Quantities.
        /// </summary>
        /// <param name="extractedText">Raw text extracted from the file.</param>
        /// <returns>List of OCRModel objects with PartNumber and Quantity.</returns>
        private List<OCRModel> ParsePartsAndQuantities(string extractedText) {
            var parts = new List<OCRModel>();
            var lines = extractedText.Split("\n", StringSplitOptions.RemoveEmptyEntries);

            for (int i = 0; i < lines.Length - 1; i++) {
                string partNumber = lines[i].Trim();
                string nextLine = lines[i + 1].Trim();

                // Check if next line is a valid quantity
                if (int.TryParse(nextLine, out int quantity) && !string.IsNullOrEmpty(partNumber)) {
                    parts.Add(new OCRModel { PartNumber = partNumber, Quantity = quantity });
                }
            }

            return parts;
        }
    }
}
