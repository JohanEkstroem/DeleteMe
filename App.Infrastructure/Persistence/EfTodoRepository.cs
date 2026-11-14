using App.Application.Todos;
using App.Domain.Todos;
using Microsoft.EntityFrameworkCore;

namespace App.Infrastructure.Persistence;

public sealed class EfTodoRepository(IDbContextFactory<TodoDbContext> contextFactory) : ITodoRepository
{
    public async Task<IReadOnlyList<Todo>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        await using var dbContext = await contextFactory.CreateDbContextAsync(cancellationToken);
        return await dbContext.Todos
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task<Todo?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await using var dbContext = await contextFactory.CreateDbContextAsync(cancellationToken);
        return await dbContext.Todos
            .AsNoTracking()
            .SingleOrDefaultAsync(todo => todo.Id == id, cancellationToken);
    }

    public async Task AddAsync(Todo todo, CancellationToken cancellationToken = default)
    {
        await using var dbContext = await contextFactory.CreateDbContextAsync(cancellationToken);
        dbContext.Todos.Add(todo);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(Todo todo, CancellationToken cancellationToken = default)
    {
        await using var dbContext = await contextFactory.CreateDbContextAsync(cancellationToken);
        dbContext.Todos.Update(todo);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(Todo todo, CancellationToken cancellationToken = default)
    {
        await using var dbContext = await contextFactory.CreateDbContextAsync(cancellationToken);
        dbContext.Todos.Remove(todo);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
