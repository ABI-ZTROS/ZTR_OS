# ZTR_OS - MSMC 技术栈全面集成 PRD

## Overview
- **Summary**: 将 MSMC（Minecraft Server Management Client）的完整技术栈、NuGet 包体系、WPF 窗口架构、WebView2 桥接方案、用户协议组件（含恶作剧效果）移植到 ZTR_OS（性能调优平台），使 ZTR_OS 获得企业级桌面应用基础设施。
- **Purpose**: 当前 ZTR_OS 使用简陋的 WPF + WebView2 静态文件映射方案，缺少日志、DI、启动流程、窗口特效、WebView2 双向通信等核心能力。通过移植 MSMC 成熟的基础设施，将 ZTR_OS 从"原型"升级为"生产级桌面应用"。
- **Target Users**: ASUS 笔记本用户、硬件发烧友、性能调优工程师

## Goals
- 引入 MSMC 全套 NuGet 包（Serilog、DI、MaterialDesign、CommunityToolkit.Mvvm、LiveChartsCore 等）
- 构建 MSMC 风格的 Boot 启动流程（启动页 + 60+ 步服务注册进度）
- 实现 WebView2 ↔ C# 双向桥接（替代当前静态文件映射方案）
- 复刻 MSMC 的用户协议窗口（含 120s 倒计时、滚动限速、40 实例恶作剧弹窗、三层抖动动画）
- 引入窗口自定义能力（Mica 背景、圆角、深色标题栏）
- 所有功能必须是真实现，禁止空壳/假实现

## Non-Goals (Out of Scope)
- 不引入 MSMC 的 Minecraft 服务器管理功能（服务器检测、插件市场、调度等）
- 不引入 MSMC 的原生 Toast 通知系统
- 不引入 MSMC 的自动更新功能
- 不引入 MSMC 的 Discord/Webhook 通知
- 不修改 ZTR_OS 的业务逻辑（HAL、API 控制器、前端页面）
- 不修改已有前端组件的业务逻辑

## Background & Context
- ZTR_OS 是 ASUS 笔记本性能调优平台，架构为 WPF + WebView2 + 内嵌 Kestrel API
- MSMC 是同开发者的 Minecraft 服务器管理工具，已具备成熟的 WPF 基础设施
- 两者共享命名空间 `io.NET.ZTR_OS`，技术栈高度兼容
- MSMC 的 NuGet 依赖（15+ 包）、DI 注册（30+ 服务）、Boot 流程（60+ 步）经过生产环境验证
- MSMC 的用户协议组件包含业界罕见的"拒绝即恶作剧"交互设计，是用户明确要求复刻的

## Functional Requirements

### FR-1: NuGet 包集成
- 引入 Serilog（日志）、Microsoft.Extensions.DependencyInjection（DI）、Microsoft.Extensions.Logging
- 引入 CommunityToolkit.Mvvm（MVVM 基础设施）
- 引入 MaterialDesignThemes + MahApps.Metro.IconPacks（UI 主题与图标）
- 引入 LiveChartsCore.SkiaSharpView.WPF（高性能图表）
- 引入 Microsoft.Web.WebView2（升级到最新版本）
- 引入 Nerdbank.GitVersioning（版本管理）
- 引入 Microsoft.CodeAnalysis.NetAnalyzers（严格代码分析）
- 引入 System.Management、System.ServiceProcess.ServiceController、YamlDotNet
- 配置 TreatWarningsAsErrors=true，严格编译策略

### FR-2: Boot 启动流程
- 实现 Boot 启动页（StartupWindow），显示品牌、进度条、实时日志流
- 实现 60+ 步的服务注册序列，每步有进度百分比、状态文字、成功/失败日志
- 实现 ForceLog 死日志机制（不依赖任何库，进程崩溃也能记录）
- 实现三层全局异常处理（AppDomain、TaskScheduler、Dispatcher）
- 实现安全文化钉死机制（解决 MaterialDesign 的 en-us 文化崩溃）
- 实现防静默退出机制（ShutdownMode.OnExplicitShutdown）

