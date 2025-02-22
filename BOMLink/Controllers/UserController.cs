using BOMLink.Models;
using BOMLink.ViewModels.UserViewModels;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace BOMLink.Controllers {
    [Authorize]
    public class UserController : Controller {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;

        public UserController(UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager) {
            _userManager = userManager;
            _signInManager = signInManager;
        }

        [AllowAnonymous]
        [HttpGet]
        public IActionResult Login(string returnUrl = "/Dashboard") {
            if (_signInManager.IsSignedIn(User)) {
                return RedirectToAction("Index", "Dashboard");
            }

            // Fix potential redirect loops
            if (!string.IsNullOrEmpty(returnUrl) && returnUrl.Contains("Login")) {
                return RedirectToAction("Index", "Dashboard");
            }

            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        [AllowAnonymous]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(string username, string password, string returnUrl = null) {
            var user = await _userManager.FindByNameAsync(username);
            if (user != null) {
                var result = await _signInManager.PasswordSignInAsync(user, password, false, lockoutOnFailure: false);

                if (result.Succeeded) {
                    // Update last login timestamp
                    user.LastLogin = DateTime.UtcNow;
                    await _userManager.UpdateAsync(user);

                    var roles = await _userManager.GetRolesAsync(user);

                    var claims = new List<Claim> {
                new Claim(ClaimTypes.Name, user.UserName),
                new Claim("FullName", $"{user.FirstName} {user.LastName}"),
                new Claim(ClaimTypes.Role, roles.FirstOrDefault() ?? "User")
            };

                    var identity = new ClaimsIdentity(claims, "Cookies");
                    var principal = new ClaimsPrincipal(identity);

                    // Ensure authentication properties are correctly set
                    var authProperties = new AuthenticationProperties {
                        IsPersistent = false,
                        ExpiresUtc = DateTime.UtcNow.AddMinutes(30),
                        AllowRefresh = true
                    };

                    await HttpContext.SignInAsync("Cookies", principal, authProperties);

                    // Redirect to ReturnUrl or Dashboard
                    if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl)) {
                        return Redirect(returnUrl);
                    }
                    return RedirectToAction("Index", "Dashboard");
                }
            }

            TempData["InvalidLogin"] = "Invalid username or password.";
            return View();
        }



        public async Task<IActionResult> Logout() {
            await _signInManager.SignOutAsync();
            return RedirectToAction("Login");
        }

        // GET: User Settings Page
        public async Task<IActionResult> Settings() {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound();

            var viewModel = new UserSettingsViewModel {
                FirstName = user.FirstName,
                LastName = user.LastName,
                Email = user.Email,
                Username = user.UserName,
                LastLogin = user.LastLogin,
                ProfilePicturePath = user.ProfilePicturePath
            };

            return View(viewModel);
        }

        // POST: Update Profile (First Name & Last Name)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateProfile(UserSettingsViewModel model) {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound();

            // Ensure Email and Username are set (even if they are not editable)
            model.Email = user.Email;
            model.Username = user.UserName;
            model.ProfilePicturePath = user.ProfilePicturePath;

            if (!ModelState.IsValid) {
                TempData["Error"] = "Please check the errors below.";
                return View("Settings", model);
            }

            user.FirstName = model.FirstName;
            user.LastName = model.LastName;

            var result = await _userManager.UpdateAsync(user);
            if (result.Succeeded) {
                TempData["Success"] = "Profile updated successfully.";
                return RedirectToAction(nameof(Settings));
            } else {
                foreach (var error in result.Errors) {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
                return View("Settings", model);
            }
        }

        // POST: Change Password
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(UserSettingsViewModel model) {
            if (string.IsNullOrEmpty(model.CurrentPassword) || string.IsNullOrEmpty(model.NewPassword)) {
                TempData["Error"] = "Please enter the current and new password.";
                return RedirectToAction("Settings");
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound();

            var changePasswordResult = await _userManager.ChangePasswordAsync(user, model.CurrentPassword, model.NewPassword);
            if (changePasswordResult.Succeeded) {
                TempData["Success"] = "Password changed successfully.";
                await _signInManager.RefreshSignInAsync(user);
            } else {
                TempData["Error"] = "Error changing password.";
            }

            return RedirectToAction(nameof(Settings));
        }

        // POST: Upload Profile Picture
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadProfilePicture(IFormFile profilePicture) {
            var user = await _userManager.GetUserAsync(User);
            if (user == null || profilePicture == null || profilePicture.Length == 0) {
                TempData["Error"] = "Invalid file. Please select an image.";
                return RedirectToAction("Settings");
            }

            string uploadFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/uploads/profiles/");
            if (!Directory.Exists(uploadFolder)) Directory.CreateDirectory(uploadFolder);

            string fileName = $"{user.Id}{Path.GetExtension(profilePicture.FileName)}";
            string filePath = Path.Combine(uploadFolder, fileName);

            using (var fileStream = new FileStream(filePath, FileMode.Create)) {
                await profilePicture.CopyToAsync(fileStream);
            }

            user.ProfilePicturePath = $"/uploads/profiles/{fileName}";
            await _userManager.UpdateAsync(user);

            TempData["Success"] = "Profile picture updated successfully.";
            return RedirectToAction("Settings");
        }

        // POST: Logout from All Sessions
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> LogoutAllSessions() {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound();

            // Update security stamp - forces all sessions to be invalid
            await _userManager.UpdateSecurityStampAsync(user);

            // Log out the current session
            await _signInManager.SignOutAsync();

            TempData["Success"] = "Logged out from all active sessions.";
            return RedirectToAction("Login");
        }
    }
}
