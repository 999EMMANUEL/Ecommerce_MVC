using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using ProyectoEcommerce.Models;
using ProyectoEcommerce.Data; //  DbContext namespace
using Microsoft.EntityFrameworkCore;

namespace ProyectoEcommerce.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly ProyectoEcommerceContext _context;

        public HomeController(ILogger<HomeController> logger, ProyectoEcommerceContext context)
        {
            _logger = logger;
            _context = context;
        }

        // GET: Home/Index
        public async Task<IActionResult> Index()
        {
            // Ejemplo: mostrar hasta 8 productos disponibles ordenados por Id descendente (ajusta según necesites)
            var productos = await _context.Products
                                 .Where(p => p.Available)          // opcional: solo disponibles
                                 .OrderByDescending(p => p.Id)
                                 .Take(8)
                                 .ToListAsync();

            return View(productos); // la vista recibirá IEnumerable<Product>
        }

        public IActionResult Privacy() => View();
        public IActionResult LegalInformation() => View();
        public IActionResult SocialMedia() => View();


        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
