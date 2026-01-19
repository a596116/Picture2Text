# PowerShell 啟動腳本

# 獲取本機 IP 地址（排除 127.0.0.1 和 169.254.x.x）
$ip = (Get-NetIPAddress -AddressFamily IPv4 | Where-Object {
    $_.IPAddress -like "192.168.*" -or 
    $_.IPAddress -like "10.*" -or 
    $_.IPAddress -like "172.16.*" -or 
    $_.IPAddress -like "172.17.*" -or 
    $_.IPAddress -like "172.18.*" -or 
    $_.IPAddress -like "172.19.*" -or 
    $_.IPAddress -like "172.20.*" -or 
    $_.IPAddress -like "172.21.*" -or 
    $_.IPAddress -like "172.22.*" -or 
    $_.IPAddress -like "172.23.*" -or 
    $_.IPAddress -like "172.24.*" -or 
    $_.IPAddress -like "172.25.*" -or 
    $_.IPAddress -like "172.26.*" -or 
    $_.IPAddress -like "172.27.*" -or 
    $_.IPAddress -like "172.28.*" -or 
    $_.IPAddress -like "172.29.*" -or 
    $_.IPAddress -like "172.30.*" -or 
    $_.IPAddress -like "172.31.*"
} | Select-Object -First 1).IPAddress

Write-Host ""
Write-Host "========================================"
Write-Host "🚀 開發伺服器啟動中..."
Write-Host "========================================"
Write-Host "📍 本機訪問: https://localhost:5173"
if ($ip) {
    Write-Host "📱 手機訪問: https://$ip:5173"
    Write-Host ""
    Write-Host "💡 提示："
    Write-Host "   - 確保手機和電腦在同一 Wi-Fi"
    Write-Host "   - 首次訪問時需要接受證書警告"
    Write-Host "   - 允許瀏覽器的攝像頭權限"
} else {
    Write-Host "⚠️  無法自動獲取 IP 地址，請手動查看網絡設置"
    Write-Host "   運行 'ipconfig' 查看 IP 地址"
}
Write-Host "========================================"
Write-Host ""

# 檢查是否有包含 IP 的證書
if (Test-Path "./localhost+3.pem") {
    $env:VITE_SSL_CERT_PATH="./localhost+3.pem"
    $env:VITE_SSL_KEY_PATH="./localhost+3-key.pem"
    Write-Host "✅ 使用自定義證書（包含 IP 地址）"
    Write-Host ""
} elseif (Test-Path "./localhost+2.pem") {
    $env:VITE_SSL_CERT_PATH="./localhost+2.pem"
    $env:VITE_SSL_KEY_PATH="./localhost+2-key.pem"
    Write-Host "✅ 使用自定義證書（僅 localhost）"
    Write-Host "⚠️  手機訪問時會有證書警告"
    Write-Host ""
} else {
    Write-Host "ℹ️  使用自動生成證書"
    Write-Host "⚠️  手機訪問時會有證書警告，建議使用 mkcert 生成證書"
    Write-Host "   參考: MOBILE_ACCESS_SETUP.md"
    Write-Host ""
}

npm run dev
