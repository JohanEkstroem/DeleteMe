using System.Security.Claims;
using App.Application.Administration;
using Microsoft.AspNetCore.Components.Authorization;

namespace App.Web.Services;

public sealed class CurrentAdminAccess(AuthenticationStateProvider authenticationStateProvider)
    : IAdminAccess
{
    public async Task<string> RequireAdminUserIdAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var user = (await authenticationStateProvider.GetAuthenticationStateAsync()).User;
        if (!user.IsInRole("Admin"))
        {
            throw new UnauthorizedAccessException("Administratörsbehörighet krävs.");
        }

        return user.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new InvalidOperationException("The authenticated administrator has no identifier.");
    }
}
