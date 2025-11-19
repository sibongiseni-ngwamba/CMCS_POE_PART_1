using Microsoft.AspNetCore.Mvc;
using CMCS_POE_PART_2.Models;

namespace CMCS_POE_PART_2.Controllers
{
    public class ApprovalController : Controller
    {
        public async Task<IActionResult> Verify()
        {
            try
            {
                if (HttpContext.Session.GetString("Role") != "Coordinator") return RedirectToAction("Index", "Home");
                var claims = await DbHelper.Instance.GetPendingClaimsAsync("Pending");
                // Policy cues (simple example)
                foreach (var c in claims)
                {
                    c.LecturerName = c.LecturerName; // already set
                                                     // Example anomaly flags via ViewData (rate > 1500 or hours > 12)
                    if (c.amount_of_rate > 1500 || c.number_of_hours > 12)
                        TempData["Warning"] = "One or more claims exceed normal policy thresholds.";
                }
                return View(claims);
            }
            catch
            {
                TempData["Error"] = "Unable to load pending claims.";
                return View(new List<Claim>());
            }
        }

        [HttpPost]
        public async Task<IActionResult> Verify(int claimId)
        {
            try
            {
                if (HttpContext.Session.GetString("Role") != "Coordinator") return BadRequest("Access denied.");
                await DbHelper.Instance.UpdateClaimStatusAsync(claimId, "Verified");
                await DbHelper.Instance.AppendAuditAsync(claimId, "Verified", HttpContext.Session.GetInt32("UserId") ?? 0);
                TempData["Success"] = "Claim verified and sent for approval!";
                return RedirectToAction("Verify");
            }
            catch
            {
                TempData["Error"] = "Verification failed—try again.";
                return RedirectToAction("Verify");
            }
        }

        public async Task<IActionResult> Approve()
        {
            try
            {
                if (HttpContext.Session.GetString("Role") != "Manager") return RedirectToAction("Index", "Home");
                var claims = await DbHelper.Instance.GetPendingClaimsAsync("Verified");
                return View(claims);
            }
            catch
            {
                TempData["Error"] = "Unable to load claims for approval.";
                return View(new List<Claim>());
            }
        }

        [HttpPost]
        public async Task<IActionResult> Approve(int claimId, bool isApproved)
        {
            try
            {
                if (HttpContext.Session.GetString("Role") != "Manager") return BadRequest("Access denied.");
                var status = isApproved ? "Approved" : "Rejected";
                await DbHelper.Instance.UpdateClaimStatusAsync(claimId, status);
                await DbHelper.Instance.AppendAuditAsync(claimId, status, HttpContext.Session.GetInt32("UserId") ?? 0);
                TempData["Success"] = isApproved ? "Claim approved!" : "Claim rejected.";
                return RedirectToAction("Approve");
            }
            catch
            {
                TempData["Error"] = "Approval action failed.";
                return RedirectToAction("Approve");
            }
        }
    }
}
