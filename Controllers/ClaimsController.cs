using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using CMCS.Models;
using System.IO;
using System.Diagnostics;  // For mini-profiler timestamps
using System.Text;  // For log builder
using System.Data.SqlClient;  // SqlException namespace
using System;  // Exception base

namespace CMCS.Controllers
{
    // Innovative: Disposable profiler for auto-scope management
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

        public void Log(string message)
        {
            LogBuilder.AppendLine($"[T+{Stopwatch.ElapsedMilliseconds}ms] {message}");
        }

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
        private readonly long _maxSize = 5 * 1024 * 1024;  // 5MB
        private readonly string _correlationId = Guid.NewGuid().ToString("N")[..8];  // Trace ID for forensics

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
            using var profiler = new ScopedProfiler(_correlationId);  // Hoisted scope—auto-dispose in finally equiv

            try
            {
                profiler.Log("ModelState check.");

                if (!ModelState.IsValid)
                {
                    profiler.Log($"ModelState invalid: {string.Join(", ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage))}");
                    TempData["Error"] = "Invalid inputs—sessions/hours/rate must be positive; module/faculty required.";
                    return View(claim);
                }

                var role = HttpContext.Session.GetString("Role");
                if (role != "Lecturer")
                {
                    profiler.Log($"Unauthorized: Role '{role}' != 'Lecturer'.");
                    return BadRequest("Access denied—lecturer only.");
                }

                var lecturerId = HttpContext.Session.GetInt32("UserId") ?? 0;
                if (lecturerId == 0)
                {
                    profiler.Log("Session invalid: lecturerId=0.");
                    TempData["Error"] = "Session expired—log in again.";
                    return RedirectToAction("Login", "Account");
                }
                claim.lecturerID = lecturerId;

                profiler.Log("Calc total start.");
                var rawTotal = claim.number_of_sessions * claim.number_of_hours * claim.amount_of_rate;
                // Innovative: Pre-calc sanity (anti-fraud flag)
                if (rawTotal > 50000m)
                {
                    profiler.Log($"Suspicious total: R{rawTotal:F2} > R50k threshold—flagged for review.");
                    TempData["Warning"] = "High-value claim flagged—expect manual review.";
                }
                claim.TotalAmount = (decimal)rawTotal;
                claim.claim_status = "Pending";
                profiler.Log($"Calc total complete: R{claim.TotalAmount:F2}.");

                // Uploads with trace
                profiler.Log($"Upload start: {files?.Count ?? 0} files.");
                var docNames = new List<string>();
                if (files != null && files.Count > 0)
                {
                    var uploadsDir = Path.Combine(_env.WebRootPath, "uploads");
                    Directory.CreateDirectory(uploadsDir);
                    foreach (var file in files)
                    {
                        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
                        profiler.Log($"File '{file.FileName}': Size={file.Length}, Ext={ext}.");
                        if (file.Length > _maxSize || !_allowedTypes.Contains(ext))
                        {
                            profiler.Log($"Invalid file: {file.FileName}.");
                            ModelState.AddModelError("files", $"File '{file.FileName}' invalid: Size <5MB, type PDF/DOCX/XLSX only.");
                            TempData["Error"] = ModelState["files"]?.Errors.FirstOrDefault()?.ErrorMessage ?? "Upload failed.";
                            return View(claim);
                        }
                        var uniqueName = $"{claim.lecturerID}_{DateTime.Now:yyyyMMddHHmmss}{ext}";
                        var filePath = Path.Combine(uploadsDir, uniqueName);
                        using (var stream = new FileStream(filePath, FileMode.Create))
                        {
                            await file.CopyToAsync(stream);
                        }
                        docNames.Add(uniqueName);
                        profiler.Log($"Uploaded: {uniqueName}.");
                    }
                }
                claim.supporting_documents = string.Join(",", docNames);
                profiler.Log($"Upload complete: '{claim.supporting_documents}'.");

                // DB Insert with trace
                profiler.Log("DB insert start.");
                var claimId = await DbHelper.Instance.CreateClaimAsync(claim);  // Capture ID
                claim.claimID = claimId;  // Backfill
                profiler.Log($"DB insert success: ID={claim.claimID}.");

                TempData["Success"] = $"Claim #{claim.claimID} submitted! Total: R{claim.TotalAmount:F2}. Pending review.";
                // Debug: TempData["DebugLog"] = profiler.LogBuilder.ToString();  // Uncomment for spew
                return RedirectToAction("Index");
            }
            catch (SqlException sqlEx) when (sqlEx.Number == 547)  // FK violation
            {
                profiler.Log($"FK Error (547): {sqlEx.Message}. CorrID: {_correlationId}.");
                TempData["Error"] = "Database integrity issue—refresh and retry (lecturer mismatch?).";
                LogToConsole(profiler.LogBuilder.ToString(), sqlEx);
            }
            catch (SqlException sqlEx) when (sqlEx.Number == 2627 || sqlEx.Number == 2601)  // Dup key/constraint
            {
                profiler.Log($"Dup Error ({sqlEx.Number}): {sqlEx.Message}. CorrID: {_correlationId}.");
                TempData["Error"] = "Duplicate entry detected—unique claim period? Try different details.";
                LogToConsole(profiler.LogBuilder.ToString(), sqlEx);
            }
            catch (Exception ex)
            {
                profiler.Log($"General Error: {ex.Message}. Stack: {ex.StackTrace?.Substring(0, 200)}... CorrID: {_correlationId}.");
                TempData["Error"] = "Submission error—please check your inputs and try again. (Debug: Connection hiccup?)";
                LogToConsole(profiler.LogBuilder.ToString(), ex);
            }
            return View(claim);
        }

        // Centralized logging stub—future AppInsights sink
        private void LogToConsole(string trace, Exception ex)
        {
            Console.WriteLine($"[{_correlationId}] TRACE:\n{trace}");
            Console.WriteLine($"[{_correlationId}] EXCEPTION: {ex}");
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
            catch (Exception ex)
            {
                Console.WriteLine($"Index Error [{_correlationId}]: {ex}");
                TempData["Error"] = "Unable to load your claims at this time.";
                return View(new List<Claim>());
            }
        }
    }
}
