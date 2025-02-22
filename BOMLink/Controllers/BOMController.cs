using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BOMLink.Data;
using BOMLink.Models;
using BOMLink.ViewModels.BOMItemViewModels;
using BOMLink.ViewModels.BOMViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BOMLink.Controllers {
    [Authorize]
    public class BOMController : Controller {
        private readonly BOMLinkContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public BOMController(BOMLinkContext context, UserManager<ApplicationUser> userManager) {
            _context = context;
            _userManager = userManager;
        }

        // GET: List all BOMs with search and sorting
        public async Task<IActionResult> Index(string searchTerm, string sortBy, string sortOrder, string customerCodeFilter, string createdByFilter) {
            var query = _context.BOMs
                .Include(b => b.Job)
                .Include(b => b.Customer)
                .Include(b => b.CreatedBy)
                .AsQueryable();

            // Collect Unique Customers & Users for Filtering
            var allCustomerCodes = await _context.Customers.Select(c => c.CustomerCode).Distinct().ToListAsync();
            var allUsers = await _context.Users.Select(u => u.UserName).Distinct().ToListAsync();

            // Apply Search Filter
            if (!string.IsNullOrEmpty(searchTerm)) {
                searchTerm = searchTerm.ToLower();
                query = query.Where(b =>
                    b.Description.ToLower().Contains(searchTerm) ||
                    (b.Customer != null && b.Customer.CustomerCode.ToLower().Contains(searchTerm)) ||
                    (b.Job != null && b.Job.Number.ToLower().Contains(searchTerm) ||
                    b.Id.ToString().Contains(searchTerm))
                );
            }

            // Apply Customer Filter
            if (!string.IsNullOrEmpty(customerCodeFilter)) {
                query = query.Where(b => b.Customer != null && b.Customer.CustomerCode == customerCodeFilter);
            }

            // Apply Created By Filter
            if (!string.IsNullOrEmpty(createdByFilter)) {
                query = query.Where(b => b.CreatedBy.UserName == createdByFilter);
            }

            // Sorting Logic
            query = sortBy switch {
                "description" => sortOrder == "desc" ? query.OrderByDescending(b => b.Description) : query.OrderBy(b => b.Description),
                "job" => sortOrder == "desc" ? query.OrderByDescending(b => b.Job.Number) : query.OrderBy(b => b.Job.Number),
                "status" => sortOrder == "desc" ? query.OrderByDescending(b => b.Status) : query.OrderBy(b => b.Status),
                "version" => sortOrder == "desc" ? query.OrderByDescending(b => b.Version) : query.OrderBy(b => b.Version),
                "createdBy" => sortOrder == "desc" ? query.OrderByDescending(b => b.CreatedBy.UserName) : query.OrderBy(b => b.CreatedBy.UserName),
                "updatedAt" => sortOrder == "desc" ? query.OrderByDescending(b => b.UpdatedAt) : query.OrderBy(b => b.UpdatedAt),
                "bomNumber" => sortOrder == "desc" ? query.OrderByDescending(b => b.Id) : query.OrderBy(b => b.Id),
                _ => query.OrderByDescending(b => b.CreatedAt) // Default: Latest first
            };

            var bomList = await query.Select(b => new BOMViewModel {
                Id = b.Id,
                Description = b.Description,
                JobNumber = b.Job != null ? b.Job.Number : "N/A", // Handle no job
                CustomerCode = b.Customer != null ? b.Customer.CustomerCode : "N/A", // Display customer if no job
                CreatedBy = b.CreatedBy.UserName,
                Status = b.Status.ToString(),
                Version = b.Version,
                CreatedAt = b.CreatedAt,
                UpdatedAt = b.UpdatedAt
            }).ToListAsync();

            var viewModel = new BOMViewModel {
                BOMs = bomList,
                SearchTerm = searchTerm,
                SortBy = sortBy,
                SortOrder = sortOrder,
                CustomerCodeFilter = customerCodeFilter,
                CreatedByFilter = createdByFilter,
                AvailableCustomers = allCustomerCodes,
                AvailableUsers = allUsers
            };

            return View(viewModel);
        }

        // GET: CreateOrEdit BOM
        public async Task<IActionResult> CreateOrEdit(int? id) {
            var viewModel = new CreateOrEditBOMViewModel {
                AvailableJobs = await _context.Jobs.OrderBy(j => j.Number).ToListAsync(),
                AvailableCustomers = await _context.Customers.OrderBy(c => c.CustomerCode).ToListAsync()
            };

            if (id.HasValue) {
                var bom = await _context.BOMs.FindAsync(id.Value);
                if (bom == null) {
                    TempData["Error"] = "BOM not found.";
                    return RedirectToAction(nameof(Index));
                }

                viewModel.Id = bom.Id;
                viewModel.Description = bom.Description;
                viewModel.JobId = bom.JobId;
                viewModel.CustomerId = bom.CustomerId;
                viewModel.Status = bom.Status;
            }

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateOrEdit(int? id, CreateOrEditBOMViewModel model) {
            if (!ModelState.IsValid) {
                model.AvailableJobs = await _context.Jobs.OrderBy(j => j.Number).ToListAsync();
                model.AvailableCustomers = await _context.Customers.OrderBy(c => c.CustomerCode).ToListAsync();
                return View(model);
            }

            // If Job is selected, use its associated Customer
            if (model.JobId.HasValue) {
                var job = await _context.Jobs.Include(j => j.Customer).FirstOrDefaultAsync(j => j.Id == model.JobId.Value);
                if (job == null) {
                    ModelState.AddModelError("JobId", "Selected Job does not exist.");
                    return View(model);
                }
                model.CustomerId = job.CustomerId; // Ensure CustomerId is always set
            }

            if (id.HasValue) {
                // Editing Existing BOM
                var existingBOM = await _context.BOMs.FindAsync(id.Value);
                if (existingBOM == null) return NotFound();

                if (existingBOM.Status == BOMStatus.Locked) {
                    TempData["Error"] = "Locked BOMs cannot be edited.";
                    return RedirectToAction("Index");
                }

                existingBOM.Description = model.Description;
                existingBOM.JobId = model.JobId;
                existingBOM.CustomerId = model.CustomerId;
                existingBOM.UpdatedAt = DateTime.UtcNow;
                existingBOM.IncrementVersion(); // Increment version on edit
                _context.BOMs.Update(existingBOM);
            } else {
                // Creating New BOM
                var userId = _userManager.GetUserId(User);
                var newBOM = new BOM {
                    Description = model.Description,
                    JobId = model.JobId,
                    CustomerId = model.CustomerId,
                    Status = BOMStatus.Draft,
                    UserId = userId,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    Version = 1.0m
                };

                _context.BOMs.Add(newBOM);
            }
            
            await _context.SaveChangesAsync();

            var bom = await _context.BOMs
                .Include(b => b.RFQs) // Reload RFQs
                .Include(b => b.BOMItems) // Reload BOM Items
                .Include(b => b.DraftBOMItems) // Reload Draft BOM Items
                .FirstOrDefaultAsync(b => b.Id == id);

            if (bom != null) {
                bom.UpdateStatus();
                await _context.SaveChangesAsync(); // Save status update
            }

            TempData["Success"] = id.HasValue ? "BOM updated successfully." : "BOM created successfully.";
            return RedirectToAction("Index");
        }

        // POST: Delete BOM
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id) {
            var bom = await _context.BOMs
                .Include(b => b.BOMItems)
                .Include(b => b.RFQs)
                .FirstOrDefaultAsync(b => b.Id == id);

            if (bom == null) {
                TempData["Error"] = "BOM not found.";
                return RedirectToAction(nameof(Index));
            }

            if(bom.Status == BOMStatus.Locked) {
                TempData["Error"] = "Locked BOMs cannot be deleted.";
                return RedirectToAction(nameof(Index));
            }

            if (bom.RFQs.Any()) {
                TempData["Error"] = "This BOM is used in RFQs and cannot be deleted.";
                return RedirectToAction(nameof(Index));
            }

            try {
                // Delete associated BOM Items first (cascade delete)
                _context.BOMItems.RemoveRange(bom.BOMItems);
                _context.BOMs.Remove(bom);
                await _context.SaveChangesAsync();

                TempData["Success"] = "BOM deleted successfully.";
            } catch (Exception ex) {
                TempData["Error"] = "Error deleting BOM: " + ex.Message;
            }

            return RedirectToAction(nameof(Index));
        }

        // GET: BOM Details
        public async Task<IActionResult> Details(int id) {
            var bom = await _context.BOMs
                .Include(b => b.Job)
                .Include(b => b.RFQs)
                .ThenInclude(r => r.Supplier)
                .Include(b => b.Customer)
                .Include(b => b.BOMItems)
                .ThenInclude(bi => bi.Part)
                .FirstOrDefaultAsync(b => b.Id == id);

            if (bom == null) {
                return NotFound();
            }

            var viewModel = new BOMDetailsViewModel {
                BOMId = bom.Id,
                BOMNumber = $"BOM-{bom.Id:D6}",
                Status = bom.Status.ToString(),
                CustomerName = bom.Customer?.Name ?? "N/A",
                JobNumber = bom.Job?.Number ?? "N/A",
                CreatedBy = bom.CreatedBy?.UserName ?? "Unknown User",
                UpdatedAt = bom.UpdatedAt,
                BOMItems = bom.BOMItems.Select(bi => new BOMItemViewModel {
                    PartId = bi.PartId,
                    PartNumber = bi.Part.PartNumber,
                    Description = bi.Part.Description,
                    Quantity = bi.Quantity,
                    Notes = bi.Notes
                }).ToList(),
                RFQs = bom.RFQs.Select(rfq => new BOMRFQViewModel {
                    RFQId = rfq.Id,
                    SupplierName = rfq.Supplier.Name,
                    Status = rfq.Status.ToString(),
                    CreatedAt = rfq.CreatedAt
                }).ToList()
            };

            return View(viewModel);
        }

        // POST: Clone BOM
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Clone(int id) {
            var existingBOM = await _context.BOMs
                .Include(b => b.BOMItems)
                .FirstOrDefaultAsync(b => b.Id == id);

            if (existingBOM == null) {
                TempData["Error"] = "BOM not found.";
                return RedirectToAction(nameof(Index));
            }

            var userId = _userManager.GetUserId(User);

            // Create a new BOM with cloned data
            var clonedBOM = new BOM {
                Description = existingBOM.Description + " (Clone)",
                JobId = existingBOM.JobId,
                CustomerId = existingBOM.CustomerId,
                UserId = userId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                Version = 1.0m,
                BOMItems = existingBOM.BOMItems.Select(item => new BOMItem {
                    PartId = item.PartId,
                    Quantity = item.Quantity
                }).ToList()
            };

            clonedBOM.UpdateStatus();

            _context.BOMs.Add(clonedBOM);
            await _context.SaveChangesAsync();

            TempData["Success"] = "BOM cloned successfully.";
            return RedirectToAction(nameof(Index));
        }
    }
}


