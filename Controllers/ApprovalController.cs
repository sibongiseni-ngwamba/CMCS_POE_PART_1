using CMCS.Models;
using CMCS_POE_PART_2.Models;
using Microsoft.AspNetCore.Mvc;

namespace CMCS.Controllers
{
    public class ApprovalController : Controller
    {
        public async Task<IActionResult> Verify()
        {
            try
            {
                if (HttpContext.Session.GetString("Role") != "Coordinator") return RedirectToAction("Index", "Home");
                var claims = await DbHelper.Instance.GetPendingClaimsAsync("Pending");
                return View(claims);
            }
            catch (Exception)
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
                TempData["Success"] = "Claim verified and sent for approval!";
                return RedirectToAction("Verify");
            }
            catch (Exception)
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
            catch (Exception)
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
                TempData["Success"] = isApproved ? "Claim approved!" : "Claim rejected.";
                return RedirectToAction("Approve");
            }
            catch (Exception)
            {
                TempData["Error"] = "Approval action failed.";
                return RedirectToAction("Approve");
            }
        }
    }
}
