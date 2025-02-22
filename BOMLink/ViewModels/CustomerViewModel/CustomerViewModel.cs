using BOMLink.Models;
using System.Collections.Generic;

namespace BOMLink.ViewModels.CustomerViewModel {
    public class CustomerViewModel {
        #region Properties
        public List<Customer> Customers { get; set; }
        public string SearchTerm { get; set; }
        public string SortBy { get; set; }
        public string SortOrder { get; set; }
        #endregion
    }
}