namespace BOMLink.ViewModels.PartViewModels {
    public class PartDetailsViewModel {
        public int Id { get; set; }
        public string PartNumber { get; set; }
        public string Description { get; set; }
        public string Manufacturer { get; set; }
        public string Unit { get; set; }
        public decimal Labour { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        // List of BOMs where this part is used
        public List<PartBOMItemViewModel> BOMItems { get; set; } = new List<PartBOMItemViewModel>();
    }

    public class PartBOMItemViewModel {
        public string BOMNumber { get; set; }
        public string BOMDescription { get; set; }
        public string BOMStatus { get; set; }
        public int QuantityUsed { get; set; }
    }
}
