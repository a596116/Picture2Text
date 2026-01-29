using Picture2Text.Api.Models;

namespace Picture2Text.Api.Services.EfExamples;

/// <summary>查詢投影用 DTO</summary>
public record UserSummaryDto(int Id, string IdNo, string Name);

/// <summary>只取 Id、Name 時用</summary>
public record UserIdNameDto(int Id, string Name);

/// <summary>登入歷史只取部分欄位</summary>
public record LoginHistoryPartialDto(int Id, DateTime AttemptedAt, bool IsSuccess);

/// <summary>分頁結果（泛型）</summary>
public record PagedResult<T>(IReadOnlyList<T> Items, int Total, int Page, int PageSize);

/// <summary>游標分頁用（大型清單避免 Offset 效能問題）</summary>
public record CursorPagedResult<T>(IReadOnlyList<T> Items, int? NextCursor, bool HasMore);

/// <summary>登入歷史查詢條件（規格模式用）</summary>
public class LoginHistoryFilter
{
    public int? UserId { get; set; }
    public bool? IsSuccess { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

// ----- JOIN 查詢結果用 DTO -----

/// <summary>登入歷史 + 使用者名稱（User JOIN LoginHistory 投影）</summary>
public record LoginHistoryWithUserNameDto(
    int HistoryId,
    int? UserId,
    string UserName,
    string AttemptedUserId,
    bool IsSuccess,
    DateTime AttemptedAt,
    string? IpAddress);

/// <summary>會話明細（User + UserSession + RefreshToken 多表 JOIN 投影）</summary>
public record SessionDetailDto(
    int SessionId,
    int UserId,
    string UserName,
    string SessionGuid,
    DateTime LoginAt,
    DateTime? RefreshTokenExpiresAt,
    bool IsActive);

/// <summary>JOIN 結果：User + LoginHistory 一筆</summary>
public class UserWithHistoryItem
{
    public User User { get; set; } = null!;
    public LoginHistory History { get; set; } = null!;
}

/// <summary>JOIN 結果：User + UserSession + RefreshToken</summary>
public class SessionWithUserAndTokenItem
{
    public User User { get; set; } = null!;
    public UserSession Session { get; set; } = null!;
    public RefreshToken Token { get; set; } = null!;
}

/// <summary>JOIN 結果：User + 其登入歷史列表（GroupJoin）</summary>
public class UserWithHistoriesItem
{
    public User User { get; set; } = null!;
    public List<LoginHistory> Histories { get; set; } = null!;
}
