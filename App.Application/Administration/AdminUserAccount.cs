namespace App.Application.Administration;

public sealed record AdminUserAccount(
    string Id,
    string Email,
    bool IsLocked,
    bool IsAdmin);
