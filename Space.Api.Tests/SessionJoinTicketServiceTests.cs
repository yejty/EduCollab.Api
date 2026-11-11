using Hub.Api.Security;
using Space.Application.Models;
using Space.Application.Services.Sessions;
using Microsoft.Extensions.Options;

namespace Space.Api.Tests;

public sealed class SessionJoinTicketServiceTests
{
    [Fact]
    public void CreateTicket_ContainsExpectedClaims()
    {
        var service = new SessionJoinTicketService(Options.Create(new SessionJoinSettings
        {
            SecretKey = "unit-test-session-join-secret-key-32chars!",
            Issuer = "Hub.Api",
            Audience = "Space.CollabServer",
            ExpirationMinutes = 10,
            ColyseusEndpoint = "ws://localhost:2567",
        }));

        var session = new LiveSession
        {
            Id = 101,
            WorkspaceId = 2,
            HostUserId = 15,
            SceneId = 42,
            SceneName = "Lab",
            Name = "Physics lab",
            IncludeAssets = true,
            DefaultRole = ColyseusRoles.Viewer,
        };

        var result = service.CreateTicket(
            session,
            subject: "user:15",
            displayName: "Anna",
            role: LiveSessionRoles.Host,
            colyseusRole: ColyseusRoles.Host);

        Assert.False(string.IsNullOrWhiteSpace(result.JoinTicket));
        Assert.Equal(101, result.SessionId);
        Assert.Equal("ws://localhost:2567", result.ColyseusEndpoint);
        Assert.Equal(LiveSessionRoles.Host, result.Role);
        Assert.Equal(ColyseusRoles.Host, result.ColyseusRole);
        Assert.True(result.ExpiresAtUtc > DateTime.UtcNow);
    }
}
