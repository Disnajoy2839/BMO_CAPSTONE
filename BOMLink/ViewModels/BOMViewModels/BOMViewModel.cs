using System;
using System.Collections.Generic;
using BOMLink.Models;

namespace BOMLink.ViewModels.BOMViewModels {
    public class BOMViewModel {
        #region Properties
        public int Id { get; set; }
        public string Description { get; set; }
        public string JobNumber { get; set; } = "N/A";
        public string CustomerCode { get; set; } = "N/A";
        public string CreatedBy { get; set; }
        public string Status { get; set; }
        public decimal Version { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public string BOMNumber => $"BOM-{Id:D6}";

        // Format Version for Display
        public string VersionFormatted => Version.ToString("0.0");

        // Search, Sort & Filter Properties
        public string SearchTerm { get; set; }
        public string SortBy { get; set; }
        public string SortOrder { get; set; }

        // Filters for Created By & Customer Code
        public string CustomerCodeFilter { get; set; }
        public string CreatedByFilter { get; set; }

        // Lists for Dropdown Filters
        public List<string> AvailableCustomers { get; set; } = new List<string>();
        public List<string> AvailableUsers { get; set; } = new List<string>();

        public List<BOMViewModel> BOMs { get; set; } = new List<BOMViewModel>();
        #endregion
    }
}
