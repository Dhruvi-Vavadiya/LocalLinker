using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace LocalLinker.Controllers
{
    public class TemplateController : Controller
    {
        // GET: TemplateController
        public ActionResult Index()
        {
            return View();
        }
        public ActionResult Index1()
        {
            return View();
        }

        public ActionResult about()
        {
            return View();
        }
        public ActionResult blogsingle()
        {
            return View();
        }
        public ActionResult blog()
        {
            return View();
        }
        public ActionResult contact()
        {
            return View();
        }

        public ActionResult main()
        {
            return View();
        }
        public ActionResult portfolio()
        {
            return View();
        }
        public ActionResult pricing()
        {
            return View();
        }
        public ActionResult services()
        {
            return View();
        }

        // GET: TemplateController/Details/5
        public ActionResult Details(int id)
        {
            return View();
        }

        // GET: TemplateController/Create
        public ActionResult Create()
        {
            return View();
        }

        // POST: TemplateController/Create
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

        // GET: TemplateController/Edit/5
        public ActionResult Edit(int id)
        {
            return View();
        }

        // POST: TemplateController/Edit/5
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

        // GET: TemplateController/Delete/5
        public ActionResult Delete(int id)
        {
            return View();
        }

        // POST: TemplateController/Delete/5
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
