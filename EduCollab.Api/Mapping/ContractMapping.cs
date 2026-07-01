using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using EduCollab.Application.Models;
using EduCollab.Application.Services.Content;
using EduCollab.Contracts.Requests.Assets;
using EduCollab.Contracts.Requests.Groups;
using EduCollab.Contracts.Requests.Flows;
using EduCollab.Contracts.Requests.Users;
using EduCollab.Contracts.Requests.Workspaces;
using EduCollab.Contracts.Responses.Assets;
using EduCollab.Contracts.Responses.Groups;
using EduCollab.Contracts.Requests.Scenes;
using EduCollab.Contracts.Responses.Flows;
using EduCollab.Contracts.Responses.Scenes;
using EduCollab.Contracts.Responses.Users;
using EduCollab.Contracts.Responses.Workspaces;

namespace EduCollab.Api.Mapping
{
    public static class ContractMapping
    {
        private static string RequireJsonContent(JsonNode? jsonContent, string paramName)
        {
            if (jsonContent is null)
                throw new ArgumentException($"{paramName} is required.", paramName);

            return jsonContent.ToJsonString();
        }

        private static JsonNode? ParseJsonContent(string? jsonContent)
        {
            if (string.IsNullOrWhiteSpace(jsonContent))
                return null;

            try
            {
                return JsonNode.Parse(jsonContent)
                    ?? throw new InvalidOperationException("Scene json content cannot be null.");
            }
            catch (JsonException ex)
            {
                throw new InvalidOperationException("Stored scene json content is invalid.", ex);
            }
        }

        public static User MapToUser(this RegisterUserRequest request)
        {
            return new User
            {
                FirstName = request.FirstName,
                LastName = request.LastName,
                Email = request.Email
            };
        }

        public static User MapToUser(this UpdateUserProfileRequest request, int id)
        {
            return new User
            {
                Id = id,
                FirstName = request.FirstName,
                LastName = request.LastName,
            };
        }

        public static Workspace MapToWorkspace(this CreateWorkspaceRequest request)
        {
            return new Workspace
            {
                Name = request.Name,
                Description = request.Description
            };
        }

        public static Workspace MapToWorkspace(this UpdateWorkspaceRequest request, int id)
        {
            return new Workspace
            {
                Id = id,
                Name = request.Name,
                Description = request.Description
            };
        }

        public static Workspace MapToWorkspace(this UpdateWorkspaceRequest request)
        {
            return new Workspace
            {
                Name = request.Name,
                Description = request.Description
            };
        }

        public static Group MapToGroup(this CreateGroupRequest request)
        {
            return new Group
            {
                Name = request.Name,
                Description = request.Description,
                ParentGroupId = request.ParentGroupId,
            };
        }

        public static Group MapToGroup(this UpdateGroupRequest request, int groupId)
        {
            return new Group
            {
                Id = groupId,
                Name = request.Name ?? string.Empty,
                Description = request.Description,
                ParentGroupId = request.ParentGroupId,
            };
        }

        public static Asset MapToAsset(this CreateAssetRequest request)
        {
            return new Asset
            {
                Name = request.Name,
                Description = request.Description,
                GroupId = request.GroupId,
                AssetType = request.AssetType,
            };
        }

        public static Asset MapToAsset(this UpdateAssetRequest request, int assetId)
        {
            return new Asset
            {
                Id = assetId,
                Name = request.Name,
                Description = request.Description,
                GroupId = request.GroupId,
                AssetType = request.AssetType,
            };
        }

        public static Scene MapToScene(this CreateSceneRequest request)
        {
            return new Scene
            {
                Name = request.Name,
                Description = request.Description,
                JsonContent = RequireJsonContent(request.JsonContent, nameof(request.JsonContent))
            };
        }

        public static Scene MapToScene(this UpdateSceneRequest request, int sceneId)
        {
            return new Scene
            {
                Id = sceneId,
                Name = request.Name,
                Description = request.Description,
                GroupId = request.GroupId,
                JsonContent = RequireJsonContent(request.JsonContent, nameof(request.JsonContent))
            };
        }

        public static Flow MapToFlow(this CreateFlowRequest request)
        {
            return new Flow
            {
                Name = request.Name,
                Description = request.Description,
                GroupId = request.GroupId,
            };
        }

        public static Flow MapToFlow(this UpdateFlowRequest request, int flowId)
        {
            return new Flow
            {
                Id = flowId,
                Name = request.Name,
                Description = request.Description,
                GroupId = request.GroupId,
            };
        }

        public static UserResponse MapToResponse(this User user)
        {
            return new UserResponse
            {
                Id = user.Id,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Email = user.Email,
                WorkspaceId = user.WorkspaceId,
            };
        }

