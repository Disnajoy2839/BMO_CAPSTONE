using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace BOMLink.ViewModels.BOMItemViewModels {
    public class CreateOrEditBOMItemViewModel {
        #region Properties
        public int Id { get; set; }
        public int BOMId { get; set; }

        [BindNever]
        public string BOMNumber { get; set; }

        [Required(ErrorMessage = "Please select a valid part.")]
        public int PartId { get; set; }

        [Required(ErrorMessage = "Please enter a quantity.")]
        [Range(1, int.MaxValue, ErrorMessage = "Quantity must be at least 1.")]
        public int Quantity { get; set; }
        public string? Notes { get; set; }
        public List<int> ExistingPartIds { get; set; } = new List<int>();

        public List<SelectListItem> AvailableParts { get; set; } = new List<SelectListItem>();
        #endregion
    }
}
