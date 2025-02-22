namespace BOMLink.Models {
    public class POItem {
        #region Properties
        public int Id { get; set; }
        public int POId { get; set; }
        public PO PO { get; set; }
        public int RFQId { get; set; }
        public RFQItem RFQItem { get; set; }
        public int Quantity { get; set; }
        public int QuantityReceived { get; set; }
        public int LeadTime { get; set; }
        #endregion
    }
}
