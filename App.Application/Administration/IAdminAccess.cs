namespace App.Application.Administration;

public interface IAdminAccess
{
    Task<string> RequireAdminUserIdAsync(CancellationToken cancellationToken = default);
}
