using Xunit;
using BOMLink.Models;
using FluentAssertions;
using System;

namespace BOMLink.Tests.UnitTests.Models {
    public class ApplicationUserTests {
        [Fact]
        public void ApplicationUser_Should_Have_Valid_First_And_Last_Name() {
            // Arrange
            var user = new ApplicationUser {
                FirstName = "John",
                LastName = "Doe",
                Role = UserRole.Admin
            };

            // Act & Assert
            user.FirstName.Should().NotBeNullOrEmpty();
            user.LastName.Should().NotBeNullOrEmpty();
            user.FirstName.Should().Be("John");
            user.LastName.Should().Be("Doe");
        }

        [Fact]
        public void ApplicationUser_Should_Have_Default_ProfilePicturePath() {
            // Arrange
            var user = new ApplicationUser();

            // Act & Assert
            user.ProfilePicturePath.Should().NotBeNullOrEmpty();
            user.ProfilePicturePath.Should().Be("/images/default-profile.png");
        }

        [Fact]
        public void ApplicationUser_Should_Have_Null_LastLogin_By_Default() {
            // Arrange
            var user = new ApplicationUser();

            // Act & Assert
            user.LastLogin.Should().BeNull();
        }

        [Fact]
        public void ApplicationUser_LastLogin_Should_Accept_NonNull_Value() {
            // Arrange
            var user = new ApplicationUser {
                LastLogin = DateTime.UtcNow
            };

            // Act & Assert
            user.LastLogin.Should().NotBeNull();
        }

        [Fact]
        public void ApplicationUser_Should_Have_Correct_RoleName() {
            // Arrange
            var user = new ApplicationUser {
                Role = UserRole.PM
            };

            // Act & Assert
            user.RoleName.Should().Be("PM");
        }

        [Fact]
        public void ApplicationUser_Should_Have_Empty_Collections_By_Default() {
            // Arrange
            var user = new ApplicationUser();

            // Act & Assert
            user.BOMs.Should().BeEmpty();
            user.Jobs.Should().BeEmpty();
            user.RFQs.Should().BeEmpty();
            user.POs.Should().BeEmpty();
        }
    }
}
