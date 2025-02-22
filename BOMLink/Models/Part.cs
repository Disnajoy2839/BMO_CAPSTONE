using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BOMLink.Models {
    public class Part {
        #region Properties
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Please enter a part number.")]
        [StringLength(50, ErrorMessage = "Part number must be less than 30 characters.")]
        public string PartNumber { get; set; }

        [Required(ErrorMessage = "Please enter a description.")]
        public string Description { get; set; }

        [Required(ErrorMessage = "Please enter a labour quantity.")]
        public decimal Labour { get; set; }

        [Required(ErrorMessage = "Please enter a unit.")]
        public UnitType Unit { get; set; }

        [Required(ErrorMessage = "Please enter a manufacturer.")]
        [ForeignKey("Manufacturer")]
        public int ManufacturerId { get; set; }
        public Manufacturer Manufacturer { get; set; }

        // Timestamps
        [Required]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Required]
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow; // Auto-update when modified

        // Navigation Property for BOMItems
        public ICollection<BOMItem> BOMItems { get; set; } = new List<BOMItem>();
        #endregion
    }

    #region Enum
    public enum UnitType {
        E,
        C,
        m,
        f
    }
    #endregion
}
