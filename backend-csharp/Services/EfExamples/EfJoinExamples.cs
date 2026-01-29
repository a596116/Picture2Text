using Microsoft.EntityFrameworkCore;
using Picture2Text.Api.Data;
using Picture2Text.Api.Models;

namespace Picture2Text.Api.Services.EfExamples;

/// <summary>
/// EF Core 多表 JOIN 範例。
/// 含：LINQ join 語法、導覽屬性（Include）、多表 JOIN 投影、Left Join 風格、原始 SQL JOIN。
/// </summary>
public static class EfJoinExamples
{
    // ----- 方式一：LINQ join（對應 SQL INNER JOIN） -----

    /// <summary>兩表 INNER JOIN：User JOIN LoginHistory，取得有登入記錄的使用者與歷史</summary>
    public static async Task<List<UserWithHistoryItem>> GetUsersWithLoginHistoryAsync(
        ApplicationDbContext context, int limit = 100)
    {
        var query =
            from u in context.Users.AsNoTracking()
            join h in context.LoginHistories.AsNoTracking() on u.Id equals h.UserId
            orderby h.AttemptedAt descending
            select new UserWithHistoryItem { User = u, History = h };

        return await query.Take(limit).ToListAsync();
    }

    /// <summary>兩表 JOIN + 投影到 DTO（報表常用：登入歷史帶使用者名稱）</summary>
    public static async Task<List<LoginHistoryWithUserNameDto>> GetLoginHistoryWithUserNameAsync(
        ApplicationDbContext context,
        DateTime? from = null,
        DateTime? to = null,
        int limit = 200)
    {
        var query =
            from h in context.LoginHistories.AsNoTracking()
            join u in context.Users.AsNoTracking() on h.UserId equals u.Id into userGroup
            from u in userGroup.DefaultIfEmpty()  // LEFT JOIN：沒有對應 User 的歷史也會出現
            orderby h.AttemptedAt descending
            select new LoginHistoryWithUserNameDto(
                h.Id,
                h.UserId,
                u != null ? u.Name : "(未知)",
                h.AttemptedUserId,
                h.IsSuccess,
                h.AttemptedAt,
                h.IpAddress);

        if (from.HasValue) query = query.Where(x => x.AttemptedAt >= from);
        if (to.HasValue) query = query.Where(x => x.AttemptedAt <= to);

        return await query.Take(limit).ToListAsync();
    }

    /// <summary>三表 INNER JOIN：User + UserSession + RefreshToken，活躍會話明細</summary>
    public static async Task<List<SessionDetailDto>> GetActiveSessionDetailsAsync(
        ApplicationDbContext context)
    {
        var query =
            from s in context.UserSessions.AsNoTracking()
            join u in context.Users.AsNoTracking() on s.UserId equals u.Id
            join rt in context.RefreshTokens.AsNoTracking() on s.RefreshTokenId equals rt.Id into rtGroup
            from rt in rtGroup.DefaultIfEmpty()
            where s.IsActive
            orderby s.LastActivityAt descending
            select new SessionDetailDto(
                s.Id,
                s.UserId,
                u.Name,
                s.SessionId,
                s.LoginAt,
                rt != null ? rt.ExpiresAt : (DateTime?)null,
                s.IsActive);

        return await query.ToListAsync();
    }

    /// <summary>三表純 INNER JOIN（無 Left）：有 User、有 Session、有 Token 的記錄</summary>
    public static async Task<List<SessionWithUserAndTokenItem>> GetSessionsWithUserAndTokenAsync(
        ApplicationDbContext context, bool activeOnly = true)
    {
        var query =
            from s in context.UserSessions.AsNoTracking()
            join u in context.Users.AsNoTracking() on s.UserId equals u.Id
            join rt in context.RefreshTokens.AsNoTracking() on s.RefreshTokenId equals rt.Id
            where !activeOnly || s.IsActive
            orderby s.LastActivityAt descending
            select new SessionWithUserAndTokenItem { User = u, Session = s, Token = rt };

        return await query.ToListAsync();
    }

