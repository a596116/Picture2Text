# 資料庫 Migration 指南

本專案使用 **Alembic** 來管理資料庫結構變更（migration）。

## 為什麼需要 Migration？

1. **版本控制**：追蹤資料庫結構的變更歷史
2. **團隊協作**：確保所有開發者的資料庫結構一致
3. **生產環境**：安全地更新生產資料庫
4. **回滾能力**：可以撤銷錯誤的變更

## 如果資料庫已經有 users 表會怎麼處理？

### 情況 1：使用 `init_db()` 或 `create_all()`

如果資料庫中已經存在 `users` 表：
- ✅ **不會報錯**：SQLAlchemy 的 `create_all()` 會跳過已存在的表
- ⚠️ **不會更新結構**：如果模型有變更，現有表的結構不會自動更新
- ⚠️ **可能導致不一致**：模型定義與實際資料庫結構可能不同

### 情況 2：使用 Alembic Migration（推薦）

使用 Alembic 可以：
- ✅ **檢測差異**：自動檢測模型與資料庫的差異
- ✅ **生成遷移腳本**：自動生成 migration 檔案
- ✅ **安全更新**：可以預覽變更，確認後再執行
- ✅ **版本追蹤**：記錄所有變更歷史

## 安裝 Alembic

```bash
pip install -r requirements.txt
```

## Migration 操作指南

### 1. 初始化（首次設置）

如果資料庫已經有表，需要先標記當前狀態為基準：

```bash
# 創建初始 migration（如果表已存在，這會是空 migration）
alembic revision --autogenerate -m "initial migration"

# 如果表已存在且結構正確，編輯生成的 migration 檔案
# 將 upgrade() 和 downgrade() 都設為 pass
```

### 2. 標記當前資料庫為已遷移（如果表已存在）

如果資料庫已經有表且結構正確：

```bash
# 創建一個空的 migration 作為基準點
alembic revision -m "baseline: existing tables"

# 編輯生成的 migration 檔案，將 upgrade() 和 downgrade() 設為 pass
# 然後執行：
alembic stamp head
```

### 3. 創建新的 Migration

當你修改了模型（如 `app/models/user.py`）後：

```bash
# 自動生成 migration（Alembic 會比較模型與資料庫）
alembic revision --autogenerate -m "描述變更內容"

# 例如：
alembic revision --autogenerate -m "add email field to users"
```

### 4. 檢查生成的 Migration

**重要**：在執行 migration 前，務必檢查生成的檔案：

```bash
# 查看生成的 migration 檔案
# 位置：alembic/versions/xxxxx_描述變更內容.py
```

檢查 `upgrade()` 和 `downgrade()` 函數是否正確。

### 5. 執行 Migration

```bash
# 查看當前 migration 狀態
alembic current

# 查看 migration 歷史
alembic history

# 執行所有待執行的 migration（升級到最新版本）
alembic upgrade head

# 執行到下一個版本
alembic upgrade +1

# 回滾到上一個版本
alembic downgrade -1

# 回滾到特定版本
alembic downgrade <revision_id>
```

### 6. 常見操作

```bash
# 查看待執行的 migration
alembic heads

# 查看 migration 樹狀結構
alembic history --verbose

# 生成 SQL 腳本（不執行，只生成 SQL）
alembic upgrade head --sql

# 在離線模式下生成 SQL（用於生產環境）
alembic upgrade head --sql > migration.sql
```

## 處理已存在的表

### 方法 1：標記為基準（推薦）

如果資料庫已經有表且結構正確：

1. 創建空 migration：
```bash
alembic revision -m "baseline: existing tables"
```

2. 編輯生成的檔案，將函數設為空：
```python
def upgrade() -> None:
    pass

def downgrade() -> None:
    pass
```

3. 標記為已執行：
```bash
alembic stamp head
```

### 方法 2：生成差異 Migration

如果表已存在但結構需要更新：

1. 生成 migration：
```bash
alembic revision --autogenerate -m "sync existing tables"
```

2. 檢查生成的 migration，確認變更正確

3. 執行 migration：
```bash
alembic upgrade head
```

## 注意事項

1. **生產環境**：
   - 務必在測試環境先測試 migration
   - 執行前備份資料庫
   - 使用 `--sql` 選項預覽 SQL

2. **團隊協作**：
   - 將 migration 檔案提交到版本控制
   - 不要修改已執行的 migration
   - 如有衝突，協調解決

3. **資料遷移**：
   - Alembic 只處理結構變更
   - 資料遷移需要手動編寫 Python 代碼
   - 在 `upgrade()` 函數中添加資料遷移邏輯

## 範例：添加新欄位

假設要在 `users` 表添加 `phone` 欄位：

1. 修改模型 `app/models/user.py`：
```python
phone = Column(String(20), nullable=True, comment="電話號碼")
```

2. 生成 migration：
```bash
alembic revision --autogenerate -m "add phone to users"
```

3. 檢查生成的檔案（`alembic/versions/xxxxx_add_phone_to_users.py`）

4. 執行 migration：
```bash
alembic upgrade head
```

## 故障排除

### Migration 失敗

如果 migration 執行失敗：

```bash
# 查看當前狀態
alembic current

# 手動標記為特定版本（謹慎使用）
alembic stamp <revision_id>
```

### 衝突解決

如果有多個分支：

```bash
# 合併分支
alembic merge -m "merge branches" <revision1> <revision2>
```

## 相關檔案

- `alembic.ini` - Alembic 配置檔案
- `alembic/env.py` - Migration 環境設定
- `alembic/versions/` - Migration 檔案目錄
- `app/models/` - SQLAlchemy 模型定義

## 參考資源

- [Alembic 官方文檔](https://alembic.sqlalchemy.org/)
- [SQLAlchemy Migration 最佳實踐](https://alembic.sqlalchemy.org/en/latest/tutorial.html)
