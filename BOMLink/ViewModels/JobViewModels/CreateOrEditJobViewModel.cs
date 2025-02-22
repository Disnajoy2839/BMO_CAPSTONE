using BOMLink.Models;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace BOMLink.ViewModels.JobViewModels {
    public class CreateOrEditJobViewModel {
        #region Properties
        public int? Id { get; set; } // Nullable for Create

        [Required(ErrorMessage = "Please enter a job number.")]
        public string Number { get; set; }

        [Required(ErrorMessage = "Please enter a description.")]
        public string Description { get; set; }

        [Required(ErrorMessage = "Please select a customer.")]
        public int CustomerId { get; set; }

        [Required(ErrorMessage = "Please enter a start date.")]
        public DateTime StartDate { get; set; } = DateTime.Now;

        public string? ContactName { get; set; }

        [Required(ErrorMessage = "Please select a status.")]
        public JobStatus Status { get; set; } = JobStatus.Pending;

        public int UserId { get; set; } // This will be set automatically

        public List<Customer> Customers { get; set; } = new(); // For Dropdown List
        #endregion
    }
}
