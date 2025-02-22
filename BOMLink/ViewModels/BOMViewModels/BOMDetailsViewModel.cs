using BOMLink.ViewModels.BOMItemViewModels;

namespace BOMLink.ViewModels.BOMViewModels {
    public class BOMDetailsViewModel {
        #region Properties
        public int BOMId { get; set; }
        public string BOMNumber { get; set; }
        public string Status { get; set; }
        public string CustomerName { get; set; }
        public string JobNumber { get; set; }
        public string CreatedBy { get; set; }
        public DateTime UpdatedAt { get; set; }
        public List<BOMItemViewModel> BOMItems { get; set; } = new List<BOMItemViewModel>();
        public List<BOMRFQViewModel> RFQs { get; set; } = new();
        #endregion
    }

    public class BOMRFQViewModel {
        #region Properties
        public int RFQId { get; set; }
        public string RFQNumber => $"RFQ-{RFQId:D6}";
        public string SupplierName { get; set; }
        public string Status { get; set; }
        public DateTime CreatedAt { get; set; }
        #endregion
    }
}