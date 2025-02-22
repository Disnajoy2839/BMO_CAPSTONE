using System.ComponentModel.DataAnnotations;
using BOMLink.Models;

namespace BOMLink.ViewModels.UserViewModels {
    public class EditUserViewModel {
        #region Properties
        [Required] public string Id { get; set; }

        [Required] public string Username { get; set; }

        [Required, EmailAddress] public string Email { get; set; }

        [Required] public string FirstName { get; set; }

        [Required] public string LastName { get; set; }

        [Required] public string Role { get; set; }
        #endregion
    }
}
