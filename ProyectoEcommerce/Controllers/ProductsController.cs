using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;                 // ← NUEVO
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using ProyectoEcommerce.Data;
using ProyectoEcommerce.Models;

namespace ProyectoEcommerce.Controllers
{
    [Authorize(Roles = "Admin")]                           // ← TODO el controlador es SOLO Admin
    public class ProductsController : Controller
    {
        private readonly ProyectoEcommerceContext _context;
        public ProductsController(ProyectoEcommerceContext context) => _context = context;

        // ========= CATÁLOGO PÚBLICO =========
        [AllowAnonymous]                                    // ← Abierto a todos
        public async Task<IActionResult> Public(string q = null, int? categoryId = null)
        {
            var query = _context.Products
                                 .Include(p => p.Category)
                                 //.Where(p => p.Available) // si quieres solo disponibles
                                 .AsQueryable();

            if (!string.IsNullOrWhiteSpace(q))
                query = query.Where(p => p.Name.Contains(q) || p.Description.Contains(q));

            if (categoryId.HasValue)
                query = query.Where(p => p.CategoryId == categoryId);

            var data = await query.OrderBy(p => p.Name).ToListAsync();

            ViewBag.Categories = await _context.Categories
                .OrderBy(c => c.Name)
                .Select(c => new SelectListItem { Value = c.CategoryId.ToString(), Text = c.Name })
                .ToListAsync();

            return View(data);                               // Views/Products/Public.cshtml
        }

        // (Opcional) Detalle público
        [AllowAnonymous]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var product = await _context.Products
                                        .Include(p => p.Category)
                                        .FirstOrDefaultAsync(m => m.Id == id);
            if (product == null) return NotFound();

            return View(product);
        }

        // ========= ADMIN (CRUD) =========
        // GET: Products
        public async Task<IActionResult> Index()
        {
            var list = _context.Products.Include(p => p.Category);
            return View(await list.ToListAsync());
        }

        // GET: Products/Create
        public IActionResult Create()
        {
            ViewData["CategoryId"] = new SelectList(_context.Categories, "CategoryId", "Name");
            return View();
        }

        // POST: Products/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,Name,Description,Price,Available,ImageUrl,Stock,CategoryId")] Product product)
        {
            if (!ModelState.IsValid)
            {
                ViewData["CategoryId"] = new SelectList(_context.Categories, "CategoryId", "Name", product.CategoryId);
                return View(product);
            }

            _context.Add(product);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        // GET: Products/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var product = await _context.Products.FindAsync(id);
            if (product == null) return NotFound();

            ViewData["CategoryId"] = new SelectList(_context.Categories, "CategoryId", "Name", product.CategoryId);
            return View(product);
        }

        // POST: Products/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Name,Description,Price,Available,ImageUrl,Stock,CategoryId")] Product product)
        {
            if (id != product.Id) return NotFound();

            if (!ModelState.IsValid)
            {
                ViewData["CategoryId"] = new SelectList(_context.Categories, "CategoryId", "Name", product.CategoryId);
                return View(product);
            }

            try
            {
                _context.Update(product);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!ProductExists(product.Id)) return NotFound();
                throw;
            }
            return RedirectToAction(nameof(Index));
        }

        // GET: Products/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var product = await _context.Products
                .Include(p => p.Category)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (product == null) return NotFound();

            return View(product);
        }

        // POST: Products/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var product = await _context.Products.FindAsync(id);
            if (product != null) _context.Products.Remove(product);

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool ProductExists(int id) =>
            _context.Products.Any(e => e.Id == id);
    }
}
