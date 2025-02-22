using System.ComponentModel.DataAnnotations;

namespace BOMLink.ViewModels.CustomerViewModels {
    public class CreateOrEditCustomerViewModel {
        #region Properties
        public int? Id { get; set; } // Nullable for Create mode

        [Required(ErrorMessage = "Please enter a customer name.")]
        public string Name { get; set; }

        [Required(ErrorMessage = "Please enter a customer code.")]
        public string CustomerCode { get; set; }

        public string? Address { get; set; }
        public string? City { get; set; }
        public string? Province { get; set; }
        public string? ContactName { get; set; }
        public string? ContactPhone { get; set; }

        [EmailAddress(ErrorMessage = "Invalid email format.")]
        public string? ContactEmail { get; set; }
        #endregion
    }
}
