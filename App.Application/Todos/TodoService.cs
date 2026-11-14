using App.Domain.Todos;

namespace App.Application.Todos;

public sealed class TodoService(ITodoRepository repository)
{
    public async Task<IReadOnlyList<TodoItem>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var todos = await repository.GetAllAsync(cancellationToken);
        return todos
            .Select(todo => new TodoItem(todo.Id, todo.Title, todo.Description, todo.IsCompleted))
            .ToArray();
    }

    public async Task<Guid> CreateAsync(
        string title,
        string? description,
        CancellationToken cancellationToken = default)
    {
        var todo = Todo.Create(title, description);
        await repository.AddAsync(todo, cancellationToken);
        return todo.Id;
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

    private async Task<Todo> GetTodoAsync(Guid id, CancellationToken cancellationToken)
    {
        return await repository.GetByIdAsync(id, cancellationToken)
            ?? throw new TodoNotFoundException(id);
    }
}
