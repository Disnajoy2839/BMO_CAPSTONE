using BOMLink.Models;
using BOMLink.ViewModels.UserViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BOMLink.Controllers {
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;

        public AdminController(UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager) {
            _userManager = userManager;
            _signInManager = signInManager;
        }

        // GET: List Users
        public async Task<IActionResult> Users(string searchTerm, string role, string sortBy, string sortOrder) {
            var users = _userManager.Users.ToList();

            // Search by Username or Email
            if (!string.IsNullOrEmpty(searchTerm)) {
                searchTerm = searchTerm.ToLower();
                users = users.Where(u => u.UserName.ToLower().Contains(searchTerm) || u.Email.ToLower().Contains(searchTerm)).ToList();
            }

            // Filter by Role
            if (!string.IsNullOrEmpty(role)) {
                users = users.Where(u => _userManager.GetRolesAsync(u).Result.Contains(role)).ToList();
            }

            // Fetch roles & map to UserViewModel list
            var userList = new List<UserViewModel>();
            foreach (var user in users) {
                var roles = await _userManager.GetRolesAsync(user);
                userList.Add(new UserViewModel {
                    Id = user.Id,
                    Username = user.UserName,
                    Email = user.Email,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    Role = roles.FirstOrDefault() ?? "No Role",
                    LastLogin = user.LastLogin,
                    IsActive = user.LockoutEnd == null || user.LockoutEnd <= DateTime.UtcNow
                });
            }

            // Sorting Logic
            userList = sortBy switch {
                "username" => sortOrder == "desc" ? userList.OrderByDescending(u => u.Username).ToList() : userList.OrderBy(u => u.Username).ToList(),
                "email" => sortOrder == "desc" ? userList.OrderByDescending(u => u.Email).ToList() : userList.OrderBy(u => u.Email).ToList(),
                "role" => sortOrder == "desc" ? userList.OrderByDescending(u => u.Role).ToList() : userList.OrderBy(u => u.Role).ToList(),
                _ => userList.OrderBy(u => u.Username).ToList()
            };

            // Populate and return ViewModel
            var viewModel = new UserViewModel {
                Users = userList,
                SearchTerm = searchTerm,
                SortBy = sortBy,
                SortOrder = sortOrder,
                RoleFilter = role
            };

            return View(viewModel);
        }

        // GET: Create User Form
        public IActionResult CreateUser() {
            return View();
        }

        // POST: Create User
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateUser(CreateUserViewModel model) {
            if (!ModelState.IsValid) {
                return View(model);
            }

            var user = new ApplicationUser {
                UserName = model.Username,
                Email = model.Email,
                FirstName = model.FirstName,
                LastName = model.LastName,
                Role = model.Role
            };

            var result = await _userManager.CreateAsync(user, model.Password);

            if (result.Succeeded) {
                await _userManager.AddToRoleAsync(user, model.Role.ToString());
                TempData["Success"] = "User created successfully.";
                return RedirectToAction("Users");
            }

            foreach (var error in result.Errors) {
                ModelState.AddModelError("", error.Description);
            }
            return View(model);
        }

        // GET: Edit User Form
        public async Task<IActionResult> EditUser(string id) {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            var model = new EditUserViewModel {
                Id = user.Id,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Username = user.UserName,
                Email = user.Email,
                Role = user.Role.ToString()
            };

            return View(model);
        }

        // POST: Edit User
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditUser(EditUserViewModel model) {
            if (!ModelState.IsValid) {
                return View(model);
            }

            var user = await _userManager.FindByIdAsync(model.Id);
            if (user == null) return NotFound();

            // Update user details
            user.UserName = model.Username;
            user.Email = model.Email;
            user.FirstName = model.FirstName;
            user.LastName = model.LastName;

            var updateResult = await _userManager.UpdateAsync(user);
            if (!updateResult.Succeeded) {
                foreach (var error in updateResult.Errors) {
                    ModelState.AddModelError("", error.Description);
                }
                return View(model);
            }

            // Ensure role updates properly
            var currentRoles = await _userManager.GetRolesAsync(user) ?? new List<string>();
            if (currentRoles.FirstOrDefault() != model.Role) {  // Only update if role is different
                await _userManager.RemoveFromRolesAsync(user, currentRoles);
                var roleAssignResult = await _userManager.AddToRoleAsync(user, model.Role);

                if (!roleAssignResult.Succeeded) {
                    foreach (var error in roleAssignResult.Errors) {
                        ModelState.AddModelError("", error.Description);
                    }
                    return View(model);
                }

                // Force role update immediately by updating security stamp
                await _userManager.UpdateSecurityStampAsync(user);
            }

            // Only log out and refresh roles if the logged-in user is editing their own account
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser != null && currentUser.Id == user.Id) {
                await _signInManager.SignOutAsync();
                await _signInManager.SignInAsync(user, isPersistent: false);
            }

            TempData["Success"] = "User updated successfully.";
            return RedirectToAction("Users");
        }

        // POST: Enable/Disable User
        [HttpPost]
        public async Task<IActionResult> ToggleUserStatus(string id) {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            if (user.LockoutEnd == null || user.LockoutEnd <= DateTime.UtcNow) {
                // Disable user (Lock them out for 100 years)
                user.LockoutEnd = DateTime.UtcNow.AddYears(100);
                user.LockoutEnabled = true; // Ensure the user is locked
            } else {
                // Enable user (Reset LockoutEnd and allow login)
                user.LockoutEnd = null;
                user.LockoutEnabled = false; // Ensure user is not locked
            }

            await _userManager.UpdateAsync(user);

            TempData["Success"] = "User status updated.";
            return RedirectToAction("Users");
        }

        // POST: Reset Password
        [HttpPost]
        public async Task<IActionResult> ResetPassword(string id) {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            var newPassword = "Temp123!"; // Generate a secure temp password in production
            var resetToken = await _userManager.GeneratePasswordResetTokenAsync(user);
            var result = await _userManager.ResetPasswordAsync(user, resetToken, newPassword);

            if (result.Succeeded) {
                TempData["Success"] = $"Password reset successfully. New password: {newPassword}";
            } else {
                TempData["Error"] = "Failed to reset password.";
            }

            return RedirectToAction("Users");
        }

        // POST: Delete User
        [HttpPost]
        public async Task<IActionResult> DeleteUser(string id) {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            var result = await _userManager.DeleteAsync(user);
            if (result.Succeeded) {
                TempData["Success"] = "User deleted successfully.";
            } else {
                TempData["Error"] = "Error deleting user.";
            }

            return RedirectToAction("Users");
        }
    }
}
