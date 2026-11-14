namespace App.Application.Administration;

public interface IAdminUserRepository
{
    Task<IReadOnlyList<AdminUserAccount>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<AdminUserAccount?> GetByIdAsync(string id, CancellationToken cancellationToken = default);

    Task SetDisabledAsync(
        string id,
        bool disabled,
        CancellationToken cancellationToken = default);

    Task DeleteAsync(string id, CancellationToken cancellationToken = default);
}
