using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ProyectoEcommerce.Data;

var builder = WebApplication.CreateBuilder(args);


builder.Services.AddDbContext<ProyectoEcommerceContext>(opciones =>
    opciones.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// --- CONFIGURACIÓN ÚNICA de Identity ---
builder.Services.AddIdentity<IdentityUser, IdentityRole>(options =>
{
    options.SignIn.RequireConfirmedAccount = false;
    options.Password.RequireDigit = true;           //  requiere dígito
    options.Password.RequireLowercase = true;       // requiere minúscula
    options.Password.RequireUppercase = true;       // requiere mayúscula
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequiredLength = 6;
})
.AddEntityFrameworkStores<ProyectoEcommerceContext>()
.AddDefaultTokenProviders()
.AddDefaultUI();

// --- Servicios adicionales ---
builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();

var app = builder.Build();

// --- Inicialización de la base de datos y roles ---
using (var scope = app.Services.CreateScope())
{
    var sp = scope.ServiceProvider;
    var db = sp.GetRequiredService<ProyectoEcommerceContext>();
    try
    {
        db.Database.Migrate();   // aplica migraciones pendientes
    }
    catch (InvalidOperationException ex) when (ex.Message.Contains("pending changes"))
    {
        // Ignorar el error de cambios pendientes temporalmente
        Console.WriteLine("Advertencia: Hay cambios pendientes en el modelo. Ejecuta 'Add-Migration'.");
    }

    var roles = sp.GetRequiredService<RoleManager<IdentityRole>>();
    var users = sp.GetRequiredService<UserManager<IdentityUser>>();

    const string ADMIN = "Admin";
    if (!await roles.RoleExistsAsync(ADMIN))
        await roles.CreateAsync(new IdentityRole(ADMIN));

    var email = "admin@demo.com";
    var pass = "Admin123!"; // para login administrador - sección de preguntas frecuentes
    var admin = await users.FindByEmailAsync(email);
    if (admin == null)
    {
        admin = new IdentityUser { UserName = email, Email = email, EmailConfirmed = true };
        if ((await users.CreateAsync(admin, pass)).Succeeded)
            await users.AddToRoleAsync(admin, ADMIN);
    }

    await DbInitializer.Initialize(sp);
}

// --- Configuración del Pipeline ---
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

app.UseAuthentication();   // habilita login
app.UseAuthorization();

app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Dashboard}/{action=Index}/{id?}");

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapRazorPages();       // expone /Identity/Account/Login

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        // Ejecutar migraciones pendientes
        var context = services.GetRequiredService<ProyectoEcommerceContext>();
        await context.Database.MigrateAsync();

        // Inicializar roles y administrador
        await DbInitializer.Initialize(services);

        Console.WriteLine("? Base de datos inicializada correctamente");
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "? Error al inicializar la base de datos");
    }
}

app.Run();

