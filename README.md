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
