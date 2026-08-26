@echo off
chcp 65001 >nul
title ZTR_OS - 一键构建

echo ========================================
echo   ZTR_OS 一键构建脚本
echo   目标: 完整编译所有项目
echo ========================================
echo.

REM 检查 .NET SDK
where dotnet >nul 2>nul
if %errorlevel% neq 0 (
    echo [错误] 未找到 dotnet SDK，请安装 .NET 9.0 SDK
    echo 下载地址: https://dotnet.microsoft.com/download
    pause
    exit /b 1
)

REM 检查 Node.js
where node >nul 2>nul
if %errorlevel% neq 0 (
    echo [错误] 未找到 Node.js，请安装 Node.js 22+
    echo 下载地址: https://nodejs.org/
    pause
    exit /b 1
)

echo [1/6] 清理旧构建...
dotnet clean ZTR_OS.sln 2>nul

echo [2/6] 还原 NuGet 包...
dotnet restore ZTR_OS.sln
if %errorlevel% neq 0 (
    echo [错误] NuGet 还原失败
    pause
    exit /b 1
)

echo [3/6] 安装前端依赖...
cd frontend
call npm ci
if %errorlevel% neq 0 (
    echo [错误] npm ci 失败，尝试 npm install...
    call npm install
    if %errorlevel% neq 0 (
        echo [错误] npm install 也失败了
        cd ..
        pause
        exit /b 1
    )
)

echo [4/6] 构建前端...
call npm run build
if %errorlevel% neq 0 (
    echo [错误] 前端构建失败
    cd ..
    pause
    exit /b 1
)
cd ..

echo [5/6] 复制前端到 API wwwroot...
if exist "src\ZTR.Api\wwwroot" rmdir /s /q "src\ZTR.Api\wwwroot"
mkdir "src\ZTR.Api\wwwroot"
xcopy /e /y "frontend\dist\*" "src\ZTR.Api\wwwroot\" >nul

echo [6/6] 编译后端和桌面...
dotnet build ZTR_OS.sln -c Release
if %errorlevel% neq 0 (
    echo [错误] 编译失败
    echo.
    echo 请检查上方错误信息
    echo.
    echo 常见问题排查:
    echo 1. 是否以管理员身份运行?
    echo 2. Windows SDK 是否已安装?
    echo 3. WebView2 Runtime  是否已安装?
    pause
    exit /b 1
)

echo.
echo ========================================
echo   构建成功!
echo ========================================
echo.
echo 桌面应用: src\ZTR.Desktop\bin\Release\net9.0-windows\
echo API 后端: src\ZTR.Api\bin\Release\net9.0-windows\
echo.
echo 运行桌面应用:
echo   cd src\ZTR.Desktop\bin\Release\net9.0-windows\
echo   ZTR.Desktop.exe
echo.
echo 运行 API 后端:
echo   cd src\ZTR.Api\bin\Release\net9.0-windows\
echo   ZTR.Api.exe
echo.
pause
