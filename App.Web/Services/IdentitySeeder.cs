using App.Infrastructure.Identity;
using App.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace App.Web.Services;

public sealed class IdentitySeeder(
    IConfiguration configuration,
    IDbContextFactory<TodoDbContext> dbContextFactory,
    UserManager<ApplicationUser> userManager,
    RoleManager<IdentityRole> roleManager,
    ILogger<IdentitySeeder> logger)
{
    private const string AdminRole = "Admin";

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        var email = configuration["Admin:Email"];
        var password = configuration["Admin:Password"];
        if (string.IsNullOrWhiteSpace(email) != string.IsNullOrWhiteSpace(password))
        {
            throw new InvalidOperationException(
                "Both Admin:Email and Admin:Password must be configured to provision an administrator.");
        }

        if (string.IsNullOrWhiteSpace(email) && string.IsNullOrWhiteSpace(password))
        {
            logger.LogWarning(
                "No administrator account was provisioned. Configure Admin:Email and Admin:Password using secrets.");
            return;
        }

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            throw new InvalidOperationException(
                "Both Admin:Email and Admin:Password must be configured to provision an administrator.");
        }

        if (!await roleManager.RoleExistsAsync(AdminRole))
        {
            var roleResult = await roleManager.CreateAsync(new IdentityRole(AdminRole));
            EnsureSucceeded(roleResult, "Could not create the administrator role.");
        }

        var admin = await userManager.FindByEmailAsync(email);
        if (admin is null)
        {
            admin = new ApplicationUser
            {
                UserName = email,
                Email = email,
                EmailConfirmed = true,
                LockoutEnabled = true
            };

            var createResult = await userManager.CreateAsync(admin, password);
            EnsureSucceeded(createResult, "Could not create the configured administrator account.");
        }
        else if (!await userManager.IsInRoleAsync(admin, AdminRole))
        {
            throw new InvalidOperationException(
                "The configured administrator email already belongs to a non-administrator account.");
        }

        if (!await userManager.IsInRoleAsync(admin, AdminRole))
        {
            var roleResult = await userManager.AddToRoleAsync(admin, AdminRole);
            EnsureSucceeded(roleResult, "Could not assign the administrator role.");
        }

        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var assignedCount = await dbContext.Todos
            .Where(todo => todo.OwnerId == null)
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(todo => todo.OwnerId, admin.Id),
                cancellationToken);

        if (assignedCount > 0)
        {
            logger.LogInformation(
                "Assigned {TodoCount} existing tasks to the configured administrator.",
                assignedCount);
        }
    }

    private static void EnsureSucceeded(IdentityResult result, string message)
    {
        if (!result.Succeeded)
        {
            var errors = string.Join("; ", result.Errors.Select(error => error.Description));
            throw new InvalidOperationException($"{message} {errors}");
        }
    }
}
