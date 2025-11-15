using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using ProyectoEcommerce.Data;
using ProyectoEcommerce.Models;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace ProyectoEcommerce.Controllers
{
    [Authorize(Roles = "Admin")]
    public class ProductsController : Controller
    {
        private readonly ProyectoEcommerceContext _context;
        private readonly IWebHostEnvironment _env;

        // Inyectamos IWebHostEnvironment además del contexto
        public ProductsController(ProyectoEcommerceContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        // ========= CATÁLOGO PÚBLICO =========
    

        [AllowAnonymous]
        public async Task<IActionResult> Public(string searchTerm = null, int? categoryId = null)
        {
            var query = _context.Products
                .Include(p => p.Category)
                .Include(p => p.Promotion) // <-- incluir promoción
                .Where(p => p.Available && p.Stock > 0)
                .AsQueryable();

            // Filtro por término de búsqueda
            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                searchTerm = searchTerm.ToLower();
                query = query.Where(p =>
                    p.Name.ToLower().Contains(searchTerm) ||
                    p.Description.ToLower().Contains(searchTerm));
            }

            // Filtro por categoría
            if (categoryId.HasValue)
                query = query.Where(p => p.CategoryId == categoryId);

            // Catálogo ordenado alfabéticamente
            var data = await query.OrderBy(p => p.Name).ToListAsync();

            // Categorías para el filtro en la vista
            ViewBag.Categories = await _context.Categories
                .OrderBy(c => c.Name)
                .Select(c => new SelectListItem
                {
                    Value = c.CategoryId.ToString(),
                    Text = c.Name
                })
                .ToListAsync();

            return View(data); // La vista recibe IEnumerable<Product> con el catálogo completo
        }




        // Acción que devuelve los nombres de las imágenes de wwwroot/images/products
        [HttpGet]
        public IActionResult ListProductImages()
        {
            var imagesDir = Path.Combine(_env.WebRootPath ?? "wwwroot", "images", "products");

            if (!Directory.Exists(imagesDir))
            {
                return Json(new { success = false, message = "La carpeta images/products no existe.", images = new string[0] });
            }

            var files = Directory.GetFiles(imagesDir)
                                 .Select(Path.GetFileName)
                                 .OrderBy(n => n)
                                 .ToArray();

            return Json(new { success = true, images = files });
        }

        // ========= VISTAS PÚBLICAS =========
        [AllowAnonymous]
        public async Task<IActionResult> DetailsPublic(int? id)
        {
            if (id == null) return NotFound();

            var product = await _context.Products
                .Include(p => p.Category)
                .Include(p => p.Promotion) // <-- incluir promoción
                .FirstOrDefaultAsync(m => m.Id == id);

            if (product == null || (!product.Available && !User.IsInRole("Admin")))
                return NotFound();

            // ===== Incremento seguro del contador de vistas =====
            // Evita contar repetidas visitas desde el mismo navegador durante 24h
            
            try
            {
                var cookieName = $"viewed_{product.Id}";
                if (!Request.Cookies.ContainsKey(cookieName))
                {
                    // Incremento atómico en la BD
                    await _context.Database.ExecuteSqlInterpolatedAsync($"UPDATE Products SET ViewCount = ViewCount + 1 WHERE Id = {product.Id}");

                    // Actualizar modelo para mostrar el valor incrementado
                    product.ViewCount++;

                    var opts = new CookieOptions
                    {
                        Expires = DateTimeOffset.UtcNow.AddMinutes(2),
                        //Expires = DateTimeOffset.UtcNow.AddHours(2),
                       // Expires = DateTimeOffset.UtcNow.AddDays(2),
                        HttpOnly = true,
                        IsEssential = true,
                        SameSite = SameSiteMode.Lax
                    };
                    Response.Cookies.Append(cookieName, "1", opts);

                }
            }
            catch
            {
                // Si falla el conteo no interrumpimos la vista 
            }

            return View(product);
        }




        [AllowAnonymous]
        [HttpGet]
        public async Task<IActionResult> GetRelatedProducts(int categoryId, int currentProductId)
        {
            var relatedProducts = await _context.Products
                .Where(p => p.CategoryId == categoryId &&
                           p.Id != currentProductId &&
                           p.Available &&
                           p.Stock > 0)
                .OrderBy(p => p.Name)
                .Take(4)
                .Select(p => new
                {
                    id = p.Id,
                    name = p.Name,
                    price = p.Price,
                    imageUrl = p.ImageUrl
                })
                .ToListAsync();

            return Json(relatedProducts);
        }

        // ========= VISTA ADMIN DETAILS  =========
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
        public async Task<IActionResult> Index(int? categoryId, string searchTerm)
        {
            var query = _context.Products.Include(p => p.Category).AsQueryable();

            if (categoryId.HasValue && categoryId > 0)
            {
                query = query.Where(p => p.CategoryId == categoryId.Value);
                ViewBag.CategoryId = categoryId.Value;

                var category = await _context.Categories
                    .FirstOrDefaultAsync(c => c.CategoryId == categoryId.Value);
                ViewBag.CategoryName = category?.Name;
            }

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                searchTerm = searchTerm.ToLower();
                query = query.Where(p =>
                    p.Name.ToLower().Contains(searchTerm) ||
                    p.Description.ToLower().Contains(searchTerm));
                ViewBag.CurrentSearch = searchTerm;
            }

            return View(await query.OrderBy(p => p.Name).ToListAsync());//Ordena alfabeticamente al crear el producto
        }


        // GET: Products/Create
        public IActionResult Create()
        {
            ViewData["CategoryId"] = new SelectList(_context.Categories, "CategoryId", "Name");
            // Lista de promociones (solo activas o todas según lo quieras)
            ViewData["PromotionId"] = new SelectList(_context.Promotions
                .OrderByDescending(p => p.StartDate)
                .Select(p => new { p.PromotionId, p.Name }), "PromotionId", "Name");
            return View();
        }

        // POST: Products/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,Name,Description,Price,Available,ImageUrl,Stock,CategoryId,IsFeatured,PromotionId")] Product product)
        {
            if (!ModelState.IsValid)
            {
                ViewData["CategoryId"] = new SelectList(_context.Categories, "CategoryId", "Name", product.CategoryId);
                ViewData["PromotionId"] = new SelectList(_context.Promotions.OrderByDescending(p => p.StartDate).Select(p => new { p.PromotionId, p.Name }), "PromotionId", "Name", product.PromotionId);
                return View(product);
            }

            product.CreatedAt = DateTime.UtcNow;
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
            ViewData["PromotionId"] = new SelectList(_context.Promotions.OrderByDescending(p => p.StartDate).Select(p => new { p.PromotionId, p.Name }), "PromotionId", "Name", product.PromotionId);
            return View(product);
        }

        // POST: Products/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Name,Description,Price,Available,ImageUrl,Stock,CategoryId,IsFeatured,PromotionId")] Product product)
        {
            if (id != product.Id) return NotFound();

            if (!ModelState.IsValid)
            {
                ViewData["CategoryId"] = new SelectList(_context.Categories, "CategoryId", "Name", product.CategoryId);
                ViewData["PromotionId"] = new SelectList(_context.Promotions.OrderByDescending(p => p.StartDate).Select(p => new { p.PromotionId, p.Name }), "PromotionId", "Name", product.PromotionId);
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


        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var product = await _context.Products.FindAsync(id);
            if (product == null)
            {
                return NotFound();
            }

            try
            {
                _context.Products.Remove(product);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"El producto '{product.Name}' fue eliminado correctamente.";
            }
            catch (DbUpdateException ex)
            {
                // Si la excepción es por la FK de ShoppingCartItems o BuyItems
                if (ex.InnerException is SqlException sqlEx &&
                    (sqlEx.Message.Contains("FK_ShoppingCartItems_Products_ProductId") ||
                     sqlEx.Message.Contains("FK_BuyItems_Products_ProductId")))
                {
                    TempData["ErrorMessage"] = $"El producto '{product.Name}' no se puede eliminar porque está registrado en carritos o compras.";
                }
                else
                {
                    TempData["ErrorMessage"] = $"Ocurrió un error al intentar eliminar el producto '{product.Name}'.";
                }
            }

            return RedirectToAction(nameof(Index));
        }


        private bool ProductExists(int id) =>
            _context.Products.Any(e => e.Id == id);
    }
}