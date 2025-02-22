using BOMLink.Data;
using BOMLink.Models;
using BOMLink.ViewModels.SupplierManufacturerViewModel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;

namespace BOMLink.Controllers {
    [Authorize]
    public class SupplierManufacturerController : Controller {
        private readonly BOMLinkContext _context;

        public SupplierManufacturerController(BOMLinkContext context) {
            _context = context;
        }

        // GET: SupplierManufacturer
        public async Task<IActionResult> Index(string searchTerm, string sortBy, string sortOrder) {
            var links = _context.SupplierManufacturer
                .Include(sm => sm.Supplier)
                .Include(sm => sm.Manufacturer)
                .AsQueryable();

            // Filtering by search term (Supplier or Manufacturer)
            if (!string.IsNullOrEmpty(searchTerm)) {
                links = links.Where(sm =>
                    sm.Supplier.Name.Contains(searchTerm) ||
                    sm.Manufacturer.Name.Contains(searchTerm));
            }

            // Sorting logic
            links = sortBy switch {
                "supplier" => sortOrder == "desc" ? links.OrderByDescending(sm => sm.Supplier.Name) : links.OrderBy(sm => sm.Supplier.Name),
                "manufacturer" => sortOrder == "desc" ? links.OrderByDescending(sm => sm.Manufacturer.Name) : links.OrderBy(sm => sm.Manufacturer.Name),
                _ => links.OrderBy(sm => sm.Supplier.Name) // Default sorting by Supplier
            };

            var viewModel = new SupplierManufacturerViewModel {
                Links = await links.ToListAsync(),
                SearchTerm = searchTerm,
                SortBy = sortBy,
                SortOrder = sortOrder
            };

            return View(viewModel);
        }

        // GET: SupplierManufacturer/Create
        [HttpGet]
        public IActionResult Create() {
            var viewModel = new SupplierManufacturerViewModel {
                Suppliers = _context.Suppliers.ToList(),
                Manufacturers = _context.Manufacturers.ToList()
            };

            return View(viewModel);
        }

        // POST: SupplierManufacturer/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(SupplierManufacturerViewModel viewModel) {
            if (!ModelState.IsValid) {
                viewModel.Suppliers = _context.Suppliers.ToList();
                viewModel.Manufacturers = _context.Manufacturers.ToList();
                return View(viewModel);
            }

            foreach (var manufacturerId in viewModel.SelectedManufacturerIds) {
                // Prevent duplicate entries
                if (!_context.SupplierManufacturer.Any(sm => sm.SupplierId == viewModel.SupplierId && sm.ManufacturerId == manufacturerId)) {
                    _context.SupplierManufacturer.Add(new SupplierManufacturer {
                        SupplierId = viewModel.SupplierId,
                        ManufacturerId = manufacturerId
                    });
                }
            }

            await _context.SaveChangesAsync();
            TempData["Success"] = "Supplier-Manufacturer link(s) added successfully.";
            return RedirectToAction(nameof(Index));
        }

        // POST: SupplierManufacturer/Delete
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id) {
            var link = await _context.SupplierManufacturer.FindAsync(id);
            if (link == null) {
                TempData["Error"] = "Supplier-Manufacturer link not found.";
                return RedirectToAction(nameof(Index));
            }

            _context.SupplierManufacturer.Remove(link);
            await _context.SaveChangesAsync();
            TempData["Success"] = "Link deleted successfully.";
            return RedirectToAction(nameof(Index));
        }
    }
}
