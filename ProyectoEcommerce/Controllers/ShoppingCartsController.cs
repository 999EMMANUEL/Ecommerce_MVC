using System;
using System.Linq;
using System.Threading.Tasks;
using System.Text.Json;
using System.Collections.Generic;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ProyectoEcommerce.Data;
using ProyectoEcommerce.Models;
using ProyectoEcommerce.Services;
using Microsoft.AspNetCore.Http;

namespace ProyectoEcommerce.Controllers
{
    [Authorize]
    public class ShoppingCartsController : Controller
    {
        private readonly ProyectoEcommerceContext _context;
        private readonly ICartService _cartService;
        private readonly ILogger<ShoppingCartsController> _logger;
        private const decimal IVA_RATE = 0.13m;

        public ShoppingCartsController(ProyectoEcommerceContext context, ICartService cartService, ILogger<ShoppingCartsController> logger)
        {
            _context = context;
            _cartService = cartService;
            _logger = logger;
        }

        // ========= MI CARRITO =========
        public async Task<IActionResult> My()
        {
            var email = User?.Identity?.Name;
            if (string.IsNullOrWhiteSpace(email)) return Challenge();

            var cart = await _cartService.GetCartByEmailAsync(email);
            if (cart == null) return View(null);

            // CUPONES VISIBLES PARA EL CLIENTE
            ViewBag.ActiveCoupons = await _context.Coupons
                .Where(c => c.IsActive && DateTime.Today >= c.ValidFrom && DateTime.Today <= c.ValidTo)
                .OrderBy(c => c.ValidTo)
                .ToListAsync();

            // CALCULAR PREVISUALIZACIÓN DEL DESCUENTO
            decimal discount = 0m;
            string couponCode = HttpContext.Session.GetString("CurrentCouponCode");

            if (!string.IsNullOrWhiteSpace(couponCode))
            {
                var coupon = await _context.Coupons.FirstOrDefaultAsync(c => c.Code == couponCode && c.IsActive);

                if (coupon != null)
                {
                    var today = DateTime.Today;
                    if (today >= coupon.ValidFrom.Date && today <= coupon.ValidTo.Date)
                    {
                        var subtotal = cart.Items.Sum(i => i.Quantity * (i.Product?.Price ?? 0m));
                        discount = subtotal * (coupon.DiscountPercent / 100m);
                        if (discount > subtotal) discount = subtotal;

                        ViewBag.CouponCode = coupon.Code;
                    }
                    else
                        HttpContext.Session.Remove("CurrentCouponCode");
                }
                else
                    HttpContext.Session.Remove("CurrentCouponCode");
            }

            ViewBag.CouponDiscount = discount;
            return View(cart);
        }

        // APLICAR CUPÓN
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApplyCoupon(string code)
        {
            var email = User?.Identity?.Name;
            if (string.IsNullOrWhiteSpace(email)) return Challenge();

            if (string.IsNullOrWhiteSpace(code))
            {
                TempData["Error"] = "Debe ingresar un código de cupón.";
                return RedirectToAction("My");
            }

            code = code.Trim().ToUpper();
            var cart = await _cartService.GetCartByEmailAsync(email);

            if (cart == null || !cart.Items.Any())
            {
                TempData["Error"] = "No hay productos en el carrito.";
                return RedirectToAction("My");
            }

            var coupon = await _context.Coupons.FirstOrDefaultAsync(c => c.Code == code && c.IsActive);
            if (coupon == null)
            {
                TempData["Error"] = "Cupón inválido o inactivo.";
                return RedirectToAction("My");
            }

            var today = DateTime.Today;
            if (today < coupon.ValidFrom.Date || today > coupon.ValidTo.Date)
            {
                TempData["Error"] = "El cupón no está vigente.";
                return RedirectToAction("My");
            }

            HttpContext.Session.SetString("CurrentCouponCode", coupon.Code);
            TempData["Success"] = $"Cupón {coupon.Code} aplicado correctamente.";
            return RedirectToAction("My");
        }

        // PAGAR (aplica descuento definitivo)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Pay()
        {
            var email = User?.Identity?.Name;
            if (string.IsNullOrWhiteSpace(email)) return Challenge();

            try
            {
                var buy = await _cartService.CreateBuyFromCartAsync(email, IVA_RATE);

                var couponCode = HttpContext.Session.GetString("CurrentCouponCode");
                if (!string.IsNullOrWhiteSpace(couponCode))
                {
                    var coupon = await _context.Coupons.FirstOrDefaultAsync(c => c.Code == couponCode && c.IsActive);
                    if (coupon != null)
                    {
                        var buyFromDb = await _context.Buys.Include(b => b.Items).FirstOrDefaultAsync(b => b.BuyId == buy.BuyId);
                        var subtotal = buyFromDb.Subtotal;
                        var discount = subtotal * (coupon.DiscountPercent / 100m);
                        if (discount > subtotal) discount = subtotal;

                        buyFromDb.CouponCode = coupon.Code;
                        buyFromDb.DiscountAmount = discount;
                        buyFromDb.Total = Math.Max(0, buyFromDb.Total - discount);

                        await _context.SaveChangesAsync();
                    }

                    HttpContext.Session.Remove("CurrentCouponCode");
                }

                TempData["Success"] = "Pago realizado con éxito.";
                return RedirectToAction("Details", "Buys", new { id = buy.BuyId });
            }
            catch
            {
                TempData["Error"] = "Error al procesar el pago.";
                return RedirectToAction("My");
            }
        }
    }
}
