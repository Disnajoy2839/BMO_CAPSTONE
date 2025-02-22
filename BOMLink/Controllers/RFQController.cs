using BOMLink.Data;
using BOMLink.Models;
using BOMLink.Services;
using BOMLink.ViewModels.RFQItemViewModel;
using BOMLink.ViewModels.RFQViewModel;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;


namespace BOMLink.Controllers {
    public class RFQController : Controller {
        private readonly BOMLinkContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public RFQController(BOMLinkContext context, UserManager<ApplicationUser> userManager) {
            _context = context;
            _userManager = userManager;
        }

        // GET: Index
        public async Task<IActionResult> Index(string searchTerm, string sortBy, string sortOrder, string statusFilter, string supplierFilter) {
            var query = _context.RFQs
                .Include(r => r.Supplier)
                .Include(r => r.BOM)
                .Include(r => r.CreatedBy)
                .AsQueryable();

            // Collect available suppliers for the filter dropdown
            var allSuppliers = await _context.Suppliers
                .Select(s => s.Name)
                .Distinct()
                .ToListAsync();

            // Search by RFQ Number, Supplier, BOM Number
            if (!string.IsNullOrEmpty(searchTerm)) {
                searchTerm = searchTerm.ToLower();
                query = query.Where(r =>
                    EF.Functions.Like(r.Id.ToString(), $"%{searchTerm}%") ||
                    EF.Functions.Like(r.Supplier.Name.ToLower(), $"%{searchTerm}%") ||
                    EF.Functions.Like(r.BOM.Id.ToString(), $"%{searchTerm}%")
                );
            }

            // Filter by Status
            if (!string.IsNullOrEmpty(statusFilter)) {
                if (Enum.TryParse<RFQ.RFQStatus>(statusFilter, out var parsedStatus)) {
                    query = query.Where(r => r.Status == parsedStatus);
                }
            }

            // Filter by Supplier
            if (!string.IsNullOrEmpty(supplierFilter)) {
                query = query.Where(r => r.Supplier.Name == supplierFilter);
            }

            // Sorting Logic
            query = sortBy switch {
                "rfqNumber" => sortOrder == "desc" ? query.OrderByDescending(r => r.Id) : query.OrderBy(r => r.Id),
                "supplier" => sortOrder == "desc" ? query.OrderByDescending(r => r.Supplier.Name) : query.OrderBy(r => r.Supplier.Name),
                "bom" => sortOrder == "desc" ? query.OrderByDescending(r => r.BOM.Id) : query.OrderBy(r => r.BOM.Id),
                "status" => sortOrder == "desc" ? query.OrderByDescending(r => r.Status) : query.OrderBy(r => r.Status),
                "createdBy" => sortOrder == "desc" ? query.OrderByDescending(r => r.CreatedBy.UserName) : query.OrderBy(r => r.CreatedBy.UserName),
                "updatedAt" => sortOrder == "desc" ? query.OrderByDescending(r => r.UpdatedAt) : query.OrderBy(r => r.UpdatedAt),
                _ => query.OrderByDescending(r => r.CreatedAt) // Default sorting
            };

            var rfqList = await query.Select(r => new RFQViewModel {
                Id = r.Id,
                SupplierName = r.Supplier.Name,
                BOMNumber = $"BOM-{r.BOM.Id:D6}",
                Status = r.Status.ToString(),
                CreatedBy = r.CreatedBy.UserName,
                CreatedAt = r.CreatedAt,
                UpdatedAt = r.UpdatedAt
            }).ToListAsync();

            var viewModel = new RFQViewModel {
                SearchTerm = searchTerm,
                SortBy = sortBy,
                SortOrder = sortOrder,
                StatusFilter = statusFilter,
                SupplierFilter = supplierFilter,
                AvailableStatuses = new List<string> { "Draft", "Sent", "Received", "Canceled" },
                AvailableSuppliers = allSuppliers
            };

            ViewBag.RFQs = rfqList; // Pass list to View
            return View(viewModel);
        }

