using BOMLink.Data;
using BOMLink.Models;
using CsvHelper.Configuration;
using CsvHelper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;
using System.Globalization;
using System.Security.Claims;
using System.Text;
using BOMLink.ViewModels.JobViewModels;
using BOMLink.ViewModels.BOMViewModels;

namespace BOMLink.Controllers {
    [Authorize]
    public class JobController : Controller {
        private readonly BOMLinkContext _context;

        public JobController(BOMLinkContext context) {
            _context = context;
        }

        // GET: Job
        public async Task<IActionResult> Index(string searchTerm, int? selectedCustomer, string sortBy, string sortOrder) {
            var jobs = _context.Jobs.Include(j => j.Customer).Include(j => j.CreatedBy).AsQueryable();

            // Filtering by customer (if selected)
            if (selectedCustomer.HasValue) {
                jobs = jobs.Where(j => j.CustomerId == selectedCustomer.Value);
            }

            // Filtering by search term
            if (!string.IsNullOrEmpty(searchTerm)) {
                jobs = jobs.Where(j => j.Number.Contains(searchTerm) || j.Description.Contains(searchTerm) || j.Customer.Name.Contains(searchTerm));
            }

            // Sorting logic
            jobs = sortBy switch {
                "customer" => sortOrder == "desc" ? jobs.OrderByDescending(j => j.Customer.Name) : jobs.OrderBy(j => j.Customer.Name),
                "status" => sortOrder == "desc" ? jobs.OrderByDescending(j => j.Status) : jobs.OrderBy(j => j.Status),
                "date" => sortOrder == "desc" ? jobs.OrderByDescending(j => j.StartDate) : jobs.OrderBy(j => j.StartDate),
                "number" => sortOrder == "desc" ? jobs.OrderByDescending(j => j.Number) : jobs.OrderBy(j => j.Number),
                "createdby" => sortOrder == "desc" ? jobs.OrderByDescending(j => j.CreatedBy) : jobs.OrderBy(j => j.CreatedBy),
                _ => jobs.OrderBy(j => j.Number)
            };


            var viewModel = new JobViewModel {
                Jobs = await jobs.ToListAsync(),
                Customers = await _context.Customers.ToListAsync(), // Populate dropdown
                SearchTerm = searchTerm,
                SelectedCustomer = selectedCustomer?.ToString(), // Convert int? to string
                SortBy = sortBy,
                SortOrder = sortOrder
            };

            return View(viewModel);
        }

        // GET: Job/CreateOrEdit
        [HttpGet]
        public async Task<IActionResult> CreateOrEdit(int? id) {
            var viewModel = new CreateOrEditJobViewModel {
                Customers = await _context.Customers.OrderBy(c => c.Name).ToListAsync()
            };

            if (id.HasValue) {
                var job = await _context.Jobs.FindAsync(id.Value);
                if (job == null) return NotFound();

                viewModel.Id = job.Id;
                viewModel.Number = job.Number;
                viewModel.Description = job.Description;
                viewModel.CustomerId = job.CustomerId;
                viewModel.StartDate = job.StartDate;
                viewModel.ContactName = job.ContactName;
                viewModel.Status = job.Status;
            }

            return View(viewModel);
        }

        // POST: Job/CreateOrEdit
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateOrEdit(CreateOrEditJobViewModel viewModel) {
            if (_context.Jobs.Any(j => j.Number == viewModel.Number && j.Id != viewModel.Id)) {
                TempData["Error"] = "Job number must be unique.";
                viewModel.Customers = await _context.Customers.OrderBy(c => c.Name).ToListAsync();
                return View(viewModel);
            }

            if (!ModelState.IsValid) {
                viewModel.Customers = await _context.Customers.OrderBy(c => c.Name).ToListAsync();
                return View(viewModel);
            }

            if (viewModel.Id.HasValue) {
                // Editing an existing job
                var job = await _context.Jobs.FindAsync(viewModel.Id);
                if (job == null) return NotFound();

                job.Number = viewModel.Number;
                job.Description = viewModel.Description;
                job.CustomerId = viewModel.CustomerId;
                job.StartDate = viewModel.StartDate;
                job.ContactName = viewModel.ContactName;
                job.Status = viewModel.Status;
            } else {
                // Creating a new job
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var newJob = new Job {
                    Number = viewModel.Number,
                    Description = viewModel.Description,
                    CustomerId = viewModel.CustomerId,
                    StartDate = viewModel.StartDate,
                    ContactName = viewModel.ContactName,
                    Status = viewModel.Status,
                    UserId = userId
                };
                _context.Jobs.Add(newJob);
            }

            await _context.SaveChangesAsync();
            TempData["Success"] = viewModel.Id.HasValue ? "Job updated successfully." : "Job created successfully.";
            return RedirectToAction(nameof(Index));
        }

