using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace BOMLink.Models {
    public class ApplicationUser : IdentityUser {
        #region Properties
        [Required(ErrorMessage = "First name is required.")]
        public string FirstName { get; set; }

        [Required(ErrorMessage = "Last name is required.")]
        public string LastName { get; set; }
        public DateTime? LastLogin { get; set; } // Stores last login timestamp
        public string ProfilePicturePath { get; set; } = "/images/default-profile.png";

        [Required(ErrorMessage = "Role is required.")]
        public UserRole Role { get; set; }
        public string RoleName => Role.ToString();
        public string FullName => $"{FirstName} {LastName}";

        // Navigation property for BOMs
        public ICollection<BOM> BOMs { get; set; } = new List<BOM>();

        // Navigation property for Jobs
        public ICollection<Job> Jobs { get; set; } = new List<Job>();

        // Navigation property for RFQ
        public ICollection<RFQ> RFQs { get; set; } = new List<RFQ>();

        // Navigation property for POs
        public ICollection<PO> POs { get; set; } = new List<PO>();
        #endregion
    }

    #region Enums
    public enum UserRole {
        Admin,
        PM,
        Receiving,
        Guest
    }
    #endregion
}
