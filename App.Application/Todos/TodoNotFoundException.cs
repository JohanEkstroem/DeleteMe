namespace App.Application.Todos;

public sealed class TodoNotFoundException(Guid id)
    : Exception($"Todo med id {id} hittades inte.")
{
    public Guid Id { get; } = id;
}
