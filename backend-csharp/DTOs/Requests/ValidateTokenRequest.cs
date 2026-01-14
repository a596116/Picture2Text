using System.ComponentModel.DataAnnotations;

namespace Picture2Text.Api.DTOs.Requests;

/// <summary>
/// 驗證 Token 請求（供其他微服務使用）
/// </summary>
public class ValidateTokenRequest
{
    /// <summary>
    /// 要驗證的 Access Token
    /// </summary>
    [Required(ErrorMessage = "Token 為必填項")]
    public string Token { get; set; } = string.Empty;
}
