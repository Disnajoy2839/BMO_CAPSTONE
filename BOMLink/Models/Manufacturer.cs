using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BOMLink.Models {
    public class Manufacturer {
        #region Properties
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Supplier name is required.")]
        [StringLength(50, ErrorMessage = "Supplier name cannot exceed 50 characters.")]
        public string Name { get; set; }

        // Timestamps
        [Required]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Required]
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow; // Auto-update when modified

        // Navigation properties for Manufacturer
        public List<SupplierManufacturer> SupplierManufacturers { get; set; } = new();
        public List<Part> Parts { get; set; } = new();
        #endregion
    }
}
