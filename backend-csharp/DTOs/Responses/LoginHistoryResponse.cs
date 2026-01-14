namespace Picture2Text.Api.DTOs.Responses;

/// <summary>
/// 登入歷史記錄資訊
/// </summary>
public class LoginHistoryInfo
{
    /// <summary>
    /// 記錄 ID
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// 登入是否成功
    /// </summary>
    public bool IsSuccess { get; set; }

    /// <summary>
    /// 失敗原因
    /// </summary>
    public string? FailureReason { get; set; }

    /// <summary>
    /// IP 地址
    /// </summary>
    public string? IpAddress { get; set; }

    /// <summary>
    /// 裝置資訊
    /// </summary>
    public string? DeviceInfo { get; set; }

    /// <summary>
    /// 登入時間
    /// </summary>
    public DateTime AttemptedAt { get; set; }

    /// <summary>
    /// 地理位置
    /// </summary>
    public string? Location { get; set; }
}

/// <summary>
/// 登入歷史列表回應
/// </summary>
public class LoginHistoryResponse : ApiResponse<List<LoginHistoryInfo>>
{
}
