using Xunit;
using BOMLink.Models;
using FluentAssertions;
using System;
using System.Collections.Generic;

namespace BOMLink.Tests.UnitTests.Models {
    public class BOMTests {
        [Fact]
        public void BOM_Should_Have_Default_Values() {
            // Arrange
            var bom = new BOM();

            // Act & Assert
            bom.Status.Should().Be(BOMStatus.Draft); // Default Status
            bom.Version.Should().Be(1.0m); // Default Version
            bom.BOMItems.Should().BeEmpty();
            bom.DraftBOMItems.Should().BeEmpty();
            bom.RFQs.Should().BeEmpty();
            bom.POs.Should().BeEmpty();
        }

        [Fact]
        public void BOM_Should_Generate_Correct_BOMNumber() {
            // Arrange
            var bom = new BOM { Id = 123 };

            // Act & Assert
            bom.BOMNumber.Should().Be("BOM-000123");
        }

        [Fact]
        public void IncrementVersion_Should_Increase_By_ZeroPointOne() {
            // Arrange
            var bom = new BOM { Version = 1.0m };

            // Act
            bom.IncrementVersion();

            // Assert
            bom.Version.Should().Be(1.1m);
        }

        [Fact]
        public void UpdateStatus_Should_Set_Incomplete_When_DraftItems_Exist() {
            // Arrange
            var bom = new BOM {
                DraftBOMItems = new List<DraftBOMItem> {
                    new DraftBOMItem { PartNumber = "XYZ-123", Quantity = 10 }
                }
            };

            // Act
            bom.UpdateStatus();

            // Assert
            bom.Status.Should().Be(BOMStatus.Incomplete);
        }

        [Fact]
        public void UpdateStatus_Should_Set_Ready_When_No_DraftItems_And_Has_BOMItems() {
            // Arrange
            var bom = new BOM {
                BOMItems = new List<BOMItem> {
                    new BOMItem { PartId = 1, Quantity = 5 }
                }
            };

            // Act
            bom.UpdateStatus();

            // Assert
            bom.Status.Should().Be(BOMStatus.Ready);
        }

        [Fact]
        public void UpdateStatus_Should_Set_Draft_When_No_Items() {
            // Arrange
            var bom = new BOM();

            // Act
            bom.UpdateStatus();

            // Assert
            bom.Status.Should().Be(BOMStatus.Draft);
        }

        [Fact]
        public void UpdateStatus_Should_Not_Change_Locked_Status() {
            // Arrange
            var bom = new BOM { Status = BOMStatus.Locked };

            // Act
            bom.UpdateStatus();

            // Assert
            bom.Status.Should().Be(BOMStatus.Locked);
        }
    }
}
