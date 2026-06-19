using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using EduCollab.Application.Models;
using EduCollab.Contracts.Requests.Assets;
using EduCollab.Contracts.Requests.Groups;
using EduCollab.Contracts.Requests.Scenes;
using EduCollab.Contracts.Requests.Users;
using EduCollab.Contracts.Requests.Workspaces;
using EduCollab.Contracts.Responses.Assets;
using EduCollab.Contracts.Responses.Groups;
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

        private static JsonNode ParseJsonContent(string jsonContent)
        {
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
            };
        }

        public static Group MapToGroup(this UpdateGroupRequest request, int groupId)
        {
            return new Group
            {
                Id = groupId,
                Name = request.Name ?? string.Empty,
                Description = request.Description,
            };
        }

        public static AssetFolder MapToAssetFolder(this CreateAssetFolderRequest request)
        {
            return new AssetFolder
            {
                Name = request.Name,
                ParentFolderId = request.ParentFolderId
            };
        }

        public static AssetFolder MapToAssetFolder(this UpdateAssetFolderRequest request, int folderId)
        {
            return new AssetFolder
            {
                Id = folderId,
                Name = request.Name,
                ParentFolderId = request.ParentFolderId
            };
        }

        public static Asset MapToAsset(this CreateAssetRequest request)
        {
            return new Asset
            {
                Name = request.Name,
                Description = request.Description,
                FolderId = request.FolderId,
                AssetType = request.AssetType,
                Version = request.Version,
            };
        }

        public static Asset MapToAsset(this UpdateAssetRequest request, int assetId)
        {
            return new Asset
            {
                Id = assetId,
                Name = request.Name,
                Description = request.Description,
                FolderId = request.FolderId,
                AssetType = request.AssetType,
                Version = request.Version,
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
                JsonContent = RequireJsonContent(request.JsonContent, nameof(request.JsonContent))
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
                CreatedAtUtc = workspace.CreatedAtUtc,
                UpdatedAtUtc = workspace.UpdatedAtUtc,
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
                CreatedAtUtc = request.CreatedAtUtc,
                ReviewedAtUtc = request.ReviewedAtUtc,
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
                Name = group.Name,
                Description = group.Description,
                CreatedAtUtc = group.CreatedAtUtc,
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
                JoinedAtUtc = member.JoinedAtUtc
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

        public static AssetFolderResponse MapToResponse(this AssetFolder folder)
        {
            return new AssetFolderResponse
            {
                Id = folder.Id,
                WorkspaceId = folder.WorkspaceId,
                ParentFolderId = folder.ParentFolderId,
                Name = folder.Name,
                Path = folder.Path,
                CreatedByUserId = folder.CreatedByUserId,
                CreatedAtUtc = folder.CreatedAtUtc,
                UpdatedAtUtc = folder.UpdatedAtUtc
            };
        }

        public static AssetFoldersResponse MapToResponse(this IEnumerable<AssetFolder> folders)
        {
            return new AssetFoldersResponse
            {
                Folders = folders.Select(f => f.MapToResponse()).ToList()
            };
        }

        public static AssetResponse MapToResponse(this Asset asset)
        {
            return new AssetResponse
            {
                Id = asset.Id,
                WorkspaceId = asset.WorkspaceId,
                FolderId = asset.FolderId,
                OwnerUserId = asset.OwnerUserId,
                Name = asset.Name,
                Description = asset.Description,
                AssetType = asset.AssetType,
                StorageUrl = asset.StorageUrl,
                Version = asset.Version,
                CurrentVersionNumber = asset.CurrentVersionNumber,
                CreatedAtUtc = asset.CreatedAtUtc,
                UpdatedAtUtc = asset.UpdatedAtUtc
            };
        }

        public static AssetsResponse MapToResponse(this IEnumerable<Asset> assets)
        {
            return new AssetsResponse
            {
                Assets = assets.Select(a => a.MapToResponse()).ToList()
            };
        }

        public static AssetVersionResponse MapToResponse(this AssetVersion version)
        {
            return new AssetVersionResponse
            {
                AssetId = version.AssetId,
                VersionNumber = version.VersionNumber,
                Name = version.Name,
                Description = version.Description,
                AssetType = version.AssetType,
                VersionLabel = version.VersionLabel,
                CreatedByUserId = version.CreatedByUserId,
                CreatedAtUtc = version.CreatedAtUtc
            };
        }

        public static AssetVersionsResponse MapToResponse(this IEnumerable<AssetVersion> versions)
        {
            return new AssetVersionsResponse
            {
                Versions = versions.Select(v => v.MapToResponse()).ToList()
            };
        }

        public static SceneVersionResponse MapToResponse(this SceneVersion version)
        {
            return new SceneVersionResponse
            {
                SceneId = version.SceneId,
                VersionNumber = version.VersionNumber,
                Name = version.Name,
                Description = version.Description,
                ETag = version.ETag,
                CreatedByUserId = version.CreatedByUserId,
                CreatedAtUtc = version.CreatedAtUtc
            };
        }

        public static SceneVersionsResponse MapToResponse(this IEnumerable<SceneVersion> versions)
        {
            return new SceneVersionsResponse
            {
                Versions = versions.Select(v => v.MapToResponse()).ToList()
            };
        }

        public static SceneResponse MapToResponse(this Scene scene)
        {
            return new SceneResponse
            {
                Id = scene.Id,
                WorkspaceId = scene.WorkspaceId,
                OwnerUserId = scene.OwnerUserId,
                Name = scene.Name,
                Description = scene.Description,
                JsonContent = ParseJsonContent(scene.JsonContent),
                ETag = scene.ETag,
                CurrentVersionNumber = scene.CurrentVersionNumber,
                CreatedAtUtc = scene.CreatedAtUtc,
                UpdatedAtUtc = scene.UpdatedAtUtc
            };
        }

        public static ScenesResponse MapToResponse(this IEnumerable<Scene> scenes)
        {
            return new ScenesResponse
            {
                Scenes = scenes.Select(s => s.MapToResponse()).ToList()
            };
        }

        public static GroupsResponse MapToResponse(this List<Group> groups)
        {
            return new GroupsResponse
            {
                Groups = groups.Select(g => g.MapToResponse()).ToList()
            };
        }
    }
}
