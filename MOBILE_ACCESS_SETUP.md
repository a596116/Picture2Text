# 手機連接電腦開發伺服器設置指南

本指南說明如何在手機上訪問電腦上運行的 Vue 開發伺服器，並使用手機攝像頭功能。

## 📱 前置條件

1. **電腦和手機必須在同一個 Wi-Fi 網絡中**
2. **電腦防火牆需要允許端口 5173 的連接**
3. **需要 HTTPS 才能使用攝像頭**（瀏覽器安全要求）

## 🚀 快速開始

### 步驟 1: 獲取電腦的 IP 地址

**Windows:**
```powershell
ipconfig
```
找到「IPv4 地址」，例如：`192.168.1.100`

**macOS/Linux:**
```bash
ifconfig
# 或
ip addr
```
找到 `inet` 地址，通常在 `en0` 或 `wlan0` 下

### 步驟 2: 配置並啟動開發伺服器

#### 方案 A: 使用自動生成的 HTTPS 證書（最簡單）

```bash
npm run dev
```

**注意：** 自動生成的證書只包含 `localhost`，手機訪問時會有證書警告，但可以手動接受。

#### 方案 B: 使用 mkcert 生成包含 IP 的證書（推薦，無警告）

1. **安裝 mkcert**（如果還沒安裝）

   **Windows (Chocolatey):**
   ```powershell
   choco install mkcert
   ```

   **macOS:**
   ```bash
   brew install mkcert
   ```

   **Linux:**
   ```bash
   # Ubuntu/Debian
   sudo apt install mkcert
   ```

2. **初始化 mkcert**
   ```bash
   mkcert -install
   ```

3. **生成包含 IP 地址的證書**

   先獲取你的 IP 地址（例如：192.168.1.100），然後執行：

   ```bash
   mkcert localhost 127.0.0.1 ::1 192.168.1.100
   ```

   **重要：** 將 `192.168.1.100` 替換為你的實際 IP 地址！

   這會生成：
   - `localhost+3.pem`（證書）
   - `localhost+3-key.pem`（私鑰）

4. **設置環境變數並啟動**

   **Windows PowerShell:**
   ```powershell
   $env:VITE_SSL_CERT_PATH="./localhost+3.pem"
   $env:VITE_SSL_KEY_PATH="./localhost+3-key.pem"
   npm run dev
   ```

   **macOS/Linux:**
   ```bash
   export VITE_SSL_CERT_PATH="./localhost+3.pem"
   export VITE_SSL_KEY_PATH="./localhost+3-key.pem"
   npm run dev
   ```

### 步驟 3: 在手機上訪問

1. **確保手機和電腦在同一個 Wi-Fi 網絡**
2. **打開手機瀏覽器**（Chrome、Safari、Edge 等）
3. **訪問：** `https://[你的IP地址]:5173`

   例如：`https://192.168.1.100:5173`

4. **首次訪問時處理證書警告：**

   **如果使用自動生成證書：**
   - Chrome/Android: 會顯示「您的連線不是私人連線」
   - 點擊「進階」→「繼續前往 [IP地址]（不安全）」
   - Safari/iOS: 會顯示「無法驗證伺服器身份」
   - 點擊「顯示詳細資料」→「訪問此網站」

   **如果使用 mkcert 證書：**
   - 通常不會有警告（證書已被系統信任）

5. **允許攝像頭權限：**
   - 瀏覽器會請求攝像頭權限
   - 點擊「允許」或「允許訪問相機」

## 🔧 常見問題

### 問題 1: 手機無法連接到電腦

**檢查清單：**
- ✅ 電腦和手機是否在同一個 Wi-Fi？
- ✅ 電腦防火牆是否允許端口 5173？
- ✅ IP 地址是否正確？
- ✅ 開發伺服器是否正在運行？

**Windows 防火牆設置：**
```powershell
# 允許端口 5173（以管理員身份運行）
New-NetFirewallRule -DisplayName "Vite Dev Server" -Direction Inbound -LocalPort 5173 -Protocol TCP -Action Allow
```

**macOS 防火牆：**
系統偏好設置 → 安全性與隱私 → 防火牆 → 選項 → 允許 Node.js

### 問題 2: 證書錯誤

**解決方法：**
- 如果使用自動生成證書，手機上會有警告，需要手動接受
- 建議使用 mkcert 生成包含 IP 的證書（方案 B）

### 問題 3: 手機無法使用攝像頭

**檢查：**
- ✅ 是否使用 HTTPS（不是 HTTP）？
- ✅ 是否允許了瀏覽器的攝像頭權限？
- ✅ 手機瀏覽器是否支援 `getUserMedia` API？