### FR-3: DI 容器与服务注册
- 构建完整的 DI 容器（ServiceCollection + ServiceProvider）
- 注册 ILogger (Serilog) 单例
- 注册 IUserAgreementService（用户协议状态持久化）
- 注册 IThemeService（主题切换）
- 注册 IWindowEffectsService（窗口特效：Mica、圆角、深色标题栏）
- 注册 IWebView2BridgeService（WebView2 JS ↔ C# 双向通信桥）
- 注册 IProcessManagerService（进程亲和性管理，从 MSMC 移植）
- 注册 IConfigurationService（配置持久化）
- 实现 BootStats 统计（成功/失败计数）

### FR-4: 用户协议完整复刻
- **UserAgreementService**: JSON 文件持久化（AppData 路径），支持版本号升级重新同意
- **UserAgreementWindow**: 
  - 120 秒阅读倒计时，失焦自动暂停
  - 滚动限速（MaxScrollDelta=28px/次），强制慢阅读
  - 滚动到底部检测，未到底不能同意
  - 同意按钮在倒计时结束 + 滚动到底部后才启用
  - **拒绝恶作剧效果**:
    - 主窗口三层抖动（窗口位置 ±50px、内容整体 ±10px、每个元素独立 ±4px）
    - 40 个随机分布的置顶警告弹窗，每个独立抖动
    - 5 秒动画后强制退出应用
  - 自定义标题栏拖动
  - 多实例通知窗口系统

### FR-5: WebView2 双向桥接
- 实现 IWebView2BridgeService，支持 C# → JS 的事件推送
- 实现 JS → C# 的命令调用（前端可调用 .NET 方法）
- 替代当前的静态文件映射方案
- 支持 SignalR Hub 直接与 WebView2 通信
- 实现脚本注入在文档创建时执行

### FR-6: 自定义窗口
- 实现 WindowEffectsService：Mica/Acrylic 背景、圆角、深色标题栏
- 实现 MaterialDesign 主题切换（亮色/暗色）
- 实现自定义 Chrome（无边框窗口 + 自绘标题栏）
- 实现窗口动画（淡入、缩放）

### FR-7: 前端资源管理
- 实现 wwwroot.zip 打包流程（前端 dist → zip → 嵌入资源）
- 实现运行时从 zip 解压或直接映射的双模式加载
- 实现 BuildFrontend Target（MSBuild 自动构建前端）
- 实现 PackFrontendToZip Target（资源打包）
- 实现 CopyFrontendToOutput Target（调试模式复制）

### FR-8: 假实现检测
- 每个服务必须有真实接口定义 + 实现类
- 每个 API 端点必须有 Controller 方法 + 实际业务逻辑
- 启动序列中每个注册步骤必须调用 AddSingleton 并验证成功
- 拒绝空壳实现（只有接口无实现、只有日志无动作、只有 UI 无后端）

## Non-Functional Requirements

### NFR-1: 严格编译
- TreatWarningsAsErrors=true，0 警告容忍
- 启用代码分析器（Microsoft.CodeAnalysis.NetAnalyzers）
- 单文件发布模式，自包含部署

### NFR-2: 性能
- 启动流程（从双击到 UI 可交互）< 3 秒（Release 模式）
- Boot 序列中单步注册耗时 < 100ms
- WebView2 加载前端 < 2 秒（本地资源）

### NFR-3: 可靠性
- 全局异常捕获覆盖所有线程（UI 线程、线程池、Task、AppDomain）
- ForceLog 死日志在任何崩溃场景下都能写入
- 用户协议状态持久化不丢失

### NFR-4: 可维护性
- 所有服务通过 DI 容器注册，便于替换和测试
- 接口与实现分离，命名遵循 MSMC 规范（I前缀接口 + 实现类）
- 启动流程每个步骤有明确日志标签（[TIME]、[METRIC]、[WINFX] 等）

### NFR-5: 安全性
- 管理员权限检测（AdminPrivilegeService 移植）
- 配置文件路径使用 AppData，不写入 Program Files
- 用户协议版本号机制确保新条款生效

