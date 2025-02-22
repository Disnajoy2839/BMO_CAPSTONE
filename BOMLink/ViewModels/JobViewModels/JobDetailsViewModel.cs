using BOMLink.Models;
using BOMLink.ViewModels.BOMViewModels;
using System;

namespace BOMLink.ViewModels.JobViewModels {
    public class JobDetailsViewModel {
        #region Properties
        public int Id { get; set; }
        public string Number { get; set; }
        public string Description { get; set; }
        public string CustomerName { get; set; }
        public DateTime StartDate { get; set; }
        public string? ContactName { get; set; }
        public JobStatus Status { get; set; }
        public string CreatedBy { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        public ICollection<BOMViewModel> BOMs { get; set; } = new List<BOMViewModel>();
        #endregion
    }
}
