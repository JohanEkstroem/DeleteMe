using App.Domain.Todos;

namespace App.Application.Todos;

public interface ITodoRepository
{
    Task<IReadOnlyList<Todo>> GetAllForUserAsync(
        string ownerId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Todo>> GetAllForAdminAsync(CancellationToken cancellationToken = default);

    Task<Todo?> GetByIdForUserAsync(
        Guid id,
        string ownerId,
        CancellationToken cancellationToken = default);

    Task<Todo?> GetByIdForAdminAsync(Guid id, CancellationToken cancellationToken = default);

    Task AddAsync(Todo todo, CancellationToken cancellationToken = default);

    Task UpdateAsync(Todo todo, CancellationToken cancellationToken = default);

    Task DeleteAsync(Todo todo, CancellationToken cancellationToken = default);
}
