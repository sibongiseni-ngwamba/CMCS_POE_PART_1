using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using CMCS_POE_PART_2.Models;
using System.IO;
using System.Diagnostics;
using System.Text;
using System.Data.SqlClient;

namespace CMCS_POE_PART_2.Controllers
{
    public class ScopedProfiler : IDisposable
    {
        public Stopwatch Stopwatch { get; } = Stopwatch.StartNew();
        public StringBuilder LogBuilder { get; } = new StringBuilder();
        private readonly string _correlationId;
        public ScopedProfiler(string correlationId)
        {
            _correlationId = correlationId;
            LogBuilder.AppendLine($"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}] [{_correlationId}] ScopedProfiler initialized.");
        }
        public void Log(string message) => LogBuilder.AppendLine($"[T+{Stopwatch.ElapsedMilliseconds}ms] {message}");
        public void Dispose()
        {
            Stopwatch.Stop();
            LogBuilder.AppendLine($"[T+{Stopwatch.ElapsedMilliseconds}ms] Profiler disposed.");
        }
    }

    public class ClaimsController : Controller
    {
        private readonly IWebHostEnvironment _env;
        private readonly string[] _allowedTypes = { ".pdf", ".docx", ".xlsx" };
        private readonly long _maxSize = 5 * 1024 * 1024;
        private readonly string _correlationId = Guid.NewGuid().ToString("N")[..8];

        public ClaimsController(IWebHostEnvironment env) => _env = env;

        public IActionResult New()
        {
            if (HttpContext.Session.GetString("Role") != "Lecturer")
                return RedirectToAction("Index", "Home");
            return View(new Claim { creating_date = DateTime.Today });
        }

        [HttpPost]
        public async Task<IActionResult> New(Claim claim, List<IFormFile>? files)
        {
            using var profiler = new ScopedProfiler(_correlationId);
            try
            {
                profiler.Log("ModelState check.");
                if (!ModelState.IsValid)
                {
                    TempData["Error"] = "Invalid inputs—sessions/hours/rate must be positive; module/faculty required.";
                    return View(claim);
                }

                var role = HttpContext.Session.GetString("Role");
                if (role != "Lecturer")
                {
                    return BadRequest("Access denied—lecturer only.");
                }

                var lecturerId = HttpContext.Session.GetInt32("UserId") ?? 0;
                if (lecturerId == 0)
                {
                    TempData["Error"] = "Session expired—log in again.";
                    return RedirectToAction("Login", "Account");
                }

                claim.lecturerID = lecturerId;
                var rawTotal = claim.number_of_sessions * claim.number_of_hours * claim.amount_of_rate;
                if (rawTotal > 50000m)
                    TempData["Warning"] = "High-value claim flagged—expect manual review.";

                claim.TotalAmount = (decimal)rawTotal;
                claim.claim_status = "Pending";

                var docNames = new List<string>();
                if (files != null && files.Count > 0)
                {
                    var uploadsDir = Path.Combine(_env.WebRootPath, "uploads");
                    Directory.CreateDirectory(uploadsDir);
                    foreach (var file in files)
                    {
                        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
                        if (file.Length > _maxSize || !_allowedTypes.Contains(ext))
                        {
                            ModelState.AddModelError("files", $"File '{file.FileName}' invalid: Size <5MB, type PDF/DOCX/XLSX only.");
                            TempData["Error"] = ModelState["files"]?.Errors.FirstOrDefault()?.ErrorMessage ?? "Upload failed.";
                            return View(claim);
                        }
                        var uniqueName = $"{claim.lecturerID}_{DateTime.Now:yyyyMMddHHmmss}{ext}";
                        var filePath = Path.Combine(uploadsDir, uniqueName);
                        using var stream = new FileStream(filePath, FileMode.Create);
                        await file.CopyToAsync(stream);
                        docNames.Add(uniqueName);
                    }
                }
                claim.supporting_documents = string.Join(",", docNames);

                var claimId = await DbHelper.Instance.CreateClaimAsync(claim);
                claim.claimID = claimId;
                await DbHelper.Instance.AppendAuditAsync(claimId, "Created", lecturerId);

                TempData["Success"] = $"Claim #{claim.claimID} submitted! Total: R{claim.TotalAmount:F2}. Pending review.";
                if (claim.TotalAmount <= 1000m)
                {
                    await DbHelper.Instance.UpdateClaimStatusAsync(claim.claimID, "Approved");
                    await DbHelper.Instance.AppendAuditAsync(claim.claimID, "Auto-Approved", lecturerId);
                    TempData["Success"] = $"Claim #{claim.claimID} auto-approved! Total: R{claim.TotalAmount:F2}.";
                }
                else
                {
                    TempData["Success"] = $"Claim #{claim.claimID} submitted! Total: R{claim.TotalAmount:F2}. Pending review.";
                }

                return RedirectToAction("Index");
            }
            catch (SqlException sqlEx) when (sqlEx.Number == 547)
            {
                TempData["Error"] = "Database integrity issue—refresh and retry (lecturer mismatch?).";
            }
            catch (SqlException sqlEx) when (sqlEx.Number == 2627 || sqlEx.Number == 2601)
            {
                TempData["Error"] = "Duplicate entry detected—unique claim period? Try different details.";
            }
            catch
            {
                TempData["Error"] = "Submission error—please check your inputs and try again.";
            }
            return View(claim);
        }

        public async Task<IActionResult> Index()
        {
            try
            {
                if (HttpContext.Session.GetString("Role") != "Lecturer") return RedirectToAction("Index", "Home");
                var lecturerId = HttpContext.Session.GetInt32("UserId") ?? 0;
                var claims = await DbHelper.Instance.GetClaimsByLecturerAsync(lecturerId);
                return View(claims);
            }
            catch
            {
                TempData["Error"] = "Unable to load your claims at this time.";
                return View(new List<Claim>());
            }
        }
    }
}
