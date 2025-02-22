using BOMLink.Models;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace BOMLink.ViewModels.PartViewModels {
    public class PartViewModel {
        #region Properties
        public List<Part> Parts { get; set; } = new();
        public List<Manufacturer> Manufacturers { get; set; } = new();
        public string SearchTerm { get; set; }
        public string SelectedManufacturer { get; set; }
        public string SortBy { get; set; }
        public string SortOrder { get; set; }
        #endregion
    }
}
