using App.Application.Administration;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace App.Infrastructure.Identity;

public sealed class IdentityAdminUserRepository(UserManager<ApplicationUser> userManager)
    : IAdminUserRepository
{
    private const string AdminRole = "Admin";

    public async Task<IReadOnlyList<AdminUserAccount>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        var users = await userManager.Users
            .OrderBy(user => user.Email)
            .ToListAsync(cancellationToken);
        var adminIds = (await userManager.GetUsersInRoleAsync(AdminRole))
            .Select(user => user.Id)
            .ToHashSet(StringComparer.Ordinal);
        var now = DateTimeOffset.UtcNow;

        return users.Select(user => ToAdminUserAccount(user, adminIds, now)).ToArray();
    }

    public async Task<AdminUserAccount?> GetByIdAsync(
        string id,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var user = await userManager.FindByIdAsync(id);
        if (user is null)
        {
            return null;
        }

        var isAdmin = await userManager.IsInRoleAsync(user, AdminRole);
        return ToAdminUserAccount(user, isAdmin, DateTimeOffset.UtcNow);
    }

    public async Task SetDisabledAsync(
        string id,
        bool disabled,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var user = await FindUserAsync(id);
        EnsureSucceeded(
            await userManager.SetLockoutEnabledAsync(user, true),
            "Could not enable account lockout.");

        DateTimeOffset? lockoutEnd = disabled ? DateTimeOffset.UtcNow.AddYears(100) : null;
        EnsureSucceeded(
            await userManager.SetLockoutEndDateAsync(user, lockoutEnd),
            "Could not update the account lockout state.");
        EnsureSucceeded(
            await userManager.UpdateSecurityStampAsync(user),
            "Could not update the account security stamp.");
    }

    public async Task DeleteAsync(string id, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var user = await FindUserAsync(id);
        EnsureSucceeded(await userManager.DeleteAsync(user), "Could not delete the account.");
    }

    private async Task<ApplicationUser> FindUserAsync(string id) =>
        await userManager.FindByIdAsync(id)
            ?? throw new AdminUserRepositoryException("The account no longer exists.");

    private static AdminUserAccount ToAdminUserAccount(
        ApplicationUser user,
        IReadOnlySet<string> adminIds,
        DateTimeOffset now) =>
        ToAdminUserAccount(user, adminIds.Contains(user.Id), now);

    private static AdminUserAccount ToAdminUserAccount(
        ApplicationUser user,
        bool isAdmin,
        DateTimeOffset now) =>
        new(
            user.Id,
            user.Email ?? user.UserName ?? "Okänd användare",
            user.LockoutEnabled && user.LockoutEnd > now,
            isAdmin);

    private static void EnsureSucceeded(IdentityResult result, string operation)
    {
        if (!result.Succeeded)
        {
            var errors = string.Join("; ", result.Errors.Select(error => error.Description));
            throw new AdminUserRepositoryException($"{operation} {errors}");
        }
    }
}
