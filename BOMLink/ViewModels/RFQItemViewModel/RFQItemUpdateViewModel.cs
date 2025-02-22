using System;
using System.ComponentModel.DataAnnotations;

namespace BOMLink.ViewModels.RFQItemViewModel {
    public class RFQItemUpdateViewModel {
        #region Properties
        public int Id { get; set; }

        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "Quantity must be at least 1.")]
        public int Quantity { get; set; }

        public string UOM { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "Price must be positive.")]
        public decimal? Price { get; set; }

        public string? ETA { get; set; }
        #endregion
    }
}
