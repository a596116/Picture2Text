using Microsoft.EntityFrameworkCore;
using Picture2Text.Api.Data;
using Picture2Text.Api.Models;

namespace Picture2Text.Api.Services.EfExamples;

/// <summary>
/// EF Core 查詢（Read）範例。
/// 含：主鍵、條件、分頁、游標分頁、Include、投影、動態條件、聚合。
/// </summary>
public static class EfReadExamples
{
    /// <summary>依主鍵查詢 FindAsync</summary>
    public static async Task<User?> GetByIdAsync(ApplicationDbContext context, int id)
    {
        return await context.Users.FindAsync(id);
    }

    /// <summary>條件單筆 FirstOrDefaultAsync</summary>
    public static async Task<User?> GetByIdNoAsync(ApplicationDbContext context, string idNo)
    {
        return await context.Users.FirstOrDefaultAsync(u => u.IdNo == idNo);
    }

    /// <summary>條件列表 + 排序 + 筆數</summary>
    public static async Task<List<LoginHistory>> GetUserLoginHistoryAsync(
        ApplicationDbContext context, int userId, int limit = 50)
    {
        return await context.LoginHistories
            .Where(h => h.UserId == userId)
            .OrderByDescending(h => h.AttemptedAt)
            .Take(limit)
            .ToListAsync();
    }

    /// <summary>Offset 分頁（小中型資料集）</summary>
    public static async Task<PagedResult<LoginHistory>> GetPagedAsync(
        ApplicationDbContext context, int userId, int page, int pageSize)
    {
        var query = context.LoginHistories
            .Where(h => h.UserId == userId)
            .OrderByDescending(h => h.AttemptedAt);

        var total = await query.CountAsync();
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PagedResult<LoginHistory>(items, total, page, pageSize);
    }

    /// <summary>大型專案：游標分頁（Keyset），大表時比 Offset 效能好，避免深分頁</summary>
    public static async Task<CursorPagedResult<LoginHistory>> GetCursorPagedAsync(
        ApplicationDbContext context,
        int userId,
        int? afterId,
        int pageSize = 20)
    {
        IQueryable<LoginHistory> query = context.LoginHistories
            .AsNoTracking()
            .Where(h => h.UserId == userId);

        if (afterId.HasValue)
            query = query.Where(h => h.Id < afterId.Value);

        var items = await query
            .OrderByDescending(h => h.AttemptedAt)
            .ThenByDescending(h => h.Id)
            .Take(pageSize + 1)
            .ToListAsync();
        var hasMore = items.Count > pageSize;
        if (hasMore) items = items.Take(pageSize).ToList();

        var nextCursor = hasMore && items.Count > 0 ? items[^1].Id : (int?)null;
        return new CursorPagedResult<LoginHistory>(items, nextCursor, hasMore);
    }

