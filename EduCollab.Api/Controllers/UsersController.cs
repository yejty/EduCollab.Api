using EduCollab.Api.Mapping;
using EduCollab.Api.Security.AccessToken;
using EduCollab.Application.Models.Users;
using EduCollab.Application.Services.Auth;
using EduCollab.Application.Services.Users;
using EduCollab.Contracts.Requests.Users;
using EduCollab.Contracts.Responses;
using EduCollab.Contracts.Responses.Users;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace EduCollab.Api.Controllers
{
    [ApiController]
    public class UsersController : ControllerBase
    {
        private readonly IUserService _userService;
        private readonly IAccessTokenService _accessTokenService;
        private readonly IRefreshTokenService _refreshTokenService;

        public UsersController(
            IUserService userService,
            IAccessTokenService accessTokenService,
            IRefreshTokenService refreshTokenService)
        {
            _userService = userService;
            _accessTokenService = accessTokenService;
            _refreshTokenService = refreshTokenService;
        }

        /// <summary>
        /// Register a new user.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <response code="201">User registered successfully.</response>
        /// <response code="400">Invalid registration attempt. Returns an error message.</response>
        [HttpPost(ApiEndpoints.Users.Register)]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Register([FromBody]CreateUserRequest request, CancellationToken cancellationToken)
        {
            var user = request.MapToUser();
            await _userService.RegisterAsync(user, request.Password, cancellationToken);
            return CreatedAtAction(nameof(GetUserById), new { id = user.Id }, user);
        }

        /// <summary>
        /// Creates a new user in the workspace based on the provided token.
        /// </summary>
        /// <param name="createUserRequest">Request body containing the user details.</param>
        /// <param name="invitationToken">Invitation token.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <response code="201">User created.</response>
        /// <response code="400">Invalid invitation attempt. Returns an error message.</response>
        /// <response code="401">User is unauthorized.</response>
        /// <response code="403">User is forbidden from accessing this resource.</response>
        [HttpPost(ApiEndpoints.Users.Accept)]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<UserResponse>> Create([FromBody]CreateUserRequest request,[FromRoute] string invitationToken, CancellationToken cancellationToken)
        {
            var user = request.MapToUser();
            await _userService.CreateAsync(user, request.Password, invitationToken, cancellationToken);
            return CreatedAtAction(nameof(GetCurrentUser), new { id = user.Id }, user);
        }

        /// <summary>
        /// Log in a user with provided credentials.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <response code="200">User logged in. Returns a new access token.</response>
        /// <response code="401">Invalid login attempt. Returns an error message.</response>
        [HttpPost(ApiEndpoints.Users.Login)]
        [ProducesResponseType(typeof(TokensResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult<TokensResponse>> Login(LoginRequest loginRequest, CancellationToken cancellationToken)
        {
            var user = await _userService.LoginAsync(loginRequest.Email, loginRequest.Password, cancellationToken);
            if (user is null)
                return Unauthorized();

            var accessToken = _accessTokenService.Create(user.Id, user.Email);
            var refreshToken = await _refreshTokenService.CreateAsync(user.Id, cancellationToken);
            var response = (accessToken, refreshToken).MapToResponse();
            return Ok(response);
        }

        /// <summary>
        /// Login with a refresh token to obtain a new access token.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <response code="200">Returns a new access token.</response>
        /// <response code="401">Invalid refresh token or user is unauthorized.</response>
        [HttpPost(ApiEndpoints.Users.Token)]
        [ProducesResponseType(typeof(TokensResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult<TokensResponse>> RefreshToken(RefreshTokenRequest refreshTokenRequest, CancellationToken cancellationToken)
        {
            var session = await _refreshTokenService.RefreshAsync(refreshTokenRequest.RefreshToken, cancellationToken);
            if (session is null)
                return Unauthorized();

            var accessToken = _accessTokenService.Create(session.User.Id, session.User.Email);
            var response = (accessToken, session.RefreshToken).MapToResponse();
            return Ok(response);
        }

        /// <summary>
        /// Retrieve the current authenticated user's information.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <response code="200">Returns the current user's information.</response>
        /// <response code="401">User is unauthorized.</response>
        /// <response code="403">User is forbidden from accessing this resource.</response>
        [Authorize]
        [HttpGet(ApiEndpoints.Users.Me)]
        [ProducesResponseType(typeof(UserResponse),StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<UserResponse>> GetCurrentUser(CancellationToken cancellationToken)
        {
            var user = await _userService.GetCurrentUserAsync(cancellationToken);
            if (user is null)
            {
                return Unauthorized();
            }
            var response = user.MapToResponse();
            return Ok(response);
        }

        /// <summary>
        /// Retrieve the authenticated user's profile by id. The id must match the caller (JWT subject).
        /// </summary>
        /// <param name="id">User Id.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <response code="200">Returns the user information.</response>
        /// <response code="400">Bad request.</response>
        /// <response code="401">Caller is not authenticated.</response>
        /// <response code="403">Caller cannot access this user id.</response>
        /// <response code="404">User not found.</response>
        [Authorize]
        [HttpGet(ApiEndpoints.Users.Get)]
        [ProducesResponseType(typeof(UserResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
        public async Task<ActionResult<UserResponse>> GetUserById(int id, CancellationToken cancellationToken)
        {
            var user = await _userService.GetUserByIdAsync(id, cancellationToken);
            if (user is null)
            {
                return NotFound();
            }
            var response = user.MapToResponse();    
            return Ok(response);
        }

        /// <summary>
        /// Update user information by user Id. The id must match the authenticated user (JWT subject).
        /// </summary>
        /// <param name="id">User Id.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <response code="200">Returns the user information.</response>
        /// <response code="400">Bad request.</response>
        /// <response code="401">Caller is not authenticated.</response>
        /// <response code="403">Caller cannot update this user id.</response>
        /// <response code="404">User not found.</response>
        [Authorize]
        [HttpPut(ApiEndpoints.Users.Update)]
        [ProducesResponseType(typeof(UserResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
        public async Task<ActionResult<UserResponse>> Update([FromRoute]int id, [FromBody]UpdateUserRequest request, CancellationToken cancellationToken)
        {
            var user = request.MapToUser(id);
            var updatedUser = await _userService.UpdateUserByIdAsync(user, cancellationToken);
            if (updatedUser is null)
            {
                return NotFound();
            }
            var response = updatedUser.MapToResponse();
            return Ok(response);
        }

        /// <summary>
        /// Reset password confirm. 
        /// </summary>
        /// <param name="confirmPasswordResetRequest">Reset password confirm.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <response code="200">Password reset confirmed.</response>
        /// <response code="400">Bad request.</response>
        [HttpPost(ApiEndpoints.Users.ResetConfirm)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> ResetPasswordConfirm(ConfirmPasswordResetRequest confirmPasswordResetRequest, CancellationToken cancellationToken)
        {
            await _userService.ConfirmResetPasswordAsync(confirmPasswordResetRequest.Email, confirmPasswordResetRequest.Token, confirmPasswordResetRequest.NewPassword, cancellationToken);
            return Ok();
        }

        /// <summary>
        /// Reset password.
        /// </summary>
        /// <param name="passwordResetRequest">The password reset request.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <response code="200">Requested for reset password.</response>
        /// <response code="400">Bad request.</response>
        [HttpPost(ApiEndpoints.Users.Reset)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> ResetPassword(PasswordResetRequest passwordResetRequest, CancellationToken cancellationToken)
        {
            await _userService.ResetPasswordAsync(passwordResetRequest.Email, cancellationToken);
            return Ok();
        }

        /// <summary>
        /// Change password.
        /// </summary>
        /// <param name="changePasswordRequest">The change password request.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <response code="200">Password was changed.</response>
        /// <response code="400">Bad request.</response>
        [Authorize]
        [HttpPost(ApiEndpoints.Users.ChangePassword)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> ChangePassword(ChangePasswordRequest changePasswordRequest, CancellationToken cancellationToken)
        {
            await _userService.ChangePasswordAsync(changePasswordRequest.Password, changePasswordRequest.NewPassword, cancellationToken);
            return Ok();
        }
    }
}
