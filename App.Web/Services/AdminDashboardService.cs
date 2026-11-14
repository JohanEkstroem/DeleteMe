using App.Application.Todos;
using App.Infrastructure.Identity;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace App.Web.Services;

public sealed class AdminDashboardService(
    AdminTodoService todoService,
    UserManager<ApplicationUser> userManager,
    AuthenticationStateProvider authenticationStateProvider)
{
    private const string AdminRole = "Admin";

    public async Task<AdminDashboardData> GetDashboardAsync(CancellationToken cancellationToken = default)
    {
        await EnsureAdminAsync();
        var users = await userManager.Users
            .OrderBy(user => user.Email)
            .ToListAsync(cancellationToken);
        var todos = await todoService.GetAllAsync(cancellationToken);
        var adminUsers = await userManager.GetUsersInRoleAsync(AdminRole);
        var adminIds = adminUsers.Select(user => user.Id).ToHashSet(StringComparer.Ordinal);
        var todosByOwner = todos
            .Where(todo => todo.OwnerId is not null)
            .GroupBy(todo => todo.OwnerId!)
            .ToDictionary(group => group.Key, group => group.ToArray(), StringComparer.Ordinal);

        var userRows = users.Select(user =>
        {
            todosByOwner.TryGetValue(user.Id, out var userTodos);
            return new AdminUserSummary(
                user.Id,
                user.Email ?? user.UserName ?? "Okänd användare",
                IsLocked(user, DateTimeOffset.UtcNow),
                adminIds.Contains(user.Id),
                userTodos?.Length ?? 0,
                userTodos?.Count(todo => todo.IsCompleted) ?? 0);
        }).ToArray();

        var emailsById = users.ToDictionary(
            user => user.Id,
            user => user.Email ?? user.UserName ?? "Okänd användare",
            StringComparer.Ordinal);
        var todoRows = todos.Select(todo => new AdminTodoSummary(
            todo.Id,
            todo.OwnerId,
            todo.OwnerId is not null && emailsById.TryGetValue(todo.OwnerId, out var email)
                ? email
                : "Oägda uppgifter",
            todo.Title,
            todo.Description,
            todo.IsCompleted)).ToArray();

        return new AdminDashboardData(
            users.Count,
            todos.Count,
            todos.Count(todo => todo.IsCompleted),
            userRows,
            todoRows);
    }

    public async Task SetUserDisabledAsync(
        string targetUserId,
        string currentAdminId,
        bool disabled)
    {
        await EnsureAdminAsync();
        if (targetUserId == currentAdminId)
        {
            throw new InvalidOperationException("Du kan inte spärra ditt eget konto.");
        }

        var user = await FindUserAsync(targetUserId);
        if (disabled && await userManager.IsInRoleAsync(user, AdminRole))
        {
            var now = DateTimeOffset.UtcNow;
            var otherActiveAdmins = (await userManager.GetUsersInRoleAsync(AdminRole))
                .Where(admin => admin.Id != targetUserId)
                .Count(admin => !IsLocked(admin, now));
            if (otherActiveAdmins == 0)
            {
                throw new InvalidOperationException("Den sista aktiva administratören kan inte spärras.");
            }
        }

        var lockoutEnabledResult = await userManager.SetLockoutEnabledAsync(user, true);
        EnsureSucceeded(lockoutEnabledResult);

        DateTimeOffset? lockoutEnd = disabled ? DateTimeOffset.UtcNow.AddYears(100) : null;
        var lockoutResult = await userManager.SetLockoutEndDateAsync(user, lockoutEnd);
        EnsureSucceeded(lockoutResult);
        var stampResult = await userManager.UpdateSecurityStampAsync(user);
        EnsureSucceeded(stampResult);
    }

    public async Task DeleteUserAsync(string targetUserId, string currentAdminId)
    {
        await EnsureAdminAsync();
        if (targetUserId == currentAdminId)
        {
            throw new InvalidOperationException("Du kan inte ta bort ditt eget konto.");
        }

        var user = await FindUserAsync(targetUserId);
        if (await userManager.IsInRoleAsync(user, AdminRole))
        {
            var admins = await userManager.GetUsersInRoleAsync(AdminRole);
            var hasAnotherActiveAdmin = admins.Any(
                admin => admin.Id != targetUserId && !IsLocked(admin, DateTimeOffset.UtcNow));
            if (!hasAnotherActiveAdmin)
            {
                throw new InvalidOperationException("Det går inte att ta bort den sista aktiva administratören.");
            }
        }

        var result = await userManager.DeleteAsync(user);
        EnsureSucceeded(result);
    }

    public async Task UpdateTodoAsync(Guid id, string title, string? description)
    {
        await EnsureAdminAsync();
        await todoService.UpdateAsync(id, title, description);
    }

    public async Task SetTodoCompletedAsync(Guid id, bool isCompleted)
    {
        await EnsureAdminAsync();
        await todoService.SetCompletedAsync(id, isCompleted);
    }

    public async Task DeleteTodoAsync(Guid id)
    {
        await EnsureAdminAsync();
        await todoService.DeleteAsync(id);
    }

    private async Task<ApplicationUser> FindUserAsync(string id) =>
        await userManager.FindByIdAsync(id)
            ?? throw new InvalidOperationException("Användaren hittades inte.");

    private async Task EnsureAdminAsync()
    {
        var authenticationState = await authenticationStateProvider.GetAuthenticationStateAsync();
        if (!authenticationState.User.IsInRole(AdminRole))
        {
            throw new UnauthorizedAccessException("Administratörsbehörighet krävs.");
        }
    }

    private static bool IsLocked(ApplicationUser user, DateTimeOffset now) =>
        user.LockoutEnabled && user.LockoutEnd > now;

    private static void EnsureSucceeded(IdentityResult result)
    {
        if (!result.Succeeded)
        {
            throw new InvalidOperationException(
                string.Join("; ", result.Errors.Select(error => error.Description)));
        }
    }
}

public sealed record AdminDashboardData(
    int UserCount,
    int TodoCount,
    int CompletedTodoCount,
    IReadOnlyList<AdminUserSummary> Users,
    IReadOnlyList<AdminTodoSummary> Todos);

public sealed record AdminUserSummary(
    string Id,
    string Email,
    bool IsLocked,
    bool IsAdmin,
    int TodoCount,
    int CompletedTodoCount);

public sealed record AdminTodoSummary(
    Guid Id,
    string? OwnerId,
    string OwnerEmail,
    string Title,
    string? Description,
    bool IsCompleted);