        // GET: Generate RFQ from BOM
        public async Task<IActionResult> GenerateRFQ(int bomId) {
            var bom = await _context.BOMs
                .Include(b => b.BOMItems)
                .ThenInclude(bi => bi.Part)
                .ThenInclude(p => p.Manufacturer)
                .FirstOrDefaultAsync(b => b.Id == bomId);

            if (bom == null || !bom.BOMItems.Any()) {
                TempData["Error"] = "BOM not found or has no items.";
                return RedirectToAction("Index", "BOM");
            }

            if(bom.Status != BOMStatus.Ready) {
                TempData["Error"] = "Only BOMs with ready status can generate RFQs.";
                return RedirectToAction("Index", "BOM");
            }

            // Step 1: Identify suppliers for manufacturers in the BOM
            var manufacturerIds = bom.BOMItems.Select(bi => bi.Part.ManufacturerId).Distinct().ToList();
            var supplierOptions = await _context.SupplierManufacturer
                .Where(sm => manufacturerIds.Contains(sm.ManufacturerId))
                .Select(sm => new {
                    sm.ManufacturerId,
                    sm.SupplierId,
                    sm.Supplier.Name
                })
                .ToListAsync();

            // Step 2: Check if supplier selection is required
            var suppliersGrouped = supplierOptions.GroupBy(s => s.ManufacturerId).ToDictionary(g => g.Key, g => g.ToList());
            bool requiresSelection = suppliersGrouped.Values.Any(suppliers => suppliers.Count > 1);

            if (requiresSelection) {
                // Redirect to Supplier Selection Page
                return RedirectToAction("SelectSuppliers", new { bomId });
            }

            // Step 3: If only one supplier per manufacturer, create RFQs immediately
            var selectedSuppliers = supplierOptions.ToDictionary(s => s.ManufacturerId, s => s.SupplierId);
            return await ProcessRFQGeneration(bomId, selectedSuppliers);
        }

        // GET: Select Suppliers for BOM
        public async Task<IActionResult> SelectSuppliers(int bomId) {
            var bom = await _context.BOMs
                .Include(b => b.BOMItems)
                .ThenInclude(bi => bi.Part)
                .ThenInclude(p => p.Manufacturer)
                .FirstOrDefaultAsync(b => b.Id == bomId);

            if (bom == null) {
                return NotFound();
            }

            var manufacturerIds = bom.BOMItems.Select(bi => bi.Part.ManufacturerId).Distinct().ToList();
            var supplierOptions = await _context.SupplierManufacturer
                .Where(sm => manufacturerIds.Contains(sm.ManufacturerId))
                .Select(sm => new {
                    ManufacturerId = sm.ManufacturerId,
                    ManufacturerName = sm.Manufacturer.Name,
                    SupplierId = sm.SupplierId,
                    SupplierName = sm.Supplier.Name
                })
                .ToListAsync();

            var groupedOptions = supplierOptions
                .GroupBy(s => s.ManufacturerId)
                .Select(g => new ManufacturerSupplierOption {
                    ManufacturerId = g.Key,
                    ManufacturerName = g.First().ManufacturerName,
                    SupplierOptions = g.Select(s => new SupplierOption {
                        SupplierId = s.SupplierId,
                        SupplierName = s.SupplierName
                    }).ToList()
                }).ToList();

            var viewModel = new SupplierSelectionViewModel {
                BOMId = bomId,
                ManufacturerOptions = groupedOptions
            };

            return View(viewModel);
        }

        // POST: Process Supplier Selection
        [HttpPost]
        public async Task<IActionResult> ProcessSupplierSelection(int bomId, Dictionary<int, int> selectedSuppliers) {
            if (selectedSuppliers == null || !selectedSuppliers.Any()) {
                TempData["Error"] = "No suppliers selected. Please select at least one.";
                return RedirectToAction("SelectSuppliers", new { bomId });
            }

            return await ProcessRFQGeneration(bomId, selectedSuppliers);
        }

