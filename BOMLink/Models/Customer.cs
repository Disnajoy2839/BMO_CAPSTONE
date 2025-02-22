using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BOMLink.Models {
    public class Customer {
        #region Properties
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Please enter a customer name.")]
        public string Name { get; set; }

        [Required(ErrorMessage = "Please enter a customer code.")]
        public string CustomerCode { get; set; }
        public string? Address { get; set; }
        public string? City { get; set; }
        public string? Province { get; set; }
        public string? ContactName { get; set; }
        public string? ContactPhone { get; set; }
        public string? ContactEmail { get; set; }

        // Timestamps
        [Required]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Required]
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow; // Auto-update when modified

        // Navigation properties for Customer
        public ICollection<Job> Jobs { get; set; } = new List<Job>();
        #endregion
    }
}
