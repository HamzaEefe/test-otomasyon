using System.Security.Claims;

namespace TestOtomasyon.Helpers
{
    public static class ClaimsPrincipalExtensions
    {
        public static bool HasAuthority(this ClaimsPrincipal user, string authorityName)
        {
            if (user?.Identity == null || !user.Identity.IsAuthenticated)
                return false;

            return user.Claims.Any(c => c.Type == "Authority" && c.Value == authorityName);
        }

        public static string? GetUserId(this ClaimsPrincipal user)
        {
            return user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        }

        public static string? GetUserName(this ClaimsPrincipal user)
        {
            return user.FindFirst("UserName")?.Value;
        }

        public static string? GetDepartmentName(this ClaimsPrincipal user)
        {
            return user.FindFirst("DepartmentName")?.Value;
        }
    }
}