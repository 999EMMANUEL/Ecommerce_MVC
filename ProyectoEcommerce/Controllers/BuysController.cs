
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using ProyectoEcommerce.Data;
using ProyectoEcommerce.Models;

namespace ProyectoEcommerce.Controllers
{
    [Authorize] // ← requiere login por defecto
    public class BuysController : Controller
    {
        private readonly ProyectoEcommerceContext _context;

        public BuysController(ProyectoEcommerceContext context)
        {
            _context = context;
        }

        // ========= ADMIN: listado global =========
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Index()
        {
            var list = _context.Buys
                .Include(b => b.Customer)
                .Include(b => b.Employee)
                .OrderByDescending(b => b.Fecha);

            return View(await list.ToListAsync());
        }

        // ========= USUARIO: Mis compras (por email) =========
        // Asumimos que Customer.Email coincide con el correo con el que el usuario inició sesión (Identity)
        public async Task<IActionResult> My()
        {
            var email = User?.Identity?.Name; // Identity usa normalmente el email como Name
            if (string.IsNullOrWhiteSpace(email))
                return Challenge(); // fuerza login

            var list = _context.Buys
                .Include(b => b.Customer)
                .Include(b => b.Employee)
                .Where(b => b.Customer.Email == email)
                .OrderByDescending(b => b.Fecha);

            return View(await list.ToListAsync()); // Views/Buys/My.cshtml
        }

        // ========= Details: Admin ve todo; usuario solo su compra =========
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var buy = await _context.Buys
                .Include(b => b.Customer)
                .Include(b => b.Employee)
                .FirstOrDefaultAsync(m => m.BuyId == id);

            if (buy == null) return NotFound();

            // Si NO es Admin, solo puede ver si la compra le pertenece
            if (!User.IsInRole("Admin"))
            {
                var email = User?.Identity?.Name;
                if (string.IsNullOrWhiteSpace(email) || !string.Equals(buy.Customer?.Email, email, System.StringComparison.OrdinalIgnoreCase))
                    return Forbid(); // 403
            }

            return View(buy);
        }

        // ========= ADMIN: Create/Edit/Delete =========
        [Authorize(Roles = "Admin")]
        public IActionResult Create()
        {
            ViewData["CustomerId"] = new SelectList(_context.Customers, "CustomerId", "Email");
            ViewData["EmployeeId"] = new SelectList(_context.Employees, "EmployeeId", "Name");
            return View();
        }

        [HttpPost, ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create([Bind("BuyId,CustomerId,EmployeeId,Fecha,Subtotal,IVA,Total")] Buy buy)
        {
            if (!ModelState.IsValid)
            {
                ViewData["CustomerId"] = new SelectList(_context.Customers, "CustomerId", "Email", buy.CustomerId);
                ViewData["EmployeeId"] = new SelectList(_context.Employees, "EmployeeId", "Name", buy.EmployeeId);
                return View(buy);
            }

            _context.Add(buy);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var buy = await _context.Buys.FindAsync(id);
            if (buy == null) return NotFound();

            ViewData["CustomerId"] = new SelectList(_context.Customers, "CustomerId", "Email", buy.CustomerId);
            ViewData["EmployeeId"] = new SelectList(_context.Employees, "EmployeeId", "Name", buy.EmployeeId);
            return View(buy);
        }

        [HttpPost, ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int id, [Bind("BuyId,CustomerId,EmployeeId,Fecha,Subtotal,IVA,Total")] Buy buy)
        {
            if (id != buy.BuyId) return NotFound();
            if (!ModelState.IsValid)
            {
                ViewData["CustomerId"] = new SelectList(_context.Customers, "CustomerId", "Email", buy.CustomerId);
                ViewData["EmployeeId"] = new SelectList(_context.Employees, "EmployeeId", "Name", buy.EmployeeId);
                return View(buy);
            }

            try
            {
                _context.Update(buy);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!BuyExists(buy.BuyId)) return NotFound();
                throw;
            }
            return RedirectToAction(nameof(Index));
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var buy = await _context.Buys
                .Include(b => b.Customer)
                .Include(b => b.Employee)
                .FirstOrDefaultAsync(m => m.BuyId == id);
            if (buy == null) return NotFound();

            return View(buy);
        }

        [HttpPost, ActionName("Delete"), ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var buy = await _context.Buys.FindAsync(id);
            if (buy != null) _context.Buys.Remove(buy);

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool BuyExists(int id) => _context.Buys.Any(e => e.BuyId == id);
    }
}

