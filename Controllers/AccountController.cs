using Microsoft.AspNetCore.Mvc;
using CMCS_POE_PART_2.Models;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace CMCS_POE_PART_2.Controllers
{
    public class AccountController : Controller
    {
        private string HashPassword(string pw)
        {
            using var sha = SHA256.Create();
            return Convert.ToBase64String(sha.ComputeHash(Encoding.UTF8.GetBytes(pw)));
        }

        public IActionResult Login() => View();

        [HttpPost]
        public async Task<IActionResult> Login(string email, string password)
        {
            try
            {
                var user = await DbHelper.Instance.GetUserByEmailAsync(email);
                if (user == null || user.password != HashPassword(password))
                {
                    TempData["Error"] = "Invalid credentials. Please register if new.";
                    return View();
                }
                HttpContext.Session.SetInt32("UserId", user.userID);
                HttpContext.Session.SetString("Role", user.role);
                HttpContext.Session.SetString("UserName", $"{user.full_names} {user.surname}");
                return RedirectToAction("Index", "Home");
            }
            catch (Exception)
            {
                TempData["Error"] = "Login service temporarily unavailable. Try again.";
                return View();
            }
        }

        public IActionResult Register() => View(new User { date = DateTime.Today });

        [HttpPost]
        public async Task<IActionResult> Register(User user, string confirmPassword)
        {
            if (!ModelState.IsValid) return View(user);

            if (string.IsNullOrEmpty(user.password) || user.password.Length < 8 || !Regex.IsMatch(user.password, @"^(?=.*[a-zA-Z])(?=.*\d)"))
            {
                ModelState.AddModelError("password", "Password must be 8+ characters with letters and numbers.");
                return View(user);
            }
            if (user.password != confirmPassword)
            {
                ModelState.AddModelError("password", "Passwords do not match.");
                return View(user);
            }

            try
            {
                var existing = await DbHelper.Instance.GetUserByEmailAsync(user.email);
                if (existing != null)
                {
                    ModelState.AddModelError("email", "Email already in use.");
                    return View(user);
                }

                user.password = HashPassword(user.password);
                await DbHelper.Instance.CreateUserAsync(user);
                TempData["Success"] = "Registration successful! Please log in.";
                return RedirectToAction("Login");
            }
            catch (Exception)
            {
                TempData["Error"] = "Registration failed. Please try again.";
                return View(user);
            }
        }

        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Login");
        }
    }
}
