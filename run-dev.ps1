# PowerShell script khởi chạy đồng thời Backend .NET API và Frontend Vue 3
# Enterprise BPM Platform

Write-Host "==========================================================" -ForegroundColor Cyan
Write-Host "🚀 Khởi động Hệ thống Enterprise BPM Platform (Fullstack)" -ForegroundColor Cyan
Write-Host "==========================================================" -ForegroundColor Cyan

$workspaceRoot = $PSScriptRoot

# 1. Khởi chạy Backend .NET API Host trong cửa sổ riêng
Write-Host "`n[1/2] Đang khởi chạy Backend API Host tại http://localhost:5000 ..." -ForegroundColor Yellow
$backendCmd = "cd '$workspaceRoot'; dotnet run --project src/Host/EnterpriseBpm.ApiHost/EnterpriseBpm.ApiHost.csproj"
Start-Process powershell -ArgumentList "-NoExit", "-Command", "$backendCmd"

# 2. Khởi chạy Frontend Vue 3 SPA trong cửa sổ riêng
Write-Host "[2/2] Đang khởi chạy Frontend Vue 3 SPA tại http://localhost:3000 ..." -ForegroundColor Yellow
$frontendCmd = "cd '$workspaceRoot/frontend'; npm run dev"
Start-Process powershell -ArgumentList "-NoExit", "-Command", "$frontendCmd"

Write-Host "`n==========================================================" -ForegroundColor Green
Write-Host "✅ ĐÃ KHỞI CHẠY THÀNH CÔNG CẢ 2 PHÂN HỆ!" -ForegroundColor Green
Write-Host "🌐 Frontend Vue 3:  http://localhost:3000" -ForegroundColor White
Write-Host "📡 Backend API:     http://localhost:5000/swagger" -ForegroundColor White
Write-Host "🔑 Tài khoản Admin: admin / Admin@123" -ForegroundColor White
Write-Host "==========================================================" -ForegroundColor Green