        public static UserWorkspacesResponse MapToWorkspacesResponse(
            this IEnumerable<Workspace> workspaces,
            IEnumerable<WorkspaceMember> memberships,
            int? activeWorkspaceId)
        {
            var membershipByWorkspaceId = memberships.ToDictionary(m => m.WorkspaceId);
            return new UserWorkspacesResponse
            {
                Workspaces = workspaces
                    .Select(workspace =>
                    {
                        var membership = membershipByWorkspaceId[workspace.Id];
                        return new UserWorkspaceMembershipResponse
                        {
                            WorkspaceId = workspace.Id,
                            WorkspaceName = workspace.Name,
                            Role = membership.Role.ToString(),
                            JoinedAt = membership.JoinedAtUtc,
                            IsActive = activeWorkspaceId == workspace.Id,
                        };
                    })
                    .ToList(),
            };
        }

        public static TokensResponse MapToResponse(this (string AccessToken, string RefreshToken) tokens)
        {
            return new TokensResponse
            {
                AccessToken = tokens.AccessToken,
                RefreshToken = tokens.RefreshToken
            };
        }
        public static WorkspaceResponse MapToResponse(this Workspace workspace)
        {
            return new WorkspaceResponse
            {
                Id = workspace.Id,
                Name = workspace.Name,
                Description = workspace.Description,
                CreatedAt = workspace.CreatedAtUtc,
                UpdatedAt = workspace.UpdatedAtUtc,
                CreatedByUserId = workspace.CreatedByUserId,
                IsArchived = workspace.IsArchived,
            };
        }

        public static WorkspacesResponse MapToResponse(this IEnumerable<Workspace> workspaces)
        {
            return new WorkspacesResponse
            {
                Workspaces = workspaces.Select(w => w.MapToResponse()).ToList(),
            };
        }

        public static WorkspaceMemberResponse MapToResponse(this WorkspaceMember workspaceMember)
        {
            return new WorkspaceMemberResponse
            {
                UserId = workspaceMember.UserId,
                Role = workspaceMember.Role.ToString(),
                JoinedAt = workspaceMember.JoinedAtUtc
            };
        }

        public static WorkspaceMembersResponse MapToResponse(this List<WorkspaceMember> workspaceMembers)
        {
            return new WorkspaceMembersResponse
            {
                Members = workspaceMembers.Select(m => m.MapToResponse()).ToList(),
            };
        }

        public static WorkspaceCreationRequestResponse MapToResponse(this WorkspaceCreationRequest request)
        {
            return new WorkspaceCreationRequestResponse
            {
                Id = request.Id,
                Name = request.Name,
                Description = request.Description,
                Status = request.Status.ToString(),
                CreatedAt = request.CreatedAtUtc,
                ReviewedAt = request.ReviewedAtUtc,
                DenialReason = request.DenialReason,
            };
        }

        public static WorkspaceCreationRequestsResponse MapToResponse(this IEnumerable<WorkspaceCreationRequest> requests)
        {
            return new WorkspaceCreationRequestsResponse
            {
                Requests = requests.Select(r => r.MapToResponse()).ToList(),
            };
        }

        public static WorkspaceMember MapToWorkspaceMember(this UpdateWorkspaceMemberRequest request, int id, int userId)
        {
            if (!WorkspaceRoleExtensions.TryFromPersisted(request.Role, out var role))
            {
                throw new ArgumentException($"Invalid workspace role '{request.Role}'.");
            }

            return new WorkspaceMember
            {
                UserId = userId,
                WorkspaceId = id,
                Role = role
            };
        }

        public static GroupResponse MapToResponse(this Group group) 
        {
            return new GroupResponse
            {
                Id = group.Id,
                ParentGroupId = group.ParentGroupId,
                Name = group.Name,
                Description = group.Description,
                Path = group.Path,
                CreatedAt = group.CreatedAtUtc,
                CreatedByUserId = group.CreatedByUserId,
                UserCount = group.UserCount
            };
        }

        public static GroupMember MapToGroupMember(this CreateGroupMemberRequest request, int groupId)
        {
            return new GroupMember
            {
                GroupId = groupId,
                UserId = request.UserId,
                JoinedAtUtc = DateTime.UtcNow
            };
        }

        public static GroupMemberResponse MapToResponse(this GroupMember member, string workspaceRole)
        {
            return new GroupMemberResponse
            {
                UserId = member.UserId,
                Role = workspaceRole,
                JoinedAt = member.JoinedAtUtc
            };
        }

