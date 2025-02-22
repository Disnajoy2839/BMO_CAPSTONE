using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using BOMLink.Data;

namespace BOMLink.Controllers {
    public class HomeController : Controller {
        private readonly BOMLinkContext _context;

        public HomeController(BOMLinkContext context) {
            _context = context;
        }

        public IActionResult Index() {
            if (!User.Identity?.IsAuthenticated ?? false) {
                TempData["SessionExpired"] = "Your session is expired. Please, log in";
                return RedirectToAction("Login", "User");
            }

            var username = User.Identity.Name;
            var user = _context.Users.FirstOrDefault(u => u.UserName == username);

            return RedirectToAction("Index", "Dashboard");
        }
    }
}