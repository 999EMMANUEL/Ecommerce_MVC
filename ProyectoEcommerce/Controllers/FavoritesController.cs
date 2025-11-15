using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProyectoEcommerce.Data;
using ProyectoEcommerce.Models;
using System.Text.Json;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using System;

namespace ProyectoEcommerce.Controllers
{
    public class FavoritesController : Controller
    {
        private readonly ProyectoEcommerceContext _context;
        private readonly UserManager<IdentityUser> _userManager;
        private const string AnonCookieName = "AnonId";

        public FavoritesController(ProyectoEcommerceContext context, UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        private string? GetAnonId() => Request.Cookies[AnonCookieName];

        private void EnsureAnonCookie(string anonId)
        {
            if (string.IsNullOrEmpty(Request.Cookies[AnonCookieName]))
            {
                var opts = new CookieOptions
                {
                    Expires = DateTimeOffset.UtcNow.AddYears(2),
                    HttpOnly = true,
                    IsEssential = true,
                    SameSite = SameSiteMode.Lax
                };
                Response.Cookies.Append(AnonCookieName, anonId, opts);
            }
        }

        public async Task<IActionResult> Index()
        {
            try
            {
                if (User.Identity?.IsAuthenticated == true)
                {
                    var userId = _userManager.GetUserId(User);
                    var favoriteProducts = await _context.Favorites
                        .Where(f => f.UserId == userId)
                        .Include(f => f.Product)
                        .Select(f => f.Product!)
                        .ToListAsync();

                    ViewBag.FavoritesCount = favoriteProducts.Count;
                    return View(favoriteProducts);
                }

                // Priorizar favoritos almacenados por cookie (persistente)
                var anonId = GetAnonId();
                if (!string.IsNullOrEmpty(anonId))
                {
                    var favoriteProducts = await _context.Favorites
                        .Where(f => f.AnonymousId == anonId)
                        .Include(f => f.Product)
                        .Select(f => f.Product!)
                        .ToListAsync();

                    ViewBag.FavoritesCount = favoriteProducts.Count;
                    return View(favoriteProducts);
                }

                // Fallback: compatibilidad con sesión
                var favoritesJson = HttpContext.Session.GetString("Favorites") ?? "[]";
                var favoriteIds = JsonSerializer.Deserialize<List<int>>(favoritesJson) ?? new List<int>();

                if (!favoriteIds.Any())
                {
                    ViewBag.FavoritesCount = 0;
                    return View(new List<Product>());
                }

                var favoriteProductsFromSession = await _context.Products
                    .Where(p => favoriteIds.Contains(p.Id))
                    .ToListAsync();

                ViewBag.FavoritesCount = favoriteProductsFromSession.Count;
                return View(favoriteProductsFromSession);
            }
            catch (Exception ex)
            {
                ViewBag.Error = "Error al cargar los favoritos: " + ex.Message;
                return View(new List<Product>());
            }
        }

        [HttpPost]
        public async Task<IActionResult> AddToFavorites([FromBody] int productId)
        {
            try
            {
                if (User.Identity?.IsAuthenticated == true)
                {
                    var userId = _userManager.GetUserId(User);
                    var exists = await _context.Favorites.AnyAsync(f => f.UserId == userId && f.ProductId == productId);
                    if (!exists)
                    {
                        _context.Favorites.Add(new Favorite { UserId = userId, ProductId = productId, CreatedAt = DateTime.UtcNow });
                        await _context.SaveChangesAsync();
                    }

                    return Json(new { success = true, message = "Producto agregado a favoritos", isFavorite = true });
                }
                else
                {
                    // Persistir usando cookie anonId -> BD
                    var anonId = GetAnonId();
                    if (string.IsNullOrEmpty(anonId))
                    {
                        anonId = Guid.NewGuid().ToString();
                        EnsureAnonCookie(anonId);
                    }

                    var exists = await _context.Favorites.AnyAsync(f => f.AnonymousId == anonId && f.ProductId == productId);
                    if (!exists)
                    {
                        _context.Favorites.Add(new Favorite { AnonymousId = anonId, ProductId = productId, CreatedAt = DateTime.UtcNow });
                        await _context.SaveChangesAsync();
                    }

                    return Json(new { success = true, message = "Producto agregado a favoritos (anónimo)", isFavorite = true });
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error: " + ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> RemoveFromFavorites([FromBody] int productId)
        {
            try
            {
                if (User.Identity?.IsAuthenticated == true)
                {
                    var userId = _userManager.GetUserId(User);
                    var fav = await _context.Favorites.FirstOrDefaultAsync(f => f.UserId == userId && f.ProductId == productId);
                    if (fav != null)
                    {
                        _context.Favorites.Remove(fav);
                        await _context.SaveChangesAsync();
                    }

                    return Json(new { success = true, message = "Producto removido de favoritos", isFavorite = false });
                }
                else
                {
                    var anonId = GetAnonId();
                    if (string.IsNullOrEmpty(anonId))
                    {
                        // Fallback: sesión
                        var favoritesJson = HttpContext.Session.GetString("Favorites") ?? "[]";
                        var favorites = JsonSerializer.Deserialize<List<int>>(favoritesJson) ?? new List<int>();
                        favorites.Remove(productId);
                        HttpContext.Session.SetString("Favorites", JsonSerializer.Serialize(favorites));
                    }
                    else
                    {
                        var fav = await _context.Favorites.FirstOrDefaultAsync(f => f.AnonymousId == anonId && f.ProductId == productId);
                        if (fav != null)
                        {
                            _context.Favorites.Remove(fav);
                            await _context.SaveChangesAsync();
                        }
                    }

                    return Json(new { success = true, message = "Producto removido de favoritos", isFavorite = false });
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error: " + ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> ToggleFavorite([FromBody] int productId)
        {
            try
            {
                if (User.Identity?.IsAuthenticated == true)
                {
                    var userId = _userManager.GetUserId(User);
                    var fav = await _context.Favorites.FirstOrDefaultAsync(f => f.UserId == userId && f.ProductId == productId);

                    if (fav != null)
                    {
                        _context.Favorites.Remove(fav);
                        await _context.SaveChangesAsync();
                        return Json(new { success = true, isFavorite = false, message = "Removido de favoritos" });
                    }
                    else
                    {
                        _context.Favorites.Add(new Favorite { UserId = userId, ProductId = productId, CreatedAt = DateTime.UtcNow });
                        await _context.SaveChangesAsync();
                        return Json(new { success = true, isFavorite = true, message = "Agregado a favoritos" });
                    }
                }
                else
                {
                    var anonId = GetAnonId();
                    if (string.IsNullOrEmpty(anonId))
                    {
                        anonId = Guid.NewGuid().ToString();
                        EnsureAnonCookie(anonId);
                    }

                    var fav = await _context.Favorites.FirstOrDefaultAsync(f => f.AnonymousId == anonId && f.ProductId == productId);
                    if (fav != null)
                    {
                        _context.Favorites.Remove(fav);
                        await _context.SaveChangesAsync();
                        return Json(new { success = true, isFavorite = false, message = "Removido de favoritos" });
                    }
                    else
                    {
                        _context.Favorites.Add(new Favorite { AnonymousId = anonId, ProductId = productId, CreatedAt = DateTime.UtcNow });
                        await _context.SaveChangesAsync();
                        return Json(new { success = true, isFavorite = true, message = "Agregado a favoritos" });
                    }
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error: " + ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetFavorites()
        {
            try
            {
                if (User.Identity?.IsAuthenticated == true)
                {
                    var userId = _userManager.GetUserId(User);
                    var favorites = await _context.Favorites
                        .Where(f => f.UserId == userId)
                        .Select(f => f.ProductId)
                        .ToListAsync();

                    return Json(new { success = true, favorites = favorites });
                }

                var anonId = GetAnonId();
                if (!string.IsNullOrEmpty(anonId))
                {
                    var favorites = await _context.Favorites
                        .Where(f => f.AnonymousId == anonId)
                        .Select(f => f.ProductId)
                        .ToListAsync();

                    return Json(new { success = true, favorites = favorites });
                }

                // Fallback sesión
                var favoritesJson = HttpContext.Session.GetString("Favorites") ?? "[]";
                var favoritesSession = JsonSerializer.Deserialize<List<int>>(favoritesJson) ?? new List<int>();

                return Json(new { success = true, favorites = favoritesSession });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error: " + ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetFavoritesCount()
        {
            try
            {
                if (User.Identity?.IsAuthenticated == true)
                {
                    var userId = _userManager.GetUserId(User);
                    var count = await _context.Favorites.CountAsync(f => f.UserId == userId);
                    return Json(new { success = true, count = count });
                }

                var anonId = GetAnonId();
                if (!string.IsNullOrEmpty(anonId))
                {
                    var count = await _context.Favorites.CountAsync(f => f.AnonymousId == anonId);
                    return Json(new { success = true, count = count });
                }

                var favoritesJson = HttpContext.Session.GetString("Favorites") ?? "[]";
                var favorites = JsonSerializer.Deserialize<List<int>>(favoritesJson) ?? new List<int>();

                return Json(new { success = true, count = favorites.Count });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error: " + ex.Message, count = 0 });
            }
        }

        [HttpGet]
        public async Task<IActionResult> CheckFavorite(int productId)
        {
            try
            {
                if (User.Identity?.IsAuthenticated == true)
                {
                    var userId = _userManager.GetUserId(User);
                    var isFavorite = await _context.Favorites.AnyAsync(f => f.UserId == userId && f.ProductId == productId);
                    return Json(new { success = true, isFavorite = isFavorite });
                }

                var anonId = GetAnonId();
                if (!string.IsNullOrEmpty(anonId))
                {
                    var isFav = await _context.Favorites.AnyAsync(f => f.AnonymousId == anonId && f.ProductId == productId);
                    return Json(new { success = true, isFavorite = isFav });
                }

                var favoritesJson = HttpContext.Session.GetString("Favorites") ?? "[]";
                var favorites = JsonSerializer.Deserialize<List<int>>(favoritesJson) ?? new List<int>();
                var isFavSession = favorites.Contains(productId);

                return Json(new { success = true, isFavorite = isFavSession });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error: " + ex.Message });
            }
        }

        // Fusiona favoritos de sesión y de cookie anónima al iniciar sesión
        [HttpPost]
        public async Task<IActionResult> MergeSessionFavorites()
        {
            try
            {
                if (!(User.Identity?.IsAuthenticated == true))
                    return Json(new { success = false, message = "No autenticado" });

                var userId = _userManager.GetUserId(User);
                int merged = 0;

                // 1) fusionar favoritos desde la sesión (compatibilidad)
                var favoritesJson = HttpContext.Session.GetString("Favorites") ?? "[]";
                var favorites = JsonSerializer.Deserialize<List<int>>(favoritesJson) ?? new List<int>();

                if (favorites.Any())
                {
                    var existing = await _context.Favorites
                        .Where(f => f.UserId == userId)
                        .Select(f => f.ProductId)
                        .ToListAsync();

                    var toAdd = favorites.Except(existing).Distinct().ToList();
                    foreach (var pid in toAdd)
                    {
                        _context.Favorites.Add(new Favorite { UserId = userId, ProductId = pid, CreatedAt = DateTime.UtcNow });
                    }
                    merged += toAdd.Count;
                    if (toAdd.Count > 0) await _context.SaveChangesAsync();

                    HttpContext.Session.Remove("Favorites");
                }

                // 2) fusionar favoritos desde cookie anónima (persistente)
                var anonId = GetAnonId();
                if (!string.IsNullOrEmpty(anonId))
                {
                    var anonFavs = await _context.Favorites
                        .Where(f => f.AnonymousId == anonId)
                        .Select(f => f.ProductId)
                        .ToListAsync();

                    if (anonFavs.Any())
                    {
                        var existingUser = await _context.Favorites
                            .Where(f => f.UserId == userId)
                            .Select(f => f.ProductId)
                            .ToListAsync();

                        var toAddAnon = anonFavs.Except(existingUser).Distinct().ToList();
                        foreach (var pid in toAddAnon)
                        {
                            _context.Favorites.Add(new Favorite { UserId = userId, ProductId = pid, CreatedAt = DateTime.UtcNow });
                        }

                        // eliminar entradas anónimas
                        var anonEntries = await _context.Favorites.Where(f => f.AnonymousId == anonId).ToListAsync();
                        if (anonEntries.Any())
                        {
                            _context.Favorites.RemoveRange(anonEntries);
                        }

                        merged += toAddAnon.Count;
                        if (toAddAnon.Count > 0 || anonEntries.Any())
                            await _context.SaveChangesAsync();

                        // borrar cookie anónima
                        Response.Cookies.Delete(AnonCookieName);
                    }
                }

                return Json(new { success = true, merged = merged });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error: " + ex.Message });
            }
        }
    }
}