using Microsoft.EntityFrameworkCore;
using ProyectoEcommerce.Models;

namespace ProyectoEcommerce.Services
{
    public interface ICartService
    {
        Task<ShoppingCart?> GetCartByEmailAsync(string email);
        Task AddToCartAsync(string email, int productId, int quantity);
        Task UpdateQuantityAsync(int cartId, int productId, int quantity);
        Task RemoveItemAsync(int cartId, int productId);
        Task<Buy> CreateBuyFromCartAsync(string email, decimal ivaRate = 0.13m);
        

     

    }
}