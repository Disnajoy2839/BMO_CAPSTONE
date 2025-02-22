using System.ComponentModel.DataAnnotations;

namespace BOMLink.ViewModels.RFQViewModel {
    public class RFQViewModel {
        #region Properties
        public int Id { get; set; }
        public string RFQNumber => $"RFQ-{Id:D6}"; // Auto-format RFQ Number

        [Required]
        public int SupplierId { get; set; }
        public string SupplierName { get; set; }

        [Required]
        public int BOMId { get; set; }
        public string BOMNumber { get; set; }

        public string Status { get; set; }
        public string CreatedBy { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        // Filters & Sorting
        public string SearchTerm { get; set; }
        public string SortBy { get; set; }
        public string SortOrder { get; set; }
        public string StatusFilter { get; set; }
        public string SupplierFilter { get; set; }

        public List<string> AvailableStatuses { get; set; } = new List<string> { "Draft", "Sent", "Received", "Canceled" };
        public List<string> AvailableSuppliers { get; set; } = new List<string>();
        #endregion
    }
}
