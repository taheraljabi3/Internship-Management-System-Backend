using System.Security.Claims;

namespace IMS.Api.Helpers
{
    public static class ClaimsPrincipalExtensions
    {
        public static long GetUserId(this ClaimsPrincipal user)
        {
            var value = user.FindFirstValue(ClaimTypes.NameIdentifier)
                        ?? user.FindFirstValue(ClaimTypes.Name)
                        ?? user.FindFirstValue("sub");

            if (string.IsNullOrWhiteSpace(value))
                throw new UnauthorizedAccessException("User id claim is missing.");

            return long.Parse(value);
        }

        public static string GetUserRole(this ClaimsPrincipal user)
        {
            return user.FindFirstValue(ClaimTypes.Role) ?? string.Empty;
        }

        public static string GetUserEmail(this ClaimsPrincipal user)
        {
            return user.FindFirstValue(ClaimTypes.Email) ?? string.Empty;
        }

        public static long? GetSessionId(this ClaimsPrincipal user)
        {
            var value = user.FindFirstValue("session_id");
            if (string.IsNullOrWhiteSpace(value))
                return null;

            return long.Parse(value);
        }
    }
}