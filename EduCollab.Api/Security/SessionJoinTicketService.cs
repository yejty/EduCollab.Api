using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using EduCollab.Application.Models;
using EduCollab.Application.Services.Sessions;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace EduCollab.Api.Security
{
    public sealed class SessionJoinTicketService : ISessionJoinTicketService
    {
        private readonly SessionJoinSettings _settings;

        public SessionJoinTicketService(IOptions<SessionJoinSettings> settings)
        {
            _settings = settings.Value;
        }

        public SessionJoinTicketResult CreateTicket(
            LiveSession session,
            string subject,
            string displayName,
            string role,
            string colyseusRole)
        {
            if (string.IsNullOrWhiteSpace(_settings.SecretKey))
                throw new InvalidOperationException("SessionJoin:SecretKey is not configured.");

            var expiresAt = DateTime.UtcNow.AddMinutes(Math.Max(1, _settings.ExpirationMinutes));
            var assetKind = session.FlowId is > 0 ? "flow" : session.SceneId is > 0 ? "scene" : string.Empty;
            var assetId = session.FlowId?.ToString()
                ?? session.SceneId?.ToString()
                ?? string.Empty;
            var assetName = session.FlowName ?? session.SceneName ?? string.Empty;

            var claims = new List<Claim>
            {
                new(JwtRegisteredClaimNames.Sub, subject),
                new("sessionId", session.Id.ToString()),
                new("workspaceId", session.WorkspaceId.ToString()),
                new("role", role),
                new("colyseusRole", colyseusRole),
                new("displayName", displayName),
                new("includeAssets", session.IncludeAssets ? "true" : "false"),
                new("assetKind", assetKind),
                new("assetId", assetId),
                new("assetName", assetName),
                new("sessionName", session.Name),
                new("hostUserId", session.HostUserId.ToString()),
            };

            var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_settings.SecretKey));
            var credentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);
            var jwt = new JwtSecurityToken(
                issuer: _settings.Issuer,
                audience: _settings.Audience,
                claims: claims,
                expires: expiresAt,
                signingCredentials: credentials);

            return new SessionJoinTicketResult
            {
                JoinTicket = new JwtSecurityTokenHandler().WriteToken(jwt),
                SessionId = session.Id,
                ColyseusEndpoint = string.IsNullOrWhiteSpace(_settings.ColyseusEndpoint)
                    ? null
                    : _settings.ColyseusEndpoint.Trim(),
                Role = role,
                ColyseusRole = colyseusRole,
                ExpiresAtUtc = expiresAt,
            };
        }
    }
}
