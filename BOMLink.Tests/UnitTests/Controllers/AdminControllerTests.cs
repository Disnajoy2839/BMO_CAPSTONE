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

namespace BOMLink.Tests.Controllers {
    public class AdminControllerTests {
        private readonly Mock<UserManager<ApplicationUser>> _mockUserManager;
        private readonly Mock<SignInManager<ApplicationUser>> _mockSignInManager;
        private readonly AdminController _controller;

        public AdminControllerTests() {
            _mockUserManager = new Mock<UserManager<ApplicationUser>>(
                new Mock<IUserStore<ApplicationUser>>().Object, null, null, null, null, null, null, null, null);

            _mockSignInManager = new Mock<SignInManager<ApplicationUser>>(
                _mockUserManager.Object,
                new Mock<IHttpContextAccessor>().Object,
                new Mock<IUserClaimsPrincipalFactory<ApplicationUser>>().Object,
                null, null, null, null);

            _controller = new AdminController(_mockUserManager.Object, _mockSignInManager.Object);

            // Mock TempData
            var tempData = new TempDataDictionary(new DefaultHttpContext(), Mock.Of<ITempDataProvider>());
            _controller.TempData = tempData;

            // Mock Admin Claims
            var claims = new List<Claim> {
                new Claim(ClaimTypes.NameIdentifier, "admin123"),
                new Claim(ClaimTypes.Name, "adminuser"),
                new Claim(ClaimTypes.Role, "Admin")
            };

            var identity = new ClaimsIdentity(claims, "mock");
            var principal = new ClaimsPrincipal(identity);

            var httpContext = new DefaultHttpContext {
                User = principal
            };

            _controller.ControllerContext = new ControllerContext { HttpContext = httpContext };
        }

        // ✅ Test: Get Users List
        [Fact]
        public async Task Users_ReturnsViewWithUserList() {
            var users = new List<ApplicationUser> {
                new ApplicationUser { Id = "1", UserName = "user1", Email = "user1@test.com" },
                new ApplicationUser { Id = "2", UserName = "user2", Email = "user2@test.com" }
            };

            _mockUserManager.Setup(u => u.Users).Returns(users.AsQueryable());

            var result = await _controller.Users(null, null, null, null) as ViewResult;

            Assert.NotNull(result);
            var model = Assert.IsType<UserViewModel>(result.Model);
            Assert.Equal(2, model.Users.Count);
        }

        // ✅ Test: Create User - Success
        [Fact]
        public async Task CreateUser_ValidUser_CreatesUserAndRedirects() {
            var model = new CreateUserViewModel {
                Username = "newuser",
                Email = "newuser@test.com",
                FirstName = "New",
                LastName = "User",
                Password = "Password123!",
                Role = UserRole.Admin
            };

            _mockUserManager.Setup(u => u.CreateAsync(It.IsAny<ApplicationUser>(), model.Password))
                .ReturnsAsync(IdentityResult.Success);

            var result = await _controller.CreateUser(model) as RedirectToActionResult;

            Assert.NotNull(result);
            Assert.Equal("Users", result.ActionName);
            Assert.True(_controller.TempData.ContainsKey("Success"));
        }

        // ✅ Test: Edit User - Success
        [Fact]
        public async Task EditUser_ValidData_UpdatesUser() {
            var user = new ApplicationUser {
                Id = "1",
                UserName = "user1",
                Email = "user1@test.com",
                FirstName = "Old",
                LastName = "Name"
            };

            _mockUserManager.Setup(u => u.FindByIdAsync("1")).ReturnsAsync(user);
            _mockUserManager.Setup(u => u.UpdateAsync(It.IsAny<ApplicationUser>())).ReturnsAsync(IdentityResult.Success);

            var model = new EditUserViewModel {
                Id = "1",
                Username = "user1updated",
                Email = "user1updated@test.com",
                FirstName = "New",
                LastName = "Name",
                Role = "Admin"
            };

            var result = await _controller.EditUser(model) as RedirectToActionResult;

            Assert.NotNull(result);
            Assert.Equal("Users", result.ActionName);
            Assert.True(_controller.TempData.ContainsKey("Success"));
        }

        // ✅ Test: Toggle User Status (Enable/Disable)
        [Fact]
        public async Task ToggleUserStatus_TogglesLockoutStatus() {
            var user = new ApplicationUser { Id = "1", UserName = "user1", LockoutEnd = null };

            _mockUserManager.Setup(u => u.FindByIdAsync("1")).ReturnsAsync(user);
            _mockUserManager.Setup(u => u.UpdateAsync(It.IsAny<ApplicationUser>())).ReturnsAsync(IdentityResult.Success);

            var result = await _controller.ToggleUserStatus("1") as RedirectToActionResult;

            Assert.NotNull(result);
            Assert.Equal("Users", result.ActionName);
            Assert.True(_controller.TempData.ContainsKey("Success"));
            Assert.NotNull(user.LockoutEnd); // Ensure user is locked
        }

        // ✅ Test: Reset Password
        [Fact]
        public async Task ResetPassword_Success() {
            var user = new ApplicationUser { Id = "1", UserName = "user1" };
            _mockUserManager.Setup(u => u.FindByIdAsync("1")).ReturnsAsync(user);
            _mockUserManager.Setup(u => u.GeneratePasswordResetTokenAsync(user)).ReturnsAsync("reset_token");
            _mockUserManager.Setup(u => u.ResetPasswordAsync(user, "reset_token", "Temp123!"))
                .ReturnsAsync(IdentityResult.Success);

            var result = await _controller.ResetPassword("1") as RedirectToActionResult;

            Assert.NotNull(result);
            Assert.Equal("Users", result.ActionName);
            Assert.True(_controller.TempData.ContainsKey("Success"));
        }

        // ✅ Test: Delete User
        [Fact]
        public async Task DeleteUser_Success() {
            var user = new ApplicationUser { Id = "1", UserName = "user1" };
            _mockUserManager.Setup(u => u.FindByIdAsync("1")).ReturnsAsync(user);
            _mockUserManager.Setup(u => u.DeleteAsync(user)).ReturnsAsync(IdentityResult.Success);

            var result = await _controller.DeleteUser("1") as RedirectToActionResult;

            Assert.NotNull(result);
            Assert.Equal("Users", result.ActionName);
            Assert.True(_controller.TempData.ContainsKey("Success"));
        }
    }
}