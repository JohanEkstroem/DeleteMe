namespace App.Application.Todos;

public sealed class TodoRepositoryException(Exception innerException)
    : Exception("The todo repository could not complete the operation.", innerException)
{
}