        public static GroupMembersResponse MapToResponse(this List<GroupMember> members, IReadOnlyDictionary<int, string> workspaceRolesByUserId)
        {
            return new GroupMembersResponse
            {
                Members = members
                    .Select(m => m.MapToResponse(
                        workspaceRolesByUserId.TryGetValue(m.UserId, out var role) ? role : string.Empty))
                    .ToList()
            };
        }

        public static AssetResponse MapToResponse(this Asset asset)
        {
            return new AssetResponse
            {
                Id = asset.Id,
                WorkspaceId = asset.WorkspaceId,
                GroupId = asset.GroupId,
                GroupIds = asset.GroupIds.Count > 0
                    ? asset.GroupIds.ToList()
                    : ResourceGroupPlacement.EffectiveGroupIds(asset.GroupIds, asset.GroupId).ToList(),
                OwnerUserId = asset.OwnerUserId,
                Name = asset.Name,
                Description = asset.Description,
                AssetType = asset.AssetType,
                StorageUrl = asset.StorageUrl,
                CreatedAt = asset.CreatedAtUtc,
                UpdatedAt = asset.UpdatedAtUtc
            };
        }

        public static AssetsResponse MapToResponse(this IEnumerable<Asset> assets)
        {
            return new AssetsResponse
            {
                Assets = assets.Select(a => a.MapToResponse()).ToList()
            };
        }

        public static SceneResponse MapToResponse(this Scene scene)
        {
            return new SceneResponse
            {
                Id = scene.Id,
                WorkspaceId = scene.WorkspaceId,
                OwnerUserId = scene.OwnerUserId,
                GroupId = scene.GroupId,
                GroupIds = scene.GroupIds.Count > 0
                    ? scene.GroupIds.ToList()
                    : ResourceGroupPlacement.EffectiveGroupIds(scene.GroupIds, scene.GroupId).ToList(),
                Name = scene.Name,
                Description = scene.Description,
                JsonContent = ParseJsonContent(scene.JsonContent),
                CreatedAt = scene.CreatedAtUtc,
                UpdatedAt = scene.UpdatedAtUtc
            };
        }

        public static FlowResponse MapToResponse(this Flow flow)
        {
            return new FlowResponse
            {
                Id = flow.Id,
                WorkspaceId = flow.WorkspaceId,
                OwnerUserId = flow.OwnerUserId,
                GroupId = flow.GroupId,
                GroupIds = flow.GroupIds.Count > 0
                    ? flow.GroupIds.ToList()
                    : ResourceGroupPlacement.EffectiveGroupIds(flow.GroupIds, flow.GroupId).ToList(),
                Name = flow.Name,
                Description = flow.Description,
                CreatedAt = flow.CreatedAtUtc,
                UpdatedAt = flow.UpdatedAtUtc
            };
        }

        public static FlowsResponse MapToResponse(this IEnumerable<Flow> flows)
        {
            return new FlowsResponse
            {
                Flows = flows.Select(f => f.MapToResponse()).ToList()
            };
        }

        public static FlowSceneResponse MapToResponse(this FlowSceneContextItem item) =>
            new()
            {
                SceneId = item.SceneId,
                FlowId = item.FlowId,
                WorkspaceId = item.WorkspaceId,
                Name = item.Name,
                GroupId = item.GroupId,
                UsableInFlow = item.UsableInFlow,
                CanViewDirectly = item.CanViewDirectly,
                ResolvedFrom = item.ResolvedFrom.ToString(),
            };

        public static FlowScenesResponse MapToResponse(this IEnumerable<FlowSceneContextItem> flowScenes) =>
            new()
            {
                Scenes = flowScenes.Select(static item => item.MapToResponse()).ToList()
            };

        public static ScenesResponse MapToResponse(this IEnumerable<Scene> scenes)
        {
            return new ScenesResponse
            {
                Scenes = scenes.Select(s => s.MapToResponse()).ToList()
            };
        }

        public static SceneAssetResponse MapToResponse(this SceneAssetContextItem item) =>
            new()
            {
                AssetId = item.AssetId,
                SceneId = item.SceneId,
                WorkspaceId = item.WorkspaceId,
                Name = item.Name,
                AssetType = item.AssetType,
                UsableInScene = item.UsableInScene,
                CanViewDirectly = item.CanViewDirectly,
                ResolvedFrom = item.ResolvedFrom.ToString(),
            };

        public static SceneAssetsResponse MapToResponse(this IEnumerable<SceneAssetContextItem> items) =>
            new()
            {
                Assets = items.Select(static item => item.MapToResponse()).ToList(),
            };

        public static GroupsResponse MapToResponse(this List<Group> groups)
        {
            return new GroupsResponse
            {
                Groups = groups.Select(g => g.MapToResponse()).ToList()
            };
        }
    }
}
