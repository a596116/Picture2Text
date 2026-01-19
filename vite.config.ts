import { defineConfig } from 'vite'
import vue from '@vitejs/plugin-vue'
import tailwindcss from '@tailwindcss/vite'
import fs from 'fs'
import path from 'path'

// HTTPS 配置選項
// 方案 1: 使用自動生成的證書（最簡單，但首次訪問需要信任證書）
const useHttps = process.env.VITE_USE_HTTPS !== 'false'

// 方案 2: 使用自定義證書（如果有的話）
const customCertPath = process.env.VITE_SSL_CERT_PATH
const customKeyPath = process.env.VITE_SSL_KEY_PATH

let httpsConfig: any = false

if (useHttps) {
  if (customCertPath && customKeyPath) {
    // 使用自定義證書
    try {
      httpsConfig = {
        cert: fs.readFileSync(customCertPath),
        key: fs.readFileSync(customKeyPath),
      }
    } catch (error) {
      console.warn('無法讀取自定義證書，將使用自動生成證書')
      httpsConfig = true // 讓 Vite 自動生成
    }
  } else {
    // 讓 Vite 自動生成自簽名證書
    httpsConfig = true
  }
}

export default defineConfig({
  plugins: [vue(), tailwindcss()],
  server: {
    host: '0.0.0.0', // 允許從其他設備訪問（包括手機）
    port: 5173,
    // https: httpsConfig,
    // 如果使用自動生成證書，首次訪問時瀏覽器會顯示警告
    // 需要點擊「進階」->「繼續前往 [IP地址]（不安全）」
    // 手機訪問時，使用電腦的 IP 地址，例如：https://192.168.1.100:5173
  },
})
