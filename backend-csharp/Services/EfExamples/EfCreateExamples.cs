using Microsoft.EntityFrameworkCore;
using Picture2Text.Api.Data;
using Picture2Text.Api.Models;

namespace Picture2Text.Api.Services.EfExamples;

/// <summary>
/// EF Core 新增（Create）範例。
/// 含：單筆、多筆、批次大量新增、新增時避免重複、主表+關聯一併新增。
/// </summary>
public static class EfCreateExamples
{
    /// <summary>單筆新增</summary>
    public static async Task<LoginHistory> CreateSingleAsync(ApplicationDbContext context)
    {
        var entity = new LoginHistory
        {
            UserId = 1,
            AttemptedUserId = "A123456789",
            IsSuccess = true,
            AttemptedAt = DateTime.UtcNow,
            IpAddress = "192.168.1.1"
        };
        context.LoginHistories.Add(entity);
        await context.SaveChangesAsync();
        return entity;
    }

    /// <summary>多筆新增 AddRange</summary>
    public static async Task<int> CreateRangeAsync(ApplicationDbContext context)
    {
        var list = new List<LoginHistory>
        {
            new() { AttemptedUserId = "A1", IsSuccess = true, AttemptedAt = DateTime.UtcNow },
            new() { AttemptedUserId = "A2", IsSuccess = false, AttemptedAt = DateTime.UtcNow }
        };
        context.LoginHistories.AddRange(list);
        return await context.SaveChangesAsync();
    }

    /// <summary>新增時若已存在則不重複（依唯一鍵檢查）</summary>
    public static async Task<User?> CreateUserIfNotExistsAsync(
        ApplicationDbContext context,
        string idNo,
        string name,
        string hashedPassword)
    {
        var exists = await context.Users.AnyAsync(u => u.IdNo == idNo);
        if (exists) return null;

        var user = new User { IdNo = idNo, Name = name, Password = hashedPassword };
        context.Users.Add(user);
        await context.SaveChangesAsync();
        return user;
    }

    /// <summary>主表 + 關聯一併新增（先存主表取 Id 再存明細）</summary>
    public static async Task<User> CreateUserWithFirstLoginAsync(
        ApplicationDbContext context,
        string idNo,
        string name,
        string hashedPassword,
        string? ipAddress)
    {
        var user = new User { IdNo = idNo, Name = name, Password = hashedPassword };
        context.Users.Add(user);
        await context.SaveChangesAsync();

        context.LoginHistories.Add(new LoginHistory
        {
            UserId = user.Id,
            AttemptedUserId = idNo,
            IsSuccess = true,
            AttemptedAt = DateTime.UtcNow,
            IpAddress = ipAddress
        });
        await context.SaveChangesAsync();
        return user;
    }

    /// <summary>大型專案：分批大量新增，避免單次 SaveChanges 過大（例如每批 500 筆）</summary>
    public static async Task<int> CreateInBatchesAsync(
        ApplicationDbContext context,
        IReadOnlyList<LoginHistory> source,
        int batchSize = 500,
        CancellationToken cancellationToken = default)
    {
        var total = 0;
        for (var i = 0; i < source.Count; i += batchSize)
        {
            var batch = source.Skip(i).Take(batchSize).ToList();
            context.LoginHistories.AddRange(batch);
            total += await context.SaveChangesAsync(cancellationToken);
        }
        return total;
    }

    /// <summary>大型專案：僅附加實體不立即寫入，由呼叫端統一 SaveChanges（配合 Unit of Work）</summary>
    public static void AttachForLaterSave(ApplicationDbContext context, LoginHistory entity)
    {
        context.LoginHistories.Add(entity);
        // 不呼叫 SaveChangesAsync，由 UoW 或 Service 層統一提交
    }
}
