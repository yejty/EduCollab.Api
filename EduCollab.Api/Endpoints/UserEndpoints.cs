using System.Text.Json.Nodes;
using EduCollab.Api.Mapping;
using EduCollab.Api.Security;
using EduCollab.Application.Exceptions;
using EduCollab.Application.Models;
using EduCollab.Application.Services.Auth;
using EduCollab.Application.Services.Users;
using EduCollab.Application.Services.Workspaces;
using EduCollab.Contracts.Requests.Users;
using EduCollab.Contracts.Responses;
using EduCollab.Contracts.Responses.Users;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EduCollab.Api.MinimalApis;

internal static class UserEndpoints
{
    public static void Map(IEndpointRouteBuilder routes)
    {
        routes.MapPost(ApiEndpoints.Users.Register, Register).AsApi("Users_Register", "Users");
        routes.MapPost(ApiEndpoints.Users.ConfirmEmail, ConfirmEmail).AsApi("Users_ConfirmEmail", "Users");
        routes.MapPost(ApiEndpoints.Users.ResendConfirmEmail, ResendEmailConfirmation).AsApi("Users_ResendEmailConfirmation", "Users");
        routes.MapPost(ApiEndpoints.Users.LoginRequestCode, RequestLoginCode).AsApi("Users_RequestLoginCode", "Users");
        routes.MapPost(ApiEndpoints.Users.LoginConfirmCode, LoginWithCode).AsApi("Users_LoginWithCode", "Users");
        routes.MapPost(ApiEndpoints.Users.Login, Login).AsApi("Users_Login", "Users");
        routes.MapPost(ApiEndpoints.Users.Token, RefreshToken).AsApi("Users_RefreshToken", "Users");
        routes.MapGet(ApiEndpoints.Users.Me, GetCurrentUser).AsApi("Users_GetCurrentUser", "Users");
        routes.MapGet(ApiEndpoints.Users.Workspaces, GetCurrentUserWorkspaces).AsApi("Users_GetCurrentUserWorkspaces", "Users");
        routes.MapPut(ApiEndpoints.Users.ActiveWorkspace, SetActiveWorkspace).AsApi("Users_SetActiveWorkspace", "Users");
        routes.MapGet(ApiEndpoints.Users.Preferences, GetPreferences).AsApi("Users_GetPreferences", "Users");
        routes.MapPut(ApiEndpoints.Users.Preferences, PutPreferences).AsApi("Users_PutPreferences", "Users");
        routes.MapGet(ApiEndpoints.Users.Get, GetUserById).AsApi("Users_GetUserById", "Users");
        routes.MapPut(ApiEndpoints.Users.Update, Update).AsApi("Users_Update", "Users");
        routes.MapDelete(ApiEndpoints.Users.Delete, Delete).AsApi("Users_Delete", "Users");
        routes.MapPost(ApiEndpoints.Users.ResetConfirm, ResetPasswordConfirm).AsApi("Users_ResetPasswordConfirm", "Users");
        routes.MapPost(ApiEndpoints.Users.Reset, ResetPassword).AsApi("Users_ResetPassword", "Users");
        routes.MapPost(ApiEndpoints.Users.ChangePassword, ChangePassword).AsApi("Users_ChangePassword", "Users");
    }

