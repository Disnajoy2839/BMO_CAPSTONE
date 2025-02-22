using System.ComponentModel.DataAnnotations;

namespace BOMLink.ViewModels.SupplierViewModels {
    public class CreateOrEditSupplierViewModel {
        #region Properties
        public int? Id { get; set; } // Nullable for Create mode

        [Required(ErrorMessage = "Please enter a supplier name.")]
        public string Name { get; set; }

        public string? ContactName { get; set; }
        public string? ContactPhone { get; set; }

        [EmailAddress(ErrorMessage = "Please enter a valid email.")]
        [Required(ErrorMessage = "Please enter a supplier email.")]
        public string ContactEmail { get; set; }

        public string? Address { get; set; }
        public string? City { get; set; }
        public string? Province { get; set; }

        [Required(ErrorMessage = "Please enter a supplier code.")]
        public string SupplierCode { get; set; }
        #endregion
    }
}
