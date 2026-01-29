using Microsoft.EntityFrameworkCore;
using Picture2Text.Api.Data;
using Picture2Text.Api.Models;

namespace Picture2Text.Api.Services.EfExamples;

/// <summary>
/// EF Core 更新（Update）範例。
/// 含：單筆更新、部分欄位、大量更新 ExecuteUpdate、樂觀並行（RowVersion）、Attach 更新。
/// </summary>
public static class EfUpdateExamples
{
    /// <summary>查出來再改（最常見）</summary>
    public static async Task<bool> UpdateUserNameAsync(
        ApplicationDbContext context, int userId, string newName)
    {
        var user = await context.Users.FindAsync(userId);
        if (user == null) return false;
        user.Name = newName;
        await context.SaveChangesAsync();
        return true;
    }

    /// <summary>只更新指定欄位（避免覆蓋其他欄位）</summary>
    public static async Task<bool> UpdateLastActivityAsync(
        ApplicationDbContext context, string sessionId, DateTime at)
    {
        var session = await context.UserSessions
            .FirstOrDefaultAsync(s => s.SessionId == sessionId);
        if (session == null) return false;
        session.LastActivityAt = at;
        await context.SaveChangesAsync();
        return true;
    }

    /// <summary>大量更新：ExecuteUpdate（EF Core 7+），不先載入實體，效能佳</summary>
    public static async Task<int> RevokeExpiredTokensAsync(ApplicationDbContext context)
    {
        return await context.RefreshTokens
            .Where(rt => rt.ExpiresAt < DateTime.UtcNow && !rt.IsRevoked)
            .ExecuteUpdateAsync(s => s
                .SetProperty(rt => rt.IsRevoked, true)
                .SetProperty(rt => rt.RevokedAt, DateTime.UtcNow));
    }

    /// <summary>依條件更新多筆（先查再改，需業務邏輯時用）</summary>
    public static async Task<int> DeactivateUserSessionsAsync(
        ApplicationDbContext context, int userId)
    {
        var sessions = await context.UserSessions
            .Where(s => s.UserId == userId && s.IsActive)
            .ToListAsync();
        foreach (var s in sessions)
        {
            s.IsActive = false;
            s.LogoutAt = DateTime.UtcNow;
        }
        return await context.SaveChangesAsync();
    }

    /// <summary>未從 DbContext 查出的實體：Attach 後標記 Modified</summary>
    public static async Task AttachAndUpdateAsync(
        ApplicationDbContext context,
        User detachedUser,
        Action<User> applyChanges)
    {
        context.Users.Attach(detachedUser);
        applyChanges(detachedUser);
        context.Entry(detachedUser).State = EntityState.Modified;
        await context.SaveChangesAsync();
    }

    /// <summary>只標記部分屬性為 Modified（避免更新整筆）</summary>
    public static async Task UpdateOnlyNameAndPasswordAsync(
        ApplicationDbContext context, int userId, string name, string hashedPassword)
    {
        var user = new User { Id = userId };
        context.Users.Attach(user);
        user.Name = name;
        user.Password = hashedPassword;
        context.Entry(user).Property(u => u.Name).IsModified = true;
        context.Entry(user).Property(u => u.Password).IsModified = true;
        await context.SaveChangesAsync();
    }

    /// <summary>大型專案：樂觀並行 - 若實體有 RowVersion/ConcurrencyToken，存檔時會檢查</summary>
    /// <remarks>
    /// 若 Model 有 byte[] RowVersion 並設 [Timestamp]，存檔時 WHERE 會帶上 RowVersion，
    /// 若影響行數為 0 表示已被他人修改，可拋出 DbUpdateConcurrencyException 由上層處理。
    /// </remarks>
    public static async Task<bool> UpdateWithConcurrencyCheckAsync(
        ApplicationDbContext context,
        User user,
        Action<User> applyChanges)
    {
        applyChanges(user);
        try
        {
            context.Users.Attach(user);
            context.Entry(user).State = EntityState.Modified;
            await context.SaveChangesAsync();
            return true;
        }
        catch (DbUpdateConcurrencyException)
        {
            // 可選擇：重新載入、提示使用者、或記錄衝突
            return false;
        }
    }

    /// <summary>大型專案：批次更新（每批 N 筆 SaveChanges，避免單次變更集過大）</summary>
    public static async Task<int> UpdateInBatchesAsync(
        ApplicationDbContext context,
        IEnumerable<int> userIds,
        Action<User> applyChanges,
        int batchSize = 100,
        CancellationToken cancellationToken = default)
    {
        var list = userIds.ToList();
        var total = 0;
        for (var i = 0; i < list.Count; i += batchSize)
        {
            var batch = list.Skip(i).Take(batchSize).ToList();
            var users = await context.Users.Where(u => batch.Contains(u.Id)).ToListAsync(cancellationToken);
            foreach (var u in users)
                applyChanges(u);
            total += await context.SaveChangesAsync(cancellationToken);
        }
        return total;
    }
}
