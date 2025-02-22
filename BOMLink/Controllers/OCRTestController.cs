using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using BOMLink.Services;

namespace BOMLink.Controllers {
    public class OCRTestController : Controller {
        private readonly AzureOCRServiceTest _ocrService;

        public OCRTestController(AzureOCRServiceTest ocrService) {
            _ocrService = ocrService;
        }

        public IActionResult Upload() {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Upload(IFormFile file) {
            if (file == null || file.Length == 0) {
                TempData["Error"] = "Please upload a valid image or PDF.";
                return RedirectToAction("Index");
            }

            using var stream = file.OpenReadStream();
            var extractedTable = await _ocrService.ExtractTableFromImageAsync(stream);

            return View("OCRResult", extractedTable);
        }
    }
}
