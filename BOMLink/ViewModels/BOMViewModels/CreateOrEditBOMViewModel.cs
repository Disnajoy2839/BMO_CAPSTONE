using BOMLink.Models;
using System.ComponentModel.DataAnnotations;

namespace BOMLink.ViewModels.BOMViewModels {
    public class CreateOrEditBOMViewModel {
        #region Properties
        public int? Id { get; set; } // Nullable for Create mode

        [Required(ErrorMessage = "Description is required.")]
        public string Description { get; set; }

        public int? JobId { get; set; }

        [Required(ErrorMessage = "A BOM must have a Customer.")]
        public int CustomerId { get; set; }

        // Status only in Edit mode
        public BOMStatus? Status { get; set; } = BOMStatus.Draft;

        // Dropdown Data
        public List<Job> AvailableJobs { get; set; } = new List<Job>();
        public List<Customer> AvailableCustomers { get; set; } = new List<Customer>();
        #endregion
    }
}
