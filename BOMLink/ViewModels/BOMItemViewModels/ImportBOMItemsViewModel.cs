using BOMLink.Models;
using System.ComponentModel.DataAnnotations;

namespace BOMLink.ViewModels.BOMItemViewModels {
    public class ImportBOMItemsViewModel {
        #region Properties
        public int BOMId { get; set; }
        public string FilePath { get; set; }

        [Required(ErrorMessage = "Please enter the Part Number column number.")]
        [Range(1, int.MaxValue, ErrorMessage = "Column number must be greater than 0.")]
        public int PartNumberColumn { get; set; }

        [Required(ErrorMessage = "Please enter the Quantity column number.")]
        [Range(1, int.MaxValue, ErrorMessage = "Column number must be greater than 0.")]
        public int QuantityColumn { get; set; }

        public List<OCRModel> ExtractedParts { get; set; } = new List<OCRModel>();
        #endregion
    }
}
