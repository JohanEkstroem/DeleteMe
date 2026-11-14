using App.Domain.Todos;

namespace App.Application.Todos;

public sealed class AdminTodoService(ITodoRepository repository)
{
    public async Task<IReadOnlyList<TodoItem>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var todos = await repository.GetAllForAdminAsync(cancellationToken);
        return todos
            .Select(ToTodoItem)
            .ToArray();
    }

    public async Task UpdateAsync(
        Guid id,
        string title,
        string? description,
        CancellationToken cancellationToken = default)
    {
        var todo = await GetTodoAsync(id, cancellationToken);
        todo.UpdateDetails(title, description);
        await repository.UpdateAsync(todo, cancellationToken);
    }

    public async Task SetCompletedAsync(
        Guid id,
        bool isCompleted,
        CancellationToken cancellationToken = default)
    {
        var todo = await GetTodoAsync(id, cancellationToken);
        todo.SetCompleted(isCompleted);
        await repository.UpdateAsync(todo, cancellationToken);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var todo = await GetTodoAsync(id, cancellationToken);
        await repository.DeleteAsync(todo, cancellationToken);
    }

    private async Task<Todo> GetTodoAsync(Guid id, CancellationToken cancellationToken) =>
        await repository.GetByIdForAdminAsync(id, cancellationToken)
            ?? throw new TodoNotFoundException(id);

    private static TodoItem ToTodoItem(Todo todo) =>
        new(todo.Id, todo.OwnerId, todo.Title, todo.Description, todo.IsCompleted);
}
