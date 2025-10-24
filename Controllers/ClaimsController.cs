using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace CMCS_POE_PART_2.Controllers
{
    public class ClaimsController : Controller
    {
        // GET: ClaimsController
        public ActionResult Index()
        {
            return View();
        }

        // GET: ClaimsController/Details/5
        public ActionResult Details(int id)
        {
            return View();
        }

        // GET: ClaimsController/Create
        public ActionResult Create()
        {
            return View();
        }

        // POST: ClaimsController/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(IFormCollection collection)
        {
            try
            {
                return RedirectToAction(nameof(Index));
            }
            catch
            {
                return View();
            }
        }

        // GET: ClaimsController/Edit/5
        public ActionResult Edit(int id)
        {
            return View();
        }

        // POST: ClaimsController/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(int id, IFormCollection collection)
        {
            try
            {
                return RedirectToAction(nameof(Index));
            }
            catch
            {
                return View();
            }
        }

        // GET: ClaimsController/Delete/5
        public ActionResult Delete(int id)
        {
            return View();
        }

        // POST: ClaimsController/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Delete(int id, IFormCollection collection)
        {
            try
            {
                return RedirectToAction(nameof(Index));
            }
            catch
            {
                return View();
            }
        }
    }
}
