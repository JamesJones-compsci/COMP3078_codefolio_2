using CodeFolio.Data;
using CodeFolio.Models;
using CodeFolio.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity.UI.Services;
using DotNetEnv; // Make sure you have DotNetEnv package installed

var builder = WebApplication.CreateBuilder(args);

// ----------------------------
// Load local .env file if exists (for dotnet run)
// ----------------------------
try
{
    Env.Load(); // loads .env in project root
    Console.WriteLine("[DEBUG] Loaded local .env file.");
}
catch
{
    Console.WriteLine("[DEBUG] No local .env file found, relying on environment variables.");
}

// ----------------------------
// Load Admin credentials
// ----------------------------
var adminEmail = Environment.GetEnvironmentVariable("ADMIN_EMAIL") ?? "admin@example.com";
var adminPassword = Environment.GetEnvironmentVariable("ADMIN_PASSWORD") ?? "ChangeMe123$";

Console.WriteLine("[DEBUG] ADMIN_EMAIL=" + adminEmail);
Console.WriteLine("[DEBUG] ADMIN_PASSWORD=" + (string.IsNullOrEmpty(adminPassword) ? "<not set>" : "****"));

// ----------------------------
// Database configuration
// ----------------------------
var dbConnectionString = Environment.GetEnvironmentVariable("DB_CONNECTION");

// If full DB_CONNECTION is provided, use it; otherwise build from components
string connectionString;
if (!string.IsNullOrEmpty(dbConnectionString))
{
    connectionString = dbConnectionString;
}
else
{
    var dbHost = Environment.GetEnvironmentVariable("DB_HOST") ?? "localhost";
    var dbPort = Environment.GetEnvironmentVariable("DB_PORT") ?? "5432";
    var dbName = Environment.GetEnvironmentVariable("DB_NAME") ?? "codefolio_local";
    var dbUser = Environment.GetEnvironmentVariable("DB_USER") ?? "postgres";
    var dbPassword = Environment.GetEnvironmentVariable("DB_PASSWORD") ?? "postgres";
    var dbSslMode = Environment.GetEnvironmentVariable("DB_SSLMODE") ?? "Require";

    connectionString = $"Host={dbHost};Port={dbPort};Database={dbName};Username={dbUser};Password={dbPassword};Pooling=true;SSL Mode={dbSslMode};Trust Server Certificate=true;";
}

// ----------------------------
// Mask sensitive info for debug
// ----------------------------
string displayConnectionString;
if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("DB_PASSWORD")))
{
    displayConnectionString = connectionString.Replace(Environment.GetEnvironmentVariable("DB_PASSWORD")!, "****");
}
else
{
    displayConnectionString = connectionString;
}
Console.WriteLine("[DEBUG] Using connection string: " + displayConnectionString);

// ----------------------------
// Configure SendGrid safely
// ----------------------------
var sendGridKey = Environment.GetEnvironmentVariable("SENDGRID_API_KEY");
var sendGridFromEmail = Environment.GetEnvironmentVariable("SENDGRID_FROM_EMAIL");
var sendGridFromName = Environment.GetEnvironmentVariable("SENDGRID_FROM_NAME");

builder.Configuration["SendGrid:ApiKey"] = sendGridKey ?? "";
builder.Configuration["SendGrid:FromEmail"] = sendGridFromEmail ?? "no-reply@example.com";
builder.Configuration["SendGrid:FromName"] = sendGridFromName ?? "CodeFolio Default Sender";

Console.WriteLine("[DEBUG] SendGrid API Key loaded: " + (string.IsNullOrEmpty(sendGridKey) ? "<not set>" : "****"));

// ----------------------------
// Add services
// ----------------------------
builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();

// Add DbContext
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(connectionString));

// Add Identity
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options => options.SignIn.RequireConfirmedAccount = true)
    .AddEntityFrameworkStores<AppDbContext>()
    .AddDefaultTokenProviders();

// Register ClaimsPrincipalFactory & RoleManager
builder.Services.AddScoped<IUserClaimsPrincipalFactory<ApplicationUser>, AppClaimPrincipalFactory>();
builder.Services.AddScoped<RoleManager<IdentityRole>>();

// Configure cookies
builder.Services.ConfigureApplicationCookie(options =>
{
    options.AccessDeniedPath = "/Home/AccessDenied";
});

// Register EmailSender
builder.Services.AddSingleton<IEmailSender, EmailSender>();

var app = builder.Build();

Console.WriteLine("[DEBUG] Starting CodeFolio app...");

// ----------------------------
// Retry loop for DB readiness
// ----------------------------
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;

    var maxRetries = 10;
    var delay = TimeSpan.FromSeconds(5);
    var dbReady = false;

    for (int i = 0; i < maxRetries; i++)
    {
        try
        {
            var context = services.GetRequiredService<AppDbContext>();
            context.Database.Migrate(); // Apply migrations
            dbReady = true;
            Console.WriteLine("[DEBUG] Database ready, migrations applied if needed...");
            break;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[DEBUG] Database not ready yet ({i + 1}/{maxRetries}): {ex.Message}");
            await Task.Delay(delay);
        }
    }

    if (!dbReady)
    {
        Console.WriteLine("[ERROR] Could not connect to the database. Exiting...");
        return;
    }

    // ----------------------------
    // Seed roles and admin user
    // ----------------------------
    var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
    var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();

    string[] roles = { "Admin", "User" };
    foreach (var role in roles)
    {
        if (!await roleManager.RoleExistsAsync(role))
            await roleManager.CreateAsync(new IdentityRole(role));
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
            Console.WriteLine("[ERROR] Failed to create admin user:");
            foreach (var err in result.Errors)
                Console.WriteLine(err.Description);
        }
    }

    if (!(await userManager.IsInRoleAsync(adminUser, "Admin")))
        await userManager.AddToRoleAsync(adminUser, "Admin");

    // Seed resume sections
    await DbInitializer.SeedResumeSections(services);
}

// ----------------------------
// Configure HTTP request pipeline
// ----------------------------
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapRazorPages();
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}"
);

app.Run();