**測試：**
在手機瀏覽器控制台（如果可用）檢查是否有錯誤訊息。

### 問題 4: IP 地址經常變化

**解決方法：**
1. **設置靜態 IP**（在路由器設置中）
2. **或每次啟動時重新生成證書**（如果使用 mkcert）

**自動獲取 IP 的腳本（Windows PowerShell）：**
```powershell
# 獲取本機 IP
$ip = (Get-NetIPAddress -AddressFamily IPv4 | Where-Object {$_.IPAddress -like "192.168.*"}).IPAddress
Write-Host "你的 IP 地址是: $ip"
Write-Host "手機訪問: https://$ip:5173"
```

**macOS/Linux 腳本：**
```bash
#!/bin/bash
IP=$(ifconfig | grep "inet " | grep -v 127.0.0.1 | awk '{print $2}' | head -1)
echo "你的 IP 地址是: $IP"
echo "手機訪問: https://$IP:5173"
```

### 問題 5: 手機瀏覽器顯示「無法連接到伺服器」

**可能原因：**
- 防火牆阻止
- IP 地址錯誤
- 開發伺服器未運行
- 網絡問題

**解決方法：**
1. 在電腦上測試：`https://localhost:5173` 是否能訪問
2. 在手機上 ping 電腦 IP：確認網絡連通
3. 檢查防火牆設置

## 📝 推薦工作流程

### 日常開發（IP 不變）

1. 使用 mkcert 生成一次包含 IP 的證書
2. 將證書路徑添加到環境變數（或創建啟動腳本）
3. 每次運行 `npm run dev`
4. 手機訪問 `https://[IP]:5173`

### 動態 IP 環境

1. 創建啟動腳本，自動獲取 IP 並生成證書
2. 或使用自動生成證書（每次訪問時手動接受警告）

## 🔐 安全注意事項

- ⚠️ 自簽名證書僅用於開發，不要用於生產環境
- ⚠️ 不要將證書私鑰提交到版本控制
- ⚠️ 在公司網絡中，可能需要 IT 部門批准
- ⚠️ 確保防火牆只允許必要的端口

## 🛠️ 便利腳本

### Windows 啟動腳本（start-dev.ps1）

```powershell
# 獲取 IP 地址
$ip = (Get-NetIPAddress -AddressFamily IPv4 | Where-Object {$_.IPAddress -like "192.168.*"}).IPAddress

Write-Host "========================================"
Write-Host "開發伺服器啟動中..."
Write-Host "你的 IP 地址: $ip"
Write-Host "手機訪問: https://$ip:5173"
Write-Host "========================================"

# 檢查是否有證書
if (Test-Path "./localhost+3.pem") {
    $env:VITE_SSL_CERT_PATH="./localhost+3.pem"
    $env:VITE_SSL_KEY_PATH="./localhost+3-key.pem"
    Write-Host "使用自定義證書"
} else {
    Write-Host "使用自動生成證書（手機訪問時會有警告）"
}

npm run dev
```

### macOS/Linux 啟動腳本（start-dev.sh）

```bash
#!/bin/bash

# 獲取 IP 地址
IP=$(ifconfig | grep "inet " | grep -v 127.0.0.1 | awk '{print $2}' | head -1)

echo "========================================"
echo "開發伺服器啟動中..."
echo "你的 IP 地址: $IP"
echo "手機訪問: https://$IP:5173"
echo "========================================"

# 檢查是否有證書
if [ -f "./localhost+3.pem" ]; then
    export VITE_SSL_CERT_PATH="./localhost+3.pem"
    export VITE_SSL_KEY_PATH="./localhost+3-key.pem"
    echo "使用自定義證書"
else
    echo "使用自動生成證書（手機訪問時會有警告）"
fi

npm run dev
```

使用方式：
```bash
# macOS/Linux
chmod +x start-dev.sh
./start-dev.sh

# Windows
.\start-dev.ps1
```

## 📱 手機瀏覽器建議

- **Android:** Chrome、Edge、Firefox
- **iOS:** Safari、Chrome

所有這些瀏覽器都支援 `getUserMedia` API 和 HTTPS。

## 🎯 測試清單

- [ ] 電腦和手機在同一 Wi-Fi
- [ ] 獲取了正確的 IP 地址
- [ ] 開發伺服器正在運行
- [ ] 防火牆允許端口 5173
- [ ] 手機可以訪問 `https://[IP]:5173`
- [ ] 處理了證書警告（如果有的話）
- [ ] 允許了瀏覽器攝像頭權限
- [ ] 可以正常使用攝像頭功能

完成以上步驟後，您就可以在手機上使用攝像頭功能了！🎉
