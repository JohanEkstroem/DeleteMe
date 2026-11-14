using App.Application.Todos;
using App.Domain.Todos;
using System.Data.Common;
using Microsoft.EntityFrameworkCore;

namespace App.Infrastructure.Persistence;

public sealed class EfTodoRepository(IDbContextFactory<TodoDbContext> contextFactory) : ITodoRepository
{
    public Task<IReadOnlyList<Todo>> GetAllAsync(CancellationToken cancellationToken = default) =>
        ExecuteAsync<IReadOnlyList<Todo>>(async () =>
        {
            await using var dbContext = await contextFactory.CreateDbContextAsync(cancellationToken);
            return await dbContext.Todos
                .AsNoTracking()
                .ToListAsync(cancellationToken);
        });

    public Task<Todo?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        ExecuteAsync(async () =>
        {
            await using var dbContext = await contextFactory.CreateDbContextAsync(cancellationToken);
            return await dbContext.Todos
                .AsNoTracking()
                .SingleOrDefaultAsync(todo => todo.Id == id, cancellationToken);
        });

    public Task AddAsync(Todo todo, CancellationToken cancellationToken = default) =>
        ExecuteAsync(async () =>
        {
            await using var dbContext = await contextFactory.CreateDbContextAsync(cancellationToken);
            dbContext.Todos.Add(todo);
            await dbContext.SaveChangesAsync(cancellationToken);
        });

    public Task UpdateAsync(Todo todo, CancellationToken cancellationToken = default) =>
        ExecuteAsync(async () =>
        {
            await using var dbContext = await contextFactory.CreateDbContextAsync(cancellationToken);
            dbContext.Todos.Update(todo);
            await dbContext.SaveChangesAsync(cancellationToken);
        });

    public Task DeleteAsync(Todo todo, CancellationToken cancellationToken = default) =>
        ExecuteAsync(async () =>
        {
            await using var dbContext = await contextFactory.CreateDbContextAsync(cancellationToken);
            dbContext.Todos.Remove(todo);
            await dbContext.SaveChangesAsync(cancellationToken);
        });

    private static async Task<TResult> ExecuteAsync<TResult>(Func<Task<TResult>> operation)
    {
        try
        {
            return await operation();
        }
        catch (DbUpdateException exception)
        {
            throw new TodoRepositoryException(exception);
        }
        catch (DbException exception)
        {
            throw new TodoRepositoryException(exception);
        }
    }

    private static async Task ExecuteAsync(Func<Task> operation)
    {
        try
        {
            await operation();
        }
        catch (DbUpdateException exception)
        {
            throw new TodoRepositoryException(exception);
        }
        catch (DbException exception)
        {
            throw new TodoRepositoryException(exception);
        }
    }
}
