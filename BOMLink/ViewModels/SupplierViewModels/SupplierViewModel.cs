using BOMLink.Models;

namespace BOMLink.ViewModels.SupplierViewModels {
    public class SupplierViewModel {
        #region Properties
        public List<Supplier> Suppliers { get; set; }
        public string SearchTerm { get; set; }
        public string SortBy { get; set; }
        public string SortOrder { get; set; }
        #endregion
    }
}
