using Xunit;
using BOMLink.Models;
using FluentAssertions;
using System;

namespace BOMLink.Tests.UnitTests.Models {
    public class BOMItemTests {
        [Fact]
        public void BOMItem_Should_Have_Default_Values() {
            // Arrange
            var bomItem = new BOMItem();

            // Act & Assert
            bomItem.Quantity.Should().Be(0); // Default integer value
            bomItem.Notes.Should().BeNull();
            bomItem.Unit.Should().Be("N/A");
            bomItem.LaborTime.Should().Be(0m);
            bomItem.TotalLabor.Should().Be(0m);
        }

        [Fact]
        public void BOMItem_Quantity_Should_Be_At_Least_One() {
            // Arrange
            var bomItem = new BOMItem { Quantity = 0 };

            // Act
            var isValid = bomItem.Quantity >= 1;

            // Assert
            isValid.Should().BeFalse();
        }

        [Fact]
        public void BOMItem_Should_Calculate_TotalLabor_Correctly() {
            // Arrange
            var part = new Part { Labour = 2.5m };
            var bomItem = new BOMItem {
                Quantity = 4,
                Part = part
            };

            // Act
            var totalLabor = bomItem.TotalLabor;

            // Assert
            totalLabor.Should().Be(10.0m);
        }

        [Fact]
        public void BOMItem_Should_Return_Correct_Unit_From_Part() {
            // Arrange
            var part = new Part { Unit = UnitType.C };
            var bomItem = new BOMItem { Part = part };

            // Act & Assert
            bomItem.Unit.Should().Be("C");
        }

        [Fact]
        public void BOMItem_Should_Return_Default_Unit_When_No_Part() {
            // Arrange
            var bomItem = new BOMItem();

            // Act & Assert
            bomItem.Unit.Should().Be("N/A");
        }

        [Fact]
        public void BOMItem_Should_Have_Correct_ForeignKeys() {
            // Arrange
            var bom = new BOM { Id = 1 };
            var part = new Part { Id = 100 };
            var bomItem = new BOMItem {
                BOMId = bom.Id,
                BOM = bom,
                PartId = part.Id,
                Part = part
            };

            // Act & Assert
            bomItem.BOMId.Should().Be(1);
            bomItem.PartId.Should().Be(100);
            bomItem.BOM.Should().Be(bom);
            bomItem.Part.Should().Be(part);
        }
    }
}