    /// <summary>Include 關聯（Eager Load）</summary>
    public static async Task<UserSession?> GetSessionWithUserAsync(
        ApplicationDbContext context, string sessionId)
    {
        return await context.UserSessions
            .Include(s => s.User)
            .Include(s => s.RefreshToken)
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.SessionId == sessionId);
    }

    /// <summary>唯讀列表 AsNoTracking</summary>
    public static async Task<List<User>> GetUsersReadOnlyAsync(ApplicationDbContext context)
    {
        return await context.Users
            .AsNoTracking()
            .OrderBy(u => u.Id)
            .ToListAsync();
    }

    /// <summary>只取部分欄位（不寫 DTO）：回傳 IQueryable，呼叫端用匿名型別投影。</summary>
    /// <example>
    /// // 方式一：呼叫端再 Select 匿名型別
    /// var list = await GetUsersQueryForProjection(context)
    ///     .Select(u => new { u.Id, u.Name })
    ///     .ToListAsync();
    /// // 方式二：查詢與使用寫在同一個方法內，直接用 var
    /// var rows = await context.Users.AsNoTracking()
    ///     .Select(u => new { u.Id, u.Name })
    ///     .ToListAsync();
    /// foreach (var r in rows) { ... r.Id ... r.Name ... }
    /// </example>
    public static IQueryable<User> GetUsersQueryForProjection(ApplicationDbContext context)
    {
        return context.Users.AsNoTracking();
    }


    /// <summary>只取部分欄位：用 Select 投影到 DTO（SQL 只會 SELECT 指定欄位）</summary>
    public static async Task<List<UserIdNameDto>> GetUserOnlyIdAndNameAsync(ApplicationDbContext context)
    {
        return await context.Users
            .AsNoTracking()
            .Select(u => new UserIdNameDto(u.Id, u.Name))
            .ToListAsync();
    }

    /// <summary>只取部分欄位：單筆，投影到 DTO</summary>
    public static async Task<UserSummaryDto?> GetUserSummaryByIdAsync(ApplicationDbContext context, int id)
    {
        return await context.Users
            .AsNoTracking()
            .Where(u => u.Id == id)
            .Select(u => new UserSummaryDto(u.Id, u.IdNo, u.Name))
            .FirstOrDefaultAsync();
    }

    /// <summary>只取部分欄位：投影到 DTO（多欄位、可重用）</summary>
    public static async Task<List<UserSummaryDto>> GetUserSummaryAsync(ApplicationDbContext context)
    {
        return await context.Users
            .AsNoTracking()
            .Select(u => new UserSummaryDto(u.Id, u.IdNo, u.Name))
            .ToListAsync();
    }

    /// <summary>只取部分欄位：LoginHistory 只取 Id、AttemptedAt、IsSuccess</summary>
    public static async Task<List<LoginHistoryPartialDto>> GetLoginHistoryPartialAsync(
        ApplicationDbContext context, int userId, int limit = 50)
    {
        return await context.LoginHistories
            .AsNoTracking()
            .Where(h => h.UserId == userId)
            .OrderByDescending(h => h.AttemptedAt)
            .Take(limit)
            .Select(h => new LoginHistoryPartialDto(h.Id, h.AttemptedAt, h.IsSuccess))
            .ToListAsync();
    }

    /// <summary>存在檢查 Any</summary>
    public static async Task<bool> UserExistsAsync(ApplicationDbContext context, string idNo)
    {
        return await context.Users.AnyAsync(u => u.IdNo == idNo);
    }

    /// <summary>聚合 Count</summary>
    public static async Task<int> CountSuccessLoginsAsync(
        ApplicationDbContext context, int userId, DateTime? since = null)
    {
        var query = context.LoginHistories.Where(h => h.UserId == userId && h.IsSuccess);
        if (since.HasValue) query = query.Where(h => h.AttemptedAt >= since);
        return await query.CountAsync();
    }

    /// <summary>動態條件 IQueryable（搜尋/報表常用）</summary>
    public static async Task<List<LoginHistory>> SearchAsync(
        ApplicationDbContext context,
        int? userId = null,
        bool? isSuccess = null,
        DateTime? start = null,
        DateTime? end = null,
        int take = 100)
    {
        IQueryable<LoginHistory> query = context.LoginHistories.AsNoTracking();
        if (userId.HasValue) query = query.Where(h => h.UserId == userId);
        if (isSuccess.HasValue) query = query.Where(h => h.IsSuccess == isSuccess);
        if (start.HasValue) query = query.Where(h => h.AttemptedAt >= start);
        if (end.HasValue) query = query.Where(h => h.AttemptedAt <= end);

        return await query
            .OrderByDescending(h => h.AttemptedAt)
            .Take(take)
            .ToListAsync();
    }

    /// <summary>大型專案：依 Filter 物件組查詢（規格模式簡化版）</summary>
    public static async Task<PagedResult<LoginHistory>> GetByFilterAsync(
        ApplicationDbContext context,
        LoginHistoryFilter filter,
        CancellationToken cancellationToken = default)
    {
        IQueryable<LoginHistory> query = context.LoginHistories.AsNoTracking();
        if (filter.UserId.HasValue) query = query.Where(h => h.UserId == filter.UserId);
        if (filter.IsSuccess.HasValue) query = query.Where(h => h.IsSuccess == filter.IsSuccess);
        if (filter.StartDate.HasValue) query = query.Where(h => h.AttemptedAt >= filter.StartDate);
        if (filter.EndDate.HasValue) query = query.Where(h => h.AttemptedAt <= filter.EndDate);

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(h => h.AttemptedAt)
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<LoginHistory>(items, total, filter.Page, filter.PageSize);
    }
}
