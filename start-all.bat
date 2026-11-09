@echo off
chcp 65001 >nul
title Enterprise BPM Platform - Fullstack Launcher

echo ================================================================
echo   🚀 ENTERPRISE BPM PLATFORM - KHỞI ĐỘNG FULLSTACK
echo ================================================================
echo.
echo [1/3] Đang khởi động Backend .NET API (Cổng 5000)...
start "Enterprise BPM - Backend API (Port 5000)" cmd /k "cd /d "%~dp0" && dotnet run --project src/Host/EnterpriseBpm.ApiHost/EnterpriseBpm.ApiHost.csproj"

echo [2/3] Đang khởi động Frontend Vue 3 (Cổng 3000)...
start "Enterprise BPM - Frontend Vue 3 (Port 3000)" cmd /k "cd /d "%~dp0frontend" && npm run dev"

echo [3/3] Đang đợi dịch vụ sẵn sàng và mở trình duyệt...
timeout /t 4 /nobreak >nul
start http://localhost:3000

echo.
echo ================================================================
echo   ✅ HỆ THỐNG ĐÃ ĐƯỢC MỞ TRÊN TRÌNH DUYỆT!
echo.
echo   🌐 Frontend Vue 3:   http://localhost:3000
echo   📡 Swagger API:      http://localhost:5000/swagger
echo   🔑 Tài khoản Admin:  admin / Admin@123
echo ================================================================
echo.
echo Bạn có thể đóng cửa sổ này, 2 cửa sổ Backend và Frontend vẫn đang chạy.
pause
