using System.ComponentModel.DataAnnotations;
using BOMLink.Models;

namespace BOMLink.ViewModels.UserViewModels {
    public class CreateUserViewModel {
        #region Properties
        [Required] public string Username { get; set; }
        [Required, EmailAddress] public string Email { get; set; }
        [Required] public string FirstName { get; set; }
        [Required] public string LastName { get; set; }
        [Required] public UserRole Role { get; set; }
        [Required, MinLength(6)] public string Password { get; set; }
        #endregion
    }
}
