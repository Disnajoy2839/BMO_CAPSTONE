using BOMLink.Models;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace BOMLink.Models {
    public class DraftBOMItem {
        #region Properties
        [Key]
        public int Id { get; set; }

        [ForeignKey("BOM")]
        public int BOMId { get; set; }
        public BOM BOM { get; set; }

        [Required]
        public string PartNumber { get; set; }  // Store the missing part number

        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "Quantity must be at least 1.")]
        public int Quantity { get; set; }

        public bool IsResolved { get; set; } = false; // Mark when added to the database
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        #endregion
    }
}
