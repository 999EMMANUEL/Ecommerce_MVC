using Microsoft.AspNetCore.Mvc;

namespace ProyectoEcommerce.Controllers
{
    public class ContactSucursalesController : Controller
    {
    


       
        public ActionResult Index(string branch)
        {

            if (!string.IsNullOrEmpty(branch))
            {
                ViewData["Branch"] = branch;
                ViewData["Title"] = $"Sucursal {GetBranchName(branch)}";
            }
            else
            {
                ViewData["Title"] = "Contacto";
            }

            return View();
        }

        private string GetBranchName(string branch)
        {
            return branch switch
            {
                "central" => "Central - Nicoya",
                "oeste" => "Plaza Oeste",
                "playa" => "Punto Express - Playa",
                _ => "Contacto"
            };
        }

        
        public ActionResult Details(int id)
        {
            return View();
        }

      
        public ActionResult Create()
        {
            return View();
        }

     
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

        
        public ActionResult Edit(int id)
        {
            return View();
        }

  
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

      
        public ActionResult Delete(int id)
        {
            return View();
        }

     
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
