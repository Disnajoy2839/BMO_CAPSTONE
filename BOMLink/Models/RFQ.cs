using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace BOMLink.Models {
    public class RFQ {
        #region Properties
        public int Id { get; set; }
        public int SupplierId { get; set; }
        public Supplier Supplier { get; set; }
        public DateTime DueDate { get; set; } = DateTime.UtcNow.AddDays(1);
        public DateTime? SentDate { get; set; }

        public int BOMId { get; set; }
        public BOM BOM { get; set; }

        // Assigned User (Automatically Captured from Logged-In User)
        [Required]
        public string UserId { get; set; } // IdentityUser uses string for ID

        [ForeignKey("UserId")]
        public ApplicationUser CreatedBy { get; set; }

        public string? Notes { get; set; } // Optional internal comments

        // Status Tracking
        [Required]
        [EnumDataType(typeof(RFQStatus))]
        public RFQStatus Status { get; set; } = RFQStatus.Draft; // Default to Draft

        [NotMapped]
        public string RFQNumber => $"RFQ-{Id:D6}";

        // Timestamps
        [Required]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Required]
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow; // Auto-update when modified

        // Navigation properties for RFQItems
        public ICollection<RFQItem> RFQItems { get; set; } = new List<RFQItem>();
        #endregion

        #region Enum
        // Enum for RFQ Status
        public enum RFQStatus {
            Draft,
            Sent,
            Received,
            Canceled
        }
        #endregion
    }
}