        // POST: Generate RFQ from BOM with selected suppliers
        [HttpPost]
        public async Task<IActionResult> GenerateRFQFromSelection(int bomId, Dictionary<int, int> selectedSuppliers) {
            var bom = await _context.BOMs
                .Include(b => b.BOMItems)
                .ThenInclude(bi => bi.Part)
                .ThenInclude(p => p.Manufacturer)
                .Include(b => b.RFQs)
                .FirstOrDefaultAsync(b => b.Id == bomId);

            if (bom == null || selectedSuppliers == null || !selectedSuppliers.Any()) {
                TempData["Error"] = "Invalid supplier selection.";
                return RedirectToAction("Index", "BOM");
            }

            using var transaction = await _context.Database.BeginTransactionAsync();

            try {
                foreach (var (manufacturerId, supplierId) in selectedSuppliers) {
                    var rfq = new RFQ {
                        SupplierId = supplierId,
                        BOMId = bom.Id,
                        UserId = _userManager.GetUserId(User),
                        Status = RFQ.RFQStatus.Draft,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };

                    _context.RFQs.Add(rfq);
                    await _context.SaveChangesAsync();

                    var bomItems = bom.BOMItems.Where(bi => bi.Part.ManufacturerId == manufacturerId).ToList();

                    foreach (var bomItem in bomItems) {
                        var rfqItem = new RFQItem {
                            RFQId = rfq.Id,
                            BOMItemId = bomItem.Id,
                            Quantity = bomItem.Quantity,
                            CreatedAt = DateTime.UtcNow,
                            UpdatedAt = DateTime.UtcNow
                        };
                        _context.RFQItems.Add(rfqItem);
                    }
                }
                await _context.SaveChangesAsync();

                bom.UpdateStatus();
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                TempData["Success"] = "RFQs generated successfully.";
                return RedirectToAction("Index", "RFQ");
            } catch (Exception) {
                await transaction.RollbackAsync();
                TempData["Error"] = "Error generating RFQs.";
                return RedirectToAction("Index", "BOM");
            }
        }

        // Helper method to process RFQ generation
        private async Task<IActionResult> ProcessRFQGeneration(int bomId, Dictionary<int, int> selectedSuppliers) {
            using var transaction = await _context.Database.BeginTransactionAsync();

            try {
                var supplierGroups = selectedSuppliers
                    .GroupBy(kvp => kvp.Value)
                    .ToDictionary(g => g.Key, g => g.Select(kvp => kvp.Key).ToList());

                foreach (var supplierGroup in supplierGroups) {
                    int supplierId = supplierGroup.Key;
                    var manufacturerIdsForSupplier = supplierGroup.Value;

                    var existingRFQ = await _context.RFQs
                        .FirstOrDefaultAsync(r => r.BOMId == bomId && r.SupplierId == supplierId);

                    if (existingRFQ == null) {
                        existingRFQ = new RFQ {
                            SupplierId = supplierId,
                            BOMId = bomId,
                            UserId = _userManager.GetUserId(User),
                            Status = RFQ.RFQStatus.Draft,
                            CreatedAt = DateTime.UtcNow,
                            UpdatedAt = DateTime.UtcNow
                        };

                        _context.RFQs.Add(existingRFQ);
                        await _context.SaveChangesAsync();
                    }

                    var bomItemsForSupplier = await _context.BOMItems
                        .Where(bi => bi.BOMId == bomId && manufacturerIdsForSupplier.Contains(bi.Part.ManufacturerId))
                        .ToListAsync();

                    foreach (var bomItem in bomItemsForSupplier) {
                        if (!_context.RFQItems.Any(ri => ri.RFQId == existingRFQ.Id && ri.BOMItemId == bomItem.Id)) {
                            _context.RFQItems.Add(new RFQItem {
                                RFQId = existingRFQ.Id,
                                BOMItemId = bomItem.Id,
                                Quantity = bomItem.Quantity
                            });
                        }
                    }
                }

                await _context.SaveChangesAsync();

                var bom = await _context.BOMs
                    .Include(b => b.BOMItems)
                    .ThenInclude(bi => bi.Part)
                    .ThenInclude(p => p.Manufacturer)
                    .Include(b => b.RFQs)
                    .FirstOrDefaultAsync(b => b.Id == bomId);
                if (bom != null) {
                    bom.UpdateStatus();
                    await _context.SaveChangesAsync();
                }
                await transaction.CommitAsync();

                return RedirectToAction("Index", "RFQ");
            } catch {
                await transaction.RollbackAsync();
                return RedirectToAction("Index", "BOM");
            }
        }

        // GET: Show BOM Selection for RFQ Generation
        public async Task<IActionResult> GenerateRFQSelection() {
            var readyBOMs = await _context.BOMs
                .Where(b => b.Status == BOMStatus.Ready) // Only Ready BOMs
                .OrderBy(b => b.Id)
                .Select(b => new BOMSelectionViewModel {
                    Id = b.Id,
                    Description = b.Description
                })
                .ToListAsync();

            var viewModel = new GenerateRFQViewModel {
                ReadyBOMs = readyBOMs
            };

            return View(viewModel);
        }

