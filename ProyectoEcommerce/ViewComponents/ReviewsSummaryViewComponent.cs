using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProyectoEcommerce.Data;
using System.Linq;
using System.Threading.Tasks;

namespace ProyectoEcommerce.ViewComponents
{
    public class ReviewsSummaryViewComponent : ViewComponent
    {
        private readonly ProyectoEcommerceContext _context;

        public ReviewsSummaryViewComponent(ProyectoEcommerceContext context)
        {
            _context = context;
        }

        public async Task<IViewComponentResult> InvokeAsync(int take = 5)
        {
            var reviews = await _context.Reviews
                .Include(r => r.Product)
                .OrderByDescending(r => r.CreatedAt)
                .Take(take)
                .ToListAsync();

            return View(reviews);
        }
    }
}
