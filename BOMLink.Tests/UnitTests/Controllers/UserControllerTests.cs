using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using BOMLink.Controllers;
using BOMLink.Models;
using BOMLink.ViewModels.UserViewModels;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc.Routing;

namespace BOMLink.Tests.Controllers {
    public class UserControllerTests {
        private readonly Mock<UserManager<ApplicationUser>> _mockUserManager;
        private readonly Mock<SignInManager<ApplicationUser>> _mockSignInManager;
        private readonly UserController _controller;

        public UserControllerTests() {
            // Mock UserManager
            _mockUserManager = new Mock<UserManager<ApplicationUser>>(
                new Mock<IUserStore<ApplicationUser>>().Object, null, null, null, null, null, null, null, null);

            // Mock SignInManager
            _mockSignInManager = new Mock<SignInManager<ApplicationUser>>(
                _mockUserManager.Object,
                new Mock<IHttpContextAccessor>().Object,
                new Mock<IUserClaimsPrincipalFactory<ApplicationUser>>().Object,
                null, null, null, null);

            // Initialize Controller
            _controller = new UserController(_mockUserManager.Object, _mockSignInManager.Object);

            // Initialize TempData
            var tempData = new TempDataDictionary(new DefaultHttpContext(), Mock.Of<ITempDataProvider>());
            _controller.TempData = tempData;

            // Mock User Claims
            var claims = new List<Claim> {
        new Claim(ClaimTypes.NameIdentifier, "123"),
        new Claim(ClaimTypes.Name, "testuser")
    };
            var identity = new ClaimsIdentity(claims, "mock");
            var principal = new ClaimsPrincipal(identity);

            var httpContext = new DefaultHttpContext {
                User = principal
            };

            // Add Mock Authentication Services
            var authenticationServiceMock = new Mock<IAuthenticationService>();
            authenticationServiceMock
                .Setup(a => a.SignInAsync(It.IsAny<HttpContext>(), It.IsAny<string>(), It.IsAny<ClaimsPrincipal>(), It.IsAny<AuthenticationProperties>()))
                .Returns(Task.CompletedTask);

            var serviceProviderMock = new Mock<IServiceProvider>();
            serviceProviderMock
                .Setup(s => s.GetService(typeof(IAuthenticationService)))
                .Returns(authenticationServiceMock.Object);

            // Add Mock for `IUrlHelperFactory`
            var urlHelperFactoryMock = new Mock<IUrlHelperFactory>();
            var urlHelperMock = new Mock<IUrlHelper>();
            urlHelperFactoryMock
                .Setup(f => f.GetUrlHelper(It.IsAny<ActionContext>()))
                .Returns(urlHelperMock.Object);
            serviceProviderMock
                .Setup(s => s.GetService(typeof(IUrlHelperFactory)))
                .Returns(urlHelperFactoryMock.Object);

            httpContext.RequestServices = serviceProviderMock.Object;
            _controller.ControllerContext = new ControllerContext { HttpContext = httpContext };
        }

        // Test: Login (GET) - Redirects if User is Already Logged In
        [Fact]
        public void Login_UserAlreadyLoggedIn_RedirectsToDashboard() {
            _mockSignInManager.Setup(s => s.IsSignedIn(It.IsAny<ClaimsPrincipal>())).Returns(true);

            var result = _controller.Login() as RedirectToActionResult;

            Assert.NotNull(result);
            Assert.Equal("Index", result.ActionName);
            Assert.Equal("Dashboard", result.ControllerName);
        }

        // Test: Login (POST) - Valid Credentials
        [Fact]
        public async Task Login_ValidUser_CreatesSessionAndRedirects() {
            var user = new ApplicationUser { UserName = "testuser", FirstName = "John", LastName = "Doe" };
            _mockUserManager.Setup(u => u.FindByNameAsync("testuser")).ReturnsAsync(user);
            _mockSignInManager.Setup(s => s.PasswordSignInAsync(user, "password", false, false))
                .ReturnsAsync(Microsoft.AspNetCore.Identity.SignInResult.Success);

            _mockUserManager.Setup(u => u.GetRolesAsync(user)).ReturnsAsync(new List<string> { "Admin" });

            var result = await _controller.Login("testuser", "password") as RedirectToActionResult;

            Assert.NotNull(result);
            Assert.Equal("Index", result.ActionName);
            Assert.Equal("Dashboard", result.ControllerName);
        }

