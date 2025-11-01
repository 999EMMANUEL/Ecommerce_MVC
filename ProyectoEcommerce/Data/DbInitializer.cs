using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

namespace ProyectoEcommerce.Data
{
    public static class DbInitializer
    {
        public static async Task Initialize(IServiceProvider serviceProvider)
        {
            var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            var userManager = serviceProvider.GetRequiredService<UserManager<IdentityUser>>();

            // Definir roles del sistema
            string[] roleNames = { "Admin", "Empleado", "Cliente" };

            // Crear roles si no existen
            foreach (var roleName in roleNames)
            {
                var roleExist = await roleManager.RoleExistsAsync(roleName);
                if (!roleExist)
                {
                    await roleManager.CreateAsync(new IdentityRole(roleName));
                }
            }

            // Crear administrador inicial SOLO si no existe ningún administrador
            var adminUsers = await userManager.GetUsersInRoleAsync("Admin");

            if (!adminUsers.Any())
            {
                var adminEmail = "admin@innovatech.com";
                var adminUser = await userManager.FindByEmailAsync(adminEmail);

                if (adminUser == null)
                {
                    var newAdmin = new IdentityUser
                    {
                        UserName = adminEmail,
                        Email = adminEmail,
                        EmailConfirmed = true
                    };

                    // Contraseña segura por defecto (cambiarla después del primer login)
                    var result = await userManager.CreateAsync(newAdmin, "Admin123!");

                    if (result.Succeeded)
                    {
                        await userManager.AddToRoleAsync(newAdmin, "Admin");
                        Console.WriteLine($"Administrador inicial creado: {adminEmail}");
                    }
                    else
                    {
                        Console.WriteLine($"Error al crear administrador: {string.Join(", ", result.Errors.Select(e => e.Description))}");
                    }
                }
                else
                {
                    // Si existe el usuario pero no tiene el rol Admin, asignarlo
                    if (!await userManager.IsInRoleAsync(adminUser, "Admin"))
                    {
                        await userManager.AddToRoleAsync(adminUser, "Admin");
                        Console.WriteLine($"Rol Admin asignado a: {adminEmail}");
                    }
                }
            }
            else
            {
                Console.WriteLine($"Ya existen {adminUsers.Count} administrador(es) en el sistema");
            }
        }
    }
}