using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BOMLink.Models {
    public class Job {
        #region Properties
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Please enter a job number.")]
        public string Number { get; set; }

        [Required(ErrorMessage = "Please enter a description.")]
        public string Description { get; set; }

        // Customer Foreign Key
        [Required(ErrorMessage = "Please enter a customer.")]
        [ForeignKey("Customer")]
        public int CustomerId { get; set; }
        public Customer Customer { get; set; }

        // Job Start Date (Required)
        [Required(ErrorMessage = "Please enter a start date.")]
        public DateTime StartDate { get; set; }

        // Contact Person (Optional)
        public string? ContactName { get; set; }

        // Job Status (Enum)
        [Required(ErrorMessage = "Please select a status.")]
        public JobStatus Status { get; set; } = JobStatus.Pending; // Default status

        // Assigned User (Automatically Captured from Logged-In User)
        [Required]
        public string UserId { get; set; }

        [ForeignKey("UserId")]
        public ApplicationUser CreatedBy { get; set; }

        // Timestamps
        [Required]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Required]
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow; // Auto-update when modified

        // Navigation properties for Job
        public ICollection<BOM> BOMs { get; set; } = new List<BOM>();
        #endregion
    }

    #region Enum
    // Enum for Job Status
    public enum JobStatus {
        Pending,
        InProgress,
        Completed,
        Canceled
    }
    #endregion
}