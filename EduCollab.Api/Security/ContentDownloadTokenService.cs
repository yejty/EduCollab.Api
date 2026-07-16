using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using EduCollab.Api.Config;
using EduCollab.Application.Security;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace EduCollab.Api.Security
{
    public sealed class ContentDownloadTokenService : IContentDownloadTokenService
    {
        public const string TokenAudience = "educollab-content-download";
        public const string PurposeAssetContent = "asset-content";
        public const string PurposeSceneContent = "scene-content";
        public const int ExpirationMinutes = 10;

        private readonly JwtOptions _options;

        public ContentDownloadTokenService(IOptions<JwtOptions> options)
        {
            _options = options.Value;
        }

        public string CreateAssetContentToken(int userId, int workspaceId, int sceneId, int assetId, int? flowId = null)
        {
            var claims = new List<Claim>
            {
                new(JwtRegisteredClaimNames.Sub, userId.ToString()),
                new("purpose", PurposeAssetContent),
                new("workspaceId", workspaceId.ToString()),
                new("sceneId", sceneId.ToString()),
                new("assetId", assetId.ToString()),
            };
            if (flowId is > 0)
                claims.Add(new Claim("flowId", flowId.Value.ToString()));

            return WriteToken(claims);
        }

        public bool TryValidateAssetContentToken(
            string token,
            int userId,
            out int workspaceId,
            out int sceneId,
            out int assetId,
            out int? flowId)
        {
            workspaceId = 0;
            sceneId = 0;
            assetId = 0;
            flowId = null;

            if (!TryValidatePrincipal(token, userId, out var principal))
                return false;

            var purpose = principal.FindFirst("purpose")?.Value;
            if (!string.Equals(purpose, PurposeAssetContent, StringComparison.Ordinal))
                return false;

            if (!int.TryParse(principal.FindFirst("workspaceId")?.Value, out workspaceId) || workspaceId <= 0)
                return false;

            if (!int.TryParse(principal.FindFirst("sceneId")?.Value, out sceneId) || sceneId <= 0)
                return false;

            if (!int.TryParse(principal.FindFirst("assetId")?.Value, out assetId) || assetId <= 0)
                return false;

            var flowIdRaw = principal.FindFirst("flowId")?.Value;
            if (!string.IsNullOrWhiteSpace(flowIdRaw)
                && int.TryParse(flowIdRaw, out var parsedFlowId)
                && parsedFlowId > 0)
            {
                flowId = parsedFlowId;
            }

            return true;
        }

        public string CreateSceneContentToken(int userId, int workspaceId, int flowId, int sceneId)
        {
            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, userId.ToString()),
                new Claim("purpose", PurposeSceneContent),
                new Claim("workspaceId", workspaceId.ToString()),
                new Claim("flowId", flowId.ToString()),
                new Claim("sceneId", sceneId.ToString()),
            };

            return WriteToken(claims);
        }

        public bool TryValidateSceneContentToken(
            string token,
            int userId,
            out int workspaceId,
            out int flowId,
            out int sceneId)
        {
            workspaceId = 0;
            flowId = 0;
            sceneId = 0;

            if (!TryValidatePrincipal(token, userId, out var principal))
                return false;

            var purpose = principal.FindFirst("purpose")?.Value;
            if (!string.Equals(purpose, PurposeSceneContent, StringComparison.Ordinal))
                return false;

            if (!int.TryParse(principal.FindFirst("workspaceId")?.Value, out workspaceId) || workspaceId <= 0)
                return false;

            if (!int.TryParse(principal.FindFirst("flowId")?.Value, out flowId) || flowId <= 0)
                return false;

            if (!int.TryParse(principal.FindFirst("sceneId")?.Value, out sceneId) || sceneId <= 0)
                return false;

            return true;
        }

        private string WriteToken(IEnumerable<Claim> claims)
        {
            var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.SecretKey));
            var credentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);

            var jwt = new JwtSecurityToken(
                issuer: _options.Issuer,
                audience: TokenAudience,
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(ExpirationMinutes),
                signingCredentials: credentials);

            return new JwtSecurityTokenHandler().WriteToken(jwt);
        }

        private bool TryValidatePrincipal(string token, int userId, out ClaimsPrincipal principal)
        {
            principal = new ClaimsPrincipal();
            if (string.IsNullOrWhiteSpace(token))
                return false;

            var handler = new JwtSecurityTokenHandler();
            try
            {
                principal = handler.ValidateToken(
                    token,
                    new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidIssuer = _options.Issuer,
                        ValidateAudience = true,
                        ValidAudience = TokenAudience,
                        ValidateIssuerSigningKey = true,
                        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.SecretKey)),
                        ValidateLifetime = true,
                        ClockSkew = TimeSpan.FromSeconds(30),
                    },
                    out _);

                var sub = principal.FindFirst(JwtRegisteredClaimNames.Sub)?.Value
                    ?? principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                return int.TryParse(sub, out var tokenUserId) && tokenUserId == userId;
            }
            catch (SecurityTokenException)
            {
                return false;
            }
            catch (ArgumentException)
            {
                return false;
            }
        }
    }
}
