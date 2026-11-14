using App.Application.Administration;
using App.Application.Todos;
using App.Domain.Todos;

namespace App.Application.Tests.Administration;

public sealed class AdminManagementServiceTests
{
    [Fact]
    public async Task GetDashboardAsync_AggregatesUsersAndTodos()
    {
        var users = new FakeAdminUserRepository(
        [
            new AdminUserAccount("user-a", "a@example.test", false, false),
            new AdminUserAccount("user-b", "b@example.test", true, true)
        ]);
        var userTodo = Todo.Create("user-a", "Task", null);
        userTodo.SetCompleted(true);
        var todos = new FakeTodoRepository([userTodo]);
        var service = CreateService("admin-id", users, todos);

        var dashboard = await service.GetDashboardAsync();

        Assert.Equal(2, dashboard.UserCount);
        Assert.Equal(1, dashboard.TodoCount);
        Assert.Equal(1, dashboard.CompletedTodoCount);
        Assert.Equal(1, dashboard.Users.Single(user => user.Id == "user-a").CompletedTodoCount);
        Assert.Equal("a@example.test", dashboard.Todos.Single().OwnerEmail);
    }

    [Fact]
    public async Task SetUserDisabledAsync_RejectsDisablingLastActiveAdmin()
    {
        var users = new FakeAdminUserRepository(
        [
            new AdminUserAccount("target-admin", "admin@example.test", false, true)
        ]);
        var service = CreateService("another-admin", users, new FakeTodoRepository([]));

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.SetUserDisabledAsync("target-admin", true));

        Assert.Contains("sista aktiva administratören", exception.Message);
        Assert.False(users.SetDisabledCalled);
    }

    [Fact]
    public async Task DeleteUserAsync_RejectsDeletingCurrentAdmin()
    {
        var users = new FakeAdminUserRepository(
        [
            new AdminUserAccount("admin-id", "admin@example.test", false, true)
        ]);
        var service = CreateService("admin-id", users, new FakeTodoRepository([]));

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.DeleteUserAsync("admin-id"));

        Assert.False(users.DeleteCalled);
    }

    [Fact]
    public async Task GetDashboardAsync_RejectsNonAdminBeforeReadingRepositories()
    {
        var users = new FakeAdminUserRepository([]);
        var todos = new FakeTodoRepository([]);
        var service = CreateService(null, users, todos);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => service.GetDashboardAsync());

        Assert.False(users.GetAllCalled);
        Assert.False(todos.GetAllForAdminCalled);
    }

    private static AdminManagementService CreateService(
        string? currentAdminId,
        FakeAdminUserRepository users,
        FakeTodoRepository todos) =>
        new(new FakeAdminAccess(currentAdminId), users, todos);

    private sealed class FakeAdminAccess(string? currentAdminId) : IAdminAccess
    {
        public Task<string> RequireAdminUserIdAsync(CancellationToken cancellationToken = default)
        {
            if (currentAdminId is null)
            {
                throw new UnauthorizedAccessException();
            }

            return Task.FromResult(currentAdminId);
        }
    }

    private sealed class FakeAdminUserRepository(IEnumerable<AdminUserAccount> accounts)
        : IAdminUserRepository
    {
        private readonly List<AdminUserAccount> _accounts = accounts.ToList();

        public bool GetAllCalled { get; private set; }

        public bool SetDisabledCalled { get; private set; }

        public bool DeleteCalled { get; private set; }

        public Task<IReadOnlyList<AdminUserAccount>> GetAllAsync(
            CancellationToken cancellationToken = default)
        {
            GetAllCalled = true;
            return Task.FromResult<IReadOnlyList<AdminUserAccount>>(_accounts);
        }

        public Task<AdminUserAccount?> GetByIdAsync(
            string id,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(_accounts.SingleOrDefault(user => user.Id == id));

        public Task SetDisabledAsync(
            string id,
            bool disabled,
            CancellationToken cancellationToken = default)
        {
            SetDisabledCalled = true;
            return Task.CompletedTask;
        }

        public Task DeleteAsync(string id, CancellationToken cancellationToken = default)
        {
            DeleteCalled = true;
            return Task.CompletedTask;
        }
    }

    private sealed class FakeTodoRepository(IEnumerable<Todo> items) : ITodoRepository
    {
        private readonly List<Todo> _items = items.ToList();

        public bool GetAllForAdminCalled { get; private set; }

        public Task<IReadOnlyList<Todo>> GetAllForUserAsync(
            string ownerId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<Todo>>(_items.Where(todo => todo.OwnerId == ownerId).ToArray());

        public Task<IReadOnlyList<Todo>> GetAllForAdminAsync(
            CancellationToken cancellationToken = default)
        {
            GetAllForAdminCalled = true;
            return Task.FromResult<IReadOnlyList<Todo>>(_items);
        }

        public Task<Todo?> GetByIdForUserAsync(
            Guid id,
            string ownerId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(_items.SingleOrDefault(todo => todo.Id == id && todo.OwnerId == ownerId));

        public Task<Todo?> GetByIdForAdminAsync(
            Guid id,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(_items.SingleOrDefault(todo => todo.Id == id));

        public Task AddAsync(Todo todo, CancellationToken cancellationToken = default)
        {
            _items.Add(todo);
            return Task.CompletedTask;
        }

        public Task UpdateAsync(Todo todo, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task DeleteAsync(Todo todo, CancellationToken cancellationToken = default)
        {
            _items.Remove(todo);
            return Task.CompletedTask;
        }
    }
}
