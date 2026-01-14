using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Picture2Text.Api.Models;

/// <summary>
/// 使用者會話模型 - 追蹤活躍的登入會話
/// </summary>
[Table("UserSession")]
public class UserSession
{
    [Key]
    [Column("ID")]
    public int Id { get; set; }

    /// <summary>
    /// 關聯的使用者 ID
    /// </summary>
    [Required]
    [Column("UserID")]
    public int UserId { get; set; }

    /// <summary>
    /// 會話唯一識別碼
    /// </summary>
    [Required]
    [StringLength(100)]
    [Column("SessionId")]
    public string SessionId { get; set; } = string.Empty;

    /// <summary>
    /// 關聯的 Refresh Token ID
    /// </summary>
    [Column("RefreshTokenId")]
    public int? RefreshTokenId { get; set; }

    /// <summary>
    /// 裝置名稱/類型
    /// </summary>
    [StringLength(200)]
    [Column("DeviceName")]
    public string? DeviceName { get; set; }

    /// <summary>
    /// IP 地址
    /// </summary>
    [StringLength(50)]
    [Column("IpAddress")]
    public string? IpAddress { get; set; }

    /// <summary>
    /// User-Agent
    /// </summary>
    [StringLength(1000)]
    [Column("UserAgent")]
    public string? UserAgent { get; set; }

    /// <summary>
    /// 登入時間
    /// </summary>
    [Required]
    [Column("LoginAt")]
    public DateTime LoginAt { get; set; }

    /// <summary>
    /// 最後活躍時間
    /// </summary>
    [Required]
    [Column("LastActivityAt")]
    public DateTime LastActivityAt { get; set; }

    /// <summary>
    /// 登出時間
    /// </summary>
    [Column("LogoutAt")]
    public DateTime? LogoutAt { get; set; }

    /// <summary>
    /// 會話過期時間
    /// </summary>
    [Required]
    [Column("ExpiresAt")]
    public DateTime ExpiresAt { get; set; }

    /// <summary>
    /// 是否為當前活躍會話
    /// </summary>
    [Required]
    [Column("IsActive")]
    public bool IsActive { get; set; } = true;

    // Navigation properties
    [ForeignKey("UserId")]
    public virtual User User { get; set; } = null!;

    [ForeignKey("RefreshTokenId")]
    public virtual RefreshToken? RefreshToken { get; set; }
}
