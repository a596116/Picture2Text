using System.IdentityModel.Tokens.Jwt;
using BCrypt.Net;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Picture2Text.Api.Data;
using Picture2Text.Api.DTOs.Requests;
using Picture2Text.Api.DTOs.Responses;
using Picture2Text.Api.Models;

namespace Picture2Text.Api.Services;

public class AuthService
{
    private readonly ApplicationDbContext _context;
    private readonly JwtService _jwtService;
    private readonly RefreshTokenService _refreshTokenService;
    private readonly SessionService _sessionService;
    private readonly LoginHistoryService _loginHistoryService;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public AuthService(
        ApplicationDbContext context,
        JwtService jwtService,
        RefreshTokenService refreshTokenService,
        SessionService sessionService,
        LoginHistoryService loginHistoryService,
        IHttpContextAccessor httpContextAccessor)
    {
        _context = context;
        _jwtService = jwtService;
        _refreshTokenService = refreshTokenService;
        _sessionService = sessionService;
        _loginHistoryService = loginHistoryService;
        _httpContextAccessor = httpContextAccessor;
    }

    /// <summary>
    /// 使用者登入
    /// </summary>
    public async Task<TokenResponse> LoginAsync(LoginRequest request)
    {
        var attemptedUserId = request.UserId.ToString();
        
        // 檢查是否被暴力破解（15分鐘內超過5次失敗）
        var recentFailures = await _loginHistoryService.GetRecentFailedAttemptsAsync(attemptedUserId, 15);
        if (recentFailures >= 5)
        {
            await RecordLoginAttempt(attemptedUserId, null, false, "帳號已被鎖定");
            
            return new TokenResponse
            {
                Code = 429,
                Message = "登入嘗試次數過多，請稍後再試",
                Data = null
            };
        }

        // 驗證使用者
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == request.UserId);

