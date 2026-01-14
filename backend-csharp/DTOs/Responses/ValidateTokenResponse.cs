namespace Picture2Text.Api.DTOs.Responses;

/// <summary>
/// Token 驗證結果資料
/// </summary>
public class ValidateTokenData
{
    /// <summary>
    /// Token 是否有效
    /// </summary>
    public bool IsValid { get; set; }

    /// <summary>
    /// 使用者 ID
    /// </summary>
    public int? UserId { get; set; }

    /// <summary>
    /// 使用者身份證號
    /// </summary>
    public string? IdNo { get; set; }

    /// <summary>
    /// 使用者姓名
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// Token 過期時間
    /// </summary>
    public DateTime? ExpiresAt { get; set; }

    /// <summary>
    /// Token 的唯一識別碼（jti）
    /// </summary>
    public string? TokenId { get; set; }

    /// <summary>
    /// 錯誤訊息（如果無效）
    /// </summary>
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// Token 驗證回應
/// </summary>
public class ValidateTokenResponse : ApiResponse<ValidateTokenData>
{
}
