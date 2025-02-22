using System;
using System.Collections.Generic;

namespace BOMLink.ViewModels.UserViewModels {
    public class UserViewModel {
        #region Properties
        public string Id { get; set; }
        public string Username { get; set; }
        public string Email { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Role { get; set; }
        public DateTime? LastLogin { get; set; }
        public bool IsActive { get; set; }

        // Search, sort, and filtering
        public string SearchTerm { get; set; }
        public string SortBy { get; set; }
        public string SortOrder { get; set; }
        public string RoleFilter { get; set; }

        // Holds the list of users
        public List<UserViewModel> Users { get; set; } = new List<UserViewModel>();
        #endregion
    }
}