        // POST: Job/Delete
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id) {
            var job = await _context.Jobs
                .Include(j => j.BOMs)
                .FirstOrDefaultAsync(j => j.Id == id);

            if (job == null) {
                TempData["Error"] = "Job not found.";
                return RedirectToAction(nameof(Index));
            }

            if (job.BOMs.Count > 0) {
                TempData["Error"] = "Cannot delete job with associated BOMs.";
                return RedirectToAction(nameof(Index));
            }

            try {
                _context.Jobs.Remove(job);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Job deleted successfully.";
            } catch (Exception ex) {
                TempData["Error"] = "Error deleting job: " + ex.Message;
            }

            return RedirectToAction(nameof(Index));
        }

        // GET: Job/Details/{id}
        [HttpGet]
        public async Task<IActionResult> Details(int id) {
            var job = await _context.Jobs
                .Include(j => j.Customer)
                .Include(j => j.BOMs)
                .ThenInclude(b => b.CreatedBy)
                .Include(j => j.CreatedBy)
                .FirstOrDefaultAsync(j => j.Id == id);
            if (job == null) return NotFound();

            var viewModel = new JobDetailsViewModel {
                Id = job.Id,
                Number = job.Number,
                Description = job.Description,
                CustomerName = job.Customer.Name,
                StartDate = job.StartDate,
                ContactName = job.ContactName,
                Status = job.Status,
                CreatedBy = $"{job.CreatedBy.UserName}",
                CreatedAt = job.CreatedAt,
                UpdatedAt = job.UpdatedAt,
                BOMs = job.BOMs.Select(b => new BOMViewModel {
                    Id = b.Id,
                    Description = b.Description,
                    CreatedBy = b.CreatedBy.UserName,
                    Status = b.Status.ToString(),
                    Version = b.Version,
                    CreatedAt = b.CreatedAt,
                    UpdatedAt = b.UpdatedAt
                }).ToList()
            };

            return View(viewModel);
        }

        // GET: Job/ExportToCSV
        public IActionResult ExportToCSV() {
            var jobs = _context.Jobs.Include(j => j.Customer).ToList();

            var csvBuilder = new StringBuilder();
            csvBuilder.AppendLine("JobId,Number,Description,Customer,StartDate,Status");

            foreach (var job in jobs) {
                csvBuilder.AppendLine($"{job.Id},{job.Number},{job.Description},{job.Customer?.Name},{job.StartDate:yyyy-MM-dd},{job.Status}");
            }

            return File(Encoding.UTF8.GetBytes(csvBuilder.ToString()), "text/csv", "Jobs.csv");
        }

