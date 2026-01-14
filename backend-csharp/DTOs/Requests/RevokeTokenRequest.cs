using System.ComponentModel.DataAnnotations;

namespace Picture2Text.Api.DTOs.Requests;

/// <summary>
/// 撤銷 Token 請求
/// </summary>
public class RevokeTokenRequest
{
    /// <summary>
    /// 要撤銷的 Refresh Token（可選，如果不提供則撤銷當前使用者的所有 token）
    /// </summary>
    public string? RefreshToken { get; set; }

    /// <summary>
    /// 是否撤銷所有裝置的 token
    /// </summary>
    public bool RevokeAllDevices { get; set; } = false;
}
