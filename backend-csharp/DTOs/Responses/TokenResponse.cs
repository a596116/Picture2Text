namespace Picture2Text.Api.DTOs.Responses;

/// <summary>
/// Token 回應資料
/// </summary>
public class TokenData
{
    /// <summary>
    /// Access Token（用於 API 訪問）
    /// </summary>
    public string AccessToken { get; set; } = string.Empty;

    /// <summary>
    /// Refresh Token（用於刷新 Access Token）
    /// </summary>
    public string RefreshToken { get; set; } = string.Empty;

    /// <summary>
    /// Token 類型
    /// </summary>
    public string TokenType { get; set; } = "Bearer";

    /// <summary>
    /// Access Token 過期時間
    /// </summary>
    public DateTime ExpiresAt { get; set; }

    /// <summary>
    /// Refresh Token 過期時間
    /// </summary>
    public DateTime RefreshTokenExpiresAt { get; set; }

    /// <summary>
    /// 會話 ID
    /// </summary>
    public string SessionId { get; set; } = string.Empty;
}

/// <summary>
/// Token 回應
/// </summary>
public class TokenResponse : ApiResponse<TokenData>
{
}
