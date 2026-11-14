using App.Domain.Todos;
using App.Infrastructure.Identity;
using App.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace App.Infrastructure.Tests;

public sealed class EfTodoRepositoryTests
{
    [Fact]
    public async Task GetAllForUserAsync_ReturnsOnlyThatUsersTodos()
    {
        await using var fixture = await CreateFixtureAsync();

        var todos = await fixture.Repository.GetAllForUserAsync("user-a");

        Assert.Equal([fixture.UserATodo.Id], todos.Select(todo => todo.Id));
    }

    [Fact]
    public async Task GetByIdForUserAsync_CannotRetrieveAnotherUsersTodo()
    {
        await using var fixture = await CreateFixtureAsync();

        var todo = await fixture.Repository.GetByIdForUserAsync(fixture.UserBTodo.Id, "user-a");

        Assert.Null(todo);
    }

    [Fact]
    public async Task GetAllForAdminAsync_IncludesTodosFromEveryUser()
    {
        await using var fixture = await CreateFixtureAsync();

        var todos = await fixture.Repository.GetAllForAdminAsync();

        Assert.Equal(2, todos.Count);
        Assert.Contains(todos, todo => todo.OwnerId == "user-a");
        Assert.Contains(todos, todo => todo.OwnerId == "user-b");
    }

    private static async Task<RepositoryFixture> CreateFixtureAsync()
    {
        var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<TodoDbContext>()
            .UseSqlite(connection)
            .Options;

        var userATodo = Todo.Create("user-a", "Todo A", null);
        var userBTodo = Todo.Create("user-b", "Todo B", null);
        await using (var dbContext = new TodoDbContext(options))
        {
            await dbContext.Database.EnsureCreatedAsync();
            dbContext.Users.AddRange(
                new ApplicationUser { Id = "user-a", UserName = "user-a" },
                new ApplicationUser { Id = "user-b", UserName = "user-b" });
            dbContext.Todos.AddRange(userATodo, userBTodo);
            await dbContext.SaveChangesAsync();
        }

        var repository = new EfTodoRepository(new TestDbContextFactory(options));
        return new RepositoryFixture(connection, repository, userATodo, userBTodo);
    }

    private sealed class TestDbContextFactory(DbContextOptions<TodoDbContext> options)
        : IDbContextFactory<TodoDbContext>
    {
        public TodoDbContext CreateDbContext() => new(options);

        public Task<TodoDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(CreateDbContext());
        }
    }

    private sealed class RepositoryFixture(
        SqliteConnection connection,
        EfTodoRepository repository,
        Todo userATodo,
        Todo userBTodo) : IAsyncDisposable
    {
        public EfTodoRepository Repository { get; } = repository;

        public Todo UserATodo { get; } = userATodo;

        public Todo UserBTodo { get; } = userBTodo;

        public ValueTask DisposeAsync() => connection.DisposeAsync();
    }
}
