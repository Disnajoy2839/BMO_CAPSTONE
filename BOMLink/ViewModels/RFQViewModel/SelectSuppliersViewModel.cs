namespace BOMLink.ViewModels.RFQViewModel {
    public class SupplierSelectionViewModel {
        public int BOMId { get; set; }
        public List<ManufacturerSupplierOption> ManufacturerOptions { get; set; }
    }

    public class ManufacturerSupplierOption {
        public int ManufacturerId { get; set; }
        public string ManufacturerName { get; set; }
        public List<SupplierOption> SupplierOptions { get; set; }
    }

    public class SupplierOption {
        public int SupplierId { get; set; }
        public string SupplierName { get; set; }
    }
}
