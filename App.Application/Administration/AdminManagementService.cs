using App.Application.Todos;
using App.Domain.Todos;

namespace App.Application.Administration;

public sealed class AdminManagementService(
    IAdminAccess adminAccess,
    IAdminUserRepository userRepository,
    ITodoRepository todoRepository)
{
    private const string UnownedTodos = "Oägda uppgifter";

    public async Task<AdminDashboardData> GetDashboardAsync(
        CancellationToken cancellationToken = default)
    {
        await adminAccess.RequireAdminUserIdAsync(cancellationToken);
        var users = await userRepository.GetAllAsync(cancellationToken);
        var todos = await todoRepository.GetAllForAdminAsync(cancellationToken);
        var todosByOwner = todos
            .Where(todo => todo.OwnerId is not null)
            .GroupBy(todo => todo.OwnerId!)
            .ToDictionary(group => group.Key, group => group.ToArray(), StringComparer.Ordinal);

        var userRows = users.Select(user =>
        {
            todosByOwner.TryGetValue(user.Id, out var userTodos);
            return new AdminUserSummary(
                user.Id,
                user.Email,
                user.IsLocked,
                user.IsAdmin,
                userTodos?.Length ?? 0,
                userTodos?.Count(todo => todo.IsCompleted) ?? 0);
        }).ToArray();

        var emailsById = users.ToDictionary(user => user.Id, user => user.Email, StringComparer.Ordinal);
        var todoRows = todos.Select(todo => new AdminTodoSummary(
            todo.Id,
            todo.OwnerId,
            todo.OwnerId is not null && emailsById.TryGetValue(todo.OwnerId, out var email)
                ? email
                : UnownedTodos,
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
        bool disabled,
        CancellationToken cancellationToken = default)
    {
        var currentAdminId = await adminAccess.RequireAdminUserIdAsync(cancellationToken);
        if (targetUserId == currentAdminId)
        {
            throw new InvalidOperationException("Du kan inte spärra ditt eget konto.");
        }

        var target = await FindUserAsync(targetUserId, cancellationToken);
        if (disabled && target.IsAdmin)
        {
            var users = await userRepository.GetAllAsync(cancellationToken);
            var hasAnotherActiveAdmin = users.Any(
                user => user.Id != targetUserId && user.IsAdmin && !user.IsLocked);
            if (!hasAnotherActiveAdmin)
            {
                throw new InvalidOperationException("Den sista aktiva administratören kan inte spärras.");
            }
        }

        await userRepository.SetDisabledAsync(targetUserId, disabled, cancellationToken);
    }

    public async Task DeleteUserAsync(
        string targetUserId,
        CancellationToken cancellationToken = default)
    {
        var currentAdminId = await adminAccess.RequireAdminUserIdAsync(cancellationToken);
        if (targetUserId == currentAdminId)
        {
            throw new InvalidOperationException("Du kan inte ta bort ditt eget konto.");
        }

        var target = await FindUserAsync(targetUserId, cancellationToken);
        if (target.IsAdmin)
        {
            var users = await userRepository.GetAllAsync(cancellationToken);
            var hasAnotherActiveAdmin = users.Any(
                user => user.Id != targetUserId && user.IsAdmin && !user.IsLocked);
            if (!hasAnotherActiveAdmin)
            {
                throw new InvalidOperationException("Det går inte att ta bort den sista aktiva administratören.");
            }
        }

        await userRepository.DeleteAsync(targetUserId, cancellationToken);
    }

    public async Task UpdateTodoAsync(
        Guid id,
        string title,
        string? description,
        CancellationToken cancellationToken = default)
    {
        await adminAccess.RequireAdminUserIdAsync(cancellationToken);
        var todo = await GetTodoAsync(id, cancellationToken);
        todo.UpdateDetails(title, description);
        await todoRepository.UpdateAsync(todo, cancellationToken);
    }

    public async Task SetTodoCompletedAsync(
        Guid id,
        bool isCompleted,
        CancellationToken cancellationToken = default)
    {
        await adminAccess.RequireAdminUserIdAsync(cancellationToken);
        var todo = await GetTodoAsync(id, cancellationToken);
        todo.SetCompleted(isCompleted);
        await todoRepository.UpdateAsync(todo, cancellationToken);
    }

    public async Task DeleteTodoAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        await adminAccess.RequireAdminUserIdAsync(cancellationToken);
        var todo = await GetTodoAsync(id, cancellationToken);
        await todoRepository.DeleteAsync(todo, cancellationToken);
    }

    private async Task<AdminUserAccount> FindUserAsync(string id, CancellationToken cancellationToken) =>
        await userRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new InvalidOperationException("Användaren hittades inte.");

    private async Task<Todo> GetTodoAsync(Guid id, CancellationToken cancellationToken) =>
        await todoRepository.GetByIdForAdminAsync(id, cancellationToken)
            ?? throw new TodoNotFoundException(id);
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
