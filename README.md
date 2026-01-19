# 发票识别系统

基于 Vue 3 + TypeScript + Tailwind CSS + Element Plus 的发票图片识别系统。

## 功能特性

- 支持拖拽上传发票图片
- 支持批量上传多张发票
- 图片转 Base64 编码
- AI 识别发票信息（当前使用模拟数据）
- 可编辑识别结果
- 批量保存发票数据

## 技术栈

- Vue 3
- TypeScript
- Tailwind CSS
- Element Plus
- Vite

## 安装依赖

```bash
npm install
```

## 运行项目

```bash
npm run dev
```

项目将在 `http://localhost:5173` 运行

### 攝像頭訪問配置（Windows 環境）

由於瀏覽器安全限制，訪問攝像頭需要 HTTPS 環境。本專案已配置自動 HTTPS 支援。

#### 方案 1: 使用 localhost（最簡單）

大多數瀏覽器允許在 `localhost` 或 `127.0.0.1` 上訪問攝像頭，即使沒有 HTTPS：

```bash
npm run dev
```

然後訪問 `http://localhost:5173` 即可。

#### 方案 2: 使用自動生成的 HTTPS 證書（推薦）

專案已配置自動生成自簽名證書：

```bash
npm run dev
```

然後訪問 `https://localhost:5173`，首次訪問時：
1. 瀏覽器會顯示「您的連線不是私人連線」警告
2. 點擊「進階」或「Advanced」
3. 點擊「繼續前往 localhost（不安全）」或「Proceed to localhost (unsafe)」

之後就可以正常使用攝像頭功能了。

#### 方案 3: 使用自定義證書

如果您有自簽名證書，可以設置環境變數：

```bash
# Windows PowerShell
$env:VITE_SSL_CERT_PATH="path/to/cert.pem"
$env:VITE_SSL_KEY_PATH="path/to/key.pem"
npm run dev

# Windows CMD
set VITE_SSL_CERT_PATH=path/to/cert.pem
set VITE_SSL_KEY_PATH=path/to/key.pem
npm run dev
```

#### 方案 4: 禁用 HTTPS（僅用於測試）

如果確定使用 localhost 且瀏覽器允許，可以禁用 HTTPS：

```bash
# Windows PowerShell
$env:VITE_USE_HTTPS="false"
npm run dev

# Windows CMD
set VITE_USE_HTTPS=false
npm run dev
```

#### 使用 mkcert 生成受信任的本地證書（可選，最穩定）

1. 安裝 mkcert：
   ```bash
   # Windows - 使用 Chocolatey
   choco install mkcert
   
   # macOS
   brew install mkcert
   
   # Linux (Ubuntu/Debian)
   sudo apt install mkcert
   ```

2. 初始化 mkcert：
   ```bash
   mkcert -install
   ```

3. 生成證書：
   ```bash
   # 僅本地訪問
   mkcert localhost 127.0.0.1 ::1
   
   # 如果需要手機訪問，需要包含電腦的 IP 地址
   mkcert localhost 127.0.0.1 ::1 192.168.1.100
   ```

4. 設置環境變數：
   ```bash
   # Windows PowerShell
   $env:VITE_SSL_CERT_PATH="./localhost+2.pem"
   $env:VITE_SSL_KEY_PATH="./localhost+2-key.pem"
   npm run dev
   
   # macOS/Linux
   export VITE_SSL_CERT_PATH="./localhost+2.pem"
   export VITE_SSL_KEY_PATH="./localhost+2-key.pem"
   npm run dev
   ```

這樣生成的證書會被系統信任，不會有瀏覽器警告。

### 📱 手機連接設置

如果您需要在手機上訪問開發伺服器並使用攝像頭，請參考 **[MOBILE_ACCESS_SETUP.md](./MOBILE_ACCESS_SETUP.md)** 詳細指南。

**快速步驟：**
1. 確保手機和電腦在同一 Wi-Fi
2. 獲取電腦 IP 地址（Windows: `ipconfig`，macOS/Linux: `ifconfig`）
3. 使用 mkcert 生成包含 IP 的證書（見上方）
4. 手機訪問 `https://[你的IP]:5173`
5. 允許攝像頭權限

## 构建项目

```bash
npm run build
```

## 项目结构

```
Picture2Text/
├── src/
│   ├── api/
│   │   └── invoice.ts          # API 接口（目前是模拟数据）
│   ├── components/
│   │   └── InvoiceUpload.vue   # 发票上传组件
│   ├── types/
│   │   └── invoice.ts          # TypeScript 类型定义
│   ├── App.vue                 # 主组件
│   ├── main.ts                 # 入口文件
│   └── style.css               # Tailwind CSS 样式
├── index.html                  # HTML 入口
├── package.json                # 项目配置
├── tsconfig.json               # TypeScript 配置
├── vite.config.ts              # Vite 配置
└── tailwind.config.js          # Tailwind CSS 配置
```

## 使用说明

1. 点击或拖拽发票图片到上传区域
2. 系统会自动识别发票信息（目前生成模拟数据）
3. 可以编辑任何识别出的字段
4. 点击"保存全部发票"按钮保存数据

## API 集成

目前项目使用模拟数据。要集成真实的后端 API，请修改 `src/api/invoice.ts` 文件中被注释的代码：

```typescript
// 取消注释实际 API 调用部分
export const recognizeInvoice = async (base64: string): Promise<RecognizeResponse> => {
  try {
    const response = await fetch('/api/invoice/recognize', {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
      },
      body: JSON.stringify({ image: base64 })
    })
    const data = await response.json()
    return data
  } catch (error) {
    return {
      success: false,
      message: '网络错误，请重试'
    }
  }
}
```

## 识别字段

- 发票代码
- 发票号码
- 开票日期
- 金额
- 税额
- 价税合计
- 销售方名称
- 销售方纳税人识别号
- 购买方名称
- 购买方纳税人识别号
- 备注

## License

MIT
