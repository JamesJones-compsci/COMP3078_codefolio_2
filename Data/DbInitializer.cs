using CodeFolio.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace CodeFolio.Data;

public class DbInitializer
{
    public static async Task SeedAdmin(IServiceProvider serviceProvider)
    {
        var userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();

        string adminEmail = Environment.GetEnvironmentVariable("ADMIN_EMAIL");
        string adminPassword = Environment.GetEnvironmentVariable("ADMIN_PASSWORD");
        Console.WriteLine($"[DEBUG] Loaded admin password: {adminPassword}");
        
        if (string.IsNullOrWhiteSpace(adminPassword))
        {
            throw new Exception("ADMIN_PASSWORD is not set in environment variables.");
        }
        
        // Ensure roles exist
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
                EmailConfirmed = true,
                FirstName = "Admin",
                LastName = "User"
            };

            var result = await userManager.CreateAsync(adminUser, adminPassword);
            if (!result.Succeeded)
            {
                throw new Exception("Failed to create Admin user: " + string.Join(", ", result.Errors.Select(e => e.Description)));
            }
            
            
            // Add FirstName and LastName claims
            await userManager.AddClaimAsync(adminUser, new System.Security.Claims.Claim("FirstName", adminUser.FirstName));
            await userManager.AddClaimAsync(adminUser, new System.Security.Claims.Claim("LastName", adminUser.LastName));
        }
        
        if (!await userManager.IsInRoleAsync(adminUser, "Admin"))
        {
            await userManager.AddToRoleAsync(adminUser, "Admin");
        }
    }
    
    
    // Added this method to seed ResumeSections
    public static async Task SeedResumeSections(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        // Delete all existing ResumeSections first 
        if (await context.ResumeSections.AnyAsync())
        {
            context.ResumeSections.RemoveRange(context.ResumeSections);
            await context.SaveChangesAsync();
        }

        // Then re-add the default seed data
        context.ResumeSections.AddRange(
                new ResumeSection
                {
                    ResumeTitle = "Highlight of Skills",
                    ResumeContent = "     "
                    
                },
                new ResumeSection
                {
                    ResumeTitle = "Computer Programming Skills",
                    ResumeContent = "<hr>" +
                                    "<ul>" +
                                    "<li>Proficiency in Object-Oriented Programming (Java, C#)</li>" +
                                    "<li>Web Development Fundamentals (ASP.NET, HTML, CSS, JavaScript)</li>" +
                                    "<li>Relational Databases & SQL (Schema Design, Query Optimization)</li>" +
                                    "<li>Full-Stack Development Experience</li>" +
                                    "<li>Understanding of Software Development Life Cycle (SDLC), Agile methodologies (Scrum), and Waterfall model</li>" +
                                    "</ul>"
                    
                },
                new ResumeSection
                {
                    ResumeTitle = "Core Competency Skills",
                    ResumeContent = "<hr>" +
                                    "<ul>" +
                                    "<li>Clear and Effective Communication</li>" +
                                    "<li>Strong Interpersonal Skills</li>" +
                                    "<li>Proactive Work Ethic</li>" +
                                    "<li>Team Collaboration across the SDLC, including Agile methodologies (Scrum) and traditional models like Waterfall</li>" +
                                    "</ul>"
                    
                },
                new ResumeSection
                {
                    ResumeTitle = "Educational Experience",
                    ResumeContent =
                        "<hr>" +
                        "<p><strong>Computer Programming and Analysis</strong> | Sept. 2023 – Apr. 2025<br />George Brown College, Toronto</p>" +
                        "<ul>" +
                        "<li>Full-stack development, web applications, and mobile app development experience</li>" +
                        "<li>Proficiency in software development methodologies</li>" +
                        "<li>Studied database management and optimization</li>" +
                        "</ul>"
                },
                new ResumeSection
                {
                    ResumeTitle = "Work History",
                    ResumeContent = "<hr>" +
                                    "<p><strong>Fitness Coach/Owner</strong> | Nov. 2020 – Present<br />JJ’s Fitness Toronto</p>" +
                                    "<ul>" +
                                    "<li>Develop and implement structured training programs</li>" +
                                    "<li>Analyze client progress and adjust plans</li>" +
                                    "<li>Manage business operations, budgeting, and marketing</li>" +
                                    "</ul>"
                },
                new ResumeSection
                {
                    ResumeTitle = "Special Projects",
                    ResumeContent = "     "
                    
                },
                new ResumeSection
                {
                    ResumeTitle = " ",
                    ResumeContent = "<p><strong>Project Management Tool – Web Application Development</strong> | Jan. 2025</p>" +
                                    "<ul>" +
                                    "<li>Developed dynamic web application featuring front-end UI/UX design, back-end logic, and database integration</li>" +
                                    "<li>Integrated PostgreSQL database management for storing and retrieving data efficiently" +
                                    "<li>Focused on scalability and maintainability, applying best coding practices to optimize performance</li>" +
                                    "</ul>"
                }
            );
            await context.SaveChangesAsync();
     }
 }
