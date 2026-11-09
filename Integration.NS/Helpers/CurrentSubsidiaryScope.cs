using System.Globalization;
using Microsoft.AspNetCore.Http;

namespace Integration.NS.Helpers;

/// <summary>The signed-in subsidiary, rather than all subsidiaries the employee can access.</summary>
internal static class CurrentSubsidiaryScope
{
    public static int RequireId(IHttpContextAccessor accessor)
    {
        var user = accessor.HttpContext?.User;
        if (user?.Identity?.IsAuthenticated != true ||
            !int.TryParse(user.FindFirst("NsSubsidiaryId")?.Value, NumberStyles.None,
                CultureInfo.InvariantCulture, out var subsidiaryId) || subsidiaryId <= 0)
        {
            throw new UnauthorizedAccessException("Authenticated NetSuite subsidiary identity is unavailable. Please sign in again.");
        }

        return subsidiaryId;
    }
}