    /// <summary>Register a new user. A confirmation email is sent; sign-in is blocked until email is confirmed.</summary>
    /// <response code="201">Registration succeeded and a confirmation email was sent.</response>
    /// <response code="400">Registration request was invalid.</response>
    [ProducesResponseType(typeof(RegistrationSubmittedResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public static async Task<IResult> Register(
        HttpContext httpContext,
        RegisterUserRequest request,
        IUserService userService,
        CancellationToken cancellationToken)
    {
        var user = request.MapToUser();
        try
        {
            var registered = await userService.RegisterAsync(user, request.Password, cancellationToken);
            if (!registered)
            {
                return ApiResults.BadRequest(httpContext, "registration_failed", "Registration could not be completed.");
            }
        }
        catch (ArgumentException ex)
        {
            return ApiResults.BadRequest(httpContext, "registration_failed", ex.Message);
        }

        return ApiResults.Status(StatusCodes.Status201Created, new RegistrationSubmittedResponse());
    }

    /// <summary>Confirm email after registration; returns access and refresh tokens for immediate sign-in.</summary>
    /// <response code="200">Email was confirmed and tokens were issued.</response>
    /// <response code="400">Confirmation token was invalid or expired.</response>
    [ProducesResponseType(typeof(TokensResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public static async Task<IResult> ConfirmEmail(
        HttpContext httpContext,
        ConfirmEmailRequest request,
        IUserService userService,
        IAccessTokenService accessTokenService,
        IRefreshTokenService refreshTokenService,
        CancellationToken cancellationToken)
    {
        User? user;
        try
        {
            user = await userService.ConfirmEmailAsync(request.Email, request.Token, cancellationToken);
        }
        catch (ArgumentException ex)
        {
            return ApiResults.BadRequest(httpContext, "confirmation_failed", ex.Message);
        }

        if (user is null)
        {
            return ApiResults.BadRequest(httpContext, "confirmation_failed", "Invalid or expired confirmation token.");
        }

        var accessToken = accessTokenService.Create(user.Id, user.Email);
        var refreshToken = await refreshTokenService.CreateAsync(user.Id, cancellationToken);
        return ApiResults.Ok((accessToken, refreshToken).MapToResponse());
    }

    /// <summary>Resend the confirmation email for an unconfirmed account.</summary>
    /// <response code="200">The request was accepted.</response>
    [ProducesResponseType(StatusCodes.Status200OK)]
    public static async Task<IResult> ResendEmailConfirmation(
        ResendEmailConfirmationRequest request,
        IUserService userService,
        CancellationToken cancellationToken)
    {
        await userService.ResendEmailConfirmationAsync(request.Email, cancellationToken);
        return ApiResults.Ok();
    }

    /// <summary>Send a one-time 6-digit sign-in code to the confirmed email address.</summary>
    /// <response code="200">The sign-in code was sent.</response>
    /// <response code="401">No account exists for the provided email address.</response>
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public static async Task<IResult> RequestLoginCode(
        HttpContext httpContext,
        RequestLoginCodeRequest request,
        IUserService userService,
        CancellationToken cancellationToken)
    {
        try
        {
            await userService.RequestLoginCodeAsync(request.Email, cancellationToken);
        }
        catch (ArgumentException ex)
        {
            return ApiResults.Unauthorized(httpContext, "user_not_found", ex.Message);
        }

        return ApiResults.Ok();
    }

    /// <summary>Log in a user with a previously sent 6-digit sign-in code.</summary>
    /// <response code="200">The sign-in code was valid and tokens were issued.</response>
    /// <response code="401">The sign-in code was invalid, expired, or locked.</response>
    [ProducesResponseType(typeof(TokensResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public static async Task<IResult> LoginWithCode(
        HttpContext httpContext,
        ConfirmLoginCodeRequest request,
        IUserService userService,
        IAccessTokenService accessTokenService,
        IRefreshTokenService refreshTokenService,
        CancellationToken cancellationToken)
    {
        var result = await userService.LoginWithCodeAsync(request.Email, request.Code, cancellationToken);
        if (result.User is null)
        {
            if (result.IsLocked)
            {
                return ApiResults.Unauthorized(httpContext, "login_code_locked", "Too many incorrect attempts. Request a new sign-in code.");
            }

            var description = result.RemainingAttempts is int attempts
                ? $"Invalid sign-in code. {attempts} attempt(s) remaining."
                : "Invalid or expired sign-in code.";
            return ApiResults.Unauthorized(httpContext, "invalid_login_code", description);
        }

        var accessToken = accessTokenService.Create(result.User.Id, result.User.Email);
        var refreshToken = await refreshTokenService.CreateAsync(result.User.Id, cancellationToken);
        return ApiResults.Ok((accessToken, refreshToken).MapToResponse());
    }

    /// <summary>Log in a user with provided credentials.</summary>
    /// <response code="200">User logged in. Returns a new access token.</response>
    /// <response code="401">Invalid login attempt.</response>
    [ProducesResponseType(typeof(TokensResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public static async Task<IResult> Login(
        HttpContext httpContext,
        LoginRequest loginRequest,
        IUserService userService,
        IAccessTokenService accessTokenService,
        IRefreshTokenService refreshTokenService,
        CancellationToken cancellationToken)
    {
        var loginResult = await userService.LoginAsync(loginRequest.Email, loginRequest.Password, cancellationToken);
        if (loginResult.UserNotFound)
        {
            return ApiResults.Unauthorized(httpContext, "user_not_found", "No account found for this email address.");
        }

        if (loginResult.User is null)
        {
            return ApiResults.Unauthorized(httpContext, "invalid_login", "Invalid credentials or the email address has not been confirmed.");
        }

        var accessToken = accessTokenService.Create(loginResult.User.Id, loginResult.User.Email);
        var refreshToken = await refreshTokenService.CreateAsync(loginResult.User.Id, cancellationToken);
        return ApiResults.Ok((accessToken, refreshToken).MapToResponse());
    }

    /// <summary>Login with a refresh token to obtain a new access token.</summary>
    /// <response code="200">Returns a new access token.</response>
    /// <response code="401">Invalid refresh token or user is unauthorized.</response>
    [ProducesResponseType(typeof(TokensResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public static async Task<IResult> RefreshToken(
        HttpContext httpContext,
        RefreshTokenRequest refreshTokenRequest,
        IAccessTokenService accessTokenService,
        IRefreshTokenService refreshTokenService,
        CancellationToken cancellationToken)
    {
        var session = await refreshTokenService.RefreshAsync(refreshTokenRequest.RefreshToken, cancellationToken);
        if (session is null)
        {
            return ApiResults.Unauthorized(httpContext, "invalid_refresh_token", "Refresh token is invalid, expired, or revoked.");
        }

        var accessToken = accessTokenService.Create(session.User.Id, session.User.Email);
        return ApiResults.Ok((accessToken, session.RefreshToken).MapToResponse());
    }

    /// <summary>Retrieve the current authenticated user's information.</summary>
    /// <response code="200">Returns the current user's information.</response>
    /// <response code="401">User is unauthorized.</response>
    [Authorize]
    [ProducesResponseType(typeof(UserResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public static async Task<IResult> GetCurrentUser(
        HttpContext httpContext,
        IUserService userService,
        CancellationToken cancellationToken)
    {
        var user = await userService.GetCurrentUserAsync(cancellationToken);
        if (user is null)
        {
            return ApiResults.Unauthorized(httpContext, "unauthorized", "Authentication is required for this operation.");
        }

        return ApiResults.Ok(user.MapToResponse());
    }

    /// <summary>List workspaces the current user belongs to, including the active workspace id.</summary>
    /// <response code="200">Workspace memberships for the current user.</response>
    /// <response code="401">Caller is not authenticated.</response>
    [Authorize]
    [ProducesResponseType(typeof(UserWorkspacesResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public static async Task<IResult> GetCurrentUserWorkspaces(
        HttpContext httpContext,
        IUserService userService,
        IWorkspaceService workspaceService,
        CancellationToken cancellationToken)
    {
        var user = await userService.GetCurrentUserAsync(cancellationToken);
        if (user is null)
        {
            return ApiResults.Unauthorized(httpContext, "unauthorized", "Authentication is required for this operation.");
        }

        var workspaces = await workspaceService.GetCurrentUserWorkspacesAsync(cancellationToken);
        var memberships = await workspaceService.GetCurrentUserWorkspaceMembershipsAsync(cancellationToken);
        return ApiResults.Ok(workspaces.MapToWorkspacesResponse(memberships, user.WorkspaceId));
    }

    /// <summary>Set the caller's active workspace for current-workspace routes.</summary>
    /// <response code="200">Active workspace was updated.</response>
    /// <response code="400">Invalid workspace id or update failed.</response>
    /// <response code="401">Caller is not authenticated.</response>
    /// <response code="403">Caller is not a member of the workspace.</response>
    [Authorize]
    [ProducesResponseType(typeof(UserResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public static async Task<IResult> SetActiveWorkspace(
        HttpContext httpContext,
        SetActiveWorkspaceRequest request,
        IUserService userService,
        IWorkspaceService workspaceService,
        CancellationToken cancellationToken)
    {
        if (request.WorkspaceId <= 0)
        {
            return ApiResults.BadRequest(httpContext, "invalid_workspace_id", "workspaceId must be a positive integer.");
        }

        try
        {
            var updated = await workspaceService.SetActiveWorkspaceAsync(request.WorkspaceId, cancellationToken);
            if (!updated)
            {
                return ApiResults.BadRequest(httpContext, "update_failed", "Active workspace could not be updated.");
            }
        }
        catch (AccessDeniedException ex)
        {
            return ApiResults.Forbidden(httpContext, "access_denied", ex.Message);
        }

        var user = await userService.GetCurrentUserAsync(cancellationToken);
        if (user is null)
        {
            return ApiResults.Unauthorized(httpContext, "unauthorized", "Authentication is required for this operation.");
        }

        return ApiResults.Ok(user.MapToResponse());
    }

    /// <summary>Retrieve opaque web-client preferences for the current user.</summary>
    /// <remarks>The backend treats this document as opaque JSON and stores it outside the database.</remarks>
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public static async Task<IResult> GetPreferences(IUserPreferencesService userPreferencesService, CancellationToken cancellationToken)
    {
        var json = await userPreferencesService.GetCurrentUserPreferencesAsync(cancellationToken);
        return ApiResults.Text(json ?? "{}", "application/json");
    }

    /// <summary>Replace opaque web-client preferences for the current user.</summary>
    /// <remarks>The request body is stored as opaque JSON without schema binding or validation.</remarks>
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public static async Task<IResult> PutPreferences(
        JsonNode preferences,
        IUserPreferencesService userPreferencesService,
        CancellationToken cancellationToken)
    {
        var savedJson = await userPreferencesService.SaveCurrentUserPreferencesAsync(preferences.ToJsonString(), cancellationToken);
        return ApiResults.Text(savedJson, "application/json");
    }

    /// <summary>Retrieve the user's profile by id.</summary>
    /// <response code="200">Returns the user information.</response>
    /// <response code="401">Caller is not authenticated.</response>
    /// <response code="404">User not found.</response>
    [Authorize]
    [ProducesResponseType(typeof(UserResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public static async Task<IResult> GetUserById(HttpContext httpContext, int id, IUserService userService, CancellationToken cancellationToken)
    {
        var user = await userService.GetUserByIdAsync(id, cancellationToken);
        return user is null ? ApiResults.NotFound(httpContext) : ApiResults.Ok(user.MapToResponse());
    }

    /// <summary>Update user information by user Id. The id must match the authenticated user.</summary>
    /// <response code="200">Returns the user information.</response>
    /// <response code="404">User not found.</response>
    [Authorize]
    [ProducesResponseType(typeof(UserResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public static async Task<IResult> Update(
        HttpContext httpContext,
        int id,
        UpdateUserProfileRequest request,
        IUserService userService,
        CancellationToken cancellationToken)
    {
        var updatedUser = await userService.UpdateUserByIdAsync(request.MapToUser(id), cancellationToken);
        return updatedUser is null ? ApiResults.NotFound(httpContext) : ApiResults.Ok(updatedUser.MapToResponse());
    }

    /// <summary>Delete user by user Id. The id must match the authenticated user.</summary>
    /// <response code="204">User deleted successfully.</response>
    /// <response code="404">User not found.</response>
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public static async Task<IResult> Delete(HttpContext httpContext, int id, IUserService userService, CancellationToken cancellationToken)
    {
        var deleted = await userService.DeleteUserByIdAsync(id, cancellationToken);
        return deleted ? ApiResults.NoContent() : ApiResults.NotFound(httpContext);
    }

    /// <summary>Reset password confirm.</summary>
    /// <response code="200">Password reset confirmed.</response>
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public static async Task<IResult> ResetPasswordConfirm(
        ConfirmPasswordResetRequest confirmPasswordResetRequest,
        IUserService userService,
        CancellationToken cancellationToken)
    {
        await userService.ConfirmResetPasswordAsync(
            confirmPasswordResetRequest.Email,
            confirmPasswordResetRequest.Token,
            confirmPasswordResetRequest.NewPassword,
            cancellationToken);
        return ApiResults.Ok();
    }

    /// <summary>Reset password.</summary>
    /// <response code="200">Requested for reset password.</response>
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public static async Task<IResult> ResetPassword(
        PasswordResetRequest passwordResetRequest,
        IUserService userService,
        CancellationToken cancellationToken)
    {
        await userService.ResetPasswordAsync(passwordResetRequest.Email, cancellationToken);
        return ApiResults.Ok();
    }

    /// <summary>Change password.</summary>
    /// <response code="200">Password was changed.</response>
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public static async Task<IResult> ChangePassword(
        ChangePasswordRequest changePasswordRequest,
        IUserService userService,
        CancellationToken cancellationToken)
    {
        await userService.ChangePasswordAsync(changePasswordRequest.Password, changePasswordRequest.NewPassword, cancellationToken);
        return ApiResults.Ok();
    }
}
