using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Picture2Text.Api.Models;

/// <summary>
/// 登入歷史記錄模型 - 用於安全審計
/// </summary>
[Table("LoginHistory")]
public class LoginHistory
{
    [Key]
    [Column("ID")]
    public int Id { get; set; }

    /// <summary>
    /// 關聯的使用者 ID（可為空，登入失敗時可能找不到使用者）
    /// </summary>
    [Column("UserID")]
    public int? UserId { get; set; }

    /// <summary>
    /// 嘗試登入的帳號
    /// </summary>
    [Required]
    [StringLength(50)]
    [Column("AttemptedUserId")]
    public string AttemptedUserId { get; set; } = string.Empty;

    /// <summary>
    /// 登入是否成功
    /// </summary>
    [Required]
    [Column("IsSuccess")]
    public bool IsSuccess { get; set; }

    /// <summary>
    /// 失敗原因
    /// </summary>
    [StringLength(200)]
    [Column("FailureReason")]
    public string? FailureReason { get; set; }

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
    /// 裝置資訊
    /// </summary>
    [StringLength(500)]
    [Column("DeviceInfo")]
    public string? DeviceInfo { get; set; }

    /// <summary>
    /// 登入時間
    /// </summary>
    [Required]
    [Column("AttemptedAt")]
    public DateTime AttemptedAt { get; set; }

    /// <summary>
    /// 地理位置（可選）
    /// </summary>
    [StringLength(200)]
    [Column("Location")]
    public string? Location { get; set; }

    // Navigation property
    [ForeignKey("UserId")]
    public virtual User? User { get; set; }
}
