using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BOMLink.Models {
    public class SupplierManufacturer {
        #region Properties
        [Key]
        public int Id { get; set; }

        [ForeignKey("Supplier")]
        [Required(ErrorMessage = "Please select at least one supplier.")]
        public int SupplierId { get; set; }
        public Supplier Supplier { get; set; }

        [ForeignKey("Manufacturer")]
        [Required(ErrorMessage = "Please select at least one manufacturer.")]
        public int ManufacturerId { get; set; }
        public Manufacturer Manufacturer { get; set; }
        #endregion
    }
}
