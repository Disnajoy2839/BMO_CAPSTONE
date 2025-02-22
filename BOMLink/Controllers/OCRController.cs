using BOMLink.Services;
using Microsoft.AspNetCore.Mvc;
using BOMLink.Models;

namespace BOMLink.Controllers {
    public class OCRController : Controller {
        private readonly AzureOCRService _ocrService; // Injected Azure OCR Service

        // Constructor to initialize the OCR service
        public OCRController(AzureOCRService ocrService) {
            _ocrService = ocrService;
        }

        /// <summary>
        /// Handles file upload and OCR processing.
        /// Extracts text from an uploaded image/PDF and parses BOM data.
        /// </summary>
        /// <param name="file">Uploaded file (image or PDF).</param>
        /// <returns>Redirects to Upload page with extracted data.</returns>
        [HttpPost]
        public async Task<IActionResult> Upload(IFormFile file) {
            // Validate if a file was uploaded
            if (file == null || file.Length == 0)
                return BadRequest("No file uploaded.");

            using var stream = file.OpenReadStream();

            // Step 1: Extract text using Azure OCR
            var extractedText = await _ocrService.ExtractTextFromImageAsync(stream);

            // Step 2: Parse extracted text into structured BOM data
            var extractedParts = ParsePartsAndQuantities(extractedText);
            System.Diagnostics.Debug.WriteLine("Parsed OCR Data: " + System.Text.Json.JsonSerializer.Serialize(extractedParts));

            // Step 3: Store extracted BOM data temporarily using TempData
            TempData["ExtractedParts"] = System.Text.Json.JsonSerializer.Serialize(extractedParts);

            // Redirect to GET Upload action to display results
            return RedirectToAction("Upload");
        }

        /// <summary>
        /// Parses extracted text and identifies Part Numbers and Quantities.
        /// </summary>
        /// <param name="extractedText">Raw text extracted from the file.</param>
        /// <returns>List of OCRModel objects with PartNumber and Quantity.</returns>
        private List<OCRModel> ParsePartsAndQuantities(string extractedText) {
            var parts = new List<OCRModel>();
            var lines = extractedText.Split("\n", StringSplitOptions.RemoveEmptyEntries);

            // Loop through each line and match part numbers with their corresponding quantities
            for (int i = 0; i < lines.Length - 1; i++) {
                string partNumber = lines[i].Trim();  // Read part number
                string nextLine = lines[i + 1].Trim(); // Read next line (potential quantity)

                // If next line contains a valid quantity, store the part and quantity
                if (int.TryParse(nextLine, out int quantity) && !string.IsNullOrEmpty(partNumber)) {
                    System.Diagnostics.Debug.WriteLine($"Parsed Part: {partNumber}, Quantity: {quantity}");
                    parts.Add(new OCRModel { PartNumber = partNumber, Quantity = quantity });
                }
            }

            return parts;
        }

        /// <summary>
        /// Displays the Upload page and shows extracted BOM data (if available).
        /// </summary>
        /// <returns>View with extracted BOM data.</returns>
        [HttpGet]
        public IActionResult Upload() {
            // Retrieve extracted data from TempData
            if (TempData["ExtractedParts"] != null) {
                var extractedPartsJson = TempData["ExtractedParts"].ToString();
                System.Diagnostics.Debug.WriteLine("Retrieved TempData in Upload(): " + extractedPartsJson);

                // Deserialize JSON string into a List of OCRModel objects
                ViewBag.ExtractedParts = System.Text.Json.JsonSerializer.Deserialize<List<OCRModel>>(extractedPartsJson);
            }

            return View();
        }
    }
}
