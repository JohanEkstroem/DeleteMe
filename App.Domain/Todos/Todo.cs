namespace App.Domain.Todos;

public sealed class Todo
{
    private Todo()
    {
    }

    private Todo(string title, string? description)
    {
        Id = Guid.NewGuid();
        UpdateDetails(title, description);
    }

    public Guid Id { get; private set; }

    public string Title { get; private set; } = string.Empty;

    public string? Description { get; private set; }

    public bool IsCompleted { get; private set; }

    public static Todo Create(string title, string? description)
    {
        return new Todo(title, description);
    }

    public void UpdateDetails(string title, string? description)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            throw new ArgumentException("Titeln får inte vara tom.", nameof(title));
        }

        Title = title.Trim();
        Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
    }

    public void SetCompleted(bool isCompleted)
    {
        IsCompleted = isCompleted;
    }
}
