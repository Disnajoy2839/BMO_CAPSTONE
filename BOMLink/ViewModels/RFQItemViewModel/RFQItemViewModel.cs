namespace BOMLink.ViewModels.RFQItemViewModel {
    public class RFQItemViewModel {
        #region Properties
        public int Id { get; set; }
        public int RFQId { get; set; }
        public int BOMItemId { get; set; }
        public string PartNumber { get; set; }
        public string Description { get; set; }
        public int Quantity { get; set; }
        public string? UOM { get; set; } = string.Empty;
        public decimal? Price { get; set; }
        public string? ETA { get; set; } = string.Empty;
        public string Manufacturer { get; set; }
        public decimal PartTotal { get; set; }
        #endregion
    }
}
