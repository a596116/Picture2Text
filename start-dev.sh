#!/bin/bash

# 獲取本機 IP 地址（排除 127.0.0.1）
IP=$(ifconfig | grep "inet " | grep -v 127.0.0.1 | awk '{print $2}' | head -1)

# 如果找不到 IP，嘗試其他方法
if [ -z "$IP" ]; then
    IP=$(ipconfig getifaddr en0 2>/dev/null || ipconfig getifaddr en1 2>/dev/null || echo "未找到")
fi

echo ""
echo "========================================"
echo "🚀 開發伺服器啟動中..."
echo "========================================"
echo "📍 本機訪問: https://localhost:5173"
if [ "$IP" != "未找到" ]; then
    echo "📱 手機訪問: https://$IP:5173"
    echo ""
    echo "💡 提示："
    echo "   - 確保手機和電腦在同一 Wi-Fi"
    echo "   - 首次訪問時需要接受證書警告"
    echo "   - 允許瀏覽器的攝像頭權限"
else
    echo "⚠️  無法自動獲取 IP 地址，請手動查看網絡設置"
fi
echo "========================================"
echo ""

# 檢查是否有包含 IP 的證書
if [ -f "./localhost+3.pem" ]; then
    export VITE_SSL_CERT_PATH="./localhost+3.pem"
    export VITE_SSL_KEY_PATH="./localhost+3-key.pem"
    echo "✅ 使用自定義證書（包含 IP 地址）"
    echo ""
elif [ -f "./localhost+2.pem" ]; then
    export VITE_SSL_CERT_PATH="./localhost+2.pem"
    export VITE_SSL_KEY_PATH="./localhost+2-key.pem"
    echo "✅ 使用自定義證書（僅 localhost）"
    echo "⚠️  手機訪問時會有證書警告"
    echo ""
else
    echo "ℹ️  使用自動生成證書"
    echo "⚠️  手機訪問時會有證書警告，建議使用 mkcert 生成證書"
    echo "   參考: MOBILE_ACCESS_SETUP.md"
    echo ""
fi

npm run dev
