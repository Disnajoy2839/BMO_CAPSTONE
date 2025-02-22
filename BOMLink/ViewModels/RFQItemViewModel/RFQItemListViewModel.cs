namespace BOMLink.ViewModels.RFQItemViewModel {
    public class RFQItemListViewModel {
        #region Properties
        public int RFQId { get; set; }
        public string RFQNumber { get; set; }
        public string SupplierName { get; set; }
        public List<RFQItemViewModel> Items { get; set; } = new List<RFQItemViewModel>();

        // Whether the RFQ is still editable (Only Draft status)
        public bool CanEdit { get; set; }
        #endregion
    }
}
