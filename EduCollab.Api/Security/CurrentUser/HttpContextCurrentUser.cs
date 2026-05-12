using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using EduCollab.Application.Identity;
using Microsoft.AspNetCore.Http;

namespace EduCollab.Api.Security.CurrentUser
{
    public sealed class HttpContextCurrentUser : ICurrentUser
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public HttpContextCurrentUser(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public int? UserId
        {
            get
            {
                var user = _httpContextAccessor.HttpContext?.User;
                if (user?.Identity?.IsAuthenticated != true)
                    return null;

                var idString = user.FindFirstValue(ClaimTypes.NameIdentifier)
                    ?? user.FindFirstValue(JwtRegisteredClaimNames.Sub);

                return int.TryParse(idString, out var id) ? id : null;
            }
        }
    }
}
