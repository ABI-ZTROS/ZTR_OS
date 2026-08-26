@echo off
chcp 65001 >nul
setlocal enabledelayedexpansion

echo ========================================
echo   ZTR_OS 强制修复脚本（无需 Git）
echo ========================================
echo.

set "BASE_URL=https://raw.githubusercontent.com/ABI-ZTROS/ZTR_OS/main"
set "ROOT=%~dp0"

if "%ROOT:~-1%"=="\" set "ROOT=%ROOT:~0,-1%"

echo 项目根目录: %ROOT%
echo.

set "DOWNLOADED=0"
set "FAILED=0"

call :download "src\ZTR.Desktop\ZTR.Desktop.csproj"
call :download "src\ZTR.Desktop\Features\WebView2\Services\WebView2BridgeService.cs"
call :download "src\ZTR.Desktop\BootGuard.cs"
call :download "src\ZTR.Desktop\Features\Config\Services\ConfigurationService.cs"
call :download "src\ZTR.Desktop\Features\Process\Services\ProcessManagerService.cs"
call :download "src\ZTR.Desktop\Features\Theme\Services\ThemeService.cs"
call :download "src\ZTR.Desktop\Features\UserAgreement\Views\UserAgreementWindow.xaml.cs"
call :download "src\ZTR.Desktop\GlobalExceptionHandler.cs"
call :download "src\ZTR.Api\ZTR.Api.csproj"
call :download "src\ZTR.Service\ZTR.Service.csproj"
call :download "src\ZTR.HAL\SystemSensorFallback.cs"
call :download "src\ZTR.Intelligence\DecisionLogger.cs"
call :download "src\ZTR.Intelligence\OnlineLearner.cs"
call :download "src\ZTR.Api\Middleware\IntegrationMiddleware.cs"
call :download "Directory.Build.props"

echo.
echo 清理编译缓存...
for /d /r "%ROOT%\src" %%d in (obj bin) do (
    if exist "%%d" (
        rmdir /s /q "%%d" 2>nul
        echo   已删除: %%d
    )
)
if exist "%ROOT%\.vs" (
    rmdir /s /q "%ROOT%\.vs" 2>nul
    echo   已删除: .vs
)
if exist "%ROOT%\frontend\node_modules" (
    rmdir /s /q "%ROOT%\frontend\node_modules" 2>nul
    echo   已删除: node_modules
)

echo.
echo ========================================
echo   完成！成功下载: %DOWNLOADED% / 失败: %FAILED%
echo ========================================
echo.
echo 请在 Visual Studio 中：
echo   1. 关闭并重新打开解决方案
echo   2. 按 Ctrl+Shift+B 重新构建
echo   3. 或在终端执行: dotnet restore ^&^& dotnet build ZTR_OS.sln
echo.
pause
exit /b 0

:download
set "REL=%~1"
set "LOCAL=%ROOT%\%~1"
set "URL=%BASE_URL%/%~1"

echo [%~1]
echo   URL: %URL%
echo   本地: %LOCAL%

for %%F in ("%LOCAL%") do set "DIR=%%~dpF"
if not exist "%DIR%" mkdir "%DIR%" 2>nul

curl -sL -o "%LOCAL%" "%URL%" 2>nul
if %errorlevel%==0 (
    if exist "%LOCAL%" (
        set /a DOWNLOADED+=1
        echo   [OK] 下载成功
    ) else (
        set /a FAILED+=1
        echo   [FAIL] 下载失败
    )
) else (
    set /a FAILED+=1
    echo   [FAIL] curl 执行失败 (错误码: %errorlevel%)
)
echo.
exit /b 0
