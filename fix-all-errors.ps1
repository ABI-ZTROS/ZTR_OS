# ZTR_OS 一键修复所有编译错误脚本
# 在 PowerShell 中运行：powershell -ExecutionPolicy Bypass -File fix-all-errors.ps1

param(
    [string]$ProjectRoot = $PSScriptRoot,
    [string]$Branch = "main",
    [string]$Repo = "ABI-ZTROS/ZTR_OS"
)

$ErrorActionPreference = "Continue"
$BaseUrl = "https://raw.githubusercontent.com/$Repo/$Branch"

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  ZTR_OS 一键修复脚本" -ForegroundColor Cyan
Write-Host "  仓库: $Repo / 分支: $Branch" -ForegroundColor Cyan
Write-Host "  项目根目录: $ProjectRoot" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# 需要修复的文件列表
$files = @(
    @{
        Path = "src\ZTR.Desktop\ZTR.Desktop.csproj"
        Desc = "桌面应用项目文件（添加 TypeScriptCompileBlocked=true）"
    },
    @{
        Path = "src\ZTR.Desktop\Features\WebView2\Services\WebView2BridgeService.cs"
        Desc = "修复 CS1061: GetValue→GetString"
    },
    @{
        Path = "src\ZTR.Desktop\BootGuard.cs"
        Desc = "修复 CS0117: GetAvailableBrowserVersionAsync→文件检查"
    },
    @{
        Path = "src\ZTR.Desktop\Features\Config\Services\ConfigurationService.cs"
        Desc = "修复 CS0103: 添加 using System.IO"
    },
    @{
        Path = "src\ZTR.Desktop\Features\Process\Services\ProcessManagerService.cs"
        Desc = "修复 CS0234: DiagnosticsProcess 别名"
    },
    @{
        Path = "src\ZTR.Desktop\Features\Theme\Services\ThemeService.cs"
        Desc = "修复 CS0103: 添加 using System.IO/Json"
    },
    @{
        Path = "src\ZTR.Desktop\Features\UserAgreement\Views\UserAgreementWindow.xaml.cs"
        Desc = "修复 CS0308/CS0266: 添加 DI using"
    },
    @{
        Path = "src\ZTR.Desktop\GlobalExceptionHandler.cs"
        Desc = "修复 CS0117: Dispatcher→Application.Current"
    },
    @{
        Path = "src\ZTR.Api\ZTR.Api.csproj"
        Desc = "API 项目（添加 TypeScriptCompileBlocked=true）"
    },
    @{
        Path = "src\ZTR.Service\ZTR.Service.csproj"
        Desc = "修复 MSB4025: XML 标签格式"
    },
    @{
        Path = "src\ZTR.HAL\SystemSensorFallback.cs"
        Desc = "修复 CS1503: ManagementPath 构造"
    },
    @{
        Path = "src\ZTR.Intelligence\DecisionLogger.cs"
        Desc = "修复 CS0103: 添加 using System.IO"
    },
    @{
        Path = "src\ZTR.Intelligence\OnlineLearner.cs"
        Desc = "修复 CS0103: 添加 using System.IO"
    },
    @{
        Path = "src\ZTR.Api\Middleware\IntegrationMiddleware.cs"
        Desc = "修复 CS0103: 添加 using System.IO"
    },
    @{
        Path = "Directory.Build.props"
        Desc = "移除全局 TargetFramework 冲突"
    }
)

$success = 0
$failed = 0

foreach ($file in $files) {
    $localPath = Join-Path $ProjectRoot $file.Path
    $url = "$BaseUrl/$($file.Path -replace '\\','/')"
    
    Write-Host "[$($file.Desc)]" -ForegroundColor Yellow
    Write-Host "  下载: $url" -ForegroundColor Gray
    
    try {
        $content = Invoke-WebRequest -Uri $url -UseBasicParsing -TimeoutSec 10
        $dir = Split-Path $localPath -Parent
        if (-not (Test-Path $dir)) {
            New-Item -ItemType Directory -Path $dir -Force | Out-Null
        }
        [System.IO.File]::WriteAllText($localPath, $content.Content)
        Write-Host "  [OK] 已更新: $($file.Path)" -ForegroundColor Green
        $success++
    }
    catch {
        Write-Host "  [FAIL] $($_.Exception.Message)" -ForegroundColor Red
        $failed++
    }
    Write-Host ""
}

# ============ 清理编译缓存 ============
Write-Host "清理编译缓存..." -ForegroundColor Yellow
Get-ChildItem -Path (Join-Path $ProjectRoot "src") -Recurse -Directory -Include "obj","bin" -ErrorAction SilentlyContinue | Remove-Item -Recurse -Force -ErrorAction SilentlyContinue
Write-Host "  [OK] 已清理 obj/bin 目录" -ForegroundColor Green

# 清理 .vs 缓存
$vsDir = Join-Path $ProjectRoot ".vs"
if (Test-Path $vsDir) {
    Remove-Item -Recurse -Force $vsDir -ErrorAction SilentlyContinue
    Write-Host "  [OK] 已清理 .vs 目录" -ForegroundColor Green
}

# 清理前端 node_modules
$nodeModules = Join-Path $ProjectRoot "frontend\node_modules"
if (Test-Path $nodeModules) {
    Remove-Item -Recurse -Force $nodeModules -ErrorAction SilentlyContinue
    Write-Host "  [OK] 已清理 node_modules" -ForegroundColor Green
}

Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  完成！成功: $success / 失败: $failed" -ForegroundColor $(if($failed -eq 0){'Green'}else{'Red'})
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "下一步操作：" -ForegroundColor Yellow
Write-Host "  1. 在 Visual Studio 中重新加载项目" -ForegroundColor White
Write-Host "  2. 执行: dotnet restore" -ForegroundColor White
Write-Host "  3. 执行: dotnet build ZTR_OS.sln" -ForegroundColor White
Write-Host "  4. 或直接按 Ctrl+Shift+B" -ForegroundColor White
