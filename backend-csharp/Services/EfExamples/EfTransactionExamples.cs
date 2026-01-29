using Microsoft.EntityFrameworkCore;
using Picture2Text.Api.Data;
using Picture2Text.Api.Models;

namespace Picture2Text.Api.Services.EfExamples;

/// <summary>
/// EF Core 交易（Transaction）範例。
/// 含：單一 DbContext 交易、巢狀 SavePoints、跨多步驟一致性、重試考量。
/// </summary>
public static class EfTransactionExamples
{
    /// <summary>基本交易：多步驟同一交易，失敗整批滾回</summary>
    public static async Task CreateUserWithFirstLoginInTransactionAsync(
        ApplicationDbContext context,
        string idNo,
        string name,
        string hashedPassword,
        string? ipAddress)
    {
        await using var transaction = await context.Database.BeginTransactionAsync();
        try
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

            await transaction.CommitAsync();
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    /// <summary>使用 SavePoint（EF Core 6+），可部分滾回</summary>
    public static async Task TransactionWithSavePointAsync(ApplicationDbContext context)
    {
        await using var transaction = await context.Database.BeginTransactionAsync();
        try
        {
            context.LoginHistories.Add(new LoginHistory
            {
                AttemptedUserId = "A1",
                IsSuccess = true,
                AttemptedAt = DateTime.UtcNow
            });
            await context.SaveChangesAsync();

            await transaction.CreateSavepointAsync("AfterFirstInsert");

            context.LoginHistories.Add(new LoginHistory
            {
                AttemptedUserId = "A2",
                IsSuccess = false,
                AttemptedAt = DateTime.UtcNow
            });
            await context.SaveChangesAsync();

            // 若後續邏輯失敗，可只滾回 SavePoint 之後的變更
            // await transaction.RollbackToSavepointAsync("AfterFirstInsert");

            await transaction.CommitAsync();
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    /// <summary>大型專案：交易內多次 SaveChanges（同一 Transaction 涵蓋）</summary>
    public static async Task MultiStepInSingleTransactionAsync(
        ApplicationDbContext context,
        Action<ApplicationDbContext> step1,
        Action<ApplicationDbContext> step2)
    {
        await using var transaction = await context.Database.BeginTransactionAsync();
        try
        {
            step1(context);
            await context.SaveChangesAsync();

            step2(context);
            await context.SaveChangesAsync();

            await transaction.CommitAsync();
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    /// <summary>大型專案：共用交易（當多個 DbContext 需同一交易時，使用 Database.GetDbConnection() + 外部 Transaction）</summary>
    /// <remarks>
    /// 若有多個 DbContext（例如 ReadDbContext / WriteDbContext 分離），
    /// 可從其中一個 context.Database.GetDbConnection() 開啟 Connection 與 Transaction，
    /// 再傳入各 DbContext 的 Database.UseTransaction(transaction)，使多個 context 共用同一交易。
    /// 本範例僅示範單一 context 使用 UseTransaction 的寫法。
    /// </remarks>
    public static async Task UseExternalTransactionAsync(
        ApplicationDbContext context,
        Func<ApplicationDbContext, System.Data.Common.DbTransaction, Task> runWithTransaction)
    {
        await using var connection = context.Database.GetDbConnection();
        await connection.OpenAsync();
        await using var transaction = await connection.BeginTransactionAsync();
        await context.Database.UseTransactionAsync(transaction);
        try
        {
            await runWithTransaction(context, transaction);
            await transaction.CommitAsync();
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }
}