        // Test: Login (POST) - Invalid Credentials
        [Fact]
        public async Task Login_InvalidUser_ReturnsError() {
            _mockUserManager.Setup(u => u.FindByNameAsync("invaliduser")).ReturnsAsync((ApplicationUser)null);

            var result = await _controller.Login("invaliduser", "password") as ViewResult;

            Assert.NotNull(result);
            Assert.True(_controller.TempData.ContainsKey("InvalidLogin"));
        }

        // Test: Logout
        [Fact]
        public async Task Logout_CallsSignOut() {
            var result = await _controller.Logout() as RedirectToActionResult;

            _mockSignInManager.Verify(s => s.SignOutAsync(), Times.Once);
            Assert.NotNull(result);
            Assert.Equal("Login", result.ActionName);
        }

        // Test: Update Profile
        [Fact]
        public async Task UpdateProfile_ValidData_UpdatesUserProfile() {
            var user = new ApplicationUser { FirstName = "OldName", LastName = "Doe" };
            _mockUserManager.Setup(u => u.GetUserAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync(user);
            _mockUserManager.Setup(u => u.UpdateAsync(It.IsAny<ApplicationUser>())).ReturnsAsync(IdentityResult.Success);

            var model = new UserSettingsViewModel { FirstName = "NewName", LastName = "Doe" };

            var result = await _controller.UpdateProfile(model) as RedirectToActionResult;

            Assert.NotNull(result);
            Assert.Equal("Settings", result.ActionName);
            Assert.Equal("NewName", user.FirstName);
        }

        // Test: Change Password
        [Fact]
        public async Task ChangePassword_ValidData_ChangesPassword() {
            var user = new ApplicationUser {
                Id = "123",
                UserName = "testuser",
                Email = "test@example.com"
            };

            var model = new UserSettingsViewModel {
                CurrentPassword = "oldPassword",
                NewPassword = "newPassword"
            };

            _mockUserManager.Setup(u => u.GetUserAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync(user);
            _mockUserManager.Setup(u => u.ChangePasswordAsync(user, model.CurrentPassword, model.NewPassword))
                .ReturnsAsync(IdentityResult.Success);

            var result = await _controller.ChangePassword(model) as RedirectToActionResult;

            Assert.NotNull(result);
            Assert.Equal(nameof(UserController.Settings), result.ActionName);
            Assert.True(_controller.TempData.ContainsKey("Success"));
            Assert.Equal("Password changed successfully.", _controller.TempData["Success"]);
        }

        // Test: Upload Profile Picture
        [Fact]
        public async Task UploadProfilePicture_ValidFile_UpdatesProfile() {
            var user = new ApplicationUser { UserName = "testuser" };
            _mockUserManager.Setup(u => u.GetUserAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync(user);
            _mockUserManager.Setup(u => u.UpdateAsync(It.IsAny<ApplicationUser>())).ReturnsAsync(IdentityResult.Success);

            var fileMock = new Mock<IFormFile>();
            fileMock.Setup(f => f.Length).Returns(1000);
            fileMock.Setup(f => f.FileName).Returns("profile.png");

            var result = await _controller.UploadProfilePicture(fileMock.Object) as RedirectToActionResult;

            Assert.NotNull(result);
            Assert.Equal("Settings", result.ActionName);
            Assert.True(_controller.TempData.ContainsKey("Success"));
        }

        //// Test: Logout All Sessions
        //[Fact]
        //public async Task LogoutAllSessions_ValidUser_LogsOutAllSessions() {
        //    // Arrange
        //    var user = new ApplicationUser { UserName = "testuser" };

        //    _mockUserManager.Setup(u => u.GetUserAsync(It.IsAny<ClaimsPrincipal>()))
        //        .ReturnsAsync(user);

        //    _mockUserManager.Setup(u => u.UpdateSecurityStampAsync(It.IsAny<ApplicationUser>()))
        //        .Returns(Task.CompletedTask);  // ✅ Ensures correct return type

        //    _mockSignInManager.Setup(s => s.SignOutAsync())
        //        .Returns(Task.CompletedTask);  // ✅ Ensures correct return type

        //    // Act
        //    var result = await _controller.LogoutAllSessions() as RedirectToActionResult;

        //    // Assert
        //    _mockUserManager.Verify(u => u.UpdateSecurityStampAsync(user), Times.Once);
        //    _mockSignInManager.Verify(s => s.SignOutAsync(), Times.Once);
        //    Assert.NotNull(result);
        //    Assert.Equal("Login", result.ActionName);
        //}
    }
}
