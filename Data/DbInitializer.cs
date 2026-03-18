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

    // Then re-add the default seed data matching your updated static Resume
    context.ResumeSections.AddRange(
        new ResumeSection
        {
            ResumeTitle = "Highlight of Skills",
            ResumeContent = "<hr>" +
                            "<ul>" +
                            "<li>Proficiency in Object-Oriented Programming (Java, C#, Python)</li>" +
                            "<li>Web Development: Frontend (HTML, CSS, JavaScript, React), Backend (ASP.NET, Spring Boot, Node.js)</li>" +
                            "<li>Full-Stack Development Experience, including MERN & MEAN architectures</li>" +
                            "<li>Database Management: SQL, PostgreSQL, MongoDB; schema design & query optimization</li>" +
                            "<li>Cloud & Microservices: Docker, REST APIs, microservices architecture, JWT authentication, OAuth2, Keycloak</li>" +
                            "<li>Understanding of SDLC, Agile methodologies (Scrum), and Waterfall model</li>" +
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
                            "<li>Team Collaboration across SDLC and cloud-based/microservices projects</li>" +
                            "<li>Problem-solving in secure and scalable software environments</li>" +
                            "</ul>"
        },
        new ResumeSection
        {
            ResumeTitle = "Educational Experience",
            ResumeContent = "<hr>" +
                            "<p><strong>Computer Programming and Analysis</strong> | Sept. 2023 – Apr. 2025<br />George Brown College, Toronto</p>" +
                            "<ul>" +
                            "<li>Full-stack development, cloud services, microservices, and secure web applications</li>" +
                            "<li>Developed proficiency in software development methodologies, testing, and best practices</li>" +
                            "<li>Studied database management, optimization techniques, and cloud integration</li>" +
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
            ResumeContent = "<hr>" +
                            "<p><strong>CodeFolio – Personal Portfolio Web App</strong> | Mar. 2026<br /><em>Full Stack Development I</em></p>" +
                            "<ul>" +
                            "<li>Developed a full-stack portfolio web application using ASP.NET Core MVC, EF Core, PostgreSQL, Razor Pages, HTML/CSS/Bootstrap, and C#</li>" +
                            "<li>Implemented role-based authentication, dynamic project & resume management, and email notifications via SendGrid</li>" +
                            "<li>Containerized the application using Docker and deployed it to a cloud platform</li>" +
                            "<li>Gained experience in secure backend development, responsive front-end design, and full-stack application architecture best practices</li>" +
                            "</ul>" +

                            "<p class='mt-3'><strong>Classmate – Full-Stack Microservices Student Collaboration Platform</strong> | Dec. 2025<br /><em>Full Stack Development / Self-Directed Advanced Project</em></p>" +
                            "<ul>" +
                            "<li>Built a secure full-stack microservices application using Spring Boot, React, Node.js, MongoDB/Postgres, Docker, and Docker Compose</li>" +
                            "<li>Implemented authentication/authorization with Keycloak, OAuth2, OpenID Connect, and JWT for protected REST APIs</li>" +
                            "<li>Designed RESTful microservices and integrated frontend-backend communication while containerizing the environment for reproducibility</li>" +
                            "<li>Strengthened skills in distributed systems, secure API design, microservices architecture, and version control with Git/GitHub</li>" +
                            "</ul>" +

                            "<p class='mt-3'><strong>Customer Churn Prediction Using Machine Learning</strong> | Dec. 2025<br /><em>Machine Learning / Data Analytics</em></p>" +
                            "<ul>" +
                            "<li>Developed ML pipeline in Python using Pandas, NumPy, Scikit-learn, Matplotlib, and Seaborn to predict customer churn</li>" +
                            "<li>Performed data cleaning, feature engineering, encoding categorical variables, and model evaluation using confusion matrices and ROC curves</li>" +
                            "<li>Gained hands-on experience in interpreting ML results for business decisions and debugging preprocessing pipelines</li>" +
                            "</ul>"
        }
    );

        await context.SaveChangesAsync();
    }
 }
