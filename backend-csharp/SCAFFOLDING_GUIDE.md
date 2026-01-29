# ASP.NET Core 程式碼產生指南（類似 NestJS CLI）

本文件說明如何用 .NET 官方工具產生 **Controller**（以及相關程式碼），並與 Node.js NestJS 的 `nest g controller` / `nest g service` 對照。

---

## 與 NestJS 對照

| 功能           | NestJS                         | ASP.NET Core                                      |
|----------------|--------------------------------|---------------------------------------------------|
| 產生 Controller | `nest g controller users`      | `dotnet aspnet-codegenerator controller ...`     |
| 產生 Service    | `nest g service users`         | 無內建指令，需手寫或自訂範本                       |
| 產生 Module     | `nest g module users`         | 無直接對應（ASP.NET Core 無 Module 概念）         |
| 一鍵 CRUD API   | 需搭配其他套件/手寫            | aspnet-codegenerator 可依 Entity 產生 CRUD Controller |

---

## 方式一：aspnet-codegenerator（建議）

### 1. 安裝全域工具

```bash
dotnet tool install -g dotnet-aspnet-codegenerator
```

若已安裝過要更新：

```bash
dotnet tool update -g dotnet-aspnet-codegenerator
```

### 2. 專案加入 Code Generation 套件

在 `Picture2Text.Api.csproj` 的 `<ItemGroup>` 中加入：

```xml
<PackageReference Include="Microsoft.VisualStudio.Web.CodeGeneration.Design" Version="9.0.0" />
```

完整範例（與現有套件並列）：

```xml
<ItemGroup>
  <PackageReference Include="Microsoft.AspNetCore.OpenApi" Version="9.0.0" />
  <!-- ... 其他套件 ... -->
  <PackageReference Include="Microsoft.VisualStudio.Web.CodeGeneration.Design" Version="9.0.0" />
</ItemGroup>
```

然後執行：

```bash
dotnet restore
```

### 3. 產生 API Controller

在專案目錄（含 `.csproj` 的目錄）執行。

**語法：**

```bash
dotnet aspnet-codegenerator controller ^
  -name <Controller名稱> ^
  -api ^
  -async ^
  -m <Entity 完整類別名> ^
  -dc <DbContext 完整類別名> ^
  -namespace <Controller 命名空間>
```

**本專案範例（依你的 Models / Data 調整）：**

```bash
# 以 User 為例（Entity: User, DbContext: ApplicationDbContext）
dotnet aspnet-codegenerator controller -name UsersController -api -async -m Picture2Text.Api.Models.User -dc Picture2Text.Api.Data.ApplicationDbContext -namespace Picture2Text.Api.Controllers
```

- `-name UsersController`：產生 `UsersController.cs`
- `-api`：產生 Web API 風格（回傳 JSON，非 MVC View）
- `-async`：非同步 Action
- `-m Picture2Text.Api.Models.User`：對應的 Entity
- `-dc Picture2Text.Api.Data.ApplicationDbContext`：你的 DbContext
- `-namespace Picture2Text.Api.Controllers`：Controller 的命名空間

產生後會依 `User` 與 `ApplicationDbContext` 自動生出含 CRUD 的 Controller（Get/Post/Put/Delete）。

**Linux / macOS 用反斜線續行：**

```bash
dotnet aspnet-codegenerator controller \
  -name UsersController \
  -api \
  -async \
  -m Picture2Text.Api.Models.User \
  -dc Picture2Text.Api.Data.ApplicationDbContext \
  -namespace Picture2Text.Api.Controllers
```

### 4. 常用參數說明

| 參數          | 說明 |
|---------------|------|
| `-name`       | Controller 類別名稱（建議以 Controller 結尾） |
| `-api`        | 產生 Web API Controller |
| `-async`      | Action 使用 async/await |
| `-m`          | Entity 完整名稱（含命名空間） |
| `-dc`         | DbContext 完整名稱（含命名空間） |
| `-namespace`  | Controller 命名空間 |
| `-outDir`     | 輸出目錄（預設為 Controllers） |
| `-force`      | 覆寫已存在的檔案 |

---

## 方式二：dotnet scaffold（互動式，預覽）

微軟較新的互動式 scaffold 工具，會用問答方式引導你選專案類型並產生程式碼。

```bash
dotnet tool install --global Microsoft.dotnet-scaffold
```

安裝後在方案或專案目錄執行：

```bash
dotnet scaffold
```

依提示選擇 Web API、Blazor 等即可。目前為預覽版，若要以「可重現」的指令為主，建議仍以 **aspnet-codegenerator** 為準。

---

## Service 怎麼產生？

ASP.NET Core **沒有**像 `nest g service users` 的內建指令，通常做法：

1. **手動新增**  
   在 `Services` 資料夾新增 `XXXService.cs`，依專案既有風格（例如 `AuthService`、`SessionService`）撰寫介面與實作。

2. **IDE 範本**  
   Visual Studio / Rider 的「新增檔案」可選「類別」或自訂範本，快速產生空的 Service 類別。

3. **自訂 dotnet template**  
   用 `dotnet new` 自訂範本，例如 `dotnet new service -n UserService`，需自行建立與維護範本專案。

本專案已有 `AuthService`、`SessionService`、`JwtService` 等，可複製其中一個當範本再改名稱與邏輯。

---

## 本專案快速檢查清單

1. 安裝工具：`dotnet tool install -g dotnet-aspnet-codegenerator`
2. 在 `Picture2Text.Api.csproj` 加入 `Microsoft.VisualStudio.Web.CodeGeneration.Design` 並 `dotnet restore`
3. 確認 Entity 與 DbContext 命名空間與類別名（如 `Models/User.cs`、`Data/ApplicationDbContext.cs`）
4. 在專案目錄執行上述 `dotnet aspnet-codegenerator controller ...` 指令
5. 產生後的 Controller 需在 `Program.cs` 或依專案慣例註冊路由/服務（若使用最小 API 或自訂路由，請依專案調整）

---

## 參考連結

- [ASP.NET Core 程式碼產生器 (aspnet-codegenerator)](https://learn.microsoft.com/aspnet/core/fundamentals/tools/dotnet-aspnet-codegenerator)
- [dotnet scaffold 介紹](https://devblogs.microsoft.com/dotnet/introducing-dotnet-scaffold/)
