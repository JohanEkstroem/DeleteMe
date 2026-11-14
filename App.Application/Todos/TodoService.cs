using App.Domain.Todos;

namespace App.Application.Todos;

public sealed class TodoService(ITodoRepository repository)
{
    public async Task<IReadOnlyList<TodoItem>> GetAllAsync(
        string ownerId,
        CancellationToken cancellationToken = default)
    {
        var todos = await repository.GetAllForUserAsync(ownerId, cancellationToken);
        return todos
            .Select(ToTodoItem)
            .ToArray();
    }

    public async Task<Guid> CreateAsync(
        string ownerId,
        string title,
        string? description,
        CancellationToken cancellationToken = default)
    {
        var todo = Todo.Create(ownerId, title, description);
        await repository.AddAsync(todo, cancellationToken);
        return todo.Id;
    }

    public async Task UpdateAsync(
        string ownerId,
        Guid id,
        string title,
        string? description,
        CancellationToken cancellationToken = default)
    {
        var todo = await GetTodoAsync(id, ownerId, cancellationToken);
        todo.UpdateDetails(title, description);
        await repository.UpdateAsync(todo, cancellationToken);
    }

    public async Task SetCompletedAsync(
        string ownerId,
        Guid id,
        bool isCompleted,
        CancellationToken cancellationToken = default)
    {
        var todo = await GetTodoAsync(id, ownerId, cancellationToken);
        todo.SetCompleted(isCompleted);
        await repository.UpdateAsync(todo, cancellationToken);
    }

    public async Task DeleteAsync(
        string ownerId,
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var todo = await GetTodoAsync(id, ownerId, cancellationToken);
        await repository.DeleteAsync(todo, cancellationToken);
    }

    private async Task<Todo> GetTodoAsync(
        Guid id,
        string ownerId,
        CancellationToken cancellationToken)
    {
        return await repository.GetByIdForUserAsync(id, ownerId, cancellationToken)
            ?? throw new TodoNotFoundException(id);
    }

    private static TodoItem ToTodoItem(Todo todo) =>
        new(todo.Id, todo.OwnerId, todo.Title, todo.Description, todo.IsCompleted);
}
