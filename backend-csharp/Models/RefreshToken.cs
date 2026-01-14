using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Picture2Text.Api.Models;

/// <summary>
/// Refresh Token 模型 - 用於延長使用者登入狀態
/// </summary>
[Table("RefreshToken")]
public class RefreshToken
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
    /// Refresh Token 值（已加密）
    /// </summary>
    [Required]
    [StringLength(500)]
    [Column("Token")]
    public string Token { get; set; } = string.Empty;

    /// <summary>
    /// Token 的唯一識別碼（jti）
    /// </summary>
    [Required]
    [StringLength(100)]
    [Column("TokenId")]
    public string TokenId { get; set; } = string.Empty;

    /// <summary>
    /// 創建時間
    /// </summary>
    [Required]
    [Column("CreatedAt")]
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// 過期時間
    /// </summary>
    [Required]
    [Column("ExpiresAt")]
    public DateTime ExpiresAt { get; set; }

    /// <summary>
    /// 是否已被撤銷
    /// </summary>
    [Required]
    [Column("IsRevoked")]
    public bool IsRevoked { get; set; }

    /// <summary>
    /// 撤銷時間
    /// </summary>
    [Column("RevokedAt")]
    public DateTime? RevokedAt { get; set; }

    /// <summary>
    /// 替換此 Token 的新 Token ID（Token Rotation）
    /// </summary>
    [StringLength(100)]
    [Column("ReplacedByToken")]
    public string? ReplacedByToken { get; set; }

    /// <summary>
    /// 裝置資訊
    /// </summary>
    [StringLength(500)]
    [Column("DeviceInfo")]
    public string? DeviceInfo { get; set; }

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
    /// 最後使用時間
    /// </summary>
    [Column("LastUsedAt")]
    public DateTime? LastUsedAt { get; set; }

    // Navigation property
    [ForeignKey("UserId")]
    public virtual User User { get; set; } = null!;

    /// <summary>
    /// 檢查 Token 是否有效
    /// </summary>
    public bool IsActive => !IsRevoked && DateTime.UtcNow < ExpiresAt;
}
