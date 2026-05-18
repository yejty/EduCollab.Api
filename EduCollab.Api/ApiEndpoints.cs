namespace EduCollab.Api
{
    public static class ApiEndpoints
    {
        private const string ApiBase = "api";

        public static class Users
        {
            private const string Base = $"{ApiBase}/users";
            public const string Create = Base;
            public const string Get = $"{Base}/{{id}}";
            public const string Update = $"{Base}/{{id}}";
            public const string Delete = $"{Base}/{{id}}";

            public const string Register = $"{Base}/register";

            public const string ConfirmEmail = $"{Base}/registration-confirm";

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
            public const string Get = $"{Base}/{{id}}";
            public const string Update = $"{Base}/{{id}}";
            public const string Delete = $"{Base}/{{id}}";

            public const string GetMembers = $"{Base}/{{id}}/users";
            public const string CreateMember = $"{Base}/{{id}}/users";
            public const string GetMember = $"{Base}/{{id}}/users/{{userId}}";
            public const string UpdateMember = $"{Base}/{{id}}/users/{{userId}}";
            public const string DeleteMember = $"{Base}/{{id}}/users/{{userId}}";

            public const string Invite = $"{Base}/{{id}}/invite";
            public const string Accept = $"{Base}/{{id}}/invite/{{invitationToken}}/accept";

        }
    }
}