## Constraints
- **技术**: 必须基于 .NET 9.0 net9.0-windows，WPF，与现有 ZTR_OS 架构兼容
- **技术**: 前端仍使用 React 18 + TypeScript 5 + Vite 5，不做修改
- **技术**: Kestrel 内嵌 API 服务器必须保留（已有业务逻辑依赖）
- **业务**: 所有移植的代码必须保持 ZTR_OS 命名空间（与 MSMC 不同）
- **业务**: 恶作剧效果必须完整复刻（40 弹窗 + 三层抖动 + 强制退出）
- **业务**: 用户协议必须在启动早期（任何 UI 之前）弹出

## Assumptions
- 用户在 Windows 10 1809+ 或 Windows 11 上运行
- WebView2 Runtime 已安装（或系统自动分发）
- 管理员权限运行（性能调优需要硬件访问权限）
- MSMC 的 NuGet 依赖版本兼容 .NET 9.0

## Acceptance Criteria

### AC-1: NuGet 包完整性
- **Given**: ZTR_OS Desktop 项目已引入所有必需 NuGet 包
- **When**: 执行 dotnet build
- **Then**: 所有 NuGet 包正确还原，编译成功，无警告
- **Verification**: `programmatic`

### AC-2: Boot 启动流程
- **Given**: 用户双击 exe 启动
- **When**: 启动流程执行
- **Then**: 显示启动页 → 进度条从 0% 到 100% → 显示每步注册日志 → 加载完成后切换到主窗口
- **Verification**: `programmatic` (日志输出验证) + `human-judgment` (视觉体验)

### AC-3: 用户协议恶作剧效果
- **Given**: 用户首次启动（或协议版本更新后）
- **When**: 用户点击"不同意"按钮
- **Then**: 主窗口三层抖动 + 40 个警告弹窗随机分布 + 5 秒后强制退出
- **Verification**: `programmatic` (检查窗口数量和动画逻辑) + `human-judgment` (实际运行验证)

### AC-4: 用户协议同意流程
- **Given**: 用户首次启动
- **When**: 用户等待 120 秒倒计时 + 滚动到底部 + 点击同意
- **Then**: 协议状态持久化到文件，下次启动不再弹窗
- **Verification**: `programmatic` (检查 user_agreement.json 文件内容)

### AC-5: DI 容器
- **Given**: Boot 流程完成
- **When**: 主窗口需要解析服务
- **Then**: 所有服务可通过 IServiceProvider 成功解析，无 null 引用
- **Verification**: `programmatic`

### AC-6: WebView2 桥接
- **Given**: 前端页面已加载
- **When**: 前端调用 window.__bridge__.invoke('method', args)
- **Then**: C# 端对应方法被调用，返回值传回前端
- **Verification**: `programmatic`

### AC-7: 假实现检测
- **Given**: 任意已注册服务
- **When**: 检查服务实现
- **Then**: 每个服务有接口定义、实现类、DI 注册、实际业务方法（非空方法体）
- **Verification**: `programmatic` (代码结构扫描) + `human-judgment` (逐文件审查)

### AC-8: 全局异常处理
- **Given**: 应用运行中发生未处理异常
- **When**: 异常分别在 UI 线程、线程池、Task 中发生
- **Then**: 异常被捕获，写入日志，应用不崩溃（或优雅退出）
- **Verification**: `programmatic` (异常注入测试)

### AC-9: 日志系统
- **Given**: 应用运行中
- **When**: 执行任意操作
- **Then**: Serilog 日志写入 logs/ 目录，ForceLog 死日志在极端情况下仍可写入
- **Verification**: `programmatic`

### AC-10: 编译通过
- **Given**: 完整源码
- **When**: 执行 dotnet build（Release 配置）
- **Then**: 编译成功，0 错误 0 警告
- **Verification**: `programmatic`

## Open Questions
- [ ] 是否需要保留当前的 MainWindow（WebView2 容器）结构，还是完全替换为 MSMC 风格的多窗口架构？
- [ ] LiveChartsCore 引入后是否替换现有前端 Canvas 仪表？
- [ ] 是否需要引入 MaterialDesign 主题到 WebView2 前端（通过 JS 桥）？
- [ ] 40 个恶作剧弹窗是否在 Release 版也保留？（MSMC 是全版本保留）
