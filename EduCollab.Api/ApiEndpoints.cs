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

            public const string Register = $"{Base}/register";
            public const string Invite = $"{Base}/invite";
            public const string Accept = $"{Base}/{{invitationToken}}/accept";
            public const string Login = $"{Base}/login"; 
            public const string Token = $"{Base}/token";
            public const string Me = $"{Base}/me";
            public const string ResetConfirm = $"{Base}/reset-confirm";
            public const string Reset = $"{Base}/reset";
            public const string ChangePassword = $"{Base}/change-password";
        }
    }
}
