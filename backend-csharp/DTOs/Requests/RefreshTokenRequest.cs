using System.ComponentModel.DataAnnotations;

namespace Picture2Text.Api.DTOs.Requests;

/// <summary>
/// 刷新 Token 請求
/// </summary>
public class RefreshTokenRequest
{
    /// <summary>
    /// Refresh Token
    /// </summary>
    [Required(ErrorMessage = "Refresh Token 為必填項")]
    public string RefreshToken { get; set; } = string.Empty;
}
