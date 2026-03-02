using CodeFolio.Data;
using CodeFolio.Models;
using CodeFolio.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity.UI.Services;

var builder = WebApplication.CreateBuilder(args);

// Load environment variables from .env file
DotNetEnv.Env.Load();  
Console.WriteLine("[DEBUG] ADMIN_PASSWORD=" + Environment.GetEnvironmentVariable("ADMIN_PASSWORD"));

// Inject environment variables into configuration
builder.Configuration["ConnectionStrings:DefaultConnection"] = Environment.GetEnvironmentVariable("DB_CONNECTION");
builder.Configuration["SendGrid:ApiKey"] = Environment.GetEnvironmentVariable("SENDGRID_API_KEY");
builder.Configuration["SendGrid:FromEmail"] = Environment.GetEnvironmentVariable("SENDGRID_FROM_EMAIL");
builder.Configuration["SendGrid:FromName"] = Environment.GetEnvironmentVariable("SENDGRID_FROM_NAME");

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();     

// Add the context to the service collection with a connection string
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));


// Add Identity with a default role (including RoleManager)
// Adding required services to install ASP.NET Core Identity
// Add Identity with Role management (including RoleManager)
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options => options.SignIn.RequireConfirmedAccount = true)
    .AddEntityFrameworkStores<AppDbContext>()
    .AddDefaultTokenProviders();  // This should be sufficient for most cases

// Register your custom ClaimsPrincipalFactory
builder.Services.AddScoped<IUserClaimsPrincipalFactory<ApplicationUser>, AppClaimPrincipalFactory>();

// Add the RoleManager service explicitly (this is needed to handle roles)
builder.Services.AddScoped<RoleManager<IdentityRole>>();

// Configure custom Access Denied path
builder.Services.ConfigureApplicationCookie(options =>
{
    options.AccessDeniedPath = "/Home/AccessDenied";
});

// Inject our SendGrid email sender
builder.Services.AddSingleton<IEmailSender, EmailSender>();

var app = builder.Build();

// Seed Admin User and Role
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    await DbInitializer.SeedAdmin(services);    // Make sure DbInitializer uses RoleManager
    await DbInitializer.SeedResumeSections(services); //  New seeding for resume sections
    
    var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
    var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();

    string[] roles = { "Admin", "User" };
    foreach (var role in roles)
    {
        if (!await roleManager.RoleExistsAsync(role))
            await roleManager.CreateAsync(new IdentityRole(role));
    }

    string adminEmail = Environment.GetEnvironmentVariable("ADMIN_EMAIL");
    var adminUser = await userManager.FindByEmailAsync(adminEmail);

    if (adminUser != null && !(await userManager.IsInRoleAsync(adminUser, "Admin")))
    {
        await userManager.AddToRoleAsync(adminUser, "Admin");
    }
}



// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();           // Serve CSS/JS/img etc.
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.MapRazorPages();
app.MapStaticAssets();

app.MapControllerRoute(
        name: "default",
        pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();


app.Run();