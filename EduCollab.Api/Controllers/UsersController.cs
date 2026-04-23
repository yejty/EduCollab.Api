using EduCollab.Application.Models.Users;
using EduCollab.Application.Services.Users;
using EduCollab.Contracts.Requests.Users;
using EduCollab.Contracts.Responses;
using EduCollab.Contracts.Responses.Users;
using Microsoft.AspNetCore.Mvc;

namespace EduCollab.Api.Controllers
{
    [ApiController]
    public class UsersController : ControllerBase
    {
        private readonly IUserService _userService;

        public UsersController(IUserService userService)
        {
            _userService = userService;
        }

        /// <summary>
        /// Register a new user.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <response code="201">User registered successfully.</response>
        /// <response code="400">Invalid registration attempt. Returns an error message.</response>
        [HttpPost]
        [Route("api/users/register")]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
        public async Task<ActionResult> Register(CreateUserRequest registerUserRequest, CancellationToken cancellationToken)
        {
            await _userService.RegisterAsync(registerUserRequest.FirstName, registerUserRequest.LastName, registerUserRequest.Email, registerUserRequest.Password, cancellationToken);
            return CreatedAtAction(nameof(GetCurrentUser), routeValues: null, value: null);
        }

        /// <summary>
        /// Invite a new user.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <response code="200">User invited successfully.</response>
        /// <response code="400">Invalid invitation attempt. Returns an error message.</response>
        /// <response code="401">User is unauthorized.</response>
        /// <response code="403">User is forbidden from accessing this resource.</response>
        [HttpPost]
        [Route("api/users/invite")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
        public async Task<ActionResult> Invite(InviteUserRequest inviteUserRequest, CancellationToken cancellationToken)
        {
            await _userService.InviteAsync(inviteUserRequest.Email, cancellationToken);
            return Ok();
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
        [HttpPost]
        [Route("api/users/{invitationToken}")]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
        public async Task<ActionResult> Create(CreateUserRequest createUserRequest, string invitationToken, CancellationToken cancellationToken)
        {
            await _userService.CreateAsync(createUserRequest.FirstName, createUserRequest.LastName, createUserRequest.Email, createUserRequest.Password, invitationToken, cancellationToken);
            return CreatedAtAction(nameof(GetCurrentUser), routeValues: null, value: null);
        }

        /// <summary>
        /// Log in a user with provided credentials.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <response code="200">User logged in. Returns a new access token.</response>
        /// <response code="401">Invalid login attempt. Returns an error message.</response>
        [HttpPost]
        [Route("api/users/login")]
        [ProducesResponseType(typeof(TokensResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult<TokensResponse>> Login(LoginRequest loginRequest, CancellationToken cancellationToken)
        {
            await _userService.LoginAsync(loginRequest.Email, loginRequest.Password, cancellationToken);
            // TODO: ProducesResponseType documents TokensResponse, but Ok() sends no body — return Ok(tokens) once IUserService returns tokens.
            return Ok();
        }

        /// <summary>
        /// Login with a refresh token to obtain a new access token.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <response code="200">Returns a new access token.</response>
        /// <response code="401">Invalid refresh token or user is unauthorized.</response>
        [HttpPost]
        [Route("api/users/token")]
        [ProducesResponseType(typeof(TokensResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult<TokensResponse>> RefreshToken(RefreshTokenRequest refreshTokenRequest, CancellationToken cancellationToken)
        {
            await _userService.RefreshTokenAsync(refreshTokenRequest.RefreshToken, cancellationToken);
            // TODO: ProducesResponseType documents TokensResponse, but Ok() sends no body — return Ok(tokens) once IUserService returns tokens.
            return Ok();
        }

        /// <summary>
        /// Retrieve the current authenticated user's information.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <response code="200">Returns the current user's information.</response>
        /// <response code="401">User is unauthorized.</response>
        /// <response code="403">User is forbidden from accessing this resource.</response>
        [HttpGet]
        [Route("api/users/me")]
        [ProducesResponseType(typeof(UserResponse),StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<UserResponse>> GetCurrentUser(CancellationToken cancellationToken)
        {
            await _userService.GetCurrentUserAsync(cancellationToken);
            // TODO: ProducesResponseType documents UserResponse, but Ok() sends no body — return Ok(user) once IUserService returns UserResponse.
            return Ok();
        }

        /// <summary>
        /// Retrieve user information needed to complete registration after creating a user. 
        /// </summary>
        /// <param name="id">User Id.</param>
        /// <param name="token">A token to authorize the request.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <response code="200">Returns the user information.</response>
        /// <response code="400">Bad request.</response>
        [HttpGet]
        [Route("api/users/{id}")]
        [ProducesResponseType(typeof(UserResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<UserResponse>> GetUserById(int id, string token, CancellationToken cancellationToken)
        {
            await _userService.GetUserByIdAsync(id, token, cancellationToken);
            // TODO: ProducesResponseType documents UserResponse, but Ok() sends no body — return Ok(user) once IUserService returns UserResponse.
            return Ok();
        }

        /// <summary>
        /// Update user information by user Id.
        /// </summary>
        /// <param name="id">User Id.</param>
        /// <param name="token">A token to authorize the request.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <response code="200">Returns the user information.</response>
        /// <response code="400">Bad request.</response>
        [HttpPut]
        [Route("api/users/{id}")]
        [ProducesResponseType(typeof(UserResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<UserResponse>> UpdateUserById(int id, string token, UpdateUserRequest updateUserRequest, CancellationToken cancellationToken)
        {
            await _userService.UpdateUserByIdAsync(id, token, cancellationToken);
            // TODO: ProducesResponseType documents UserResponse, but Ok() sends no body — return Ok(user) once IUserService returns UserResponse (and pass updateUserRequest into the service).
            return Ok();
        }

        /// <summary>
        /// Reset password confirm. 
        /// </summary>
        /// <param name="confirmPasswordResetRequest">Reset password confirm.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <response code="200">Password reset confirmed.</response>
        /// <response code="400">Bad request.</response>
        [HttpPost]
        [Route("api/users/reset-confirm")]
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
        [HttpPost]
        [Route("api/users/reset")]
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
        [HttpPost]
        [Route("api/users/change-password")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> ChangePassword(ChangePasswordRequest changePasswordRequest, CancellationToken cancellationToken)
        {
            await _userService.ChangePasswordAsync(changePasswordRequest.Password, changePasswordRequest.NewPassword, cancellationToken);
            return Ok();
        }
    }
}
