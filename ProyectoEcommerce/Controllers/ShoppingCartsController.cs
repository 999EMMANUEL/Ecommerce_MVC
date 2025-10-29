using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProyectoEcommerce.Data;
using ProyectoEcommerce.Models;

namespace ProyectoEcommerce.Controllers
{
    [Authorize] // requiere login por defecto
    public class ShoppingCartsController : Controller
    {
        private readonly ProyectoEcommerceContext _context;

        public ShoppingCartsController(ProyectoEcommerceContext context)
        {
            _context = context;
        }

        // ========= ADMIN: listado global =========
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Index()
        {
            var query = _context.ShoppingCarts
                .Include(s => s.Customer)
                .OrderByDescending(s => s.CreatedDate);
            return View(await query.ToListAsync());
        }

        // ========= Mi carrito (usuario autenticado) =========
        public async Task<IActionResult> My()
        {
            var email = User?.Identity?.Name;
            if (string.IsNullOrWhiteSpace(email)) return Challenge();

            var customer = await _context.Customers.FirstOrDefaultAsync(c => c.Email == email);
            if (customer == null)
            {
                // No hay Customer para este usuario: decide qué hacer (perfil, etc.)
                // Redirigimos a Customers/My para que complete su perfil (ajústalo si quieres otro flujo)
                return RedirectToAction("My", "Customers");
            }

            var cart = await _context.ShoppingCarts
                .Include(s => s.Customer)
                // .Include(s => s.Items).ThenInclude(i => i.Product)   // ← descomenta si tienes Items
                .FirstOrDefaultAsync(s => s.CustomerId == customer.CustomerId);

            if (cart == null)
            {
                cart = new ShoppingCart
                {
                    CustomerId = customer.CustomerId,
                    CreatedDate = DateTime.UtcNow
                };
                _context.ShoppingCarts.Add(cart);
                await _context.SaveChangesAsync();
            }

            return View(cart); // Views/ShoppingCarts/My.cshtml
        }

        // ========= Details: Admin ve todo; dueño puede ver el suyo =========
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var cart = await _context.ShoppingCarts
                .Include(s => s.Customer)
                // .Include(s => s.Items).ThenInclude(i => i.Product)   // ← si tienes Items
                .FirstOrDefaultAsync(m => m.ShoppingCartId == id);
            if (cart == null) return NotFound();

            if (!User.IsInRole("Admin"))
            {
                var email = User?.Identity?.Name;
                if (string.IsNullOrWhiteSpace(email) ||
                    !string.Equals(cart.Customer?.Email, email, StringComparison.OrdinalIgnoreCase))
                {
                    return Forbid();
                }
            }

            return View(cart);
        }

        // ========= CRUD ADMIN =========
        [Authorize(Roles = "Admin")]
        public IActionResult Create()
        {
            ViewData["CustomerId"] = new Microsoft.AspNetCore.Mvc.Rendering.SelectList(_context.Customers, "CustomerId", "Email");
            return View();
        }

        [HttpPost, ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create([Bind("ShoppingCartId,CreatedDate,CustomerId")] ShoppingCart shoppingCart)
        {
            if (!ModelState.IsValid)
            {
                ViewData["CustomerId"] = new Microsoft.AspNetCore.Mvc.Rendering.SelectList(_context.Customers, "CustomerId", "Email", shoppingCart.CustomerId);
                return View(shoppingCart);
            }

            _context.Add(shoppingCart);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var shoppingCart = await _context.ShoppingCarts.FindAsync(id);
            if (shoppingCart == null) return NotFound();

            ViewData["CustomerId"] = new Microsoft.AspNetCore.Mvc.Rendering.SelectList(_context.Customers, "CustomerId", "Email", shoppingCart.CustomerId);
            return View(shoppingCart);
        }

        [HttpPost, ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int id, [Bind("ShoppingCartId,CreatedDate,CustomerId")] ShoppingCart shoppingCart)
        {
            if (id != shoppingCart.ShoppingCartId) return NotFound();
            if (!ModelState.IsValid)
            {
                ViewData["CustomerId"] = new Microsoft.AspNetCore.Mvc.Rendering.SelectList(_context.Customers, "CustomerId", "Email", shoppingCart.CustomerId);
                return View(shoppingCart);
            }

            try
            {
                _context.Update(shoppingCart);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!ShoppingCartExists(shoppingCart.ShoppingCartId)) return NotFound();
                throw;
            }
            return RedirectToAction(nameof(Index));
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var shoppingCart = await _context.ShoppingCarts
                .Include(s => s.Customer)
                .FirstOrDefaultAsync(m => m.ShoppingCartId == id);
            if (shoppingCart == null) return NotFound();

            return View(shoppingCart);
        }

        [HttpPost, ActionName("Delete"), ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var shoppingCart = await _context.ShoppingCarts.FindAsync(id);
            if (shoppingCart != null)
            {
                _context.ShoppingCarts.Remove(shoppingCart);
            }
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool ShoppingCartExists(int id)
            => _context.ShoppingCarts.Any(e => e.ShoppingCartId == id);
    }
}
