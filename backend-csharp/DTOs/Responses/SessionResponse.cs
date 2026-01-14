namespace Picture2Text.Api.DTOs.Responses;

/// <summary>
/// 會話資訊
/// </summary>
public class SessionInfo
{
    /// <summary>
    /// 會話 ID
    /// </summary>
    public string SessionId { get; set; } = string.Empty;

    /// <summary>
    /// 裝置名稱
    /// </summary>
    public string? DeviceName { get; set; }

    /// <summary>
    /// IP 地址
    /// </summary>
    public string? IpAddress { get; set; }

    /// <summary>
    /// 登入時間
    /// </summary>
    public DateTime LoginAt { get; set; }

    /// <summary>
    /// 最後活躍時間
    /// </summary>
    public DateTime LastActivityAt { get; set; }

    /// <summary>
    /// 是否為當前會話
    /// </summary>
    public bool IsCurrent { get; set; }

    /// <summary>
    /// 過期時間
    /// </summary>
    public DateTime ExpiresAt { get; set; }
}

/// <summary>
/// 會話列表回應
/// </summary>
public class SessionListResponse : ApiResponse<List<SessionInfo>>
{
}
