using BOMLink.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace BOMLink.Data {
    public class BOMLinkContext : IdentityDbContext<ApplicationUser> {
        public BOMLinkContext(DbContextOptions<BOMLinkContext> options) : base(options) { }

        public DbSet<BOM> BOMs { get; set; }
        public DbSet<BOMItem> BOMItems { get; set; }
        public DbSet<DraftBOMItem> DraftBOMItems { get; set; }
        public DbSet<Customer> Customers { get; set; }
        public DbSet<Job> Jobs { get; set; }
        public DbSet<Manufacturer> Manufacturers { get; set; }
        public DbSet<Part> Parts { get; set; }
        public DbSet<PO> POs { get; set; }
        public DbSet<POItem> POItems { get; set; }
        public DbSet<RFQ> RFQs { get; set; }
        public DbSet<RFQItem> RFQItems { get; set; }
        public DbSet<Supplier> Suppliers { get; set; }
        public DbSet<SupplierManufacturer> SupplierManufacturer { get; set; }
        public DbSet<ApplicationUser> Users { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder) {
            // User data
            // Define Composite Primary Key for IdentityUserRole
            modelBuilder.Entity<IdentityUserRole<string>>()
                .HasKey(iur => new { iur.UserId, iur.RoleId });

            // Seed Roles
            modelBuilder.Entity<IdentityRole>().HasData(
                new IdentityRole { Id = "1", Name = UserRole.Admin.ToString(), NormalizedName = "ADMIN" },
                new IdentityRole { Id = "2", Name = UserRole.PM.ToString(), NormalizedName = "PM" },
                new IdentityRole { Id = "3", Name = UserRole.Receiving.ToString(), NormalizedName = "RECEIVING" },
                new IdentityRole { Id = "4", Name = UserRole.Guest.ToString(), NormalizedName = "GUEST" }
            );

            // Admin User (Precomputed Static Hash)
            var admin = new ApplicationUser {
                Id = "1",
                UserName = "admin",
                NormalizedUserName = "ADMIN",
                Email = "admin@bomlink.com",
                NormalizedEmail = "ADMIN@BOMLINK.COM",
                FirstName = "Admin",
                LastName = "User",
                Role = UserRole.Admin,
                EmailConfirmed = true,
                SecurityStamp = "STATIC_SECURITY_STAMP_1", // Use fixed string instead of Guid.NewGuid()
                ConcurrencyStamp = "STATIC_CONCURRENCY_STAMP_1" // Use fixed string instead of Guid.NewGuid()
            };

            // Precomputed hashed password (instead of dynamically hashing it)
            admin.PasswordHash = "AQAAAAEAACcQAAAAEK9vBdtmDOq5FQfTfIHMxK835sGFRz/FevGOC092eFhYuHK0Q9BrEG8/HpLlb7dVow==";

            modelBuilder.Entity<ApplicationUser>().HasData(admin);
            modelBuilder.Entity<IdentityUserRole<string>>().HasData(
                new IdentityUserRole<string> { UserId = "1", RoleId = "1" }
            ); // Assign Admin Role

            // User 1 (Precomputed Static Hash)
            var user1 = new ApplicationUser {
                Id = "2",
                UserName = "JDS",
                NormalizedUserName = "JDS",
                Email = "sathler@ymail.com",
                NormalizedEmail = "SATHLER@YMAIL.COM",
                FirstName = "First",
                LastName = "User",
                Role = UserRole.PM,
                EmailConfirmed = true,
                SecurityStamp = "STATIC_SECURITY_STAMP_2", // Use fixed string instead of Guid.NewGuid()
                ConcurrencyStamp = "STATIC_CONCURRENCY_STAMP_2" // Use fixed string instead of Guid.NewGuid()
            };

            user1.PasswordHash = "AQAAAAEAACcQAAAAECUKpOK7uSJAXy6UL1uAxk4kRNFkBnw1JCdknbTQ8Gp9hhE4/1oZ/9FXemSviL6SuQ=="; // Precomputed hash

            modelBuilder.Entity<ApplicationUser>().HasData(user1);
            modelBuilder.Entity<IdentityUserRole<string>>().HasData(
                new IdentityUserRole<string> { UserId = "2", RoleId = "2" }
            ); // Assign Project Manager Role


            // Part data
            modelBuilder.Entity<Part>()
                .HasIndex(m => m.PartNumber)
                .IsUnique();  // Ensure unique part numbers
            modelBuilder.Entity<Part>()
                .Property(p => p.Labour)
                .HasPrecision(18, 2); // Set precision for Labour column    
            modelBuilder.Entity<Part>()
                .Property(p => p.Unit)
                .HasConversion<string>(); // Store Enum as String
            modelBuilder.Entity<Part>()
                .HasOne(p => p.Manufacturer)
                .WithMany()
                .HasForeignKey(p => p.ManufacturerId)
                .OnDelete(DeleteBehavior.Restrict); // Define foreign key relationship with Manufacturer
            modelBuilder.Entity<Part>()
                .HasOne(p => p.Manufacturer)
                .WithMany(m => m.Parts)
                .HasForeignKey(p => p.ManufacturerId)
                .OnDelete(DeleteBehavior.Restrict); // Prevent deleting manufacturer if they have parts
            modelBuilder.Entity<Part>().HasData(
                new Part { Id = 1, PartNumber = "P1001", Description = "Circuit Breaker", Labour = 2.5m, Unit = UnitType.E, ManufacturerId = 1, CreatedAt = new DateTime(2021, 1, 1), UpdatedAt = new DateTime(2021, 1, 1) },
                new Part { Id = 2, PartNumber = "P1002", Description = "Relay", Labour = 1.0m, Unit = UnitType.E, ManufacturerId = 2, CreatedAt = new DateTime(2021, 1, 1), UpdatedAt = new DateTime(2021, 1, 1) },
                new Part { Id = 3, PartNumber = "P1003", Description = "Switch", Labour = 0.5m, Unit = UnitType.E, ManufacturerId = 3, CreatedAt = new DateTime(2021, 1, 1), UpdatedAt = new DateTime(2021, 1, 1) }
            );

            // Job data
            modelBuilder.Entity<Job>()
                .HasIndex(m => m.Number)
                .IsUnique();  // Ensure unique job numbers
            modelBuilder.Entity<Job>()
                .Property(j => j.StartDate)
                .HasDefaultValueSql("GETUTCDATE()"); // Uses the database default UTC date
            modelBuilder.Entity<Job>().HasData(
                new Job { Id = 1, Number = "J0001", Description = "Job 1", CustomerId = 1, ContactName = "John Doe", Status = JobStatus.Pending, UserId = "2", StartDate = new DateTime(2021, 1, 1), CreatedAt = new DateTime(2021, 1, 1), UpdatedAt = new DateTime(2021, 1, 1) },
                new Job { Id = 2, Number = "J0002", Description = "Job 2", CustomerId = 2, ContactName = "Jane Doe", Status = JobStatus.Completed, UserId = "2", StartDate = new DateTime(2021, 1, 1), CreatedAt = new DateTime(2021, 1, 1), UpdatedAt = new DateTime(2021, 1, 1) },
                new Job { Id = 3, Number = "J0003", Description = "Job 3", CustomerId = 3, ContactName = "Jack Doe", Status = JobStatus.Canceled, UserId = "2", StartDate = new DateTime(2021, 1, 1), CreatedAt = new DateTime(2021, 1, 1), UpdatedAt = new DateTime(2021, 1, 1) }
            );
            modelBuilder.Entity<Job>()
                .HasOne(j => j.CreatedBy)
                .WithMany(u => u.Jobs)
                .HasForeignKey(j => j.UserId)
                .OnDelete(DeleteBehavior.Restrict); // Prevent deleting user if they have created jobs
            modelBuilder.Entity<Job>()
                .HasOne(j => j.Customer)
                .WithMany(c => c.Jobs)
                .HasForeignKey(j => j.CustomerId)
                .OnDelete(DeleteBehavior.Restrict); // Prevent deleting customer if they have jobs

            // Manufacturer data
            modelBuilder.Entity<Manufacturer>()
                .HasIndex(m => m.Name)
                .IsUnique();  // Ensure unique manufacturer names
            modelBuilder.Entity<Manufacturer>().HasData(
                new Manufacturer { Id = 1, Name = "Schneider", CreatedAt = new DateTime(2024, 2, 2), UpdatedAt = new DateTime(2024, 2, 6) },
                new Manufacturer { Id = 2, Name = "Phoenix Contact", CreatedAt = new DateTime(2024, 2, 2), UpdatedAt = new DateTime(2024, 2, 6) },
                new Manufacturer { Id = 3, Name = "Mersen", CreatedAt = new DateTime(2024, 2, 2), UpdatedAt = new DateTime(2024, 2, 6) }
            );

            // Supplier data
            modelBuilder.Entity<Supplier>()
                .HasIndex(m => m.Name)
                .IsUnique();  // Ensure unique supplier names
            modelBuilder.Entity<Supplier>()
                .HasIndex(m => m.SupplierCode)
                .IsUnique();  // Ensure unique supplier code
            modelBuilder.Entity<Supplier>().HasData(
                new Supplier { Id = 1, Name = "Graybar", ContactEmail = "sathler@ymail.com", SupplierCode = "GRAELE", CreatedAt = new DateTime(2021, 1, 1), UpdatedAt = new DateTime(2021, 1, 1) },
                new Supplier { Id = 2, Name = "House of Electric", ContactEmail = "sathler@ymail.com", SupplierCode = "HOUELE", CreatedAt = new DateTime(2021, 1, 1), UpdatedAt = new DateTime(2021, 1, 1) },
                new Supplier { Id = 3, Name = "Hammond", ContactEmail = "sathler@ymail.com", SupplierCode = "HAMMND", CreatedAt = new DateTime(2021, 1, 1), UpdatedAt = new DateTime(2021, 1, 1) }
            );

            // SupplierManufacturer data
            modelBuilder.Entity<SupplierManufacturer>()
                .HasOne(sm => sm.Supplier)
                .WithMany(s => s.SupplierManufacturers)
                .HasForeignKey(sm => sm.SupplierId)
                .OnDelete(DeleteBehavior.Cascade); // Cascade delete SupplierManufacturer when Supplier is deleted
            modelBuilder.Entity<SupplierManufacturer>()
                .HasOne(sm => sm.Manufacturer)
                .WithMany(m => m.SupplierManufacturers)
                .HasForeignKey(sm => sm.ManufacturerId)
                .OnDelete(DeleteBehavior.Cascade); // Cascade delete SupplierManufacturer when Manufacturer is deleted
            modelBuilder.Entity<SupplierManufacturer>().HasData(
                new SupplierManufacturer { Id = 1, SupplierId = 1, ManufacturerId = 1 }, // Graybar - Schneider
                new SupplierManufacturer { Id = 2, SupplierId = 1, ManufacturerId = 2 }, // Graybar - Phoenix Contact
                new SupplierManufacturer { Id = 3, SupplierId = 2, ManufacturerId = 3 }, // House of Electric - Mersen
                new SupplierManufacturer { Id = 4, SupplierId = 3, ManufacturerId = 1 }, // Hammond - Schneider
                new SupplierManufacturer { Id = 5, SupplierId = 3, ManufacturerId = 3 }  // Hammond - Siemens
            );

            // Customer data
            modelBuilder.Entity<Customer>()
                .HasIndex(m => m.Name)
                .IsUnique(); // Ensure unique customer names
            modelBuilder.Entity<Customer>()
                .HasIndex(m => m.CustomerCode)
                .IsUnique(); // Ensure unique customer codes
            modelBuilder.Entity<Customer>().HasData(
                new Customer { Id = 1, Name = "ABC Company", CustomerCode = "ABCCO", CreatedAt = new DateTime(2021, 1, 1), UpdatedAt = new DateTime(2021, 1, 1) },
                new Customer { Id = 2, Name = "XYZ Company", CustomerCode = "XYZCO", CreatedAt = new DateTime(2021, 1, 1), UpdatedAt = new DateTime(2021, 1, 1) },
                new Customer { Id = 3, Name = "123 Company", CustomerCode = "123CO", CreatedAt = new DateTime(2021, 1, 1), UpdatedAt = new DateTime(2021, 1, 1) }
            );

            //BOM data
            modelBuilder.Entity<BOM>()
                .HasOne(b => b.CreatedBy)
                .WithMany(u => u.BOMs)
                .HasForeignKey(b => b.UserId)
                .OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<BOM>()
                .Property(b => b.Status)
                .HasConversion<string>();
            modelBuilder.Entity<BOM>()
                .Property(b => b.Version)
                .HasPrecision(4, 1);
            modelBuilder.Entity<BOM>()
                .HasMany(bom => bom.BOMItems)
                .WithOne(bi => bi.BOM)
                .HasForeignKey(bi => bi.BOMId)
                .OnDelete(DeleteBehavior.Cascade); // Cascade delete BOM items when BOM is deleted.
            modelBuilder.Entity<BOM>()
                .HasMany(b => b.RFQs)
                .WithOne(r => r.BOM)
                .HasForeignKey(r => r.BOMId)
                .OnDelete(DeleteBehavior.Restrict); // Prevent deleting BOM if used in RFQ
            modelBuilder.Entity<BOM>()
                .HasOne(b => b.Job)
                .WithMany(j => j.BOMs)
                .HasForeignKey(b => b.JobId)
                .OnDelete(DeleteBehavior.Restrict); // Prevent deleting Job if used in BOM
            modelBuilder.Entity<BOM>()
                .Property(b => b.UpdatedAt)
                .HasDefaultValueSql("GETUTCDATE()");
            modelBuilder.Entity<BOM>().HasData(
                new BOM {
                    Id = 1,
                    JobId = 1,
                    CustomerId = 1,
                    Description = "Main Electrical Panel Assembly",
                    UserId = "1", // Admin User
                    Status = BOMStatus.Ready,
                    Version = 1.0m,
                    CreatedAt = new DateTime(2024, 02, 01, 10, 00, 00), // Static Date
                    UpdatedAt = new DateTime(2024, 02, 05, 15, 30, 00)  // Static Date
                },
                new BOM {
                    Id = 2,
                    JobId = 2,
                    CustomerId = 2,
                    Description = "Control Cabinet Wiring",
                    UserId = "2", // Project Manager
                    Status = BOMStatus.Ready,
                    Version = 1.0m,
                    CreatedAt = new DateTime(2024, 01, 28, 09, 45, 00), // Static Date
                    UpdatedAt = new DateTime(2024, 02, 02, 12, 15, 00)  // Static Date
                },
                new BOM {
                    Id = 3,
                    CustomerId = 3,
                    Description = "Power Distribution System",
                    UserId = "2", // Project Manager
                    Status = BOMStatus.Locked,
                    Version = 1.1m,
                    CreatedAt = new DateTime(2024, 01, 20, 14, 00, 00), // Static Date
                    UpdatedAt = new DateTime(2024, 02, 01, 08, 30, 00)  // Static Date
                }
            );

            // BOMItem data
            modelBuilder.Entity<BOMItem>()
                .HasOne(bi => bi.BOM)
                .WithMany(b => b.BOMItems)
                .HasForeignKey(bi => bi.BOMId)
                .OnDelete(DeleteBehavior.Cascade); // Cascade delete BOM items when BOM is deleted.
            modelBuilder.Entity<BOMItem>()
                .HasOne(bi => bi.Part)
                .WithMany(p => p.BOMItems)
                .HasForeignKey(bi => bi.PartId)
                .OnDelete(DeleteBehavior.Restrict); // Prevent accidental deletion of referenced Parts.
            modelBuilder.Entity<BOMItem>().HasData(
                new BOMItem { Id = 1, BOMId = 1, PartId = 1, Quantity = 5, Notes = "Use high-voltage-rated components", CreatedAt = new DateTime(2024, 2, 1), UpdatedAt = new DateTime(2024, 2, 5) },
                new BOMItem { Id = 2, BOMId = 1, PartId = 2, Quantity = 10, Notes = "Double-check wiring diagrams", CreatedAt = new DateTime(2024, 2, 1), UpdatedAt = new DateTime(2024, 2, 5) },
                new BOMItem { Id = 3, BOMId = 2, PartId = 3, Quantity = 8, Notes = "Ensure safety testing after installation", CreatedAt = new DateTime(2024, 2, 2), UpdatedAt = new DateTime(2024, 2, 6) }
            );

            // DraftBOMItem data
            modelBuilder.Entity<BOM>()
                .HasMany(b => b.DraftBOMItems)
                .WithOne(d => d.BOM)
                .HasForeignKey(d => d.BOMId)
                .OnDelete(DeleteBehavior.Cascade); ; // Auto-delete drafts when BOM is deleted

            // RFQ data
            modelBuilder.Entity<RFQ>()
                .HasOne(r => r.BOM)
                .WithMany(b => b.RFQs)
                .HasForeignKey(r => r.BOMId)
                .OnDelete(DeleteBehavior.Restrict); // Prevent deleting BOM if used in RFQ
            modelBuilder.Entity<RFQ>()
                .HasOne(r => r.CreatedBy)
                .WithMany(u => u.RFQs)
                .HasForeignKey(r => r.UserId)
                .OnDelete(DeleteBehavior.Restrict); // Prevent deleting user if they have created RFQs
            modelBuilder.Entity<RFQ>()
                .HasMany(r => r.RFQItems)
                .WithOne(ri => ri.RFQ)
                .HasForeignKey(ri => ri.RFQId)
                .OnDelete(DeleteBehavior.Cascade); // Cascade delete RFQ items when RFQ is deleted
            modelBuilder.Entity<RFQ>()
                .HasOne(r => r.Supplier)
                .WithMany(s => s.RFQs)
                .HasForeignKey(r => r.SupplierId)
                .OnDelete(DeleteBehavior.Restrict); // Prevent deleting Supplier if linked to RFQ
            modelBuilder.Entity<RFQ>().HasData(
                new RFQ {
                    Id = 1,
                    SupplierId = 1,
                    BOMId = 1,
                    UserId = "2",
                    Notes = "Urgent request, need pricing ASAP",
                    Status = RFQ.RFQStatus.Draft,
                    DueDate = new DateTime(2024, 3, 1),
                    SentDate = null,
                    CreatedAt = new DateTime(2024, 2, 15, 10, 0, 0),
                    UpdatedAt = new DateTime(2024, 2, 15, 10, 0, 0)
                },
                new RFQ {
                    Id = 2,
                    SupplierId = 2,
                    BOMId = 2,
                    UserId = "1",
                    Notes = "Request for bulk order pricing",
                    Status = RFQ.RFQStatus.Sent,
                    DueDate = new DateTime(2024, 3, 1),
                    SentDate = new DateTime(2024, 2, 15, 10, 0, 0),
                    CreatedAt = new DateTime(2024, 2, 15, 10, 0, 0),
                    UpdatedAt = new DateTime(2024, 2, 15, 10, 0, 0)
                },
                new RFQ {
                    Id = 3,
                    SupplierId = 3,
                    BOMId = 3,
                    UserId = "2",
                    Notes = "Need price breakdown per unit",
                    Status = RFQ.RFQStatus.Received,
                    DueDate = new DateTime(2024, 3, 1),
                    SentDate = new DateTime(2024, 2, 15, 10, 0, 0),
                    CreatedAt = new DateTime(2024, 2, 15, 10, 0, 0),
                    UpdatedAt = new DateTime(2024, 2, 15, 10, 0, 0)
                }
            );

            // RFQItem data
            modelBuilder.Entity<RFQItem>()
                .HasOne(ri => ri.RFQ)
                .WithMany(r => r.RFQItems)
                .HasForeignKey(ri => ri.RFQId)
                .OnDelete(DeleteBehavior.Cascade); // Delete RFQItems when RFQ is deleted
            modelBuilder.Entity<RFQItem>()
                .HasOne(ri => ri.BOMItem)
                .WithMany()
                .HasForeignKey(ri => ri.BOMItemId)
                .OnDelete(DeleteBehavior.Restrict); // Prevent BOMItem deletion if used in RFQ
            modelBuilder.Entity<RFQItem>()
                .Property(ri => ri.Price)
                .HasPrecision(18, 2); // Set precision for Price column
            modelBuilder.Entity<RFQItem>().HasData(
                new RFQItem {
                    Id = 1,
                    RFQId = 1,
                    BOMItemId = 1,
                    Quantity = 10,
                    UOM = null, // Will be filled after reply
                    Price = null, // Will be filled after reply
                    ETA = null, // Will be filled after reply
                    CreatedAt = new DateTime(2024, 2, 15, 10, 0, 0),
                    UpdatedAt = new DateTime(2024, 2, 15, 10, 0, 0)
                },
                new RFQItem {
                    Id = 2,
                    RFQId = 1,
                    BOMItemId = 2,
                    Quantity = 5,
                    UOM = null,
                    Price = null,
                    ETA = null,
                    CreatedAt = new DateTime(2024, 2, 15, 10, 0, 0),
                    UpdatedAt = new DateTime(2024, 2, 15, 10, 0, 0)
                },
                new RFQItem {
                    Id = 3,
                    RFQId = 2,
                    BOMItemId = 3,
                    Quantity = 20,
                    UOM = null,
                    Price = null,
                    ETA = null,
                    CreatedAt = new DateTime(2024, 2, 15, 10, 0, 0),
                    UpdatedAt = new DateTime(2024, 2, 15, 10, 0, 0)
                }
            );

            modelBuilder.Entity<PO>()
                .HasOne(b => b.User)
                .WithMany(u => u.POs)
                .HasForeignKey(b => b.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<PO>()
                .HasMany(p => p.POItems)
                .WithOne(pi => pi.PO)
                .HasForeignKey(pi => pi.POId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<POItem>()
                    .HasOne(poItem => poItem.RFQItem)
                    .WithMany()
                    .HasForeignKey(poItem => poItem.RFQId)
                    .OnDelete(DeleteBehavior.Restrict);

            base.OnModelCreating(modelBuilder);
        }

        public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) {
            var timestamp = DateTime.UtcNow;

            foreach (var entry in ChangeTracker.Entries()
                .Where(e => e.State == EntityState.Modified || e.State == EntityState.Added || e.State == EntityState.Deleted)) {

                if (entry.Entity is Part part) part.UpdatedAt = timestamp;
                if (entry.Entity is Customer customer) customer.UpdatedAt = timestamp;
                if (entry.Entity is Job job) job.UpdatedAt = timestamp;
                if (entry.Entity is Supplier supplier) supplier.UpdatedAt = timestamp;
                if (entry.Entity is Manufacturer manufacturer) manufacturer.UpdatedAt = timestamp;
                if (entry.Entity is BOM bom) bom.UpdatedAt = timestamp;
                if (entry.Entity is BOMItem bomItem) {
                    bomItem.UpdatedAt = timestamp;

                    // Also update the parent BOM's UpdatedAt
                    if (bomItem.BOM != null) {
                        bomItem.BOM.UpdatedAt = timestamp;
                    }
                }
                if (entry.Entity is RFQ rfq) rfq.UpdatedAt = timestamp;
                if (entry.Entity is RFQItem rfqItem) {
                    rfqItem.UpdatedAt = timestamp;

                    // Also update the parent RFQ's UpdatedAt
                    if (rfqItem.RFQ != null) {
                        rfqItem.RFQ.UpdatedAt = timestamp;
                    }
                }
                //if (entry.Entity is PO po) po.UpdatedAt = timestamp;
                //if (entry.Entity is POItem poItem) poItem.UpdatedAt = timestamp;
            }

            return await base.SaveChangesAsync(cancellationToken);
        }
    }
}