        // GET: Job/ExportToExcel
        public IActionResult ExportToExcel() {
            var jobs = _context.Jobs.Include(j => j.Customer).ToList();
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

            using var package = new ExcelPackage();
            var worksheet = package.Workbook.Worksheets.Add("Jobs");

            // Headers
            worksheet.Cells[1, 1].Value = "JobId";
            worksheet.Cells[1, 2].Value = "Number";
            worksheet.Cells[1, 3].Value = "Description";
            worksheet.Cells[1, 4].Value = "Customer";
            worksheet.Cells[1, 5].Value = "StartDate";
            worksheet.Cells[1, 6].Value = "Status";
            worksheet.Cells[1, 7].Value = "ContactName";
            
            int row = 2;
            foreach (var job in jobs) {
                worksheet.Cells[row, 1].Value = job.Id;
                worksheet.Cells[row, 2].Value = job.Number;
                worksheet.Cells[row, 3].Value = job.Description;
                worksheet.Cells[row, 4].Value = job.Customer?.Name;
                worksheet.Cells[row, 5].Value = job.StartDate.ToString("yyyy-MM-dd");
                worksheet.Cells[row, 6].Value = job.ContactName?.ToString();
                worksheet.Cells[row, 7].Value = job.Status.ToString();
                row++;
            }

            using var stream = new MemoryStream();
            package.SaveAs(stream);
            stream.Position = 0;

            return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "Jobs.xlsx");
        }

        // Import Jobs
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Import(IFormFile file) {
            if (file == null || file.Length == 0) {
                TempData["Error"] = "Please select a file to upload.";
                return RedirectToAction(nameof(Index));
            }

            var fileExtension = Path.GetExtension(file.FileName).ToLower();
            int importedCount = 0;

            try {
                importedCount = fileExtension switch {
                    ".csv" => await ImportCsv(file),
                    ".xlsx" => await ImportExcel(file),
                    _ => throw new Exception("Invalid file format. Please upload a CSV or Excel file.")
                };

                if (TempData["Error"] == null) {
                    TempData[importedCount > 0 ? "Success" : "Info"] = importedCount > 0
                    ? $"{importedCount} job(s) imported successfully."
                    : "No new jobs were imported (duplicates may have been skipped).";
                }
            } catch (Exception ex) {
                TempData["Error"] = "Error importing file: " + ex.Message;
            }

            return RedirectToAction(nameof(Index));
        }

        // CSV Import Method
        private async Task<int> ImportCsv(IFormFile file) {
            int count = 0;

            using var reader = new StreamReader(file.OpenReadStream());
            using var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture) {
                HeaderValidated = null,
                MissingFieldFound = null
            });

            // Read header row
            csv.Read();
            csv.ReadHeader();

            // Validate required columns
            if (csv.HeaderRecord == null || !csv.HeaderRecord.Contains("Number") || !csv.HeaderRecord.Contains("Description") || !csv.HeaderRecord.Contains("Customer") || !csv.HeaderRecord.Contains("StartDate") || !csv.HeaderRecord.Contains("Status")) {
                TempData["Error"] = "Invalid format: Column headers must contains: 'Number', 'Description', 'Customer', 'StartDate' and 'Status'.";
                return 0;
            }

            var customers = _context.Customers.ToList(); // Cache customers to match by name

            // Get logged-in user ID
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            // Process each row
            while (csv.Read()) {
                string number = csv.GetField("Number")?.Trim();
                string description = csv.GetField("Description")?.Trim();
                string customerName = csv.GetField("Customer")?.Trim();
                DateTime startDate = DateTime.Parse(csv.GetField("StartDate"));
                string statusText = csv.GetField("Status")?.Trim();
                string contactName = csv.GetField("ContactName")?.Trim();

                // Find Customer
                var customer = customers.FirstOrDefault(c => c.Name == customerName);
                if (customer == null) continue; // Skip if customer doesn't exist

                // Convert Status Enum
                if (!Enum.TryParse(statusText, out JobStatus status)) continue;

                // Ensure data is valid
                if (!string.IsNullOrEmpty(number) && !string.IsNullOrEmpty(description) && !string.IsNullOrEmpty(customer.Name) && (startDate != null) && !string.IsNullOrEmpty(status.ToString()) && !_context.Jobs.Any(j => j.Number == number)) {
                    _context.Jobs.Add(new Job {
                        Number = number,
                        Description = description,
                        CustomerId = customer.Id,
                        StartDate = startDate,
                        Status = status,
                        UserId = userId
                    });
                    count++;
                }
            }

            await _context.SaveChangesAsync();
            return count;
        }

        // Excel Import Method
        private async Task<int> ImportExcel(IFormFile file) {
            int count = 0;
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

            using var stream = new MemoryStream();
            await file.CopyToAsync(stream);
            using var package = new ExcelPackage(stream);
            var worksheet = package.Workbook.Worksheets[0];

            if (worksheet.Dimension.Columns < 6) {
                TempData["Error"] = "Invalid format: The file must have at least six columns: 'JobId', 'Number', 'Description', 'Customer', 'StartDate', 'Status'.";
                return 0;
            }

            // Validate headers
            string numberHeader = worksheet.Cells[1, 2].Text.Trim().ToLower();
            string descriptionHeader = worksheet.Cells[1, 3].Text.Trim().ToLower();
            string customerHeader = worksheet.Cells[1, 4].Text.Trim().ToLower();
            string dateHeader = worksheet.Cells[1, 5].Text.Trim().ToLower();
            string statusHeader = worksheet.Cells[1, 6].Text.Trim().ToLower();

            if (numberHeader != "number" || descriptionHeader != "description" || customerHeader != "customer" || dateHeader != "startdate" || statusHeader != "status") {
                TempData["Error"] = "Invalid format: Column headers must include 'Number', 'Description', 'Customer', 'StartDate', 'Status'.";
                return 0;
            }

            var customers = _context.Customers.ToList(); // Cache customers

            // Get logged-in user ID
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            for (int row = 2; row <= worksheet.Dimension.Rows; row++) {
                string number = worksheet.Cells[row, 2].Text.Trim();
                string description = worksheet.Cells[row, 3].Text.Trim();
                string customerName = worksheet.Cells[row, 4].Text.Trim();
                DateTime startDate = DateTime.Parse(worksheet.Cells[row, 5].Text);
                string contactName = worksheet.Cells[row, 6].Text.Trim();
                string statusText = worksheet.Cells[row, 7].Text.Trim();

                var customer = customers.FirstOrDefault(c => c.Name == customerName);
                if (customer == null) continue; // Skip if customer not found

                if (!Enum.TryParse(statusText, out JobStatus status)) continue;

                // Ensure data is valid
                if (!string.IsNullOrEmpty(number) && !string.IsNullOrEmpty(description) && !string.IsNullOrEmpty(customer.Name) && (startDate != null) && !string.IsNullOrEmpty(status.ToString()) && !_context.Jobs.Any(j => j.Number == number)) {
                    _context.Jobs.Add(new Job {
                        Number = number,
                        Description = description,
                        CustomerId = customer.Id,
                        StartDate = startDate,
                        Status = status,
                        UserId = userId
                    });
                    count++;
                }
            }

            await _context.SaveChangesAsync();
            return count;
        }
    }
}