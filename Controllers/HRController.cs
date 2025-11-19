using CMCS_POE_PART_2.Models;
using CMCS_POE_PART_2.Reports;
using Microsoft.AspNetCore.Mvc;
using QuestPDF.Fluent;
using System.Text;

namespace CMCS_POE_PART_2.Controllers
{
    public class HRController : Controller
    {
        public async Task<IActionResult> Dashboard(string? from = null, string? to = null)
        {
            if (HttpContext.Session.GetString("Role") != "HRManager") return RedirectToAction("Index", "Home");

            DateTime? fromDate = string.IsNullOrWhiteSpace(from) ? null : DateTime.Parse(from);
            DateTime? toDate = string.IsNullOrWhiteSpace(to) ? null : DateTime.Parse(to);

            var claims = await DbHelper.Instance.GetApprovedClaimsAsync(fromDate, toDate);
            ViewBag.From = from;
            ViewBag.To = to;
            return View(claims);
        }

        // CSV export
        public async Task<IActionResult> ExportCsv(string? from, string? to)
        {
            if (HttpContext.Session.GetString("Role") != "HRManager") return Unauthorized();

            DateTime? fromDate = string.IsNullOrWhiteSpace(from) ? null : DateTime.Parse(from);
            DateTime? toDate = string.IsNullOrWhiteSpace(to) ? null : DateTime.Parse(to);
            var claims = await DbHelper.Instance.GetApprovedClaimsAsync(fromDate, toDate);

            var sb = new StringBuilder();
            sb.AppendLine("CMCS Portal - Invoice Report");
            sb.AppendLine($"Generated On: {DateTime.Now:yyyy-MM-dd}");
            sb.AppendLine();

            sb.AppendLine("InvoiceID,Lecturer,Module,Faculty,Total (R),Date,Documents");

            foreach (var c in claims)
            {
                sb.AppendLine($"{c.claimID},\"{c.LecturerName}\",\"{c.module_name}\",\"{c.faculty_name}\",\"R {c.TotalAmount:F2}\",{c.creating_date:yyyy-MM-dd},\"{c.supporting_documents}\"");
            }

            // Per-lecturer subtotals
            var subtotals = claims
                .GroupBy(c => c.LecturerName)
                .Select(g => new { Lecturer = g.Key, Total = g.Sum(c => c.TotalAmount) });

            sb.AppendLine();
            sb.AppendLine("Lecturer Subtotals");
            foreach (var s in subtotals)
            {
                sb.AppendLine($"\"{s.Lecturer}\",,,\"R {s.Total:F2}\"");
            }

            // Grand total
            var grandTotal = claims.Sum(c => c.TotalAmount);
            sb.AppendLine();
            sb.AppendLine($"Grand Total,,,,\"R {grandTotal:F2}\"");

            return File(Encoding.UTF8.GetBytes(sb.ToString()), "text/csv", $"Invoices_{DateTime.Now:yyyyMMdd}.csv");
        }



        // Simple PDF export using HTML -> PDF via inline (for demo; replace with a library in prod)
        public async Task<IActionResult> ExportPdf(string? from, string? to)
        {
            if (HttpContext.Session.GetString("Role") != "HRManager") return Unauthorized();

            DateTime? fromDate = string.IsNullOrWhiteSpace(from) ? null : DateTime.Parse(from);
            DateTime? toDate = string.IsNullOrWhiteSpace(to) ? null : DateTime.Parse(to);
            var claims = await DbHelper.Instance.GetApprovedClaimsAsync(fromDate, toDate);

            var pdf = new ApprovedClaimsReport(claims);
            var stream = pdf.GeneratePdf();

            return File(stream, "application/pdf", $"ApprovedClaims_{DateTime.Now:yyyyMMdd}.pdf");
        }


        // Lecturer management
        public async Task<IActionResult> Lecturers(string? q = null)
        {
            if (HttpContext.Session.GetString("Role") != "HRManager") return RedirectToAction("Index", "Home");
            var list = await DbHelper.Instance.GetAllLecturersAsync(q);
            ViewBag.Query = q;
            return View(list);
        }

        public async Task<IActionResult> EditLecturer(int id)
        {
            if (HttpContext.Session.GetString("Role") != "HRManager") return RedirectToAction("Index", "Home");
            var lecturer = await DbHelper.Instance.GetUserByIdAsync(id);
            if (lecturer == null || lecturer.role != "Lecturer") return NotFound();
            return View(lecturer);
        }


        [HttpPost]
        public async Task<IActionResult> EditLecturer(User user)
        {
            if (HttpContext.Session.GetString("Role") != "HRManager")
                return RedirectToAction("Index", "Home");

            if (!ModelState.IsValid)
            {
                TempData["Error"] = "Validation failed. Please check all fields.";
                return View(user);
            }

            // Ensure role is preserved
            var existing = await DbHelper.Instance.GetUserByIdAsync(user.userID);
            if (existing == null || existing.role != "Lecturer")
            {
                TempData["Error"] = "Lecturer not found or invalid role.";
                return RedirectToAction("Lecturers");
            }

            user.role = "Lecturer"; // enforce role
            await DbHelper.Instance.UpdateLecturerAsync(user);

            TempData["Success"] = "Lecturer details updated successfully.";
            return RedirectToAction("Lecturers");
        }

    }
}
