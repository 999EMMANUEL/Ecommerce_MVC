using Microsoft.EntityFrameworkCore;
using ProyectoEcommerce.Data;
using ProyectoEcommerce.Models;

namespace ProyectoEcommerce.Services
{
    public class CartService : ICartService
    {
        private readonly ProyectoEcommerceContext _context;
        public CartService(ProyectoEcommerceContext context) => _context = context;



        public async Task<ShoppingCart?> GetCartByEmailAsync(string email)
        {
            if (string.IsNullOrWhiteSpace(email)) return null;
            var customer = await _context.Customers.FirstOrDefaultAsync(c => c.Email == email);
            if (customer == null) return null;

            var cart = await _context.ShoppingCarts
                .Include(s => s.Items).ThenInclude(i => i.Product)
                .FirstOrDefaultAsync(s => s.CustomerId == customer.CustomerId);

            return cart;
        }

        public async Task AddToCartAsync(string email, int productId, int quantity)
        {
            if (quantity <= 0) quantity = 1;
            var customer = await _context.Customers.FirstOrDefaultAsync(c => c.Email == email);
            if (customer == null) throw new InvalidOperationException("Customer no encontrado.");

            var cart = await _context.ShoppingCarts
                .Include(s => s.Items)
                .FirstOrDefaultAsync(s => s.CustomerId == customer.CustomerId);

            if (cart == null)
            {
                cart = new ShoppingCart { CustomerId = customer.CustomerId, CreatedDate = DateTime.UtcNow };
                _context.ShoppingCarts.Add(cart);
                await _context.SaveChangesAsync();
            }

            var product = await _context.Products.FindAsync(productId);
            if (product == null) throw new InvalidOperationException("Producto no encontrado.");

            var item = await _context.ShoppingCartItems
                .FirstOrDefaultAsync(i => i.ShoppingCartId == cart.ShoppingCartId && i.ProductId == productId);

            if (item != null)
            {
                item.Quantity += quantity;
                _context.ShoppingCartItems.Update(item);
            }
            else
            {
                _context.ShoppingCartItems.Add(new ShoppingCartItem
                {
                    ShoppingCartId = cart.ShoppingCartId,
                    ProductId = productId,
                    Quantity = quantity,
                    AddedDate = DateTime.UtcNow
                });
            }

            await _context.SaveChangesAsync();
        }

        public async Task UpdateQuantityAsync(int cartId, int productId, int quantity)
        {
            var item = await _context.ShoppingCartItems
                .FirstOrDefaultAsync(i => i.ShoppingCartId == cartId && i.ProductId == productId);
            if (item == null) return;

            if (quantity <= 0)
                _context.ShoppingCartItems.Remove(item);
            else
            {
                item.Quantity = quantity;
                _context.ShoppingCartItems.Update(item);
            }
            await _context.SaveChangesAsync();
        }

        public async Task RemoveItemAsync(int cartId, int productId)
        {
            var item = await _context.ShoppingCartItems
                .FirstOrDefaultAsync(i => i.ShoppingCartId == cartId && i.ProductId == productId);
            if (item != null)
            {
                _context.ShoppingCartItems.Remove(item);
                await _context.SaveChangesAsync();
            }
        }

        public async Task<Buy> CreateBuyFromCartAsync(string email, decimal ivaRate = 0.13m)
        {
            if (string.IsNullOrWhiteSpace(email)) throw new InvalidOperationException("Usuario no autenticado.");

            var customer = await _context.Customers.FirstOrDefaultAsync(c => c.Email == email)
                         ?? throw new InvalidOperationException("Customer no encontrado.");

            // Usamos la execution strategy para ejecutar todo el flujo transaccional como unidad reintentable
            var strategy = _context.Database.CreateExecutionStrategy();

            return await strategy.ExecuteAsync(async () =>
            {
                // Volver a cargar el carrito dentro del delegado (evita usar entidades previamente rastreadas si el delegado se reintenta)
                var cart = await _context.ShoppingCarts
                    .Include(s => s.Items).ThenInclude(i => i.Product)
                    .FirstOrDefaultAsync(s => s.CustomerId == customer.CustomerId);

                if (cart == null || !cart.Items.Any()) throw new InvalidOperationException("Carrito vacío.");

                // Iniciar transacción manual dentro del delegado
                await using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    decimal subtotal = 0;
                    decimal iva = 0;
                    decimal total = 0;

                    foreach (var ci in cart.Items)
                    {
                        // Insertar cuando se calcule el precio unitario para la compra
                        var product = await _context.Products.Include(p => p.Promotion).FirstOrDefaultAsync(p => p.Id == ci.ProductId);
                        var unitPrice = product.CalculateEffectivePrice(DateTime.UtcNow);
                        subtotal += unitPrice * ci.Quantity;
                    }

                    iva = Math.Round(subtotal * ivaRate, 2);
                    total = subtotal + iva;

                    var buy = new Buy
                    {
                        CustomerId = customer.CustomerId,
                        EmployeeId = _context.Employees.Select(e => (int?)e.EmployeeId).FirstOrDefault(),
                        Fecha = DateTime.UtcNow,
                        Subtotal = subtotal,
                        IVA = iva,
                        Total = total,
                        Paid = true
                    };

                    _context.Buys.Add(buy);
                    await _context.SaveChangesAsync(); // genera BuyId

                    foreach (var ci in cart.Items)
                    {
                        // Crear BuyItem
                        var product = await _context.Products.Include(p => p.Promotion).FirstOrDefaultAsync(p => p.Id == ci.ProductId);
                        var unitPrice = product.CalculateEffectivePrice(DateTime.UtcNow);
                        var subtotalItem = unitPrice * ci.Quantity;

                        _context.BuyItems.Add(new BuyItem
                        {
                            BuyId = buy.BuyId,
                            ProductId = ci.ProductId,
                            Quantity = ci.Quantity,
                            UnitPrice = unitPrice,
                            Subtotal = subtotalItem
                        });

                        // Reducir stock de forma segura (volver a cargar producto por si acaso)
                        var prod = await _context.Products.FirstOrDefaultAsync(p => p.Id == ci.ProductId);
                        if (prod != null)
                        {
                            prod.Stock = Math.Max(0, prod.Stock - ci.Quantity);
                            _context.Products.Update(prod);
                        }
                    }

                    // Eliminar carrito (cascade eliminará ShoppingCartItems)
                    _context.ShoppingCarts.Remove(cart);

                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    return buy;
                }
                catch
                {
                    await transaction.RollbackAsync();
                    throw;
                }
            });
        }
    }
}