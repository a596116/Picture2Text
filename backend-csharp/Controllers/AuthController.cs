using Microsoft.AspNetCore.Mvc;
using Picture2Text.Api.DTOs.Requests;
using Picture2Text.Api.DTOs.Responses;
using Picture2Text.Api.Services;

namespace Picture2Text.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly AuthService _authService;

    public AuthController(AuthService authService)
    {
        _authService = authService;
    }

    /// <summary>
    /// 使用者登入
    /// </summary>
    /// <param name="request">登入請求</param>
    /// <returns>JWT Token</returns>
    [HttpPost("login")]
    [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationErrorResponse), StatusCodes.Status422UnprocessableEntity)]
    [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<LoginResponse>> Login([FromBody] LoginRequest request)
    {
        var response = await _authService.LoginAsync(request);

        if (response.Code != 200)
        {
            return Unauthorized(response);
        }

        return Ok(response);
    }
}
