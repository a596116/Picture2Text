using System.ComponentModel.DataAnnotations;

namespace Picture2Text.Api.DTOs.Requests;

public class LoginRequest
{
    [Required(ErrorMessage = "使用者 ID 為必填")]
    public int UserId { get; set; }

    [Required(ErrorMessage = "密碼為必填")]
    [StringLength(255, MinimumLength = 6, ErrorMessage = "密碼長度必須在 6 到 255 個字元之間")]
    public string Password { get; set; } = string.Empty;
}
