using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using ProyectoEcommerce.Services;
using ProyectoEcommerce.Models;

namespace ProyectoEcommerce.ViewComponents
{
    [ViewComponent(Name = "CartTodo")]
    public class CartTodoViewComponent : ViewComponent
    {
        private readonly ICartService _cartService;
        public CartTodoViewComponent(ICartService cartService) => _cartService = cartService;

        public async Task<IViewComponentResult> InvokeAsync()
        {
            var email = HttpContext?.User?.Identity?.Name;
            ShoppingCart? cart = null;

            if (!string.IsNullOrWhiteSpace(email))
                cart = await _cartService.GetCartByEmailAsync(email);

            
            return View("Default", cart);
        }
    }
}