    // ----- 方式二：導覽屬性 + Include（EF 自動 JOIN，取回完整實體） -----

    /// <summary>Include 多表：一次載入 Session + User + RefreshToken（Eager Load）</summary>
    public static async Task<List<UserSession>> GetSessionsWithRelatedAsync(
        ApplicationDbContext context, bool activeOnly = true)
    {
        var query = context.UserSessions
            .Include(s => s.User)
            .Include(s => s.RefreshToken)
            .AsNoTracking();

        if (activeOnly) query = query.Where(s => s.IsActive);

        return await query.OrderByDescending(s => s.LastActivityAt).ToListAsync();
    }

    /// <summary>從「多」的那邊 Include「一」：LoginHistory 帶出 User</summary>
    public static async Task<List<LoginHistory>> GetLoginHistoryWithUserAsync(
        ApplicationDbContext context, int userId, int limit = 50)
    {
        return await context.LoginHistories
            .Include(h => h.User)
            .AsNoTracking()
            .Where(h => h.UserId == userId)
            .OrderByDescending(h => h.AttemptedAt)
            .Take(limit)
            .ToListAsync();
    }

    // ----- 方式三：Select 內直接取關聯（單一 SQL，無 Include 實體） -----

    /// <summary>單一查詢 + 投影：只取需要的欄位，JOIN 在 Select 內完成（EF 會產生 LEFT JOIN User）</summary>
    public static async Task<List<LoginHistoryWithUserNameDto>> GetHistoryWithUserNameProjectionAsync(
        ApplicationDbContext context, int? userId = null, int limit = 100)
    {
        var query = context.LoginHistories.AsNoTracking();
        if (userId.HasValue) query = query.Where(h => h.UserId == userId);

        return await query
            .OrderByDescending(h => h.AttemptedAt)
            .Take(limit)
            .Select(h => new LoginHistoryWithUserNameDto(
                h.Id,
                h.UserId,
                h.User != null ? h.User.Name : "(未知)",
                h.AttemptedUserId,
                h.IsSuccess,
                h.AttemptedAt,
                h.IpAddress))
            .ToListAsync();
    }

    // ----- 方式四：GroupJoin（對應 SQL LEFT JOIN / 一對多） -----

    /// <summary>GroupJoin：每個使用者與其登入歷史（一對多），用於「使用者 + 最近 N 筆歷史」</summary>
    public static async Task<List<UserWithHistoriesItem>> GetUsersWithRecentHistoryAsync(
        ApplicationDbContext context, int historyPerUser = 5)
    {
        var query =
            from u in context.Users.AsNoTracking()
            join h in context.LoginHistories.AsNoTracking()
                on u.Id equals h.UserId into histories
            select new UserWithHistoriesItem
            {
                User = u,
                Histories = histories.OrderByDescending(x => x.AttemptedAt).Take(historyPerUser).ToList()
            };

        return await query.ToListAsync();
    }

    // ----- 方式五：原始 SQL（單表或 JOIN 篩選） -----

    /// <summary>原始 SQL：多表 JOIN 僅用於篩選條件（例如只取「有對應使用者」的登入歷史）</summary>
    /// <remarks>
    /// FromSqlRaw 回傳型別需對應單一實體，故 JOIN 多用於 WHERE 條件。
    /// 若要回傳 DTO 且含多表欄位，建議用上面 LINQ join + Select 投影。
    /// </remarks>
    public static async Task<List<LoginHistory>> GetHistoryByRawSqlWithJoinFilterAsync(
        ApplicationDbContext context, DateTime from, DateTime to, int limit = 100)
    {
        return await context.LoginHistories
            .FromSqlRaw(
                @"SELECT h.* FROM LoginHistory h
                  INNER JOIN [User] u ON h.UserID = u.ID
                  WHERE h.AttemptedAt >= {0} AND h.AttemptedAt <= {1}
                  ORDER BY h.AttemptedAt DESC",
                from, to)
            .AsNoTracking()
            .Take(limit)
            .ToListAsync();
    }
}
