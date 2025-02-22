using BOMLink.Models;
using BOMLink.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BOMLink.Controllers {
    public class POController : Controller {
        public IActionResult Index() {
            return View();
        }

        //// POController - GET: Create PO
        //public async Task<IActionResult> Create() {
        //    var viewModel = new CreatePOViewModel {
        //        AvailableBOMs = await _context.BOMs
        //            .Where(b => b.Status != BOMStatus.Draft) // Exclude Draft BOMs
        //        .OrderBy(b => b.Id)
        //            .ToListAsync(),
        //        AvailableSuppliers = await _context.Suppliers.OrderBy(s => s.Name).ToListAsync()
        //    };
        //    return View(viewModel);
        //}

    }
}
