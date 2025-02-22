using System.ComponentModel.DataAnnotations;

namespace BOMLink.ViewModels.UserViewModels {
    public class UserSettingsViewModel {
        #region Properties
        [Required]
        public string FirstName { get; set; }

        [Required]
        public string LastName { get; set; }

        [Required]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string Username { get; set; } = string.Empty;

        public DateTime? LastLogin { get; set; }

        public string? ProfilePicturePath { get; set; }

        // Password Change Fields
        [DataType(DataType.Password)]
        public string? CurrentPassword { get; set; } // Optional for name updates

        [MinLength(6, ErrorMessage = "New password must be at least 6 characters.")]
        [DataType(DataType.Password)]
        public string? NewPassword { get; set; } // Optional for name updates

        [Compare("NewPassword", ErrorMessage = "Passwords do not match.")]
        [DataType(DataType.Password)]
        public string? ConfirmPassword { get; set; } // Optional for name updates
        #endregion
    }
}
