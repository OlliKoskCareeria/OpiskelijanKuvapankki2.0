using System.Security.Claims;

namespace OpiskelijanKuvapankki2_0.Extensions
{
    public static class ClaimsPrincipalExtensions
    {
        public static int GetUserId(this ClaimsPrincipal user)
        {
            var claim = user.FindFirst(ClaimTypes.NameIdentifier);

            if (claim == null)
                throw new UnauthorizedAccessException("User ID claim not found");

            return int.Parse(claim.Value);
        }
    }

}