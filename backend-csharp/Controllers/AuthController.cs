using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Picture2Text.Api.DTOs.Requests;
using Picture2Text.Api.DTOs.Responses;
using Picture2Text.Api.Services;
using System.Security.Claims;

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
    /// <returns>Access Token 和 Refresh Token</returns>
    [HttpPost("login")]
    [ProducesResponseType(typeof(TokenResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationErrorResponse), StatusCodes.Status422UnprocessableEntity)]
    [ProducesResponseType(typeof(TokenResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(TokenResponse), StatusCodes.Status429TooManyRequests)]
    public async Task<ActionResult<TokenResponse>> Login([FromBody] LoginRequest request)
    {
        var response = await _authService.LoginAsync(request);

        return response.Code switch
        {
            200 => Ok(response),
            429 => StatusCode(429, response),
            _ => Unauthorized(response)
        };
    }

    /// <summary>
    /// 刷新 Access Token
    /// </summary>
    /// <param name="request">Refresh Token 請求</param>
    /// <returns>新的 Access Token 和 Refresh Token</returns>
    [HttpPost("refresh")]
    [ProducesResponseType(typeof(TokenResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationErrorResponse), StatusCodes.Status422UnprocessableEntity)]
    [ProducesResponseType(typeof(TokenResponse), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<TokenResponse>> RefreshToken([FromBody] RefreshTokenRequest request)
    {
        var response = await _authService.RefreshTokenAsync(request);

        if (response.Code != 200)
        {
            return Unauthorized(response);
        }

        return Ok(response);
    }

    /// <summary>
    /// 撤銷 Token（登出）
    /// </summary>
    /// <param name="request">撤銷請求</param>
    /// <returns>操作結果</returns>
    [Authorize]
    [HttpPost("revoke")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<object>>> RevokeToken([FromBody] RevokeTokenRequest request)
    {
        var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        var response = await _authService.RevokeTokenAsync(request, userId);

        if (response.Code != 200)
        {
            return BadRequest(response);
        }

        return Ok(response);
    }

    /// <summary>
    /// 驗證 Token（供其他微服務/API Gateway 使用）
    /// </summary>
    /// <param name="request">Token 驗證請求</param>
    /// <returns>驗證結果和使用者資訊</returns>
    [HttpPost("validate")]
    [ProducesResponseType(typeof(ValidateTokenResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationErrorResponse), StatusCodes.Status422UnprocessableEntity)]
    [ProducesResponseType(typeof(ValidateTokenResponse), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ValidateTokenResponse>> ValidateToken([FromBody] ValidateTokenRequest request)
    {
        var response = await _authService.ValidateTokenAsync(request);

        if (response.Code == 200 && response.Data?.IsValid == true)
        {
            // ✨ 添加使用者資訊到 Response Header（供 API Gateway 使用）
            // Nginx 的 auth_request_set 可以從這些 header 中提取資訊
            Response.Headers.Append("X-User-Id", response.Data.UserId?.ToString() ?? "");
            Response.Headers.Append("X-User-Name", response.Data.Name ?? "");
            Response.Headers.Append("X-User-IdNo", response.Data.IdNo ?? "");
        }

        if (response.Code != 200)
        {
            return Unauthorized(response);
        }

        return Ok(response);
    }

    /// <summary>
    /// 取得當前登入使用者資訊（快速驗證端點）
    /// </summary>
    /// <returns>使用者基本資訊</returns>
    [Authorize]
    [HttpGet("me")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public IActionResult GetCurrentUser()
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var idNo = User.FindFirst(ClaimTypes.Name)?.Value;
        var sessionId = User.FindFirst("sessionId")?.Value;

        return Ok(new ApiResponse<object>
        {
            Code = 200,
            Message = "獲取成功",
            Data = new
            {
                UserId = userId,
                IdNo = idNo,
                SessionId = sessionId
            }
        });
    }
}

