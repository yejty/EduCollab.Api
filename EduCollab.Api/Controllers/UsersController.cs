using EduCollab.Api.Mapping;
using EduCollab.Api.Security;
using EduCollab.Application.Models;
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
        /// Register a new user. A confirmation email is sent; sign-in is blocked until email is confirmed.
        /// </summary>
        /// <param name="request">Registration payload.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <response code="201">Registration succeeded and a confirmation email was sent.</response>
        /// <response code="400">Registration request was invalid.</response>
        [HttpPost(ApiEndpoints.Users.Register)]
        [ProducesResponseType(typeof(RegistrationSubmittedResponse), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Register([FromBody] RegisterUserRequest request, CancellationToken cancellationToken)
        {
            if (!string.Equals(request.Password, request.ConfirmPassword, StringComparison.Ordinal))
            {
                return BadRequest(new ErrorResponse
                {
                    Error = "password_mismatch",
                    ErrorDescription = "Password and confirmation password do not match.",
                });
            }

            var user = request.MapToUser();
            try
            {
                var registered = await _userService.RegisterAsync(user, request.Password, cancellationToken);
                if (!registered)
                {
                    return BadRequest(new ErrorResponse
                    {
                        Error = "registration_failed",
                        ErrorDescription = "Registration could not be completed.",
                    });
                }
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new ErrorResponse
                {
                    Error = "registration_failed",
                    ErrorDescription = ex.Message,
                });
            }

            return StatusCode(StatusCodes.Status201Created, new RegistrationSubmittedResponse());
        }

        /// <summary>
        /// Confirm email after registration; returns access and refresh tokens for immediate sign-in.
        /// </summary>
        /// <param name="request">Email confirmation payload.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <response code="200">Email was confirmed and tokens were issued.</response>
        /// <response code="400">Confirmation token was invalid or expired.</response>
        [HttpPost(ApiEndpoints.Users.ConfirmEmail)]
        [ProducesResponseType(typeof(TokensResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<TokensResponse>> ConfirmEmail([FromBody] ConfirmEmailRequest request, CancellationToken cancellationToken)
        {
            User? user;
            try
            {
                user = await _userService.ConfirmEmailAsync(request.Email, request.Token, cancellationToken);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new ErrorResponse
                {
                    Error = "confirmation_failed",
                    ErrorDescription = ex.Message,
                });
            }

            if (user is null)
            {
                return BadRequest(new ErrorResponse
                {
                    Error = "confirmation_failed",
                    ErrorDescription = "Invalid or expired confirmation token.",
                });
            }

            var accessToken = _accessTokenService.Create(user.Id, user.Email);
            var refreshToken = await _refreshTokenService.CreateAsync(user.Id, cancellationToken);
            return Ok((accessToken, refreshToken).MapToResponse());
        }

        /// <summary>
        /// Resend the confirmation email for an unconfirmed account. For privacy, the same response is returned even when the email does not exist.
        /// </summary>
        /// <param name="request">Email confirmation resend payload.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <response code="200">The request was accepted.</response>
        [HttpPost(ApiEndpoints.Users.ResendConfirmEmail)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> ResendEmailConfirmation([FromBody] ResendEmailConfirmationRequest request, CancellationToken cancellationToken)
        {
            await _userService.ResendEmailConfirmationAsync(request.Email, cancellationToken);
            return Ok();
        }

        /// <summary>
        /// Send a one-time 6-digit sign-in code to the confirmed email address.
        /// </summary>
        /// <param name="request">Sign-in code request payload.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <response code="200">The request was accepted. For privacy, the same response is returned even when the email does not exist.</response>
        [HttpPost(ApiEndpoints.Users.LoginRequestCode)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> RequestLoginCode([FromBody] RequestLoginCodeRequest request, CancellationToken cancellationToken)
        {
            await _userService.RequestLoginCodeAsync(request.Email, cancellationToken);
            return Ok();
        }

        /// <summary>
        /// Log in a user with a previously sent 6-digit sign-in code.
        /// </summary>
        /// <param name="request">Email and one-time sign-in code.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <response code="200">The sign-in code was valid and tokens were issued.</response>
        /// <response code="401">The sign-in code was invalid, expired, or locked after too many attempts.</response>
        [HttpPost(ApiEndpoints.Users.LoginConfirmCode)]
        [ProducesResponseType(typeof(TokensResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult<TokensResponse>> LoginWithCode([FromBody] ConfirmLoginCodeRequest request, CancellationToken cancellationToken)
        {
            var result = await _userService.LoginWithCodeAsync(request.Email, request.Code, cancellationToken);
            if (result.User is null)
            {
                if (result.IsLocked)
                {
                    return Unauthorized(new ErrorResponse
                    {
                        Error = "login_code_locked",
                        ErrorDescription = "Too many incorrect attempts. Request a new sign-in code.",
                    });
                }

                var description = result.RemainingAttempts is int attempts
                    ? $"Invalid sign-in code. {attempts} attempt(s) remaining."
                    : "Invalid or expired sign-in code.";

                return Unauthorized(new ErrorResponse
                {
                    Error = "invalid_login_code",
                    ErrorDescription = description,
                });
            }

            var accessToken = _accessTokenService.Create(result.User.Id, result.User.Email);
            var refreshToken = await _refreshTokenService.CreateAsync(result.User.Id, cancellationToken);
            return Ok((accessToken, refreshToken).MapToResponse());
        }

        /// <summary>
        /// Log in a user with provided credentials.
        /// </summary>
        /// <param name="loginRequest">Login credentials.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <response code="200">User logged in. Returns a new access token.</response>
        /// <response code="401">Invalid login attempt. Returns an error message.</response>
        [HttpPost(ApiEndpoints.Users.Login)]
        [ProducesResponseType(typeof(TokensResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult<TokensResponse>> Login([FromBody] LoginRequest loginRequest, CancellationToken cancellationToken)
        {
            var user = await _userService.LoginAsync(loginRequest.Email, loginRequest.Password, cancellationToken);
            if (user is null)
            {
                return Unauthorized(new ErrorResponse
                {
                    Error = "invalid_login",
                    ErrorDescription = "Invalid credentials or the email address has not been confirmed.",
                });
            }

            var accessToken = _accessTokenService.Create(user.Id, user.Email);
            var refreshToken = await _refreshTokenService.CreateAsync(user.Id, cancellationToken);
            var response = (accessToken, refreshToken).MapToResponse();
            return Ok(response);
        }

        /// <summary>
        /// Login with a refresh token to obtain a new access token.
        /// </summary>
        /// <param name="refreshTokenRequest">Refresh token payload.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <response code="200">Returns a new access token.</response>
        /// <response code="401">Invalid refresh token or user is unauthorized.</response>
        [HttpPost(ApiEndpoints.Users.Token)]
        [ProducesResponseType(typeof(TokensResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult<TokensResponse>> RefreshToken([FromBody] RefreshTokenRequest refreshTokenRequest, CancellationToken cancellationToken)
        {
            var session = await _refreshTokenService.RefreshAsync(refreshTokenRequest.RefreshToken, cancellationToken);
            if (session is null)
            {
                return Unauthorized(new ErrorResponse
                {
                    Error = "invalid_refresh_token",
                    ErrorDescription = "Refresh token is invalid, expired, or revoked.",
                });
            }

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
                return Unauthorized(new ErrorResponse
                {
                    Error = "unauthorized",
                    ErrorDescription = "Authentication is required for this operation.",
                });
            }
            var response = user.MapToResponse();
            return Ok(response);
        }

        /// <summary>
        /// Retrieve the user's profile by id. 
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
        /// <param name="request">Profile fields to update.</param>
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
        public async Task<ActionResult<UserResponse>> Update([FromRoute]int id, [FromBody]UpdateUserProfileRequest request, CancellationToken cancellationToken)
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
        /// Delete user by user Id. The id must match the authenticated user (JWT subject).
        /// </summary>
        /// <param name="id">User Id.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <response code="204">User deleted successfully.</response>
        /// <response code="400">Bad request.</response>
        /// <response code="401">Caller is not authenticated.</response>
        /// <response code="403">Caller cannot delete this user.</response>
        /// <response code="404">User not found.</response>
        [Authorize]
        [HttpDelete(ApiEndpoints.Users.Delete)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Delete([FromRoute] int id, CancellationToken cancellationToken)
        {
            var deleted = await _userService.DeleteUserByIdAsync(id, cancellationToken);
            if (!deleted)
            {
                return NotFound();
            }
            return NoContent();
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
        public async Task<IActionResult> ResetPasswordConfirm([FromBody] ConfirmPasswordResetRequest confirmPasswordResetRequest, CancellationToken cancellationToken)
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
        public async Task<IActionResult> ResetPassword([FromBody] PasswordResetRequest passwordResetRequest, CancellationToken cancellationToken)
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
        /// <response code="401">Caller is not authenticated.</response>
        [Authorize]
        [HttpPost(ApiEndpoints.Users.ChangePassword)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest changePasswordRequest, CancellationToken cancellationToken)
        {
            await _userService.ChangePasswordAsync(changePasswordRequest.Password, changePasswordRequest.NewPassword, cancellationToken);
            return Ok();
        }
    }
}
