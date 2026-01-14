using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Picture2Text.Api.DTOs.Responses;
using Picture2Text.Api.Services;
using System.Security.Claims;

namespace Picture2Text.Api.Controllers;

/// <summary>
/// 會話管理控制器
/// </summary>
[Authorize]
[ApiController]
[Route("api/[controller]")]
public class SessionController : ControllerBase
{
    private readonly SessionService _sessionService;
    private readonly LoginHistoryService _loginHistoryService;

    public SessionController(SessionService sessionService, LoginHistoryService loginHistoryService)
    {
        _sessionService = sessionService;
        _loginHistoryService = loginHistoryService;
    }

    /// <summary>
    /// 取得當前使用者的所有活躍會話
    /// </summary>
    /// <returns>會話列表</returns>
    [HttpGet("active")]
    [ProducesResponseType(typeof(SessionListResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<SessionListResponse>> GetActiveSessions()
    {
        var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        var currentSessionId = User.FindFirst("sessionId")?.Value;

        var sessions = await _sessionService.GetUserActiveSessionsAsync(userId);

        var sessionInfos = sessions.Select(s => new SessionInfo
        {
            SessionId = s.SessionId,
            DeviceName = s.DeviceName,
            IpAddress = s.IpAddress,
            LoginAt = s.LoginAt,
            LastActivityAt = s.LastActivityAt,
            IsCurrent = s.SessionId == currentSessionId,
            ExpiresAt = s.ExpiresAt
        }).ToList();

        return Ok(new SessionListResponse
        {
            Code = 200,
            Message = "獲取成功",
            Data = sessionInfos
        });
    }

    /// <summary>
    /// 結束指定的會話
    /// </summary>
    /// <param name="sessionId">會話 ID</param>
    /// <returns>操作結果</returns>
    [HttpDelete("{sessionId}")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<object>>> EndSession(string sessionId)
    {
        var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        
        // 驗證會話屬於當前使用者
        var sessions = await _sessionService.GetUserActiveSessionsAsync(userId);
        var session = sessions.FirstOrDefault(s => s.SessionId == sessionId);

        if (session == null)
        {
            return NotFound(new ApiResponse<object>
            {
                Code = 404,
                Message = "會話不存在或已結束",
                Data = null
            });
        }

        var success = await _sessionService.EndSessionAsync(sessionId);

        if (!success)
        {
            return NotFound(new ApiResponse<object>
            {
                Code = 404,
                Message = "會話不存在",
                Data = null
            });
        }

        return Ok(new ApiResponse<object>
        {
            Code = 200,
            Message = "會話已結束",
            Data = null
        });
    }

    /// <summary>
    /// 取得登入歷史記錄
    /// </summary>
    /// <param name="limit">返回記錄數量（預設 50）</param>
    /// <returns>登入歷史列表</returns>
    [HttpGet("history")]
    [ProducesResponseType(typeof(LoginHistoryResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<LoginHistoryResponse>> GetLoginHistory([FromQuery] int limit = 50)
    {
        var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

        var histories = await _loginHistoryService.GetUserLoginHistoryAsync(userId, limit);

        var historyInfos = histories.Select(h => new LoginHistoryInfo
        {
            Id = h.Id,
            IsSuccess = h.IsSuccess,
            FailureReason = h.FailureReason,
            IpAddress = h.IpAddress,
            DeviceInfo = h.DeviceInfo,
            AttemptedAt = h.AttemptedAt,
            Location = h.Location
        }).ToList();

        return Ok(new LoginHistoryResponse
        {
            Code = 200,
            Message = "獲取成功",
            Data = historyInfos
        });
    }
}
