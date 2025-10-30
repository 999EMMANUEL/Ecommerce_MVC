using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProyectoEcommerce.Models;

namespace ProyectoEcommerce.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminUsersController : Controller
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public AdminUsersController(
            UserManager<IdentityUser> userManager,
            RoleManager<IdentityRole> roleManager)
        {
            _userManager = userManager;
            _roleManager = roleManager;
        }

        // GET: AdminUsers
        public async Task<IActionResult> Index()
        {
            var users = await _userManager.Users.ToListAsync();
            var userViewModels = new List<UserRoleViewModel>();

            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);
                userViewModels.Add(new UserRoleViewModel
                {
                    UserId = user.Id,
                    Email = user.Email,
                    UserName = user.UserName,
                    EmailConfirmed = user.EmailConfirmed,
                    Roles = roles.ToList()
                });
            }

            return View(userViewModels);
        }

        // GET: AdminUsers/ManageRoles/5
        public async Task<IActionResult> ManageRoles(string id)
        {
            if (string.IsNullOrEmpty(id))
                return NotFound();

            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
                return NotFound();

            var userRoles = await _userManager.GetRolesAsync(user);
            var allRoles = await _roleManager.Roles.ToListAsync();

            var model = new ManageUserRolesViewModel
            {
                UserId = user.Id,
                Email = user.Email,
                UserName = user.UserName,
                Roles = allRoles.Select(r => new RoleSelectionViewModel
                {
                    RoleName = r.Name,
                    IsSelected = userRoles.Contains(r.Name)
                }).ToList()
            };

            return View(model);
        }

        // POST: AdminUsers/ManageRoles
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ManageRoles(ManageUserRolesViewModel model)
        {
            var user = await _userManager.FindByIdAsync(model.UserId);
            if (user == null)
                return NotFound();

            // Obtener roles actuales del usuario
            var currentRoles = await _userManager.GetRolesAsync(user);

            // Verificar que no se está removiendo el último admin
            if (currentRoles.Contains("Admin"))
            {
                var selectedAdminRole = model.Roles.FirstOrDefault(r => r.RoleName == "Admin" && r.IsSelected);
                if (selectedAdminRole == null) // Se intenta quitar el rol Admin
                {
                    var adminUsers = await _userManager.GetUsersInRoleAsync("Admin");
                    if (adminUsers.Count == 1)
                    {
                        TempData["Error"] = "No se puede eliminar el último administrador del sistema.";
                        return RedirectToAction(nameof(ManageRoles), new { id = model.UserId });
                    }
                }
            }

            // Remover todos los roles actuales
            var removeResult = await _userManager.RemoveFromRolesAsync(user, currentRoles);
            if (!removeResult.Succeeded)
            {
                TempData["Error"] = "Error al actualizar roles del usuario.";
                return RedirectToAction(nameof(ManageRoles), new { id = model.UserId });
            }

            // Agregar los roles seleccionados
            var selectedRoles = model.Roles.Where(r => r.IsSelected).Select(r => r.RoleName).ToList();

            // Si no se seleccionó ningún rol, asignar "Cliente" por defecto
            if (!selectedRoles.Any())
            {
                selectedRoles.Add("Cliente");
            }

            var addResult = await _userManager.AddToRolesAsync(user, selectedRoles);
            if (!addResult.Succeeded)
            {
                TempData["Error"] = "Error al asignar nuevos roles.";
                return RedirectToAction(nameof(ManageRoles), new { id = model.UserId });
            }

            TempData["Success"] = $"Roles actualizados correctamente para {user.Email}";
            return RedirectToAction(nameof(Index));
        }

        // GET: AdminUsers/Details/5
        public async Task<IActionResult> Details(string id)
        {
            if (string.IsNullOrEmpty(id))
                return NotFound();

            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
                return NotFound();

            var roles = await _userManager.GetRolesAsync(user);

            var model = new UserRoleViewModel
            {
                UserId = user.Id,
                Email = user.Email,
                UserName = user.UserName,
                EmailConfirmed = user.EmailConfirmed,
                Roles = roles.ToList()
            };

            return View(model);
        }

        // POST: AdminUsers/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
                return NotFound();

            // Verificar que no se está eliminando el último admin
            var roles = await _userManager.GetRolesAsync(user);
            if (roles.Contains("Admin"))
            {
                var adminUsers = await _userManager.GetUsersInRoleAsync("Admin");
                if (adminUsers.Count == 1)
                {
                    TempData["Error"] = "No se puede eliminar el único administrador del sistema.";
                    return RedirectToAction(nameof(Index));
                }
            }

            var result = await _userManager.DeleteAsync(user);
            if (result.Succeeded)
            {
                TempData["Success"] = "Usuario eliminado correctamente.";
            }
            else
            {
                TempData["Error"] = "Error al eliminar el usuario.";
            }

            return RedirectToAction(nameof(Index));
        }

        // GET: AdminUsers/ToggleEmailConfirmation/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleEmailConfirmation(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
                return NotFound();

            user.EmailConfirmed = !user.EmailConfirmed;
            var result = await _userManager.UpdateAsync(user);

            if (result.Succeeded)
            {
                TempData["Success"] = $"Confirmación de email actualizada para {user.Email}";
            }
            else
            {
                TempData["Error"] = "Error al actualizar la confirmación de email.";
            }

            return RedirectToAction(nameof(Index));
        }
    }
}