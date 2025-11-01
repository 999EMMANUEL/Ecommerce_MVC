using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;          // ← NUEVO
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProyectoEcommerce.Data;
using ProyectoEcommerce.Models;

namespace ProyectoEcommerce.Controllers
{
    [Authorize(Roles = "Admin")]                    // ← TODO el controlador es SOLO Admin por defecto
    public class CategoriesController : Controller
    {
        private readonly ProyectoEcommerceContext _context;

        public CategoriesController(ProyectoEcommerceContext context)
        {
            _context = context;
        }

        // ========= PÁGINA PÚBLICA (visible a clientes) =========
        [AllowAnonymous]                            // ← Cualquiera puede entrar
        public async Task<IActionResult> Public()
        {
            var data = await _context.Categories
                //.Where(c => c.Activa)             // ← si tienes un flag Activa, puedes filtrar
                .OrderBy(c => c.Name)
                .ToListAsync();

            return View(data); // Views/Categories/Public.cshtml
        }

        // (Opcional) Detalle público
      
        [AllowAnonymous]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var category = await _context.Categories
                .Include(c => c.Products)  // Modelos de Productos
                .FirstOrDefaultAsync(m => m.CategoryId == id);

            if (category == null) return NotFound();

            return View(category);
        }

        // ========= ADMIN (CRUD) =========
        public async Task<IActionResult> Index()
        {
            return View(await _context.Categories.ToListAsync());
        }

        // GET: Categories/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Categories/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("CategoryId,Name,Description")] Category category)
        {
            if (!ModelState.IsValid) return View(category);

            _context.Add(category);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        // GET: Categories/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var category = await _context.Categories.FindAsync(id);
            if (category == null) return NotFound();

            return View(category);
        }

        // POST: Categories/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("CategoryId,Name,Description")] Category category)
        {
            if (id != category.CategoryId) return NotFound();

            if (!ModelState.IsValid) return View(category);

            try
            {
                _context.Update(category);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!CategoryExists(category.CategoryId)) return NotFound();
                throw;
            }
            return RedirectToAction(nameof(Index));
        }

        // GET: Categories/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var category = await _context.Categories
                .FirstOrDefaultAsync(m => m.CategoryId == id);
            if (category == null) return NotFound();

            return View(category);
        }

        // POST: Categories/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var category = await _context.Categories.FindAsync(id);
            if (category != null)
            {
                _context.Categories.Remove(category);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }

        private bool CategoryExists(int id)
        {
            return _context.Categories.Any(e => e.CategoryId == id);
        }


        // Método para cargar categorías en el menú (API)
        [AllowAnonymous]
        [HttpGet]
        public async Task<IActionResult> GetCategoriesForMenu()
        {
            var categories = await _context.Categories
                .OrderBy(c => c.Name)
                .Select(c => new
                {
                    categoryId = c.CategoryId,
                    name = c.Name
                })
                .ToListAsync();

            return Json(categories);
        }
    }
}
