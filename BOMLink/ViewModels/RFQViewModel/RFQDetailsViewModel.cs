using BOMLink.ViewModels.RFQItemViewModel;

namespace BOMLink.ViewModels.RFQViewModel {
    public class RFQDetailsViewModel {
        #region Properties
        public int Id { get; set; }
        public string RFQNumber => $"RFQ-{Id:D6}"; // Auto-format RFQ Number
        public string SupplierName { get; set; }
        public string BOMNumber { get; set; }
        public string Status { get; set; }
        public string CreatedBy { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public DateTime? SentDate { get; set; }
        public decimal RFQTotal { get; set; }

        // RFQ Items
        public List<RFQItemViewModel.RFQItemViewModel> RFQItems { get; set; } = new List<RFQItemViewModel.RFQItemViewModel>();

        // Status Check
        public bool CanSendRFQ => Status == "Draft";
        public bool CanEditRFQ => Status == "Draft";
        #endregion
    }

}