        // POST: Generate RFQ from Selected BOM
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GenerateRFQSelection(int bomId) {
            if (bomId <= 0) {
                TempData["Error"] = "Invalid BOM selected.";
                return RedirectToAction("GenerateRFQSelection");
            }

            return await GenerateRFQ(bomId); // Calls existing method
        }

        // POST: RFQ/Delete
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id) {
            var rfq = await _context.RFQs
                .Include(r => r.RFQItems) // Include related RFQ Items
                .Include(b => b.BOM)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (rfq == null) {
                TempData["Error"] = "RFQ not found.";
                return RedirectToAction("Index");
            }

            // Prevent deletion if RFQ has been sent
            if (rfq.Status != RFQ.RFQStatus.Draft) {
                TempData["Error"] = "Only draft RFQs can be deleted.";
                return RedirectToAction("Index");
            }

            try {
                _context.RFQItems.RemoveRange(rfq.RFQItems); // Delete RFQ Items first
                _context.RFQs.Remove(rfq);

                await _context.SaveChangesAsync();

                var bom = await _context.BOMs
                    .Include(b => b.RFQs) // Reload RFQs
                    .Include(b => b.BOMItems) // Reload BOM Items
                    .Include(b => b.DraftBOMItems) // Reload Draft BOM Items
                    .FirstOrDefaultAsync(b => b.Id == rfq.BOMId);

                if (bom != null) {
                    bom.UpdateStatus();
                    await _context.SaveChangesAsync(); // Save status update
                }

                TempData["Success"] = "RFQ deleted successfully.";
            } catch (Exception) {
                TempData["Error"] = "Error deleting RFQ.";
            }

            return RedirectToAction("Index");
        }

        // GET: Details
        public async Task<IActionResult> Details(int id) {
            var rfq = await _context.RFQs
                .Include(r => r.Supplier)
                .Include(r => r.BOM)
                .Include(r => r.CreatedBy)
                .Include(r => r.RFQItems)
                .ThenInclude(ri => ri.BOMItem)
                .ThenInclude(bi => bi.Part)
                .ThenInclude(p => p.Manufacturer)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (rfq == null) {
                return NotFound();
            }

            var viewModel = new RFQDetailsViewModel {
                Id = rfq.Id,
                SupplierName = rfq.Supplier.Name,
                BOMNumber = $"BOM-{rfq.BOM.Id:D6}",
                Status = rfq.Status.ToString(),
                CreatedBy = rfq.CreatedBy.UserName,
                CreatedAt = rfq.CreatedAt,
                UpdatedAt = rfq.UpdatedAt,
                SentDate = rfq.SentDate,
                RFQItems = rfq.RFQItems.Select(ri => new RFQItemViewModel {
                    PartNumber = ri.BOMItem.Part.PartNumber,
                    Description = ri.BOMItem.Part.Description,
                    Quantity = ri.BOMItem.Quantity,
                    UOM = ri.UOM ?? String.Empty,
                    Price = ri.Price,
                    ETA = ri.ETA ?? String.Empty,
                    Manufacturer = ri.BOMItem.Part.Manufacturer.Name ?? String.Empty,
                    PartTotal = (ri.Price ?? 0) * ri.BOMItem.Quantity
                }).ToList(),
                RFQTotal = rfq.RFQItems.Sum(ri => (ri.Price ?? 0) * ri.BOMItem.Quantity)
            };

            return View(viewModel);
        }

        // POST: Send RFQ
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SendRFQ(int id) {
            var rfq = await _context.RFQs
                .Include(r => r.Supplier)
                .Include(r => r.CreatedBy)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (rfq == null) {
                TempData["Error"] = "RFQ not found.";
                return RedirectToAction("Index");
            }

            if (rfq.Status != RFQ.RFQStatus.Draft) {
                TempData["Error"] = "Only draft RFQs can be sent.";
                return RedirectToAction("Details", new { id });
            }

            var rfqService = new RFQService(_context, _userManager);
            await rfqService.SendRFQEmail(id);

            // Update Status to "Sent"
            rfq.Status = RFQ.RFQStatus.Sent;
            rfq.SentDate = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            TempData["Success"] = "RFQ sent successfully.";
            return RedirectToAction("Details", new { id });
        }
    }
}
