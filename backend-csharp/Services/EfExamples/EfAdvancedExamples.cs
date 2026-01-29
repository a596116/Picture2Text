using Microsoft.EntityFrameworkCore;
using Picture2Text.Api.Data;
using Picture2Text.Api.Models;

namespace Picture2Text.Api.Services.EfExamples;

/// <summary>
/// EF Core 進階範例：大型專案常見情境。
/// 含：原始 SQL、編譯查詢、Split Query、ChangeTracker、審計欄位、規格查詢建構、連線重試建議。
/// </summary>
public static class EfAdvancedExamples
{
    // ----- 原始 SQL -----

    /// <summary>FromSqlInterpolated 參數化查詢（避免 SQL 注入）</summary>
    public static async Task<User?> GetUserByRawSqlAsync(
        ApplicationDbContext context, string idNo)
    {
        return await context.Users
            .FromSqlInterpolated($"SELECT * FROM [User] WHERE ID_NO = {idNo}")
            .AsNoTracking()
            .FirstOrDefaultAsync();
    }

    /// <summary>FromSqlRaw 帶參數</summary>
    public static async Task<List<LoginHistory>> GetHistoryByRawSqlAsync(
        ApplicationDbContext context, int userId, int limit)
    {
        return await context.LoginHistories
            .FromSqlRaw(
                "SELECT * FROM LoginHistory WHERE UserID = {0} ORDER BY AttemptedAt DESC",
                userId)
            .AsNoTracking()
            .Take(limit)
            .ToListAsync();
    }

    /// <summary>執行任意 SQL（不回傳實體）ExecuteSqlRawAsync</summary>
    public static async Task<int> ExecuteRawSqlAsync(
        ApplicationDbContext context, string newName, int userId)
    {
        return await context.Database.ExecuteSqlInterpolatedAsync(
            $"UPDATE [User] SET Name = {newName} WHERE ID = {userId}");
    }

    // ----- 編譯查詢（熱路徑效能） -----

    private static readonly Func<ApplicationDbContext, int, Task<User?>> GetUserByIdCompiled =
        EF.CompileAsyncQuery((ApplicationDbContext ctx, int id) =>
            ctx.Users.FirstOrDefault(u => u.Id == id));

    /// <summary>大型專案：編譯查詢，高頻呼叫時可減少編譯開銷</summary>
    public static Task<User?> GetUserByIdCompiledAsync(ApplicationDbContext context, int id)
    {
        return GetUserByIdCompiled(context, id);
    }

    // ----- Split Query（避免 Include 多對多造成笛卡爾積） -----

    /// <summary>大型專案：Split Query，多個 Include 時拆成多個 SQL 減少資料膨脹</summary>
    public static async Task<UserSession?> GetSessionWithSplitQueryAsync(
        ApplicationDbContext context, string sessionId)
    {
        return await context.UserSessions
            .Include(s => s.User)
            .Include(s => s.RefreshToken)
            .AsSplitQuery()  // 多個 Include 時可避免單一巨大 JOIN
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.SessionId == sessionId);
    }

    // ----- ChangeTracker -----

    /// <summary>取得所有已修改的實體與欄位</summary>
    public static IEnumerable<(object Entity, IEnumerable<string> ModifiedProperties)>
        GetModifiedEntities(ApplicationDbContext context)
    {
        return context.ChangeTracker
            .Entries()
            .Where(e => e.State == EntityState.Modified)
            .Select(e => (
                e.Entity,
                e.Properties.Where(p => p.IsModified).Select(p => p.Metadata.Name)
            ));
    }

    /// <summary>重設實體為 Unchanged（不寫入變更）</summary>
    public static void DiscardChanges(ApplicationDbContext context, object entity)
    {
        context.Entry(entity).State = EntityState.Unchanged;
    }

    /// <summary>清空 ChangeTracker（例如長時間背景作業後釋放追蹤）</summary>
    public static void ClearChangeTracker(ApplicationDbContext context)
    {
        context.ChangeTracker.Clear();
    }

    // ----- 審計欄位（大型專案常見） -----

    /// <summary>大型專案：在 SaveChanges 前統一寫入審計欄位（若實體有 CreatedAt/UpdatedAt 等）</summary>
    /// <remarks>
    /// 若使用介面 IAuditable（CreatedAt, UpdatedAt, CreatedBy, UpdatedBy），
    /// 可在 DbContext override SaveChangesAsync 中遍歷 ChangeTracker 統一設定。
    /// 此處示範在外部呼叫的寫法，實際建議放在 DbContext.SaveChangesAsync 覆寫或 Interceptor。
    /// </remarks>
    public static void SetAuditFieldsBeforeSave(
        ApplicationDbContext context,
        int? currentUserId,
        DateTime utcNow)
    {
        foreach (var entry in context.ChangeTracker.Entries())
        {
            if (entry.State == EntityState.Added)
            {
                // 若實體有 CreatedAt / CreatedBy
                // entry.Property("CreatedAt").CurrentValue = utcNow;
                // entry.Property("CreatedBy").CurrentValue = currentUserId;
            }
            else if (entry.State == EntityState.Modified)
            {
                // entry.Property("UpdatedAt").CurrentValue = utcNow;
                // entry.Property("UpdatedBy").CurrentValue = currentUserId;
            }
        }
    }

    // ----- 規格查詢建構（Specification-like） -----

    /// <summary>大型專案：可組合的查詢建構（回傳 IQueryable 供呼叫端再組）</summary>
    public static IQueryable<LoginHistory> BuildLoginHistoryQuery(
        ApplicationDbContext context,
        int? userId = null,
        bool? isSuccess = null,
        DateTime? from = null,
        DateTime? to = null,
        bool noTracking = true)
    {
        var query = context.LoginHistories.AsQueryable();
        if (noTracking) query = query.AsNoTracking();
        if (userId.HasValue) query = query.Where(h => h.UserId == userId);
        if (isSuccess.HasValue) query = query.Where(h => h.IsSuccess == isSuccess);
        if (from.HasValue) query = query.Where(h => h.AttemptedAt >= from);
        if (to.HasValue) query = query.Where(h => h.AttemptedAt <= to);
        return query.OrderByDescending(h => h.AttemptedAt);
    }

    /// <summary>使用上面建構的查詢做分頁</summary>
    public static async Task<PagedResult<LoginHistory>> GetPagedFromBuilderAsync(
        ApplicationDbContext context,
        LoginHistoryFilter filter,
        CancellationToken cancellationToken = default)
    {
        var query = BuildLoginHistoryQuery(
            context,
            filter.UserId,
            filter.IsSuccess,
            filter.StartDate,
            filter.EndDate);

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<LoginHistory>(items, total, filter.Page, filter.PageSize);
    }

    // ----- 連線重試（建議在 DI 設定） -----

    /// <summary>
    /// 大型專案：連線重試建議在註冊 DbContext 時設定，例如：
    /// services.AddDbContext&lt;ApplicationDbContext&gt;(options => {
    ///     options.UseSqlServer(connStr, sql => {
    ///         sql.EnableRetryOnFailure(3, TimeSpan.FromSeconds(5), null);
    ///     });
    /// });
    /// 寫入時若遇暫時性錯誤會自動重試，不需在每個方法內手動重試。
    /// </summary>
    public static void DocumentRetryConfiguration() { }
}
