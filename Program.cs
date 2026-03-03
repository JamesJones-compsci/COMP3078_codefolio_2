using CodeFolio.Data;
using CodeFolio.Models;
using CodeFolio.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity.UI.Services;

var builder = WebApplication.CreateBuilder(args);

// Load environment variables from Docker
Console.WriteLine("[DEBUG] ADMIN_PASSWORD=" + Environment.GetEnvironmentVariable("ADMIN_PASSWORD"));

// Inject environment variables into configuration
builder.Configuration["ConnectionStrings:DefaultConnection"] = Environment.GetEnvironmentVariable("DB_CONNECTION");
builder.Configuration["SendGrid:ApiKey"] = Environment.GetEnvironmentVariable("SENDGRID_API_KEY");
builder.Configuration["SendGrid:FromEmail"] = Environment.GetEnvironmentVariable("SENDGRID_FROM_EMAIL");
builder.Configuration["SendGrid:FromName"] = Environment.GetEnvironmentVariable("SENDGRID_FROM_NAME");

// Add services to the container
builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();

// Add DbContext
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

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

// Retry loop for database readiness
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
            Console.WriteLine("[DEBUG] Database ready, applying migrations...");
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

    // Seed roles
    var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
    var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();

    string[] roles = { "Admin", "User" };
    foreach (var role in roles)
    {
        if (!await roleManager.RoleExistsAsync(role))
            await roleManager.CreateAsync(new IdentityRole(role));
    }

    // Seed admin user
    string adminEmail = Environment.GetEnvironmentVariable("ADMIN_EMAIL");
    string adminPassword = Environment.GetEnvironmentVariable("ADMIN_PASSWORD");

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

    // Seed resume sections (if needed)
    await DbInitializer.SeedResumeSections(services);
}

// Configure HTTP request pipeline
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