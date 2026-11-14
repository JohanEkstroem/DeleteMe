namespace App.Domain.Todos;

public sealed class Todo
{
    private Todo()
    {
    }

    private Todo(string ownerId, string title, string? description)
    {
        Id = Guid.NewGuid();
        if (string.IsNullOrWhiteSpace(ownerId))
        {
            throw new ArgumentException("Ägare måste anges.", nameof(ownerId));
        }

        OwnerId = ownerId;
        UpdateDetails(title, description);
    }

    public Guid Id { get; private set; }

    public string? OwnerId { get; private set; }

    public string Title { get; private set; } = string.Empty;

    public string? Description { get; private set; }

    public bool IsCompleted { get; private set; }

    public static Todo Create(string ownerId, string title, string? description)
    {
        return new Todo(ownerId, title, description);
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
