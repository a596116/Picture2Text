using Microsoft.EntityFrameworkCore;
using Picture2Text.Api.Data;
using Picture2Text.Api.Models;

namespace Picture2Text.Api.Services.EfExamples;

/// <summary>
/// EF Core 刪除（Delete）範例。
/// 含：單筆、RemoveRange、ExecuteDelete、軟刪除、批次刪除。
/// </summary>
public static class EfDeleteExamples
{
    /// <summary>單筆刪除</summary>
    public static async Task<bool> DeleteUserAsync(ApplicationDbContext context, int userId)
    {
        var user = await context.Users.FindAsync(userId);
        if (user == null) return false;
        context.Users.Remove(user);
        await context.SaveChangesAsync();
        return true;
    }

    /// <summary>多筆刪除 RemoveRange（先查再刪）</summary>
    public static async Task<int> DeleteOldLoginHistoryAsync(
        ApplicationDbContext context, int daysToKeep = 90)
    {
        var cutoff = DateTime.UtcNow.AddDays(-daysToKeep);
        var toDelete = await context.LoginHistories
            .Where(h => h.AttemptedAt < cutoff)
            .ToListAsync();
        context.LoginHistories.RemoveRange(toDelete);
        return await context.SaveChangesAsync();
    }

    /// <summary>大量刪除 ExecuteDelete（EF Core 7+），不載入實體，效能佳</summary>
    public static async Task<int> ExecuteDeleteOldHistoryAsync(
        ApplicationDbContext context, int daysToKeep = 90)
    {
        var cutoff = DateTime.UtcNow.AddDays(-daysToKeep);
        return await context.LoginHistories
            .Where(h => h.AttemptedAt < cutoff)
            .ExecuteDeleteAsync();
    }

    /// <summary>依主鍵刪除（僅知 Id 時）</summary>
    public static async Task DeleteByKeyAsync(ApplicationDbContext context, int historyId)
    {
        var entity = new LoginHistory { Id = historyId };
        context.LoginHistories.Attach(entity);
        context.LoginHistories.Remove(entity);
        await context.SaveChangesAsync();
    }

    /// <summary>大型專案：分批 ExecuteDelete，避免單次鎖定過久（例如每次刪 5000 筆）</summary>
    public static async Task<int> ExecuteDeleteInBatchesAsync(
        ApplicationDbContext context,
        int daysToKeep = 90,
        int batchSize = 5000,
        CancellationToken cancellationToken = default)
    {
        var cutoff = DateTime.UtcNow.AddDays(-daysToKeep);
        var total = 0;
        int deleted;
        do
        {
            deleted = await context.LoginHistories
                .Where(h => h.AttemptedAt < cutoff)
                .Take(batchSize)
                .ExecuteDeleteAsync(cancellationToken);
            total += deleted;
        } while (deleted == batchSize);

        return total;
    }

    /// <summary>大型專案：軟刪除 - 僅更新 IsDeleted 等欄位（需實體有對應欄位與全域查詢過濾）</summary>
    /// <remarks>
    /// 若實體有 IsDeleted、DeletedAt，並在 DbContext 用 HasQueryFilter 過濾，
    /// 則「刪除」改為更新 IsDeleted = true, DeletedAt = UtcNow。
    /// 本範例假設未來擴充用，目前 LoginHistory 無 IsDeleted，可改寫為更新備用欄位或註解說明。
    /// </remarks>
    public static async Task<int> SoftDeleteByUserIdAsync(
        ApplicationDbContext context,
        string tableOrEntityDescription,
        int userId)
    {
        // 示意：若為 UserSession 可改為更新 IsActive = false, LogoutAt = now
        var count = await context.UserSessions
            .Where(s => s.UserId == userId && s.IsActive)
            .ExecuteUpdateAsync(s => s
                .SetProperty(x => x.IsActive, false)
                .SetProperty(x => x.LogoutAt, DateTime.UtcNow));
        return count;
    }
}
