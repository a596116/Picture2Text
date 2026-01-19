# Windows 環境攝像頭訪問解決方案

在 Windows 公司環境中，如果沒有 HTTPS 證書，瀏覽器會阻止訪問攝像頭。以下是幾種解決方案：

## 🔍 問題說明

現代瀏覽器（Chrome、Edge、Firefox）為了安全起見，要求使用 HTTPS 才能訪問攝像頭和麥克風等敏感設備。在本地開發環境中，有以下幾種解決方案：

## ✅ 解決方案

### 方案 1: 使用 localhost（最簡單，推薦優先嘗試）

**優點：**
- 無需任何配置
- 大多數瀏覽器允許在 localhost 上訪問攝像頭

**步驟：**
1. 直接運行 `npm run dev`
2. 訪問 `http://localhost:5173`（注意是 `http` 不是 `https`）
3. 如果瀏覽器允許，就可以直接使用攝像頭

**注意：** 如果公司瀏覽器政策較嚴格，此方法可能無效。

---

### 方案 2: 使用 Vite 自動生成的 HTTPS 證書（已配置）

**優點：**
- 無需額外工具
- 已在本專案中配置好

**步驟：**
1. 運行 `npm run dev`
2. 訪問 `https://localhost:5173`（注意是 `https`）
3. 首次訪問時，瀏覽器會顯示安全警告：
   - Chrome/Edge: 點擊「進階」→「繼續前往 localhost（不安全）」
   - Firefox: 點擊「進階」→「接受風險並繼續」
4. 之後就可以正常使用攝像頭了

**缺點：**
- 每次訪問都會有警告（可以手動信任證書）

---

### 方案 3: 使用 mkcert 生成受信任的本地證書（最穩定，推薦）

**優點：**
- 證書被系統信任，無瀏覽器警告
- 一次設置，永久使用

**步驟：**

#### 3.1 安裝 mkcert

**使用 Chocolatey（如果有）：**
```powershell
choco install mkcert
```

**使用 Scoop（如果有）：**
```powershell
scoop install mkcert
```

**手動安裝：**
1. 下載 mkcert: https://github.com/FiloSottile/mkcert/releases
2. 下載 `mkcert-v1.4.4-windows-amd64.exe`（或最新版本）
3. 重命名為 `mkcert.exe`
4. 放到系統 PATH 中（如 `C:\Windows\System32`）或專案目錄

#### 3.2 初始化 mkcert

```powershell
mkcert -install
```

這會在系統中安裝本地 CA（證書頒發機構）。

#### 3.3 生成證書

在專案根目錄執行：

```powershell
mkcert localhost 127.0.0.1 ::1
```

這會生成兩個文件：
- `localhost+2.pem`（證書）
- `localhost+2-key.pem`（私鑰）

#### 3.4 配置環境變數

**PowerShell：**
```powershell
$env:VITE_SSL_CERT_PATH="./localhost+2.pem"
$env:VITE_SSL_KEY_PATH="./localhost+2-key.pem"
npm run dev
```

**CMD：**
```cmd
set VITE_SSL_CERT_PATH=./localhost+2.pem
set VITE_SSL_KEY_PATH=./localhost+2-key.pem
npm run dev
```

**永久設置（可選）：**
在系統環境變數中添加：
- `VITE_SSL_CERT_PATH` = `C:\path\to\your\project\localhost+2.pem`
- `VITE_SSL_KEY_PATH` = `C:\path\to\your\project\localhost+2-key.pem`

#### 3.5 將證書添加到 .gitignore

```bash
echo "*.pem" >> .gitignore
```

---

### 方案 4: 使用公司提供的證書

如果公司有提供開發環境的證書：

1. 將證書文件放到專案目錄
2. 設置環境變數：

```powershell
$env:VITE_SSL_CERT_PATH="./company-cert.pem"
$env:VITE_SSL_KEY_PATH="./company-key.pem"
npm run dev
```

---

### 方案 5: 使用瀏覽器特殊標誌（不推薦，僅測試用）

**警告：** 此方法會降低瀏覽器安全性，僅用於開發測試。

**Chrome/Edge：**
```powershell
# 關閉所有 Chrome 窗口後執行
& "C:\Program Files\Google\Chrome\Application\chrome.exe" --unsafely-treat-insecure-origin-as-secure=http://localhost:5173 --user-data-dir="C:\temp\chrome-dev"
```

**Firefox：**
1. 訪問 `about:config`
2. 搜索 `media.getusermedia.insecure.enabled`
3. 設置為 `true`
4. 搜索 `media.getusermedia.screensharing.allowed_domains`
5. 添加 `localhost`

---

## 🧪 測試攝像頭是否可用

1. 啟動開發伺服器
2. 訪問應用
3. 點擊「使用相機拍照」按鈕
4. 如果看到相機預覽，說明配置成功
5. 如果看到錯誤訊息，請檢查：
   - 瀏覽器控制台是否有錯誤
   - 是否允許了攝像頭權限
   - 是否使用了 HTTPS（或 localhost）

---

## 🔧 故障排除

### 問題 1: 瀏覽器仍然阻止訪問

**解決方法：**
- 確保使用 `https://localhost:5173` 而不是 `http://`
- 檢查瀏覽器是否允許了攝像頭權限
- 嘗試清除瀏覽器緩存和 Cookie

### 問題 2: 證書錯誤

**解決方法：**
- 檢查證書文件路徑是否正確
- 確認證書文件存在且可讀
- 查看終端是否有錯誤訊息

### 問題 3: 公司防火牆阻止

**解決方法：**
- 聯繫 IT 部門請求開放本地 HTTPS 端口
- 或使用方案 1（localhost HTTP）

### 問題 4: 多個設備訪問

如果需要從其他設備（如手機）訪問：

1. 確保 Vite 配置中 `host: '0.0.0.0'`（已配置）
2. 找到電腦的 IP 地址：
   ```powershell
   ipconfig
   ```
3. 在其他設備訪問：`https://[你的IP]:5173`
4. 如果使用 mkcert，需要生成包含 IP 的證書：
   ```powershell
   mkcert localhost 127.0.0.1 ::1 [你的IP]
   ```

---

## 📝 推薦方案順序

1. **優先嘗試：** 方案 1（localhost HTTP）- 最簡單
2. **如果不行：** 方案 3（mkcert）- 最穩定
3. **臨時方案：** 方案 2（自動生成證書）- 有警告但可用
4. **公司環境：** 方案 4（公司證書）- 如果有提供

---

## 🔒 安全注意事項

- 自簽名證書僅用於本地開發，不要用於生產環境
- 不要將證書私鑰提交到版本控制系統
- 在公司環境中，建議使用公司提供的證書或聯繫 IT 部門

---

## 📚 相關資源

- [mkcert 官方文檔](https://github.com/FiloSottile/mkcert)
- [Vite 伺服器配置](https://vitejs.dev/config/server-options.html)
- [瀏覽器媒體權限 API](https://developer.mozilla.org/en-US/docs/Web/API/MediaDevices/getUserMedia)
