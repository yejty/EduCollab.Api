using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using EduCollab.Contracts.Requests.Scenes;
using EduCollab.Contracts.Requests.Sessions;
using EduCollab.Contracts.Responses.Scenes;
using EduCollab.Contracts.Responses.Sessions;

namespace EduCollab.Api.Tests.Integration;

[Trait("Category", "Integration")]
public sealed class LiveSessionsIntegrationTests
{
    [Fact]
    public async Task CreateSession_JoinTicket_Bootstrap_AndGuestJoin_Work()
    {
        await using var factory = await PostgresIntegrationApiFactory.CreateInitializedAsync();
        using var hostClient = factory.CreateClient();
        using var guestClient = factory.CreateClient();

        var hostEmail = $"host-{Guid.NewGuid():N}@example.com";
        const string hostPassword = "Host123!";

        var hostTokens = await hostClient.RegisterAndConfirmAsync(factory, "Host User", hostEmail, hostPassword);
        hostClient.SetBearerToken(hostTokens.AccessToken);

        await hostClient.CreateApprovedWorkspaceAsync(
            factory,
            hostEmail,
            "Live Sessions Workspace",
            "Phase 3/4 live sessions");

        var createSceneResponse = await hostClient.PostAsJsonAsync("/api/workspace/scenes", new CreateSceneRequest
        {
            Name = "Lab Scene",
            JsonContent = JsonNode.Parse("""{ "objects": [] }"""),
        });
        createSceneResponse.EnsureSuccessStatusCode();
        var scene = await createSceneResponse.ReadAsJsonAsync<SceneResponse>();

        var createSessionResponse = await hostClient.PostAsJsonAsync("/api/workspace/sessions", new CreateLiveSessionRequest
        {
            Name = "Physics lab",
            SceneId = checked((int)scene.Id),
            IncludeAssets = false,
            AllowGuestLink = true,
            DefaultRole = "viewer",
        });
        createSessionResponse.EnsureSuccessStatusCode();
        Assert.Equal(HttpStatusCode.Created, createSessionResponse.StatusCode);

        var created = await createSessionResponse.ReadAsJsonAsync<LiveSessionResponse>();
        Assert.Equal("Pending", created.Status);
        Assert.True(created.AllowGuestLink);
        Assert.False(string.IsNullOrWhiteSpace(created.GuestLinkUrl));
        Assert.Contains("/sessions/join/", created.GuestLinkUrl);

        var guestToken = created.GuestLinkUrl!.Split('/').Last();

        var joinTicketResponse = await hostClient.PostAsync(
            $"/api/workspace/sessions/{created.Id}/join-ticket",
            null);
        joinTicketResponse.EnsureSuccessStatusCode();
        var hostTicket = await joinTicketResponse.ReadAsJsonAsync<SessionJoinTicketResponse>();
        Assert.False(string.IsNullOrWhiteSpace(hostTicket.JoinTicket));
        Assert.Equal("host", hostTicket.Role);
        Assert.Equal("host", hostTicket.ColyseusRole);

        var bootstrapResponse = await hostClient.GetAsync($"/api/workspace/sessions/{created.Id}/bootstrap");
        bootstrapResponse.EnsureSuccessStatusCode();
        var bootstrap = await bootstrapResponse.ReadAsJsonAsync<SessionBootstrapResponse>();
        Assert.Equal(created.Id, bootstrap.SessionId);
        Assert.Equal("scene", bootstrap.AssetKind);
        Assert.Single(bootstrap.Scenes);
        Assert.Equal(checked((int)scene.Id), bootstrap.Scenes[0].SceneId);

        var guestJoinResponse = await guestClient.PostAsJsonAsync("/api/public/sessions/join", new PublicSessionJoinRequest
        {
            GuestToken = Uri.UnescapeDataString(guestToken),
            DisplayName = "Guest Anna",
        });
        guestJoinResponse.EnsureSuccessStatusCode();
        var guestTicket = await guestJoinResponse.ReadAsJsonAsync<SessionJoinTicketResponse>();
        Assert.False(string.IsNullOrWhiteSpace(guestTicket.JoinTicket));
        Assert.Equal(created.Id, guestTicket.SessionId);
        Assert.Equal("guest", guestTicket.Role);
        Assert.Equal("viewer", guestTicket.ColyseusRole);

        var guestBootstrapResponse = await guestClient.GetAsync(
            $"/api/public/sessions/bootstrap?guestToken={Uri.EscapeDataString(Uri.UnescapeDataString(guestToken))}");
        guestBootstrapResponse.EnsureSuccessStatusCode();
        var guestBootstrap = await guestBootstrapResponse.ReadAsJsonAsync<SessionBootstrapResponse>();
        Assert.Equal(created.Id, guestBootstrap.SessionId);
        Assert.Single(guestBootstrap.Scenes);

        var roomStarted = await guestClient.PostAsync(
            $"/api/internal/sessions/{created.Id}/room-started",
            null);
        Assert.Equal(HttpStatusCode.Unauthorized, roomStarted.StatusCode);

        using var internalRequest = new HttpRequestMessage(
            HttpMethod.Post,
            $"/api/internal/sessions/{created.Id}/room-started");
        internalRequest.Headers.Add("X-Api-Key", "integration-tests-internal-api-key");
        var roomStartedOk = await guestClient.SendAsync(internalRequest);
        Assert.Equal(HttpStatusCode.NoContent, roomStartedOk.StatusCode);

        var detailResponse = await hostClient.GetAsync($"/api/workspace/sessions/{created.Id}");
        detailResponse.EnsureSuccessStatusCode();
        var detail = await detailResponse.ReadAsJsonAsync<LiveSessionResponse>();
        Assert.Equal("Active", detail.Status);

        var endResponse = await hostClient.PostAsync($"/api/workspace/sessions/{created.Id}/end", null);
        endResponse.EnsureSuccessStatusCode();
        var ended = await endResponse.ReadAsJsonAsync<LiveSessionResponse>();
        Assert.Equal("Ended", ended.Status);
    }

