using BOMLink.Models;
using System.ComponentModel.DataAnnotations;

namespace BOMLink.ViewModels.PartViewModels {
    public class CreateOrEditPartViewModel {
        #region Properties
        public int? Id { get; set; } // Nullable for Create

        [Required(ErrorMessage = "Please enter a part number.")]
        public string PartNumber { get; set; }

        [Required(ErrorMessage = "Please enter a description.")]
        public string Description { get; set; }

        [Required(ErrorMessage = "Please enter a labour quantity.")]
        public decimal Labour { get; set; }

        [Required(ErrorMessage = "Please select a unit.")]
        public UnitType Unit { get; set; } // Enum for Unit

        [Required(ErrorMessage = "Please select a manufacturer.")]
        public int ManufacturerId { get; set; }

        public List<Manufacturer> Manufacturers { get; set; } = new(); // Dropdown List
        #endregion
    }
}