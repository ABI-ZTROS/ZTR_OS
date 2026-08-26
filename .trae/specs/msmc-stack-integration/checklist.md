# ZTR_OS - MSMC 技术栈全面集成 - 验证清单

## NuGet 与编译
- [x] ZTR.Desktop.csproj 已引入 Serilog、Microsoft.Extensions.DependencyInjection、Microsoft.Extensions.Logging
- [x] ZTR.Desktop.csproj 已引入 CommunityToolkit.Mvvm
- [x] ZTR.Desktop.csproj 已引入 MaterialDesignThemes、MahApps.Metro.IconPacks.FontAwesome6、MahApps.Metro.IconPacks.Material
- [x] ZTR.Desktop.csproj 已引入 LiveChartsCore.SkiaSharpView.WPF
- [x] ZTR.Desktop.csproj 已引入 Nerdbank.GitVersioning、Microsoft.CodeAnalysis.NetAnalyzers
- [x] ZTR.Desktop.csproj 已引入 YamlDotNet、System.Management、System.ServiceProcess.ServiceController
- [x] ZTR.Desktop.csproj 已引入 Microsoft.Toolkit.Uwp.Notifications
- [x] TreatWarningsAsErrors=true 已配置
- [x] 单文件发布设置已配置（PublishSingleFile、SelfContained、EnableCompressionInSingleFile）
- [x] InvariantGlobalization=false 已配置
- [ ] dotnet build (Release) 通过，0 错误 0 警告

## 日志基础设施
- [x] ForceLog 死日志机制实现（不依赖 Serilog）
- [x] Serilog 精简配置（主日志 Warning+，5MB 滚动 × 5 份保留）
- [ ] Serilog 调试日志（Debug+，2MB 滚动 × 3 份保留）
- [x] 三层全局异常处理（AppDomain、TaskScheduler、Dispatcher）
- [x] 强制崩溃转储（WriteForceCrashDump）
- [x] 7 天旧日志自动清理
- [ ] 日志路径正确：{BaseDirectory}/logs/

## 安全文化与启动防护
- [x] LCID=1033 钉死线程文化实现
- [x] FrameworkElement.LanguageProperty.OverrideMetadata(Empty) 实现
- [x] DispatcherHooks.OperationStarted 兜底实现
- [x] ShutdownMode.OnExplicitShutdown 防静默退出
- [ ] 中文 Windows 系统上 MaterialDesign 控件正常显示

## DI 容器与服务注册
- [x] App.Services 全局访问点实现
- [x] BootStats 统计类实现（Ok/Fail 计数）
- [x] Register<TService,TImpl>、RegisterInstance<TService>、RegisterType<TImpl> 辅助方法实现
- [x] ILogger (Serilog) 单例注册
- [x] IUserAgreementService 注册
- [x] IThemeService 注册
- [x] IWindowEffectsService 注册
- [x] IWebView2BridgeService 注册
- [x] IProcessManagerService 注册
- [ ] ServiceProvider 构建成功，所有服务可解析

## Boot 启动流程
- [x] StartupWindow 实现（品牌、进度条、状态文字、日志流）
- [x] 60+ 步启动序列实现
- [x] 阶段 -1：用户协议前置校验
- [ ] 阶段 0：主题服务预加载
- [ ] 阶段 1：显示启动页
- [ ] 阶段 2：后台线程执行服务注册
- [ ] 阶段 3：构建 ServiceProvider
- [x] 阶段 4：切换到主窗口
- [ ] Step 辅助方法（100ms 延迟）实现
- [ ] 启动完成时间 < 3 秒（Release）

## 用户协议（核心复刻）
- [x] IUserAgreementService 接口定义
- [x] UserAgreementService 实现（版本 3.0.0、JSON 持久化）
- [x] 存储路径：{AppData}/io.NET.ZTR_OS/user_agreement.json
- [ ] 版本升级触发重新同意逻辑
- [x] UserAgreementWindow 实现（WPF）
- [x] 120 秒倒计时（失焦暂停）
- [x] 滚动限速（MaxScrollDelta=28px）
- [x] 滚动到底部检测
- [x] 同意按钮状态机（双条件启用）
- [ ] 恶意点击防护
- [x] 恶作剧：CollectShakeElements 实现
- [x] 恶作剧：三层抖动动画（窗口 ±50px、内容 ±10px、元素 ±4px）
- [x] 恶作剧：CreateTrollWindow 实现（单实例 320×160 警告弹窗）
- [x] 恶作剧：40 个 Troll 窗口随机分布
- [x] 恶作剧：5 秒动画后 Application.Current.Shutdown()
- [ ] 恶作剧弹窗中文内容正确
- [ ] 自定义标题栏拖动
- [ ] 协议状态持久化到 JSON 文件

## WebView2 双向桥接
- [x] IWebView2BridgeService 接口定义
- [x] WebView2BridgeService 实现（C# → JS + JS → C#）
- [x] 命令路由系统实现
- [ ] 前端 bridge 对象注入：window.__bridge__
- [ ] AddScriptToExecuteOnDocumentCreatedAsync 注入
- [ ] 前端资源加载正常

## 主题与配置
- [x] IThemeService 接口定义
- [x] ThemeService 实现（亮色/暗色切换）
- [x] IConfigurationService 接口定义
- [x] 配置持久化 JSON 读写

## 窗口特效
- [x] IWindowEffectsService 接口定义
- [x] WindowEffectsService 实现（Mica、圆角、深色标题栏）
- [x] DwmSetWindowAttribute 调用正确
- [ ] 圆角窗口显示
- [ ] 深色标题栏与主题一致

## 前端资源管理
- [x] BuildFrontend Target 实现（MSBuild 自动构建前端）
- [x] PackFrontendToZip Target 实现（wwwroot.zip 嵌入资源）
- [x] CopyFrontendToOutput Target 实现
- [ ] DiagnoseFrontendPaths Target 实现
- [ ] 发布单文件包含前端资源
- [ ] 运行时可从嵌入资源加载前端

## 假实现检测
- [x] 所有接口有对应实现类（一对一映射）
- [x] 无空方法体或 NotImplementedException
- [x] 所有服务在 DI 容器中注册
- [ ] 每个服务有真实业务逻辑（非空壳）
- [ ] 生成假实现检测报告

## 集成
- [x] App.xaml.cs 完整启动管线集成

## 运行时验证
- [ ] 首次启动弹出用户协议
- [ ] 同意后不再弹窗（持久化验证）
- [ ] 不同意触发 40 弹窗 + 抖动 + 退出
- [ ] 启动页进度条从 0% 到 100%
- [ ] 主窗口加载完成并显示前端 UI
- [ ] 所有原有功能（Performance、Aura、MLP、Binding）正常工作
- [ ] 全局异常处理覆盖所有线程
