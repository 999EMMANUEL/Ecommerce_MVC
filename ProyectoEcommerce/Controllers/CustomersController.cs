
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProyectoEcommerce.Data;
using ProyectoEcommerce.Models;
using System.Security.Claims;

namespace ProyectoEcommerce.Controllers
{
    [Authorize] // requiere login por defecto
    public class CustomersController : Controller
    {
        private readonly ProyectoEcommerceContext _context;

        public CustomersController(ProyectoEcommerceContext context)
        {
            _context = context;
        }

        // ===== ADMIN: listado global =====
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Index()
        {
            return View(await _context.Customers.OrderBy(c => c.Name_full).ToListAsync());
        }

        // ===== Perfil del usuario autenticado =====
        [Authorize]
        public async Task<IActionResult> Perfil()
        {
            var email = User?.Identity?.Name;
            if (string.IsNullOrWhiteSpace(email)) return Challenge();

            var me = await _context.Customers.FirstOrDefaultAsync(c => c.Email == email);

            // Si no existe, lo creo 
             if (me == null)
    {
        var given   = User.FindFirstValue(ClaimTypes.GivenName); // nombre (si existe)
        var surname = User.FindFirstValue(ClaimTypes.Surname);   // apellido (si existe)
        var full = string.Join(" ", new[] { given, surname }
                                .Where(s => !string.IsNullOrWhiteSpace(s)));

        me = new Customer
        {
            Email = email,
            Name_full = string.IsNullOrWhiteSpace(full) ? null : full
        };
        _context.Customers.Add(me);
        await _context.SaveChangesAsync();
    }

            return View(me); // Views/Customers/Perfil.cshtml
        }



        // GET
        // GET: EditarPerfil
        public async Task<IActionResult> EditarPerfil()
        {
            var email = User?.Identity?.Name;
            if (string.IsNullOrWhiteSpace(email))
                return Challenge();

            var me = await _context.Customers.FirstOrDefaultAsync(c => c.Email == email);
            if (me == null)
                return NotFound();

            // Convertir a ViewModel
            var vm = new EditarPerfilVM
            {
                CustomerId = me.CustomerId,
                Name_full = me.Name_full,
                Telefono = me.Telefono,
                Direccion = me.Direccion
            };

            return View(vm);
        }

        // POST: EditarPerfil
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditarPerfil(EditarPerfilVM vm)
        {
            var email = User?.Identity?.Name;
            if (string.IsNullOrWhiteSpace(email))
                return Challenge();

            if (!ModelState.IsValid)
                return View(vm);

            
            var me = await _context.Customers.FirstOrDefaultAsync(c => c.Email == email);
            if (me == null)
                return NotFound();

            // Actualizar campos
            me.Name_full = vm.Name_full;
            me.Telefono = vm.Telefono;
            me.Direccion = vm.Direccion;

            await _context.SaveChangesAsync();

            TempData["ok"] = "Perfil actualizado correctamente.";
            return RedirectToAction(nameof(Perfil));
        }

        

        // ===== Details con control de acceso =====
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var customer = await _context.Customers.FirstOrDefaultAsync(m => m.CustomerId == id);
            if (customer == null) return NotFound();

            if (!User.IsInRole("Admin"))
            {
                var email = User?.Identity?.Name;
                if (string.IsNullOrWhiteSpace(email) ||
                    !string.Equals(customer.Email, email, StringComparison.OrdinalIgnoreCase))
                {
                    return Forbid();
                }
            }

            return View(customer);
        }

        // ===== CRUD Admin =====
        [Authorize(Roles = "Admin")]
        public IActionResult Create() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create([Bind("CustomerId,Name_full,Email,Telefono,Direccion")] Customer customer)
        {
            if (!ModelState.IsValid) return View(customer);

            _context.Add(customer);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var customer = await _context.Customers.FindAsync(id);
            if (customer == null) return NotFound();

            return View(customer);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int id, [Bind("CustomerId,Name_full,Email,Telefono,Direccion")] Customer customer)
        {
            if (id != customer.CustomerId) return NotFound();
            if (!ModelState.IsValid) return View(customer);

            try
            {
                _context.Update(customer);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!CustomerExists(customer.CustomerId)) return NotFound();
                throw;
            }
            return RedirectToAction(nameof(Index));
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var customer = await _context.Customers.FirstOrDefaultAsync(m => m.CustomerId == id);
            if (customer == null) return NotFound();

            return View(customer);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var customer = await _context.Customers.FindAsync(id);
            if (customer != null) _context.Customers.Remove(customer);

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool CustomerExists(int id) => _context.Customers.Any(e => e.CustomerId == id);
    }
}
