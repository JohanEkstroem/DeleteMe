using App.Domain.Todos;

namespace App.Application.Todos;

public interface ITodoRepository
{
    Task<IReadOnlyList<Todo>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<Todo?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task AddAsync(Todo todo, CancellationToken cancellationToken = default);

    Task UpdateAsync(Todo todo, CancellationToken cancellationToken = default);

    Task DeleteAsync(Todo todo, CancellationToken cancellationToken = default);
}
