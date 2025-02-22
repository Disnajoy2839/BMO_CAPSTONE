/* BOMLink/Program.cs
* Capstone Project
* Revision History
* Aline Sathler Delfino, 2025.01.25: Created, business layer.
* Aline Sathler Delfino, 2025.01.26: Database.
* Aline Sathler Delfino, 2025.02.01: Layout, Login page, hash password.
* Aline Sathler Delfino, 2025.02.02: Dashboard, Navbar, Profile Bubble, Logout.
* Aline Sathler Delfino, 2025.02.02: Manufacturer, Supplier, Customer, Job.
* Aline Sathler Delfino, 2025.02.03: Job: Validation to avoid duplicate number, import/export.
* Aline Sathler Delfino, 2025.02.03: Part, Supplier-Manufacturer Mapping.
* Aline Sathler Delfino, 2025.02.04: Automatic Logout.
* Aline Sathler Delfino, 2025.02.05: User application using AspNetCore.Identity.
* Aline Sathler Delfino, 2025.02.06: User settings, Profile Picture, User Roles.
* Aline Sathler Delfino, 2025.02.07: User management, User registration, tooltips for buttons and uniform buttons.
* Aline Sathler Delfino, 2025.02.08: BOM: Model, Index, Create, Edit, Delete, Details, Search, Sort, Filter, Relationship and Clone.
* Aline Sathler Delfino, 2025.02.08: BOMItem: Model, Index, Create.
* Aline Sathler Delfino, 2025.02.09: BOMItem: Edit, Delete, Details, Unified Create/Edit View. Part: Unified Create/Edit View and Details. BOM: Unified Create/Edit View. BOMItems: Import/Export. BOMItems: DrafItems.
* Aline Sathler Delfino, 2025.02.09: Job/Part/Customer/Supplier/Manufacturer: Unified Create and Edit View, Time Stamps, Details.
* Aline Sathler Delfino, 2025.02.10: BOMItems: Sort, Search, Filter. UnityTests: User, BOM, BOMItem, UserController, AdminController.
* Aline Sathler Delfino, 2025.02.10: OCR for BOMItems. Working for 2 columns only.
* Aline Sathler Delfino, 2025.02.11: Fixed ManufacturerId and Search/Sort/Filter in SupplierManufacturer Link. Debug BOM status.
* Aline Sathler Delfino, 2025.02.13: Fixed BOMItem status. Fixed deleting BOM if status is locked.
* Aline Sathler Delfino, 2025.02.14: Fixed partnumber to ignore non alphanumeric characteres.
* Aline Sathler Delfino, 2025.02.15: Fixed deleting: Job, Customer, Manufacturer.
* Aline Sathler Delfino, 2025.02.15: RFQ: Model, DBContext, Generate, Index. RFQItem: Model, DBContext, Index, Edit. RFQ: Delete and Send.
* Aline Sathler Delfino, 2025.02.16: RFQ: Details, Generate. Multiple bug fixes.
* Aline Sahtler Delfino, 2025.02.17: RFQ: Fix bug in generate that added didn't check BOMId before adding part to RFQ. RFQ: Send email to supplier.
*/

// fix deleting supplier after the po module is done
// labour?
// totalprice bom?
// improve bom details view with labour, totals and more information, invert order between parts and RFQ

using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using BOMLink.Data;
using BOMLink.Models;
using BOMLink.Services;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddHttpContextAccessor();
builder.Services.AddControllersWithViews();
builder.Services.AddSingleton<AzureOCRService>();

builder.Services.AddDbContext<BOMLinkContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("BOMLinkContext")));

// Register Identity Services with ApplicationUser
builder.Services.AddIdentity<ApplicationUser, IdentityRole>()
    .AddEntityFrameworkStores<BOMLinkContext>()
    .AddDefaultTokenProviders();

builder.Services.ConfigureApplicationCookie(options => {
    options.LoginPath = "/User/Login";  // Redirect to login if unauthorized
    options.LogoutPath = "/User/Logout";
    options.AccessDeniedPath = "/Home/AccessDenied";
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    options.Cookie.SameSite = SameSiteMode.Strict;
    options.ExpireTimeSpan = TimeSpan.FromMinutes(30);
    options.SlidingExpiration = true; // Reset timer if user is active
});

builder.Services.AddAuthentication("Cookies")
    .AddCookie("Cookies", options => {
        options.LoginPath = "/User/Login";
        options.LogoutPath = "/User/Logout";
        options.AccessDeniedPath = "/Home/AccessDenied";
    });

builder.Services.Configure<SecurityStampValidatorOptions>(options => {
    options.ValidationInterval = TimeSpan.Zero; // Force check on every request (Ensures the function to logout of all devices)
});

builder.Services.AddAuthorization();
builder.Services.AddScoped<AzureOCRServiceTest>();
builder.Services.AddScoped<RFQService>();

var app = builder.Build();

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseAuthentication();
app.UseRouting();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();