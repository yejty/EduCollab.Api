namespace EduCollab.Api

{

    public static class ApiEndpoints

    {

        private const string ApiBase = "api";

        private const string ApiCurrentWorkspaceBase = $"{ApiBase}/workspace";



        public static class Users

        {

            private const string Base = $"{ApiBase}/users";

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

            public const string Preferences = $"{Me}/preferences";

            public const string Workspaces = $"{Me}/workspaces";

            public const string ActiveWorkspace = $"{Me}/active-workspace";



            public const string ResetConfirm = $"{Base}/reset-password-confirm";

            public const string Reset = $"{Base}/reset-password";

            public const string ChangePassword = $"{Base}/change-password";

        }



        public static class Workspaces

        {

            private const string Base = $"{ApiBase}/admin/workspaces";

            public const string GetAll = Base;

            public const string Get = $"{Base}/{{id}}";

            public const string GetAllMembers = $"{Base}/{{id}}/users";

            public const string GetMember = $"{Base}/{{id}}/users/{{userId}}";

        }



        public static class Workspace

        {

            private const string Base = $"{ApiCurrentWorkspaceBase}";

            public const string Create = Base;

            public const string Get = Base;

            public const string Update = Base;

            public const string Delete = Base;

            public const string GetAllMembers = $"{Base}/users";

            public const string GetMember = $"{Base}/users/{{userId}}";

            public const string UpdateMember = $"{Base}/users/{{userId}}";

            public const string DeleteMember = $"{Base}/users/{{userId}}";

            public const string Invite = $"{Base}/invitations";

            public const string Thumbnail = $"{Base}/thumbnail";

            public const string RequestCreation = $"{Base}/creation-requests";

            public const string GetMyCreationRequest = $"{Base}/creation-requests/me";

        }



        public static class AdminWorkspaceCreationRequests

        {

            private const string Base = $"{ApiBase}/admin/workspace-creation-requests";

            public const string GetAll = Base;

            public const string Approve = $"{Base}/{{requestId}}/approve";

            public const string Deny = $"{Base}/{{requestId}}/deny";

        }



        public static class WorkspaceCreationReview

        {

            private const string Base = $"{ApiBase}/workspace-creation-review";

            public const string Approve = $"{Base}/{{requestId}}/{{reviewToken}}/approve";

            public const string Deny = $"{Base}/{{requestId}}/{{reviewToken}}/deny";

        }



        public static class WorkspaceInvitations

        {

            private const string Base = $"{ApiBase}/workspace-invitations";

            public const string Accept = $"{Base}/{{invitationToken}}/accept";

            public const string Join = $"{Base}/{{invitationToken}}/join";

        }



        public static class Groups

        {

            private const string Base = $"{ApiCurrentWorkspaceBase}/groups";

            public const string Create = Base;

            public const string GetAll = Base;

            public const string GetAllFlat = $"{Base}/flat";

            public const string Get = $"{Base}/{{groupId}}";

            public const string Update = $"{Base}/{{groupId}}";

            public const string Delete = $"{Base}/{{groupId}}";



            public const string GetAllMembers = $"{Base}/{{groupId}}/users";

            public const string CreateMember = $"{Base}/{{groupId}}/users";

            public const string GetMember = $"{Base}/{{groupId}}/users/{{userId}}";

            public const string DeleteMember = $"{Base}/{{groupId}}/users/{{userId}}";



            public const string GetAssets = $"{Base}/{{groupId}}/assets";

            public const string GetScenes = $"{Base}/{{groupId}}/scenes";

            public const string GetFlows = $"{Base}/{{groupId}}/flows";

        }



        public static class Assets

        {

            private const string Base = $"{ApiCurrentWorkspaceBase}/assets";

            public const string Create = Base;

            public const string GetAll = Base;

            public const string Get = $"{Base}/{{assetId}}";

            public const string Update = $"{Base}/{{assetId}}";

            public const string Delete = $"{Base}/{{assetId}}";

            public const string Content = $"{Base}/{{assetId}}/content";

        }



        public static class Scenes

        {

            private const string Base = $"{ApiCurrentWorkspaceBase}/scenes";

            public const string Create = Base;

            public const string GetAll = Base;

            public const string Get = $"{Base}/{{sceneId}}";

            public const string Update = $"{Base}/{{sceneId}}";

            public const string Delete = $"{Base}/{{sceneId}}";

        }



        public static class Flows

        {

            private const string Base = $"{ApiCurrentWorkspaceBase}/flows";

            public const string Create = Base;

            public const string GetAll = Base;

            public const string Get = $"{Base}/{{flowId}}";

            public const string Update = $"{Base}/{{flowId}}";

            public const string Delete = $"{Base}/{{flowId}}";

        }



        public static class FlowScenes

        {

            private const string Base = $"{ApiCurrentWorkspaceBase}/flow-scenes";

            public const string GetAll = Base;

            public const string Create = Base;

            public const string Delete = Base;

            public const string Content = $"{Base}/content";

        }



        public static class SceneAssets

        {

            private const string Base = $"{ApiCurrentWorkspaceBase}/scene-assets";

            public const string GetAll = Base;

            public const string Create = Base;

            public const string Delete = Base;

            public const string Content = $"{Base}/content";

        }



        public static class AssetGroups

        {

            private const string Base = $"{ApiCurrentWorkspaceBase}/asset-groups";

            public const string GetAll = Base;

            public const string Create = Base;

            public const string Update = Base;

            public const string Delete = Base;

        }



        public static class SceneGroups

        {

            private const string Base = $"{ApiCurrentWorkspaceBase}/scene-groups";

            public const string GetAll = Base;

            public const string Create = Base;

            public const string Update = Base;

            public const string Delete = Base;

        }



        public static class FlowGroups

        {

            private const string Base = $"{ApiCurrentWorkspaceBase}/flow-groups";

            public const string GetAll = Base;

            public const string Create = Base;

            public const string Update = Base;

            public const string Delete = Base;

        }

    }

}


