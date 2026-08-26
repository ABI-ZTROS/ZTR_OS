# ZTR_OS 构建指南

## 快速开始

### 方式一：一键构建（推荐）

在 Windows 上运行 `build-all.bat` 或 `build-all.ps1`：

```powershell
# 在 PowerShell 中执行
.\build-all.ps1
```

或双击 `build-all.bat`。

### 方式二：手动构建

```powershell
# 1. 克隆仓库
git clone https://github.com/ABI-ZTROS/ZTR_OS.git
cd ZTR_OS

# 2. 还原依赖
dotnet restore

# 3. 构建前端
cd frontend
npm ci
npm run build
cd ..

# 4. 复制前端到 API
mkdir src\ZTR.Api\wwwroot
xcopy /e /y frontend\dist\* src\ZTR.Api\wwwroot\

# 5. 编译
dotnet build -c Release

# 6. 运行
cd src\ZTR.Desktop\bin\Release\net9.0-windows\
.\ZTR.Desktop.exe
```

## 前置要求

### 必需软件

| 软件 | 版本要求 | 下载地址 |
|------|----------|----------|
| Windows | 10/11 x64 | - |
| .NET SDK | 9.0+ | https://dotnet.microsoft.com/download |
| Node.js | 22+ | https://nodejs.org/ |
| Visual Studio | 2022+ (可选) | https://visualstudio.microsoft.com/ |

### Windows 特有依赖

| 依赖 | 说明 |
|------|------|
| WebView2 Runtime | 桌面应用内嵌浏览器必需 |
| Windows SDK | WPF 编译必需（通过 Visual Studio 安装） |
| Microsoft Visual C++ Redistributable | Win32 API 调用必需 |

## 常见问题排查

### Q1: 编译时报错 CS0103 "当前上下文中不存在名称 Path"

**原因**: 源文件缺少 `using System.IO;` 指令

**解决方案**: 已在最新代码中修复，请执行 `git pull` 更新代码

### Q2: 编译时报错 TS6053 "node_modules/typescript/lib/lib.dom.d.ts not found"

**原因**: node_modules 不完整或已损坏

**解决方案**:
```powershell
cd frontend
Remove-Item -Recurse -Force node_modules
npm ci
npm run build
cd ..
```

### Q3: 编译时报错 MSB3073 "npm run build 已退出，代码为 1"

**原因**: 前端构建失败，通常是 TypeScript 类型错误

**解决方案**:
1. 先单独构建前端查看详细错误
```powershell
cd frontend
npm run build  # 查看具体错误
```

2. 常见修复：清除缓存后重试
```powershell
Remove-Item -Recurse -Force node_modules
npm ci
npm run build
```

### Q4: 运行时提示 "WebView2 Runtime 未安装"

**解决方案**: 下载并安装 WebView2 Runtime
- 下载地址: https://developer.microsoft.com/microsoft-edge/webview2/

### Q5: 运行时提示 "需要管理员权限"

**解决方案**: 右键可执行文件，选择"以管理员身份运行"

### Q6: 识别不到 ASUS 设备

**解决方案**:
1. 确认设备为 ASUS ROG 系列
2. 确认以管理员权限运行
3. 检查设备管理器中 ACPI 驱动状态
4. 确认 ZTR_OS 支持的设备型号

## 发布构建

### 自包含发布（单文件）

```powershell
# 发布桌面应用
dotnet publish src/ZTR.Desktop/ZTR.Desktop.csproj `
  -c Release -r win-x64 `
  --self-contained true `
  -p:PublishSingleFile=true `
  -p:EnableCompressionInSingleFile=true `
  -o ./publish/desktop

# 发布 API 后端
dotnet publish src/ZTR.Api/ZTR.Api.csproj `
  -c Release -r win-x64 `
  --self-contained true `
  -p:PublishSingleFile=true `
  -o ./publish/api
```

## CI/CD

项目已配置 GitHub Actions：

| 工作流 | 触发条件 | 说明 |
|--------|----------|------|
| [ci.yml](../.github/workflows/ci.yml) | push/PR to main | Ubuntu 上验证后端和前端 |
| [ci-windows.yml](../.github/workflows/ci-windows.yml) | push to main | Windows 上验证桌面应用编译 |
| [release.yml](../.github/workflows/release.yml) | Release 发布 | 生成 Windows/Linux 发布包 |

## 故障排除

### 完全重置

如果遇到无法解决的构建问题，执行完全重置：

```powershell
# 清理所有构建产物
dotnet clean ZTR_OS.sln
Get-ChildItem -Recurse -Directory -Filter "bin" | Remove-Item -Recurse -Force
Get-ChildItem -Recurse -Directory -Filter "obj" | Remove-Item -Recurse -Force

# 清理前端
cd frontend
Remove-Item -Recurse -Force node_modules
Remove-Item -Recurse -Force dist
cd ..

# 重新构建
.\build-all.ps1
```
