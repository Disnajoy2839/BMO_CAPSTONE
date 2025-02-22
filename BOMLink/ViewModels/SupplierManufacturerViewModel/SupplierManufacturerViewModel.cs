using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using BOMLink.Models;

namespace BOMLink.ViewModels.SupplierManufacturerViewModel {
    public class SupplierManufacturerViewModel {
        #region Properties
        public List<Supplier> Suppliers { get; set; } = new();
        public List<Manufacturer> Manufacturers { get; set; } = new();


        [Required(ErrorMessage = "Please select a supplier.")]
        public int SupplierId { get; set; }


        [Required(ErrorMessage = "Please select at least one manufacturer.")]
        public List<int> SelectedManufacturerIds { get; set; } = new();

        public List<SupplierManufacturer> Links { get; set; } = new();
        public string SearchTerm { get; set; } = "";
        public string SortBy { get; set; } = "supplier";
        public string SortOrder { get; set; } = "asc";
        #endregion
    }
}
