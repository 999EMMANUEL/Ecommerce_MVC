using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProyectoEcommerce.Data; //  DbContext namespace
using ProyectoEcommerce.Models;
using System.Diagnostics;

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
        [AllowAnonymous]
        public async Task<IActionResult> Index()
        {
            const int maxManual = 6;
            const int maxAuto = 3;

            // 1) Manuales (IsFeatured = true)
            var featuredManual = await _context.Products
                .Include(p => p.Category)
                .Where(p => p.Available && p.Stock > 0 && p.IsFeatured)
                .OrderByDescending(p => p.CreatedAt)
                .Take(maxManual)
                .ToListAsync();

            // 2) Automáticos (más vistos, excluyendo los manuales y sin ViewCount = 0)
            var featuredAuto = await _context.Products
                .Include(p => p.Category)
                .Where(p => p.Available && p.Stock > 0 && !p.IsFeatured && p.ViewCount > 0)
                .OrderByDescending(p => p.ViewCount)
                .ThenByDescending(p => p.CreatedAt)
                .Take(maxAuto)
                .ToListAsync();

            // 3) Combinar (máximo 9 en total)
            var destacados = featuredManual.Concat(featuredAuto)
                                           .Take(maxManual + maxAuto)
                                           .ToList();

            return View(destacados); // La vista recibe IEnumerable<Product>
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
