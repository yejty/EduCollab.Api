using System.Net.NetworkInformation;

namespace EduCollab.Api
{
    public static class ApiEndpoints
    {
        private const string ApiBase = "api";
        private const string ApiWorkspaceBase = $"{ApiBase}/workspaces/{{workspaceId}}";

        public static class Users
        {
            private const string Base = $"{ApiBase}/users";
            public const string Create = Base;
            public const string Get = $"{Base}/{{id}}";
            public const string Update = $"{Base}/{{id}}";
            public const string Delete = $"{Base}/{{id}}";

            public const string Register = $"{Base}/register";

            public const string ConfirmEmail = $"{Base}/registration-confirm";
            public const string ResendConfirmEmail = $"{Base}/registration-confirm/resend";

            public const string Login = $"{Base}/login"; 
            public const string LoginRequestCode = $"{Base}/login/request-code";
            public const string LoginConfirmCode = $"{Base}/login/confirm-code";
            public const string Token = $"{Base}/token";
            public const string Me = $"{Base}/me";

            public const string ResetConfirm = $"{Base}/reset-password-confirm";
            public const string Reset = $"{Base}/reset-password";
            public const string ChangePassword = $"{Base}/change-password";
        }

        public static class Workspaces
        {
            private const string Base = $"{ApiBase}/workspaces";
            public const string Create = Base;
            public const string GetAll = Base;
            public const string Get = $"{Base}/{{id}}";
            public const string Update = $"{Base}/{{id}}";
            public const string Delete = $"{Base}/{{id}}";

            public const string GetAllMembers = $"{Base}/{{id}}/users";
            public const string CreateMember = $"{Base}/{{id}}/users";
            public const string GetMember = $"{Base}/{{id}}/users/{{userId}}";
            public const string UpdateMember = $"{Base}/{{id}}/users/{{userId}}";
            public const string DeleteMember = $"{Base}/{{id}}/users/{{userId}}";

            public const string Invite = $"{Base}/{{id}}/invite";
            public const string Accept = $"{Base}/{{id}}/invite/{{invitationToken}}/accept";

        }

        public static class Groups
        {
            private const string Base = $"{ApiWorkspaceBase}/groups";
            public const string Create = Base;
            public const string GetAll = Base;
            public const string Get = $"{Base}/{{groupId}}";
            public const string Update = $"{Base}/{{groupId}}";
            public const string Delete = $"{Base}/{{groupId}}";

            public const string GetAllMembers = $"{Base}/{{groupId}}/users";
            public const string CreateMember = $"{Base}/{{groupId}}/users";
            public const string GetMember = $"{Base}/{{groupId}}/users/{{userId}}";
            public const string UpdateMember = $"{Base}/{{groupId}}/users/{{userId}}";
            public const string DeleteMember = $"{Base}/{{groupId}}/users/{{userId}}";

            public const string GetFolders = $"{Base}/{{groupId}}/folders";
            public const string GetSubFolders = $"{Base}/{{groupId}}/folders/{{folderId}}/folders";
            public const string GetAssetsInFolders = $"{Base}/{{groupId}}/folders/{{folderId}}/assets";
            public const string GetAssets = $"{Base}/{{groupId}}/assets";
        }

        public static class AssetFolders
        {
            private const string Base = $"{ApiWorkspaceBase}/asset-folders";
            public const string Create = Base;
            public const string GetAll = Base;
            public const string Get = $"{Base}/{{folderId}}";
            public const string Update = $"{Base}/{{folderId}}";
            public const string Delete = $"{Base}/{{folderId}}";
            public const string GetSubFolders = $"{Base}/{{folderId}}/folders";
            public const string GetAssets = $"{Base}/{{folderId}}/assets";
        }

        public static class Assets
        {
            private const string Base = $"{ApiWorkspaceBase}/assets";
            public const string Create = Base;
            public const string GetAll = Base;
            public const string Get = $"{Base}/{{assetId}}";
            public const string Update = $"{Base}/{{assetId}}";
            public const string Delete = $"{Base}/{{assetId}}";
            public const string GetMine = $"{Base}/mine";
            public const string Move = $"{Base}/{{assetId}}/move";
        }
    }
}
