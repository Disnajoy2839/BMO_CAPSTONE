using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace BOMLink.Models {
    public class BOMItem {
        #region Properties
        [Key]
        public int Id { get; set; }

        [ForeignKey("BOM")]
        public int BOMId { get; set; }
        public BOM BOM { get; set; }

        [ForeignKey("Part")]
        public int PartId { get; set; }
        public Part Part { get; set; }

        // Quantity of the Part in the BOM
        [Range(1, int.MaxValue, ErrorMessage = "Quantity must be at least 1.")]
        public int Quantity { get; set; }

        // Unit of Measurement (Derived from Part)
        [NotMapped]
        public string Unit => Part?.Unit.ToString() ?? "N/A";

        // Labor Time per Unit (Derived from Part)
        [NotMapped]
        public decimal LaborTime => Part?.Labour ?? 0m;

        // Total Labor Calculation
        public decimal TotalLabor => Quantity * LaborTime;

        // Notes for Special Instructions (Optional)
        public string? Notes { get; set; }

        // Track when the item was added/modified
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        #endregion
    }
}
