using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace BOMLink.Models {
    public class PO {
        #region Properties
        public int Id { get; set; }
        public int RFQId { get; set; }
        public RFQ RFQ { get; set; }
        public DateTime Date { get; set; }

        // Assigned User (Automatically Captured from Logged-In User)
        [Required]
        public string UserId { get; set; } // IdentityUser uses string for ID

        [ForeignKey("UserId")]
        public ApplicationUser User { get; set; }
        // Navigation properties for POItems
        public ICollection<POItem> POItems { get; set; } = new List<POItem>();
        #endregion
    }
}
