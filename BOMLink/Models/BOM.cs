using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;


namespace BOMLink.Models {
    public class BOM {
        #region Properties
        [Key]
        public int Id { get; set; }

        [ForeignKey("Job")]
        public int? JobId { get; set; }
        public Job? Job { get; set; }

        [ForeignKey("Customer")]
        [Required(ErrorMessage = "A BOM must always have a Customer.")]
        [Range(1, int.MaxValue, ErrorMessage = "Invalid Customer selection. Please select a valid Customer.")]
        public int CustomerId { get; set; }
        public Customer Customer { get; set; }

        [Required(ErrorMessage = "Please enter a description.")]
        public string Description { get; set; }

        // Assigned User (Automatically Captured from Logged-In User)
        [Required]
        [ForeignKey("ApplicationUser")]
        public string UserId { get; set; }

        [ForeignKey("UserId")]
        public ApplicationUser CreatedBy { get; set; }

        // Status Tracking
        [Required]
        [EnumDataType(typeof(BOMStatus))]
        public BOMStatus Status { get; set; } = BOMStatus.Draft; // Default to Draft

        // Internal Notes
        [MaxLength(500)]
        public string? Notes { get; set; } // Optional internal comments

        // Version Control
        [Required]
        [Column(TypeName = "decimal(4,1)")]
        public decimal Version { get; set; } = 1.0m; // Start at 1.0

        // Timestamps
        [Required]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Required]
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow; // Auto-update when modified

        [NotMapped]
        public string BOMNumber => $"BOM-{Id:D6}";

        // Navigation properties for BOMItems
        public ICollection<BOMItem> BOMItems { get; set; } = new List<BOMItem>();
        public ICollection<DraftBOMItem> DraftBOMItems { get; set; } = new List<DraftBOMItem>();
        public ICollection<RFQ> RFQs { get; set; } = new List<RFQ>();
        public ICollection<PO> POs { get; set; } = new List<PO>();
        #endregion

        #region Methods
        /// <summary>
        /// Increment the BOM version by 0.1
        /// </summary>
        public void IncrementVersion() {
            Version += 0.1m;
        }

        /// <summary>
        /// Update the BOM status based on the BOMItems
        /// </summary>
        public void UpdateStatus() {
            bool hasRFQs = RFQs.Any();
            bool hasBOMItems = BOMItems.Any();
            bool hasDraftBOMItems = DraftBOMItems.Any();

            if (hasRFQs) {
                Status = BOMStatus.Locked; // If at least one RFQ exists, keep BOM locked
            } else if (hasDraftBOMItems) {
                Status = BOMStatus.Incomplete; // If BOM has draft items, mark as Incomplete
            } else if (hasBOMItems) {
                Status = BOMStatus.Ready; // If BOM has items but no RFQs, it’s ready
            } else {
                Status = BOMStatus.Draft; // If nothing exists, reset to Draft
            }
        }
        #endregion
    }

    #region Enum
    // Enum for BOM Status
    public enum BOMStatus {
        Draft,        // BOM is created but has no parts yet
        Incomplete,   // BOM has Draft Items that need review
        Ready,        // BOM is fully populated and ready for RFQ
        Locked        // BOM has been sent to RFQ and cannot be changed
    }
    #endregion
}