        if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.Password))
        {
            await RecordLoginAttempt(attemptedUserId, user?.Id, false, "帳號或密碼錯誤");
            
            return new TokenResponse
            {
                Code = 401,
                Message = "使用者 ID 或密碼錯誤",
                Data = null
            };
        }

        // 取得客戶端資訊
        var httpContext = _httpContextAccessor.HttpContext;
        var ipAddress = httpContext?.Connection.RemoteIpAddress?.ToString();
        var userAgent = httpContext?.Request.Headers["User-Agent"].ToString();
        var deviceInfo = ExtractDeviceInfo(userAgent);

        // 生成 Refresh Token
        var refreshToken = await _refreshTokenService.GenerateRefreshTokenAsync(
            user.Id, ipAddress, userAgent, deviceInfo);

        // 創建會話
        var session = await _sessionService.CreateSessionAsync(
            user.Id, refreshToken.Id, ipAddress, userAgent);

        // 生成 Access Token（包含會話 ID）
        var accessToken = _jwtService.GenerateToken(user.Id, user.IdNo, session.SessionId);
        var expiresAt = _jwtService.GetTokenExpiration();

        // 記錄成功登入
        await RecordLoginAttempt(attemptedUserId, user.Id, true, null);

        return new TokenResponse
        {
            Code = 200,
            Message = "登入成功",
            Data = new TokenData
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken.Token,
                TokenType = "Bearer",
                ExpiresAt = expiresAt,
                RefreshTokenExpiresAt = refreshToken.ExpiresAt,
                SessionId = session.SessionId
            }
        };
    }

    /// <summary>
    /// 刷新 Access Token
    /// </summary>
    public async Task<TokenResponse> RefreshTokenAsync(RefreshTokenRequest request)
    {
        var refreshToken = await _refreshTokenService.ValidateRefreshTokenAsync(request.RefreshToken);

        if (refreshToken == null)
        {
            return new TokenResponse
            {
                Code = 401,
                Message = "無效或已過期的 Refresh Token",
                Data = null
            };
        }

        var user = await _context.Users.FindAsync(refreshToken.UserId);
        if (user == null)
        {
            return new TokenResponse
            {
                Code = 401,
                Message = "使用者不存在",
                Data = null
            };
        }

        // Token Rotation: 撤銷舊的 refresh token 並生成新的
        var httpContext = _httpContextAccessor.HttpContext;
        var ipAddress = httpContext?.Connection.RemoteIpAddress?.ToString();
        var userAgent = httpContext?.Request.Headers["User-Agent"].ToString();
        var deviceInfo = ExtractDeviceInfo(userAgent);

        // 生成新的 Refresh Token
        var newRefreshToken = await _refreshTokenService.GenerateRefreshTokenAsync(
            user.Id, ipAddress, userAgent, deviceInfo);

        // 撤銷舊的 Refresh Token
        await _refreshTokenService.RevokeRefreshTokenAsync(
            request.RefreshToken, 
            newRefreshToken.TokenId);

        // 查找相關會話並更新
        var session = await _context.UserSessions
            .FirstOrDefaultAsync(s => s.RefreshTokenId == refreshToken.Id && s.IsActive);

        if (session != null)
        {
            session.RefreshTokenId = newRefreshToken.Id;
            session.LastActivityAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }

        // 生成新的 Access Token
        var accessToken = _jwtService.GenerateToken(user.Id, user.IdNo, session?.SessionId);
        var expiresAt = _jwtService.GetTokenExpiration();

        return new TokenResponse
        {
            Code = 200,
            Message = "Token 刷新成功",
            Data = new TokenData
            {
                AccessToken = accessToken,
                RefreshToken = newRefreshToken.Token,
                TokenType = "Bearer",
                ExpiresAt = expiresAt,
                RefreshTokenExpiresAt = newRefreshToken.ExpiresAt,
                SessionId = session?.SessionId ?? string.Empty
            }
        };
    }

    /// <summary>
    /// 撤銷 Token（登出）
    /// </summary>
    public async Task<ApiResponse<object>> RevokeTokenAsync(RevokeTokenRequest request, int userId)
    {
        if (request.RevokeAllDevices)
        {
            // 撤銷所有裝置的 token
            await _refreshTokenService.RevokeAllUserTokensAsync(userId);
            await _sessionService.EndAllUserSessionsAsync(userId);

            return new ApiResponse<object>
            {
                Code = 200,
                Message = "已登出所有裝置",
                Data = null
            };
        }

        if (!string.IsNullOrEmpty(request.RefreshToken))
        {
            // 撤銷指定的 refresh token
            var success = await _refreshTokenService.RevokeRefreshTokenAsync(request.RefreshToken);
            
            if (!success)
            {
                return new ApiResponse<object>
                {
                    Code = 400,
                    Message = "無效的 Refresh Token",
                    Data = null
                };
            }

            // 結束相關會話
            var refreshToken = await _context.RefreshTokens
                .FirstOrDefaultAsync(rt => rt.Token == request.RefreshToken);
            
            if (refreshToken != null)
            {
                var session = await _context.UserSessions
                    .FirstOrDefaultAsync(s => s.RefreshTokenId == refreshToken.Id);
                
                if (session != null)
                {
                    await _sessionService.EndSessionAsync(session.SessionId);
                }
            }

            return new ApiResponse<object>
            {
                Code = 200,
                Message = "登出成功",
                Data = null
            };
        }

        return new ApiResponse<object>
        {
            Code = 400,
            Message = "請提供 Refresh Token 或設定 RevokeAllDevices",
            Data = null
        };
    }

    /// <summary>
    /// 驗證 Token（供其他微服務使用）
    /// </summary>
    public async Task<ValidateTokenResponse> ValidateTokenAsync(ValidateTokenRequest request)
    {
        var principal = await _jwtService.ValidateTokenAsync(request.Token);

        if (principal == null)
        {
            return new ValidateTokenResponse
            {
                Code = 401,
                Message = "Token 無效或已過期",
                Data = new ValidateTokenData
                {
                    IsValid = false,
                    ErrorMessage = "Token 無效或已過期"
                }
            };
        }

        var userId = _jwtService.GetUserIdFromToken(principal);
        var tokenId = _jwtService.GetTokenIdFromToken(principal);

        if (!userId.HasValue)
        {
            return new ValidateTokenResponse
            {
                Code = 401,
                Message = "無法從 Token 中提取使用者資訊",
                Data = new ValidateTokenData
                {
                    IsValid = false,
                    ErrorMessage = "無法從 Token 中提取使用者資訊"
                }
            };
        }

        var user = await _context.Users.FindAsync(userId.Value);
        if (user == null)
        {
            return new ValidateTokenResponse
            {
                Code = 401,
                Message = "使用者不存在",
                Data = new ValidateTokenData
                {
                    IsValid = false,
                    ErrorMessage = "使用者不存在"
                }
            };
        }

        var expClaim = principal.FindFirst(JwtRegisteredClaimNames.Exp)?.Value;
        DateTime? expiresAt = null;
        if (long.TryParse(expClaim, out var expUnix))
        {
            expiresAt = DateTimeOffset.FromUnixTimeSeconds(expUnix).UtcDateTime;
        }

        return new ValidateTokenResponse
        {
            Code = 200,
            Message = "Token 驗證成功",
            Data = new ValidateTokenData
            {
                IsValid = true,
                UserId = user.Id,
                IdNo = user.IdNo,
                Name = user.Name,
                ExpiresAt = expiresAt,
                TokenId = tokenId
            }
        };
    }

    /// <summary>
    /// 記錄登入嘗試
    /// </summary>
    private async Task RecordLoginAttempt(
        string attemptedUserId, 
        int? userId, 
        bool isSuccess, 
        string? failureReason)
    {
        var httpContext = _httpContextAccessor.HttpContext;
        var ipAddress = httpContext?.Connection.RemoteIpAddress?.ToString();
        var userAgent = httpContext?.Request.Headers["User-Agent"].ToString();
        var deviceInfo = ExtractDeviceInfo(userAgent);

        await _loginHistoryService.RecordLoginAttemptAsync(
            attemptedUserId, userId, isSuccess, failureReason, 
            ipAddress, userAgent, deviceInfo);
    }

    /// <summary>
    /// 從 User-Agent 提取裝置資訊
    /// </summary>
    private static string? ExtractDeviceInfo(string? userAgent)
    {
        if (string.IsNullOrEmpty(userAgent))
        {
            return null;
        }

        // 簡單提取，可以使用更完善的 User-Agent 解析庫
        return userAgent.Length > 500 ? userAgent[..500] : userAgent;
    }
}

