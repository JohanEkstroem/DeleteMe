using App.Domain.Todos;

namespace App.Domain.Tests.Todos;

public sealed class TodoTests
{
    private const string OwnerId = "test-user";

    [Fact]
    public void Create_TrimsTitleAndDescription()
    {
        var todo = Todo.Create(OwnerId, "  Buy milk  ", "  Oat milk  ");

        Assert.Equal(OwnerId, todo.OwnerId);
        Assert.Equal("Buy milk", todo.Title);
        Assert.Equal("Oat milk", todo.Description);
        Assert.False(todo.IsCompleted);
    }

    [Fact]
    public void Create_RejectsBlankTitle()
    {
        var exception = Assert.Throws<ArgumentException>(() => Todo.Create(OwnerId, "  ", null));

        Assert.Equal("Titeln får inte vara tom. (Parameter 'title')", exception.Message);
    }

    [Fact]
    public void Create_RejectsBlankOwner()
    {
        var exception = Assert.Throws<ArgumentException>(() => Todo.Create("  ", "Buy milk", null));

        Assert.Equal("Ägare måste anges. (Parameter 'ownerId')", exception.Message);
    }

    [Fact]
    public void UpdateDetails_RejectsBlankTitleAndClearsBlankDescription()
    {
        var todo = Todo.Create(OwnerId, "Buy milk", "Oat milk");

        todo.UpdateDetails("Buy bread", "  ");

        Assert.Equal("Buy bread", todo.Title);
        Assert.Null(todo.Description);
    }

    [Fact]
    public void SetCompleted_CanBeToggled()
    {
        var todo = Todo.Create(OwnerId, "Buy milk", null);

        todo.SetCompleted(true);
        Assert.True(todo.IsCompleted);

        todo.SetCompleted(false);
        Assert.False(todo.IsCompleted);
    }
}
