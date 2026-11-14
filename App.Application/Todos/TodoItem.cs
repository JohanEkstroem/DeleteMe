namespace App.Application.Todos;

public sealed record TodoItem(
    Guid Id,
    string? OwnerId,
    string Title,
    string? Description,
    bool IsCompleted);
