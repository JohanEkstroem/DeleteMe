using System.Security.Claims;
using App.Infrastructure.Identity;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Server;
using Microsoft.AspNetCore.Identity;

namespace App.Web.Services;

public sealed class RevalidatingIdentityAuthenticationStateProvider(
    ILoggerFactory loggerFactory,
    IServiceScopeFactory scopeFactory)
    : RevalidatingServerAuthenticationStateProvider(loggerFactory)
{
    protected override TimeSpan RevalidationInterval => TimeSpan.FromMinutes(5);

    protected override async Task<bool> ValidateAuthenticationStateAsync(
        AuthenticationState authenticationState,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        using var scope = scopeFactory.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var user = await userManager.GetUserAsync(authenticationState.User);
        if (user is null || await userManager.IsLockedOutAsync(user))
        {
            return false;
        }

        var securityStampClaim = authenticationState.User.FindFirstValue(
            userManager.Options.ClaimsIdentity.SecurityStampClaimType);
        if (securityStampClaim is null)
        {
            return false;
        }

        var currentSecurityStamp = await userManager.GetSecurityStampAsync(user);
        return string.Equals(securityStampClaim, currentSecurityStamp, StringComparison.Ordinal);
    }
}
