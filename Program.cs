using CodeFolio.Data;
using CodeFolio.Models;
using CodeFolio.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.EntityFrameworkCore;
using DotNetEnv;

var builder = WebApplication.CreateBuilder(args);

#region Logging
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
#endregion

#region Env Loading
try
{
    Env.Load();
    Console.WriteLine("[DEBUG] Loaded local .env file.");
}
catch
{
    Console.WriteLine("[DEBUG] No .env file found (using environment variables).");
}
#endregion

#region Configuration (Secrets + Connection String)

var adminEmail = Environment.GetEnvironmentVariable("ADMIN_EMAIL") ?? "admin@example.com";
var adminPassword = Environment.GetEnvironmentVariable("ADMIN_PASSWORD") ?? "ChangeMe123$";

var connectionString =
    Environment.GetEnvironmentVariable("DB_CONNECTION")
    ?? $"Host={Environment.GetEnvironmentVariable("DB_HOST") ?? "localhost"};" +
       $"Port={Environment.GetEnvironmentVariable("DB_PORT") ?? "5432"};" +
       $"Database={Environment.GetEnvironmentVariable("DB_NAME") ?? "codefolio"};" +
       $"Username={Environment.GetEnvironmentVariable("DB_USER") ?? "postgres"};" +
       $"Password={Environment.GetEnvironmentVariable("DB_PASSWORD") ?? "postgres"};" +
       $"SSL Mode={Environment.GetEnvironmentVariable("DB_SSLMODE") ?? "Require"};Trust Server Certificate=true;";

Console.WriteLine("[DEBUG] DB connection configured (hidden password).");
#endregion

#region Services
builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(connectionString));

builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    options.SignIn.RequireConfirmedAccount = true;
})
.AddEntityFrameworkStores<AppDbContext>()
.AddDefaultTokenProviders();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.AccessDeniedPath = "/Home/AccessDenied";
});

builder.Services.AddSingleton<IEmailSender, EmailSender>();

builder.Services.AddScoped<IUserClaimsPrincipalFactory<ApplicationUser>, AppClaimPrincipalFactory>();
#endregion

var app = builder.Build();

Console.WriteLine("[DEBUG] Application starting...");

#region DATABASE MIGRATION (CRITICAL - MUST RUN FIRST)
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>(); 

    try
    {
        Console.WriteLine("[DEBUG] Applying migrations...");    
        db.Database.Migrate();
        Console.WriteLine("[DEBUG] Migrations completed.");
    }
    catch (Exception ex)
    {
        Console.WriteLine("[ERROR] Migration failed: " + ex.Message);
        Console.WriteLine("[WARN] Continuing without DB migration...");
    }
}
#endregion

#region SEEDING (RUN AFTER MIGRATIONS)
using (var scope = app.Services.CreateScope())
{
    try
    {
        var services = scope.ServiceProvider;

        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();

        string[] roles = { "Admin", "User" };

        foreach (var role in roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole(role));
            }
        }

        var adminUser = await userManager.FindByEmailAsync(adminEmail);

        if (adminUser == null)
        {
            adminUser = new ApplicationUser
            {
                UserName = adminEmail,
                Email = adminEmail,
                EmailConfirmed = true
            };

            var result = await userManager.CreateAsync(adminUser, adminPassword);

            if (!result.Succeeded)
            {
                Console.WriteLine("[ERROR] Admin user creation failed:");
                foreach (var error in result.Errors)
                    Console.WriteLine(error.Description);
            }
        }

        if (!await userManager.IsInRoleAsync(adminUser, "Admin"))
        {
            await userManager.AddToRoleAsync(adminUser, "Admin");
        }

        await DbInitializer.SeedResumeSections(services);
    }
    catch (Exception ex)
    {
        Console.WriteLine("[ERROR] Seeding failed: " + ex.Message);
        Console.WriteLine("[WARN] Continuing without DB seeding...");
    }
}
#endregion

#region PIPELINE
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseStaticFiles();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapRazorPages();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

var port = Environment.GetEnvironmentVariable("PORT") ?? "8080";
app.Urls.Add($"http://*:{port}");

Console.WriteLine($"[DEBUG] Listening on port {port}");

#endregion

app.Run();