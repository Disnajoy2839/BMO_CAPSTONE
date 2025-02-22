using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BOMLink.Models {
    public class Supplier {
        #region Properties
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Please enter an unique supplier name.")]
        public string Name { get; set; }
        public string? ContactName { get; set; }
        public string? ContactPhone { get; set; }

        [EmailAddress(ErrorMessage = "Please a valid supplier email.")]
        [Required(ErrorMessage = "Please enter a supplier email.")]
        public string ContactEmail { get; set; }
        public string? Address { get; set; }
        public string? City { get; set; }
        public string? Province { get; set; }

        [Required(ErrorMessage = "Please enter an unique supplier code.")]
        public string SupplierCode { get; set; }

        // Timestamps
        [Required]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Required]
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow; // Auto-update when modified

        // Navigation properties for Supplier
        public List<SupplierManufacturer> SupplierManufacturers { get; set; } = new();
        public List<RFQ> RFQs { get; set; } = new();
        #endregion
    }
}