    [Fact]
    public async Task SessionAssets_WithoutIncludeAssets_ReturnsForbiddenOnDownload()
    {
        await using var factory = await PostgresIntegrationApiFactory.CreateInitializedAsync();
        using var client = factory.CreateClient();

        var email = $"host-{Guid.NewGuid():N}@example.com";
        var tokens = await client.RegisterAndConfirmAsync(factory, "Host User", email, "Host123!");
        client.SetBearerToken(tokens.AccessToken);
        await client.CreateApprovedWorkspaceAsync(factory, email, "Assets Gate Workspace");

        var asset = await client.PostAssetAsync("Hidden", groupId: null);
        var sceneJson = JsonNode.Parse(
            $$"""
              { "objects": [ { "assetId": {{asset.Id}} } ] }
              """);

        var createSceneResponse = await client.PostAsJsonAsync("/api/workspace/scenes", new CreateSceneRequest
        {
            Name = "Scene With Asset",
            JsonContent = sceneJson,
        });
        createSceneResponse.EnsureSuccessStatusCode();
        var scene = await createSceneResponse.ReadAsJsonAsync<SceneResponse>();

        var createSessionResponse = await client.PostAsJsonAsync("/api/workspace/sessions", new CreateLiveSessionRequest
        {
            Name = "No assets session",
            SceneId = checked((int)scene.Id),
            IncludeAssets = false,
        });
        createSessionResponse.EnsureSuccessStatusCode();
        var session = await createSessionResponse.ReadAsJsonAsync<LiveSessionResponse>();

        var contentResponse = await client.GetAsync(
            $"/api/workspace/session-assets/content?sessionId={session.Id}&sceneId={scene.Id}&assetId={asset.Id}");
        Assert.Equal(HttpStatusCode.Forbidden, contentResponse.StatusCode);
    }
}
