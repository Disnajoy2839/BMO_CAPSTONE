using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BOMLink.Models {
    public class RFQItem {
        #region Properties
        [Key]
        public int Id { get; set; }

        [Required]
        public int RFQId { get; set; }

        [ForeignKey("RFQId")]
        public RFQ RFQ { get; set; }

        [Required]
        public int BOMItemId { get; set; }

        [ForeignKey("BOMItemId")]
        public BOMItem BOMItem { get; set; }

        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "Quantity must be at least 1.")]
        public int Quantity { get; set; }

        // UOM (Will be filled after supplier reply)
        public string? UOM { get; set; }

        // Price (Will be filled after supplier reply)
        [Column(TypeName = "decimal(18,2)")]
        public decimal? Price { get; set; }

        // Estimated Time of Arrival (ETA) (Will be filled after supplier reply)
        public string? ETA { get; set; }

        // Notes for Special Instructions (Optional)
        public string? Notes { get; set; }

        // Track when the item was added/modified
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        #endregion
    }
}
