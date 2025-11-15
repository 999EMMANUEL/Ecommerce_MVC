using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProyectoEcommerce.Data;
using ProyectoEcommerce.Models;

namespace ProyectoEcommerce.Controllers
{
    // Todo el CRUD solo para Administradores
    [Authorize(Roles = "Admin")]
    public class FaqsController : Controller
    {
        private readonly ProyectoEcommerceContext _context;

        public FaqsController(ProyectoEcommerceContext context) => _context = context;
        ///{
         //   _context = context;
        //}

        // ===================== CRUD (ADMIN) =====================

        // GET: Faqs
        public async Task<IActionResult> Index()
        {
            return View(await _context.Faqs.ToListAsync());
        }

        // GET: Faqs/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var faq = await _context.Faqs.FirstOrDefaultAsync(m => m.Id == id);
            if (faq == null) return NotFound();

            return View(faq);
        }

        // GET: Faqs/Create
        public IActionResult Create() => View();

        // POST: Faqs/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,Category,Question,Answer,SortOrder,IsActive")] Faq faq)
        {
            if (!ModelState.IsValid) return View(faq);

            _context.Add(faq);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        // GET: Faqs/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var faq = await _context.Faqs.FindAsync(id);
            if (faq == null) return NotFound();

            return View(faq);
        }

        // POST: Faqs/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Category,Question,Answer,SortOrder,IsActive")] Faq faq)
        {
            if (id != faq.Id) return NotFound();
            if (!ModelState.IsValid) return View(faq);

            try
            {
                _context.Update(faq);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!FaqExists(faq.Id)) return NotFound();
                else throw;
            }
            return RedirectToAction(nameof(Index));
        }

        // GET: Faqs/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var faq = await _context.Faqs.FirstOrDefaultAsync(m => m.Id == id);
            if (faq == null) return NotFound();

            return View(faq);
        }

        // POST: Faqs/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var faq = await _context.Faqs.FindAsync(id);
            if (faq != null) _context.Faqs.Remove(faq);

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }


       

        private bool FaqExists(int id) => _context.Faqs.Any(e => e.Id == id);

        // ===================== PÚBLICA (CLIENTES) =====================

        // GET: /Faqs/Public  (visible para todos)
        [AllowAnonymous]
        [HttpGet]
        public async Task<IActionResult> Public(string q, string cat)
        {
            var faqs = _context.Faqs.Where(f => f.IsActive);

            if (!string.IsNullOrWhiteSpace(cat))
                faqs = faqs.Where(f => f.Category == cat);

            if (!string.IsNullOrWhiteSpace(q))
                faqs = faqs.Where(f => f.Question.Contains(q) || f.Answer.Contains(q));

            var data = await faqs
                .OrderBy(f => f.Category).ThenBy(f => f.SortOrder).ThenBy(f => f.Id)
                .ToListAsync();

            return View(data); // Views/Faqs/Public.cshtml
        }
    }
}
