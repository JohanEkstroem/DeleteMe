namespace App.Application.Todos;

public sealed record TodoItem(Guid Id, string Title, string? Description, bool IsCompleted);
