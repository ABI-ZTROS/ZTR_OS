# ZTR_OS 一键构建脚本
# 用法: 在 PowerShell 中运行 .\build-all.ps1

$ErrorActionPreference = "Stop"

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  ZTR_OS 一键构建脚本" -ForegroundColor Cyan
Write-Host "  目标: 完整编译所有项目" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# 检查 .NET SDK
try {
    $dotnetVersion = (dotnet --version)
    Write-Host "[OK] .NET SDK: $dotnetVersion" -ForegroundColor Green
} catch {
    Write-Host "[错误] 未找到 dotnet SDK，请安装 .NET 9.0 SDK" -ForegroundColor Red
    Write-Host "下载地址: https://dotnet.microsoft.com/download" -ForegroundColor Yellow
    Read-Host "按回车退出"
    exit 1
}

# 检查 Node.js
try {
    $nodeVersion = (node --version)
    Write-Host "[OK] Node.js: $nodeVersion" -ForegroundColor Green
} catch {
    Write-Host "[错误] 未找到 Node.js，请安装 Node.js 22+" -ForegroundColor Red
    Write-Host "下载地址: https://nodejs.org/" -ForegroundColor Yellow
    Read-Host "按回车退出"
    exit 1
}

Write-Host ""

# 步骤 1: 清理
Write-Host "[1/7] 清理旧构建..." -ForegroundColor Yellow
dotnet clean ZTR_OS.sln 2>$null
Write-Host "  完成" -ForegroundColor Gray

# 步骤 2: 还原
Write-Host "[2/7] 还原 NuGet 包..." -ForegroundColor Yellow
dotnet restore ZTR_OS.sln
if ($LASTEXITCODE -ne 0) {
    Write-Host "[错误] NuGet 还原失败" -ForegroundColor Red
    Read-Host "按回车退出"
    exit 1
}
Write-Host "  完成" -ForegroundColor Gray

# 步骤 3: 前端依赖
Write-Host "[3/7] 安装前端依赖..." -ForegroundColor Yellow
Push-Location frontend
try {
    npm ci
    if ($LASTEXITCODE -ne 0) {
        Write-Host "  npm ci 失败，尝试 npm install..." -ForegroundColor Yellow
        npm install
    }
    if ($LASTEXITCODE -ne 0) {
        Write-Host "[错误] 前端依赖安装失败" -ForegroundColor Red
        Pop-Location
        Read-Host "按回车退出"
        exit 1
    }
} catch {
    Write-Host "[错误] 前端依赖安装失败: $_" -ForegroundColor Red
    Pop-Location
    Read-Host "按回车退出"
    exit 1
}
Pop-Location
Write-Host "  完成" -ForegroundColor Gray

# 步骤 4: 前端构建
Write-Host "[4/7] 构建前端..." -ForegroundColor Yellow
Push-Location frontend
npm run build
if ($LASTEXITCODE -ne 0) {
    Write-Host "[错误] 前端构建失败" -ForegroundColor Red
    Pop-Location
    Read-Host "按回车退出"
    exit 1
}
Pop-Location
Write-Host "  完成" -ForegroundColor Gray

# 步骤 5: 复制前端
Write-Host "[5/7] 复制前端到 API wwwroot..." -ForegroundColor Yellow
$wwwrootPath = "src\ZTR.Api\wwwroot"
if (Test-Path $wwwrootPath) {
    Remove-Item -Recurse -Force $wwwrootPath
}
New-Item -ItemType Directory -Force -Path $wwwrootPath | Out-Null
Copy-Item -Recurse -Force "frontend\dist\*" $wwwrootPath
Write-Host "  完成" -ForegroundColor Gray

# 步骤 6: 编译
Write-Host "[6/7] 编译后端和桌面..." -ForegroundColor Yellow
dotnet build ZTR_OS.sln -c Release
if ($LASTEXITCODE -ne 0) {
    Write-Host "[错误] 编译失败" -ForegroundColor Red
    Write-Host ""
    Write-Host "请检查上方错误信息" -ForegroundColor Yellow
    Write-Host ""
    Write-Host "常见问题排查:" -ForegroundColor Yellow
    Write-Host "1. 是否以管理员身份运行?" -ForegroundColor White
    Write-Host "2. Windows SDK 是否已安装?" -ForegroundColor White
    Write-Host "3. WebView2 Runtime  是否已安装?" -ForegroundColor White
    Read-Host "按回车退出"
    exit 1
}
Write-Host "  完成" -ForegroundColor Gray

# 步骤 7: 验证
Write-Host "[7/7] 验证构建产物..." -ForegroundColor Yellow
$desktopPath = "src\ZTR.Desktop\bin\Release\net9.0-windows"
$apiPath = "src\ZTR.Api\bin\Release\net9.0-windows"

$desktopExists = Test-Path "$desktopPath\ZTR.Desktop.exe"
$apiExists = Test-Path "$apiPath\ZTR.Api.exe"

if ($desktopExists) {
    Write-Host "  [OK] 桌面应用: $desktopPath\ZTR.Desktop.exe" -ForegroundColor Green
} else {
    Write-Host "  [WARN] 桌面应用未找到" -ForegroundColor Yellow
}

if ($apiExists) {
    Write-Host "  [OK] API 后端: $apiPath\ZTR.Api.exe" -ForegroundColor Green
} else {
    Write-Host "  [WARN] API 后端未找到" -ForegroundColor Yellow
}

Write-Host ""
Write-Host "========================================" -ForegroundColor Green
Write-Host "  构建成功!" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Green
Write-Host ""
Write-Host "运行桌面应用:" -ForegroundColor Cyan
Write-Host "  cd $desktopPath" -ForegroundColor White
Write-Host "  .\ZTR.Desktop.exe" -ForegroundColor White
Write-Host ""
Write-Host "运行 API 后端:" -ForegroundColor Cyan
Write-Host "  cd $apiPath" -ForegroundColor White
Write-Host "  .\ZTR.Api.exe" -ForegroundColor White
Write-Host ""

Read-Host "按回车退出"
