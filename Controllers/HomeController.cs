using Microsoft.AspNetCore.Mvc;

namespace CMCS_POE_PART_2.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            var role = HttpContext.Session.GetString("Role");
            if (string.IsNullOrEmpty(role))
                return RedirectToAction("Login", "Account");
            ViewBag.Role = role;
            ViewBag.UserName = HttpContext.Session.GetString("UserName");
            return View();
        }
    }
}
