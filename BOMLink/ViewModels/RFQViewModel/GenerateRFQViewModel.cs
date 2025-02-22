namespace BOMLink.ViewModels.RFQViewModel {
    public class GenerateRFQViewModel {
        #region Properties
        public List<BOMSelectionViewModel> ReadyBOMs { get; set; } = new();
        #endregion
    }

    public class BOMSelectionViewModel {
        #region Properties
        public int Id { get; set; }
        public string Description { get; set; }
        #endregion
    }
}
