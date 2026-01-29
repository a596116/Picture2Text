# EF Examples 範例索引

本資料夾將 Entity Framework Core 的 CRUD 與進階操作拆成多個檔案，方便大型專案中依情境查找與複製。

## 檔案對照

| 檔案 | 內容 | 大型專案常見情境 |
|------|------|------------------|
| **EfExamplesDtos.cs** | 共用 DTO、分頁結果、查詢條件 | `PagedResult<T>`、`CursorPagedResult<T>`、Filter 物件 |
| **EfCreateExamples.cs** | 新增（Create） | 單筆、AddRange、避免重複、主表+關聯、**分批大量新增**、僅 Attach 不存檔（UoW） |
| **EfReadExamples.cs** | 查詢（Read） | Find、條件、**Offset 分頁**、**游標分頁（Keyset）**、Include、投影、Any/Count、**動態條件**、**Filter 物件查詢** |
| **EfUpdateExamples.cs** | 更新（Update） | 查出來再改、部分欄位、**ExecuteUpdate**、**Attach + 部分 Modified**、**樂觀並行（RowVersion）**、**批次更新** |
| **EfDeleteExamples.cs** | 刪除（Delete） | 單筆、RemoveRange、**ExecuteDelete**、**分批 ExecuteDelete**、**軟刪除** |
| **EfTransactionExamples.cs** | 交易（Transaction） | 基本交易、**SavePoint**、多次 SaveChanges、**共用交易（多 DbContext）** |
| **EfJoinExamples.cs** | 多表 JOIN | **LINQ join**（兩表/三表）、**Left Join**（DefaultIfEmpty）、**Include**、**Select 投影**、**GroupJoin**、原始 SQL JOIN 篩選 |
| **EfAdvancedExamples.cs** | 進階 | **原始 SQL**、**編譯查詢**、**Split Query**、ChangeTracker、**審計欄位**、**規格查詢建構**、連線重試建議 |

## 大型專案常見情境速查

- **大表分頁**：用游標分頁（Keyset）避免 `OFFSET` 深分頁效能問題 → `EfReadExamples.GetCursorPagedAsync`
- **大量新增**：分批 AddRange + SaveChanges，或考慮 EF Core 7+ BulkInsert → `EfCreateExamples.CreateInBatchesAsync`
- **大量更新/刪除**：優先 `ExecuteUpdate` / `ExecuteDelete`，必要時再分批 → `EfUpdateExamples`、`EfDeleteExamples`
- **樂觀並行**：實體加 RowVersion/ConcurrencyToken，存檔時處理 `DbUpdateConcurrencyException` → `EfUpdateExamples.UpdateWithConcurrencyCheckAsync`
- **軟刪除**：更新 IsDeleted/DeletedAt，並在 DbContext 用 `HasQueryFilter` 過濾 → `EfDeleteExamples.SoftDeleteByUserIdAsync`（概念）
- **審計欄位**：在 SaveChanges 覆寫或 Interceptor 中統一寫入 CreatedAt/UpdatedAt 等 → `EfAdvancedExamples.SetAuditFieldsBeforeSave`（概念）
- **多 DbContext 同一交易**：用 `Database.GetDbConnection()` 開 Transaction 再 `UseTransaction` → `EfTransactionExamples.UseExternalTransactionAsync`
- **高頻查詢**：編譯查詢 `EF.CompileAsyncQuery` → `EfAdvancedExamples.GetUserByIdCompiledAsync`
- **多表 JOIN**：LINQ join、Include、Select 投影、GroupJoin、Left Join → `EfJoinExamples`
- **複雜報表**：原始 SQL `FromSqlInterpolated` / `ExecuteSqlRaw` → `EfAdvancedExamples`
- **多 Include 造成笛卡爾積**：使用 `AsSplitQuery()` → `EfAdvancedExamples.GetSessionWithSplitQueryAsync`

## 使用方式

- 本資料夾為**範例程式碼**，不一定要掛到 DI。
- 實際業務可依需求複製到對應的 Service 或 Repository 再改寫。
- 若需統一入口，可建立一個 Facade 類別注入 `ApplicationDbContext`，內部呼叫各 static 方法或改為 instance 方法。
