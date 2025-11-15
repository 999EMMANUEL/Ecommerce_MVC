// Añadir inyección
private readonly ICartService _cartService;
public ShoppingCartsController(ProyectoEcommerceContext context, ICartService cartService)
{
    _context = context;
    _cartService = cartService;
}

// Acción AddToCart (ahora delegada)
[HttpPost, ValidateAntiForgeryToken]
public async Task<IActionResult> AddToCart(int productId, int quantity = 1, string returnUrl = null)
{
    var email = User?.Identity?.Name;
    if (string.IsNullOrWhiteSpace(email)) return Challenge();

    await _cartService.AddToCartAsync(email, productId, quantity);
    if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl)) return Redirect(returnUrl);
    return RedirectToAction("My");
}

// Acción Pay simplificada delegando al servicio
[HttpPost, ValidateAntiForgeryToken]
public async Task<IActionResult> Pay()
{
    var email = User?.Identity?.Name;
    if (string.IsNullOrWhiteSpace(email)) return Challenge();

    try
    {
        var buy = await _cartService.CreateBuyFromCartAsync(email, ivaRate: 0.13m);
        return RedirectToAction("Details", "Buys", new { id = buy.BuyId });
    }
    catch (InvalidOperationException ex)
    {
        TempData["Error"] = ex.Message;
        return RedirectToAction("My");
    }
}

// Update quantity
[HttpPost, ValidateAntiForgeryToken]
public async Task<IActionResult> UpdateQuantity(int cartId, int productId, int quantity)
{
    await _cartService.UpdateQuantityAsync(cartId, productId, quantity);
    return RedirectToAction("My");
}

// Remove item
[HttpPost, ValidateAntiForgeryToken]
public async Task<IActionResult> RemoveItem(int cartId, int productId)
{
    await _cartService.RemoveItemAsync(cartId, productId);
    return RedirectToAction("My");
}