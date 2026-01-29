# Entity Framework Core 完整範例手冊

本文件為 **單一 Markdown**，整理所有 EF Core 常用操作，以本專案的 `ApplicationDbContext` 與實體（User、LoginHistory、RefreshToken、UserSession）為例。章節分開、方便查找。

---

## 目錄

1. [一、Create（新增）](#一create新增)
2. [二、Read（查詢）](#二read查詢)
3. [三、只取部分欄位（投影）](#三只取部分欄位投影)
4. [四、多表 JOIN](#四多表-join)
5. [五、Update（更新）](#五update更新)
6. [六、Delete（刪除）](#六delete刪除)
7. [七、交易（Transaction）](#七交易transaction)
8. [八、進階（原始 SQL、編譯查詢、Split Query、ChangeTracker、審計、規格、重試）](#八進階)
9. [九、效能與建議](#九效能與建議)
10. [十、EF CLI 命令（Migration 與工具）](#十ef-cli-命令migration-與工具)

---

## 一、Create（新增）

### 1.1 單筆新增

```csharp
var history = new LoginHistory
{
    UserId = 1,
    AttemptedUserId = "A123456789",
    IsSuccess = true,
    AttemptedAt = DateTime.UtcNow,
    IpAddress = "192.168.1.1"
};
_context.LoginHistories.Add(history);
await _context.SaveChangesAsync();
// 新增後 history.Id 會由資料庫自動填入
```

### 1.2 多筆新增（AddRange）

```csharp
var list = new List<LoginHistory>
{
    new() { AttemptedUserId = "A1", IsSuccess = true, AttemptedAt = DateTime.UtcNow },
    new() { AttemptedUserId = "A2", IsSuccess = false, AttemptedAt = DateTime.UtcNow }
};
_context.LoginHistories.AddRange(list);
await _context.SaveChangesAsync();
```

### 1.3 新增並取得自動產生的主鍵

```csharp
var user = new User
{
    IdNo = "A123456789",
    Name = "王小明",
    Password = BCrypt.Net.BCrypt.HashPassword("secret")
};
_context.Users.Add(user);
await _context.SaveChangesAsync();
int newId = user.Id;
```

### 1.4 新增時若已存在則不重複（依唯一鍵檢查）

```csharp
var exists = await _context.Users.AnyAsync(u => u.IdNo == idNo);
if (exists) return null;

var user = new User { IdNo = idNo, Name = name, Password = hashedPassword };
_context.Users.Add(user);
await _context.SaveChangesAsync();
return user;
```

### 1.5 主表 + 關聯一併新增（先存主表取 Id 再存明細）

```csharp
var user = new User { IdNo = idNo, Name = name, Password = hashedPassword };
_context.Users.Add(user);
await _context.SaveChangesAsync();

_context.LoginHistories.Add(new LoginHistory
{
    UserId = user.Id,
    AttemptedUserId = idNo,
    IsSuccess = true,
    AttemptedAt = DateTime.UtcNow,
    IpAddress = ipAddress
});
await _context.SaveChangesAsync();
```

### 1.6 大型專案：分批大量新增（每批 500 筆）

```csharp
var total = 0;
for (var i = 0; i < source.Count; i += batchSize)
{
    var batch = source.Skip(i).Take(batchSize).ToList();
    _context.LoginHistories.AddRange(batch);
    total += await _context.SaveChangesAsync(cancellationToken);
}
return total;
```

### 1.7 大型專案：僅附加不立即寫入（配合 Unit of Work）

```csharp
_context.LoginHistories.Add(entity);
// 不呼叫 SaveChangesAsync，由 UoW 或 Service 層統一提交
```

---

## 二、Read（查詢）

### 2.1 依主鍵查詢（FindAsync）

```csharp
var user = await _context.Users.FindAsync(1);
// 複合主鍵：await _context.SomeSet.FindAsync(key1, key2);
```

### 2.2 單筆查詢（FirstOrDefault / First / SingleOrDefault）

```csharp
// 找不到回 null
var user = await _context.Users.FirstOrDefaultAsync(u => u.IdNo == "A123456789");

// 找不到拋例外
var singleUser = await _context.Users.FirstAsync(u => u.Id == 1);

// 確保最多一筆，多筆會拋錯
var one = await _context.Users.SingleOrDefaultAsync(u => u.IdNo == "A123456789");
```

### 2.3 條件列表（Where + 排序 + Take）

```csharp
var list = await _context.LoginHistories
    .Where(h => h.UserId == 1 && h.IsSuccess)
    .OrderByDescending(h => h.AttemptedAt)
    .Take(50)
    .ToListAsync();
```

### 2.4 Offset 分頁（小中型資料集）

```csharp
var query = _context.LoginHistories
    .Where(h => h.UserId == userId)
    .OrderByDescending(h => h.AttemptedAt);

int total = await query.CountAsync();
var items = await query
    .Skip((page - 1) * pageSize)
    .Take(pageSize)
    .ToListAsync();
```

### 2.5 大型專案：游標分頁（Keyset），大表時比 Offset 效能好

```csharp
IQueryable<LoginHistory> query = _context.LoginHistories
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
```

### 2.6 包含關聯（Include / ThenInclude）

```csharp
// 從「多」的那邊 Include「一」
var session = await _context.UserSessions
    .Include(s => s.User)
    .Include(s => s.RefreshToken)
    .AsNoTracking()
    .FirstOrDefaultAsync(s => s.SessionId == sessionId);

// LoginHistory 帶出 User
var list = await _context.LoginHistories
    .Include(h => h.User)
    .AsNoTracking()
    .Where(h => h.UserId == userId)
    .OrderByDescending(h => h.AttemptedAt)
    .Take(50)
    .ToListAsync();
```

### 2.7 唯讀查詢（AsNoTracking）

```csharp
var users = await _context.Users
    .AsNoTracking()
    .Where(u => u.Name.Contains("王"))
    .ToListAsync();
```

### 2.8 存在檢查（Any）

```csharp
bool exists = await _context.Users.AnyAsync(u => u.IdNo == "A123456789");
```

### 2.9 聚合（Count / Sum / Average）

```csharp
int count = await _context.LoginHistories
    .CountAsync(h => h.UserId == 1 && h.IsSuccess);
// var total = await _context.Orders.SumAsync(o => o.Amount);
```

### 2.10 群組（GroupBy）

```csharp
var stats = await _context.LoginHistories
    .Where(h => h.AttemptedAt >= startDate)
    .GroupBy(h => h.UserId)
    .Select(g => new
    {
        UserId = g.Key,
        SuccessCount = g.Count(x => x.IsSuccess),
        FailCount = g.Count(x => !x.IsSuccess)
    })
    .ToListAsync();
```

### 2.11 動態條件（IQueryable 組合）

```csharp
IQueryable<LoginHistory> query = _context.LoginHistories.AsNoTracking();
if (userId.HasValue) query = query.Where(h => h.UserId == userId);
if (isSuccess.HasValue) query = query.Where(h => h.IsSuccess == isSuccess);
if (start.HasValue) query = query.Where(h => h.AttemptedAt >= start);
if (end.HasValue) query = query.Where(h => h.AttemptedAt <= end);

var result = await query.OrderByDescending(h => h.AttemptedAt).Take(100).ToListAsync();
```

### 2.12 依 Filter 物件組查詢（規格模式簡化版）

```csharp
IQueryable<LoginHistory> query = _context.LoginHistories.AsNoTracking();
if (filter.UserId.HasValue) query = query.Where(h => h.UserId == filter.UserId);
if (filter.IsSuccess.HasValue) query = query.Where(h => h.IsSuccess == filter.IsSuccess);
if (filter.StartDate.HasValue) query = query.Where(h => h.AttemptedAt >= filter.StartDate);
if (filter.EndDate.HasValue) query = query.Where(h => h.AttemptedAt <= filter.EndDate);

var total = await query.CountAsync();
var items = await query
    .OrderByDescending(h => h.AttemptedAt)
    .Skip((filter.Page - 1) * filter.PageSize)
    .Take(filter.PageSize)
    .ToListAsync();
```

---

## 三、只取部分欄位（投影）

### 3.1 用 Select 投影到 DTO（建議，型別明確）

```csharp
// 只取 Id、IdNo、Name，SQL 只會 SELECT 這三個欄位
var list = await _context.Users
    .AsNoTracking()
    .Select(u => new UserSummaryDto(u.Id, u.IdNo, u.Name))
    .ToListAsync();

// 單筆
var one = await _context.Users
    .AsNoTracking()
    .Where(u => u.Id == id)
    .Select(u => new UserSummaryDto(u.Id, u.IdNo, u.Name))
    .FirstOrDefaultAsync();

// LoginHistory 只取 Id、AttemptedAt、IsSuccess
var history = await _context.LoginHistories
    .AsNoTracking()
    .Where(h => h.UserId == userId)
    .Select(h => new LoginHistoryPartialDto(h.Id, h.AttemptedAt, h.IsSuccess))
    .Take(50)
    .ToListAsync();
```

### 3.2 不寫 DTO：匿名型別（查詢與使用在同一方法內）

```csharp
// 在同一個方法裡查完就用，用 var 接
var rows = await _context.Users
    .AsNoTracking()
    .Select(u => new { u.Id, u.Name })
    .ToListAsync();

foreach (var r in rows)
{
    Console.WriteLine($"{r.Id} {r.Name}");
}
```

### 3.3 不寫 DTO：回傳 IQueryable，呼叫端再 Select 匿名型別

```csharp
// 你的方法只回傳 IQueryable
public IQueryable<User> GetUsersQuery(ApplicationDbContext context)
{
    return context.Users.AsNoTracking();
}

// 呼叫端
var list = await GetUsersQuery(context)
    .Select(u => new { u.Id, u.Name })
    .ToListAsync();
// list 型別為匿名，可用 list[0].Id, list[0].Name
```

> **注意**：匿名型別無法當作方法回傳型別，只能在同一方法內使用，或由呼叫端在 IQueryable 上再 `.Select(...).ToListAsync()`。EF 運算式樹不支援 tuple 常值，故不建議在 Select 內用 `(u.Id, u.Name)` 當回傳型別。

---

## 四、多表 JOIN

### 4.1 LINQ join（INNER JOIN）

```csharp
// 兩表：User JOIN LoginHistory
var query =
    from u in _context.Users.AsNoTracking()
    join h in _context.LoginHistories.AsNoTracking() on u.Id equals h.UserId
    orderby h.AttemptedAt descending
    select new UserWithHistoryItem { User = u, History = h };
var list = await query.Take(100).ToListAsync();
```

### 4.2 LINQ join + 投影到 DTO（登入歷史帶使用者名稱）

```csharp
var query =
    from h in _context.LoginHistories.AsNoTracking()
    join u in _context.Users.AsNoTracking() on h.UserId equals u.Id into userGroup
    from u in userGroup.DefaultIfEmpty()  // LEFT JOIN
    orderby h.AttemptedAt descending
    select new LoginHistoryWithUserNameDto(
        h.Id, h.UserId,
        u != null ? u.Name : "(未知)",
        h.AttemptedUserId, h.IsSuccess, h.AttemptedAt, h.IpAddress);
var list = await query.Take(200).ToListAsync();
```

### 4.3 三表 JOIN（User + UserSession + RefreshToken）

```csharp
// 三表 INNER + Left（Session、Token 可無）
var query =
    from s in _context.UserSessions.AsNoTracking()
    join u in _context.Users.AsNoTracking() on s.UserId equals u.Id
    join rt in _context.RefreshTokens.AsNoTracking() on s.RefreshTokenId equals rt.Id into rtGroup
    from rt in rtGroup.DefaultIfEmpty()
    where s.IsActive
    orderby s.LastActivityAt descending
    select new SessionDetailDto(s.Id, s.UserId, u.Name, s.SessionId, s.LoginAt,
        rt != null ? rt.ExpiresAt : (DateTime?)null, s.IsActive);
var list = await query.ToListAsync();
```

### 4.4 Include 多表（Eager Load，取回完整實體）

```csharp
var list = await _context.UserSessions
    .Include(s => s.User)
    .Include(s => s.RefreshToken)
    .AsNoTracking()
    .Where(s => s.IsActive)
    .OrderByDescending(s => s.LastActivityAt)
    .ToListAsync();
```

### 4.5 Select 內取關聯（單一 SQL，只取欄位）

```csharp
// EF 會產生 LEFT JOIN User，只取 Name
var list = await _context.LoginHistories
    .AsNoTracking()
    .OrderByDescending(h => h.AttemptedAt)
    .Take(100)
    .Select(h => new LoginHistoryWithUserNameDto(
        h.Id, h.UserId,
        h.User != null ? h.User.Name : "(未知)",
        h.AttemptedUserId, h.IsSuccess, h.AttemptedAt, h.IpAddress))
    .ToListAsync();
```

### 4.6 GroupJoin（一對多：每個使用者 + 最近 N 筆歷史）

```csharp
var query =
    from u in _context.Users.AsNoTracking()
    join h in _context.LoginHistories.AsNoTracking() on u.Id equals h.UserId into histories
    select new UserWithHistoriesItem
    {
        User = u,
        Histories = histories.OrderByDescending(x => x.AttemptedAt).Take(5).ToList()
    };
var list = await query.ToListAsync();
```

### 4.7 原始 SQL：JOIN 僅用於篩選（回傳單一實體）

```csharp
// FromSqlRaw 回傳型別需對應單一實體，JOIN 多用於 WHERE
var list = await _context.LoginHistories
    .FromSqlRaw(
        @"SELECT h.* FROM LoginHistory h
          INNER JOIN [User] u ON h.UserID = u.ID
          WHERE h.AttemptedAt >= {0} AND h.AttemptedAt <= {1}
          ORDER BY h.AttemptedAt DESC",
        from, to)
    .AsNoTracking()
    .Take(100)
    .ToListAsync();
```

---

## 五、Update（更新）

### 5.1 查出來再改（最常見）

```csharp
var user = await _context.Users.FindAsync(1);
if (user != null)
{
    user.Name = "新名稱";
    user.Password = BCrypt.Net.BCrypt.HashPassword("newPassword");
    await _context.SaveChangesAsync();
}
```

### 5.2 只更新指定欄位

```csharp
var session = await _context.UserSessions.FirstOrDefaultAsync(s => s.SessionId == sessionId);
if (session != null)
{
    session.LastActivityAt = DateTime.UtcNow;
    await _context.SaveChangesAsync();
}
```

### 5.3 依條件更新多筆（先查再改）

```csharp
var sessions = await _context.UserSessions
    .Where(s => s.UserId == userId && s.IsActive)
    .ToListAsync();
foreach (var s in sessions)
{
    s.IsActive = false;
    s.LogoutAt = DateTime.UtcNow;
}
await _context.SaveChangesAsync();
```

### 5.4 大量更新（ExecuteUpdate，EF Core 7+）

```csharp
await _context.RefreshTokens
    .Where(rt => rt.ExpiresAt < DateTime.UtcNow && !rt.IsRevoked)
    .ExecuteUpdateAsync(s => s
        .SetProperty(rt => rt.IsRevoked, true)
        .SetProperty(rt => rt.RevokedAt, DateTime.UtcNow));
```

### 5.5 未從 DbContext 查出的實體：Attach 後標記 Modified

```csharp
_context.Users.Attach(detachedUser);
applyChanges(detachedUser);
_context.Entry(detachedUser).State = EntityState.Modified;
await _context.SaveChangesAsync();
```

### 5.6 只標記部分屬性為 Modified（避免更新整筆）

```csharp
var user = new User { Id = userId };
_context.Users.Attach(user);
user.Name = name;
user.Password = hashedPassword;
_context.Entry(user).Property(u => u.Name).IsModified = true;
_context.Entry(user).Property(u => u.Password).IsModified = true;
await _context.SaveChangesAsync();
```

### 5.7 大型專案：樂觀並行（RowVersion / ConcurrencyToken）

```csharp
try
{
    context.Users.Attach(user);
    context.Entry(user).State = EntityState.Modified;
    await context.SaveChangesAsync();
    return true;
}
catch (DbUpdateConcurrencyException)
{
    // 重新載入、提示使用者或記錄衝突
    return false;
}
```

### 5.8 大型專案：批次更新（每批 N 筆 SaveChanges）

```csharp
for (var i = 0; i < userIds.Count(); i += batchSize)
{
    var batch = userIds.Skip(i).Take(batchSize).ToList();
    var users = await _context.Users.Where(u => batch.Contains(u.Id)).ToListAsync();
    foreach (var u in users) applyChanges(u);
    await _context.SaveChangesAsync();
}
```

---

## 六、Delete（刪除）

### 6.1 單筆刪除（先查再刪）

```csharp
var user = await _context.Users.FindAsync(userId);
if (user != null)
{
    _context.Users.Remove(user);
    await _context.SaveChangesAsync();
}
```

### 6.2 多筆刪除（RemoveRange）

```csharp
var cutoff = DateTime.UtcNow.AddDays(-90);
var toDelete = await _context.LoginHistories
    .Where(h => h.AttemptedAt < cutoff)
    .ToListAsync();
_context.LoginHistories.RemoveRange(toDelete);
await _context.SaveChangesAsync();
```

### 6.3 大量刪除（ExecuteDelete，EF Core 7+）

```csharp
var cutoff = DateTime.UtcNow.AddDays(-90);
await _context.LoginHistories
    .Where(h => h.AttemptedAt < cutoff)
    .ExecuteDeleteAsync();
```

### 6.4 依主鍵刪除（僅知 Id 時）

```csharp
var entity = new LoginHistory { Id = historyId };
_context.LoginHistories.Attach(entity);
_context.LoginHistories.Remove(entity);
await _context.SaveChangesAsync();
```

### 6.5 大型專案：分批 ExecuteDelete

```csharp
var cutoff = DateTime.UtcNow.AddDays(-90);
var total = 0;
int deleted;
do
{
    deleted = await _context.LoginHistories
        .Where(h => h.AttemptedAt < cutoff)
        .Take(5000)
        .ExecuteDeleteAsync();
    total += deleted;
} while (deleted == 5000);
```

### 6.6 軟刪除（僅更新 IsDeleted / 狀態欄位）

```csharp
// 例如將會話設為非活躍
await _context.UserSessions
    .Where(s => s.UserId == userId && s.IsActive)
    .ExecuteUpdateAsync(s => s
        .SetProperty(x => x.IsActive, false)
        .SetProperty(x => x.LogoutAt, DateTime.UtcNow));
```

---

## 七、交易（Transaction）

### 7.1 單一 DbContext 交易

```csharp
await using var transaction = await _context.Database.BeginTransactionAsync();
try
{
    _context.Users.Add(newUser);
    await _context.SaveChangesAsync();

    _context.LoginHistories.Add(new LoginHistory { UserId = newUser.Id, ... });
    await _context.SaveChangesAsync();

    await transaction.CommitAsync();
}
catch
{
    await transaction.RollbackAsync();
    throw;
}
```

### 7.2 SavePoint（EF Core 6+），可部分滾回

```csharp
await using var transaction = await _context.Database.BeginTransactionAsync();
try
{
    // 第一段操作
    await _context.SaveChangesAsync();
    await transaction.CreateSavepointAsync("AfterFirstInsert");

    // 第二段操作
    await _context.SaveChangesAsync();
    // 若失敗可：await transaction.RollbackToSavepointAsync("AfterFirstInsert");

    await transaction.CommitAsync();
}
catch
{
    await transaction.RollbackAsync();
    throw;
}
```

### 7.3 大型專案：共用交易（多個 DbContext 同一交易）

```csharp
await using var connection = _context.Database.GetDbConnection();
await connection.OpenAsync();
await using var transaction = await connection.BeginTransactionAsync();
await _context.Database.UseTransactionAsync(transaction);
try
{
    // 多個 context 可共用同一 transaction（UseTransaction）
    await runWithTransaction(_context, transaction);
    await transaction.CommitAsync();
}
catch
{
    await transaction.RollbackAsync();
    throw;
}
```

---

## 八、進階

### 8.1 原始 SQL 查詢（FromSqlInterpolated / FromSqlRaw）

```csharp
// 參數化，避免 SQL 注入
var user = await _context.Users
    .FromSqlInterpolated($"SELECT * FROM [User] WHERE ID_NO = {idNo}")
    .AsNoTracking()
    .FirstOrDefaultAsync();

var list = await _context.LoginHistories
    .FromSqlRaw("SELECT * FROM LoginHistory WHERE UserID = {0} ORDER BY AttemptedAt DESC", userId)
    .AsNoTracking()
    .Take(limit)
    .ToListAsync();
```

### 8.2 執行任意 SQL（ExecuteSqlRaw / ExecuteSqlInterpolated）

```csharp
await _context.Database.ExecuteSqlInterpolatedAsync(
    $"UPDATE [User] SET Name = {newName} WHERE ID = {userId}");
```

### 8.3 編譯查詢（高頻呼叫時減少編譯開銷）

```csharp
private static readonly Func<ApplicationDbContext, int, Task<User?>> GetUserByIdCompiled =
    EF.CompileAsyncQuery((ApplicationDbContext ctx, int id) =>
        ctx.Users.FirstOrDefault(u => u.Id == id));

// 使用
return await GetUserByIdCompiled(context, id);
```

### 8.4 Split Query（多個 Include 時避免笛卡爾積）

```csharp
var session = await _context.UserSessions
    .Include(s => s.User)
    .Include(s => s.RefreshToken)
    .AsSplitQuery()
    .AsNoTracking()
    .FirstOrDefaultAsync(s => s.SessionId == sessionId);
```

### 8.5 ChangeTracker：取得已修改的實體與欄位

```csharp
var modified = _context.ChangeTracker
    .Entries()
    .Where(e => e.State == EntityState.Modified)
    .Select(e => new { e.Entity, ModifiedProps = e.Properties.Where(p => p.IsModified).Select(p => p.Metadata.Name) });
```

### 8.6 ChangeTracker：重設 / 清空

```csharp
_context.Entry(user).State = EntityState.Unchanged;
// 或清空所有追蹤
_context.ChangeTracker.Clear();
```

### 8.7 審計欄位（SaveChanges 前統一寫入）

```csharp
foreach (var entry in _context.ChangeTracker.Entries())
{
    if (entry.State == EntityState.Added)
        // entry.Property("CreatedAt").CurrentValue = utcNow;
    else if (entry.State == EntityState.Modified)
        // entry.Property("UpdatedAt").CurrentValue = utcNow;
}
```

### 8.8 規格查詢建構（可組合的 IQueryable）

```csharp
public IQueryable<LoginHistory> BuildLoginHistoryQuery(
    ApplicationDbContext context,
    int? userId = null, bool? isSuccess = null, DateTime? from = null, DateTime? to = null)
{
    var query = context.LoginHistories.AsNoTracking();
    if (userId.HasValue) query = query.Where(h => h.UserId == userId);
    if (isSuccess.HasValue) query = query.Where(h => h.IsSuccess == isSuccess);
    if (from.HasValue) query = query.Where(h => h.AttemptedAt >= from);
    if (to.HasValue) query = query.Where(h => h.AttemptedAt <= to);
    return query.OrderByDescending(h => h.AttemptedAt);
}
```

### 8.9 連線重試（建議在 DI 註冊 DbContext 時設定）

```csharp
// Program.cs / Startup.cs
services.AddDbContext<ApplicationDbContext>(options =>
{
    options.UseSqlServer(connStr, sql =>
    {
        sql.EnableRetryOnFailure(3, TimeSpan.FromSeconds(5), null);
    });
});
```

---

## 九、效能與建議

| 情境           | 建議 |
|----------------|------|
| 唯讀列表       | `AsNoTracking()` |
| 只用到部分欄位 | `Select` 投影，不要整筆實體 |
| 大量更新/刪除  | `ExecuteUpdate` / `ExecuteDelete`（EF Core 7+） |
| 大量新增       | `AddRange` + 單次 `SaveChangesAsync`，必要時分批 |
| 分頁           | 務必 `Skip().Take()`；大表考慮游標分頁（Keyset） |
| 關聯資料       | 需要時再用 `Include`，多個 Include 可考慮 `AsSplitQuery()` |
| 高頻查詢       | 可考慮 `EF.CompileAsyncQuery` |
| 暫時性錯誤     | 在 DbContext 註冊時設定 `EnableRetryOnFailure` |

以上範例皆以非同步（async/await）撰寫，適合 ASP.NET Core。若需同步版，可改為 `ToList()`、`SaveChanges()` 等（不建議在 Web 使用）。

---

## 十、EF CLI 命令（Migration 與工具）

以下命令需在 **專案目錄**（含 `.csproj` 的目錄）執行，或透過 `--project` 指定專案。本專案為 `backend-csharp`，DbContext 為 `ApplicationDbContext`。

### 10.1 安裝與更新 dotnet-ef

```bash
# 安裝全域工具（只需執行一次）
dotnet tool install --global dotnet-ef

# 更新到最新版
dotnet tool update --global dotnet-ef

# 檢查版本
dotnet ef --version
```

### 10.2 Migrations：新增遷移

```bash
# 依目前 DbContext 與模型差異，產生新的 Migration
dotnet ef migrations add <Migration名稱>

# 範例：初次建立
dotnet ef migrations add InitialCreate

# 範例：新增欄位或表
dotnet ef migrations add AddUserEmailColumn

# 指定專案與 DbContext（專案內有多個專案 / 多個 DbContext 時）
dotnet ef migrations add AddUserEmailColumn --project Picture2Text.Api --context ApplicationDbContext
```

- 會產生 `Migrations/<時間戳>_<Migration名稱>.cs`（Up/Down）與對應的 Designer 檔。
- 名稱建議用英文、描述變更內容，例如 `AddLoginHistoryTable`、`AddUserIndex`。

### 10.3 Migrations：列出遷移

```bash
# 列出所有 Migration，並標示已套用到資料庫的項目
dotnet ef migrations list

# 輸出範例：
# 20240101000000_InitialCreate (Pending)
# 20240102000000_AddUserEmail (Applied)
```

### 10.4 Migrations：移除最後一個遷移

```bash
# 移除「最後一個」Migration 檔案（僅限尚未套用到資料庫的遷移）
dotnet ef migrations remove

# 若已執行過 database update，需先回滾資料庫再 remove
dotnet ef database update <上一個遷移名稱>
dotnet ef migrations remove
```

### 10.5 Database：套用遷移（更新資料庫）

```bash
# 套用所有尚未套用的 Migration，更新到最新
dotnet ef database update

# 套用到「指定遷移名稱」為止（可用於回滾或只更新到某一版）
dotnet ef database update <Migration名稱>

# 範例：回滾到上一個遷移
dotnet ef database update InitialCreate

# 指定連線字串（覆蓋 appsettings 的 ConnectionStrings）
dotnet ef database update --connection "Server=localhost;Database=MyDb;User Id=sa;Password=xxx;TrustServerCertificate=True"

# 指定環境（會讀取 appsettings.{Environment}.json）
dotnet ef database update --environment Development
dotnet ef database update --environment Production
```

### 10.6 Database：刪除資料庫

```bash
# 刪除資料庫（會提示確認）
dotnet ef database drop

# 不提示，直接刪除
dotnet ef database drop --force
```

### 10.7 Migrations：產生 SQL 腳本（不直接執行）

```bash
# 從空白資料庫到「目前所有遷移」的完整 SQL
dotnet ef migrations script

# 輸出到檔案
dotnet ef migrations script --output migration.sql

# 從「指定遷移」到「目前」的差異（例如只產生最近兩個遷移的 SQL）
dotnet ef migrations script <從哪個遷移> --output patch.sql

# 從 InitialCreate 到 AddUserEmail 的差異
dotnet ef migrations script InitialCreate AddUserEmail --output patch.sql

# 產生「可重複執行」的 Idempotent 腳本（依 __EFMigrationsHistory 判斷是否已套用）
dotnet ef migrations script --idempotent --output idempotent.sql
```

- **部署常用**：用 `--idempotent` 產生給 DBA 或 CI/CD 執行的腳本，避免重複套用報錯。

### 10.8 DbContext：查看資訊

```bash
# 列出專案中的 DbContext 與資料庫提供者
dotnet ef dbcontext info

# 指定專案
dotnet ef dbcontext info --project Picture2Text.Api
```

### 10.9 DbContext：從現有資料庫產生模型（Scaffold / 反向工程）

```bash
# 從資料庫產生 DbContext 與 Entity 類別
dotnet ef dbcontext scaffold "<連線字串>" Microsoft.EntityFrameworkCore.SqlServer --output-dir Models --context-dir Data --context ApplicationDbContext

# 範例（實際連線字串請替換）
dotnet ef dbcontext scaffold "Server=localhost;Database=MyDb;User Id=sa;Password=YourPassword;TrustServerCertificate=True" Microsoft.EntityFrameworkCore.SqlServer --output-dir Models --context-dir Data --context ApplicationDbContext

# 只產生指定資料表
dotnet ef dbcontext scaffold "<連線字串>" Microsoft.EntityFrameworkCore.SqlServer --table User --table LoginHistory --output-dir Models --context ApplicationDbContext

# 使用現有專案內的連線名稱（從 appsettings 讀取）
dotnet ef dbcontext scaffold "Name=ConnectionStrings:DefaultConnection" Microsoft.EntityFrameworkCore.SqlServer --output-dir Models --context ApplicationDbContext
```

- 若專案已有手寫的 DbContext/Model，scaffold 會覆寫或產生新檔，建議先備份或放到暫存目錄再手動合併。

### 10.10 常用參數總覽

| 參數 | 說明 | 範例 |
|------|------|------|
| `--project <路徑>` | 指定專案 | `--project Picture2Text.Api` |
| `--startup-project <路徑>` | 指定啟動專案（讀取組態用） | `--startup-project Picture2Text.Api` |
| `--context <名稱>` | 指定 DbContext 類別 | `--context ApplicationDbContext` |
| `--connection "<字串>"` | 覆蓋連線字串 | `--connection "Server=...;Database=..."` |
| `--environment <名稱>` | 指定環境 | `--environment Production` |
| `--output <檔案>` | 腳本輸出路徑 | `--output migration.sql` |
| `--idempotent` | 產生可重複執行的遷移腳本 | 與 `migrations script` 搭配 |
| `--force` | 不提示確認（如 database drop） | `--force` |

### 10.11 常見流程整理

| 情境 | 建議步驟 |
|------|----------|
| **初次建表** | `migrations add InitialCreate` → `database update` |
| **改模型後** | `migrations add <描述變更>` → `database update` |
| **部署到正式機** | `migrations script --idempotent --output deploy.sql`，在正式機執行 `deploy.sql` |
| **回滾一個遷移** | `database update <上一個遷移名稱>` |
| **刪掉最後一個未套用的遷移** | `migrations remove` |
| **重做資料庫** | `database drop --force` → `database update` |
| **從現有 DB 產生程式碼** | `dbcontext scaffold "<連線>" ... --output-dir Models` |

### 10.12 注意事項

1. **執行前備份**：`database update`、`database drop` 前務必備份資料庫。
2. **連線字串**：未加 `--connection` 時，會從啟動專案的 `appsettings.json`（或 `appsettings.{Environment}.json`）的 `ConnectionStrings:DefaultConnection` 讀取。
3. **多專案方案**：若方案有多個專案，需用 `--project` 指定含 DbContext 的專案；必要時用 `--startup-project` 指定讀取組態的專案。
4. **Migration 名稱**：不要隨意改已套用過的 Migration 檔名或類別名，以免與 `__EFMigrationsHistory` 不一致。

更細的連線字串範例與測試資料可參考專案內的 **`MIGRATION_COMMANDS.md`**。
