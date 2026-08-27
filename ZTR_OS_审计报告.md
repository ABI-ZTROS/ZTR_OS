# ZTR_OS 三链原则审计报告（只读 · 不修改任何源码）

> 审计方法：忽略 README / 文档 / 代码注释，只审逻辑。按《通用 Agent 权威操作规程》的**三链原则**（执行链 / 因果链 / 返回链）逐条追溯。
> 执行方式：4 路并行子代理按模块深挖 + 主代理对顶层高危项**逐坐标亲自核验**（见文末「核验记录」）。
> 红线遵守：全程只读，未改任何文件，未编造；每条结论带 `文件:行号`，可跳转查证。

---

## 0. 总纲（结论先行）

**项目本质**：ZTR_OS 不是操作系统，是一个 **.NET 9 (ASP.NET Core) + WPF/WebView2 + React** 的华硕 ROG 设备管控 + "AI 优化"产品（10 个工程、315 文件、155 个 .cs）。后端 `ZTR.Api` + `ZTR.HAL`（硬件抽象）+ `ZTR.Intelligence`（MLP 调度）+ 桌面 `ZTR.Desktop` + 前端 `frontend`。

**总体评级：🔴 高危 —— 不可直接交付。** 硬件采集中枢勉强能跑，但**实时数据链路整体断裂、桌面端双窗口 + 桥接零接通、AI 层纯空转假成功**三处是交付阻塞级硬伤；返回链普遍存在"假成功 / 状态码失真 / 语义歧义"。

**最致命的三条（任一都足以让产品上线即翻车）**：
1. **SignalR 实时推送是死链**：桥接器 `SensorSignalRBridge` 注册了但生产环境从未被解析 → 订阅从不挂载 → 传感器数据永不推前端。
2. **桌面端启动即弹两个 MainWindow**：`StartupUri` 与手动 `new MainWindow()` 双源建窗，且 `ApiServerHost` 起两套。
3. **AI 训练是假成功**：`MlpController.StartTraining` 只把 `_isTraining=true` 就回 200，从不调用任何 `Train`；`ResetModel` 把权重清零维直接废掉网络；状态字段是 Controller 实例变量，每次请求重置 → 永远报 `idle`。

---

## 1. 已主代理亲自核验的坐实项（坐标可跳转，最高可信度）

| # | 代码点 | 文件:行号 | 链 | 问题 | 修复建议 |
|---|--------|-----------|----|------|----------|
| V1 | `UseHttpsRedirection` 但只绑 http | `src/ZTR.Api/Program.cs:62` + `:118` | 执行/返回 | `ConfigureUrls` 仅 `http://localhost:port`，随后 307 重定向到不存在的 https 端点 → API 直接不可达（除非前置反代终止 TLS）。 | 删 `app.UseHttpsRedirection();`，或同时配 `https://`（需开发证书）。 |
| V2 | `SensorSignalRBridge` 注册却永不解析 | `src/ZTR.Api/Extensions/ServiceCollectionExtensions.cs:67` + `src/ZTR.HAL/SensorBackgroundService.cs:35-43` | 执行 | 桥接器在 `ServiceCollectionExtensions:67` 注册为单例，但 `SensorBackgroundService` 构造只注入 `SensorPipeline`+`SensorQueue`，**不注入桥接器**；单例懒加载，无人 `GetService` → 构造里的 `_queue.StateEnqueued += OnStateEnqueued` 永不执行 → 实时数据零推送。 | 在 `SensorBackgroundService` 注入 `SensorSignalRBridge`，启动后 `bridge` 被解析即触发订阅；或 `app.Services.GetRequiredService<SensorSignalRBridge>()` 强制解析。 |
| V3 | `StartupUri` 导致双 MainWindow | `src/ZTR.Desktop/App.xaml:4` + `src/ZTR.Desktop/App.xaml.cs:87-104` | 执行 | `App.xaml` 设 `StartupUri="MainWindow.xaml"`（WPF 自动 Load+Show 一个），`App.xaml.cs:100` 又 `new MainWindow().Show()` 并 `MainWindow=mainWindow`。启动即出现**第二个 MainWindow**（自带独立 `ApiServerHost`+WebView2）。 | 删 `App.xaml` 的 `StartupUri`，保留 `OnStartup` 手动建窗单一来源。 |
| V4 | WebView2 桥接从未挂载 | `src/ZTR.Desktop/Features/WebView2/Services/WebView2BridgeService.cs:14` + 全仓 grep | 执行/因果 | `IWebView2BridgeService` 仅在 `BootSequence.cs:55` 注册，全仓**无 `.Attach(` 调用、无 `GetService<>` 解析**；`Attach` 是唯一接线 `WebMessageReceived` 的入口 → JS↔C# 双向通道 0% 接通。 | 在 `MainWindow` 加载后 `EnsureCoreWebView2Async()` 完成处 `GetService<IWebView2BridgeService>().Attach(webView.CoreWebView2)` 并 `RegisterHandler` 各业务；前端补 `postMessage`/`__onbridgeevent`。 |
| V5 | MLP 训练是空跑假成功 | `src/ZTR.Api/Controllers/MlpController.cs:14,71,100-103` | 返回/执行 | `:14` 注入 `_scheduler` 后**全程零调用**；`:71` `StartTraining` 仅 `_isTraining=true` 即 `Ok(ApiResponse(true))`，从不调用 `OnlineLearner.Train`；`:100` `ResetModel` 把权重置 `new double[0,0]` → 后续 `Predict` 必 `IndexOutOfRange`；`:16` `_isTraining` 是实例字段，Controller 默认 scoped，每请求重置 → `GetStatus` 恒 `idle`。 | 真正接入训练循环；或删除端点返回 501。重置改为 `new MlpNetwork(_config)` 按正确维度重建。状态移到单例。 |
| V6 | 前端订阅幽灵事件 | `frontend/src/services/signalR.ts:101,105,109` + 后端 grep | 执行/因果 | 前端 `connection.on('MlpStateUpdate'/'MlpDecision'/'SystemEvent', ...)`，但后端只 `SendAsync("SensorUpdate")`(SensorHub.cs:17) 与 `("StateChange")`(StateHub.cs:16)，**从不发这三者** → 回调永不触发，MLP 实时决策/状态时间线恒空。 | 后端补发对应事件，或前端改订阅 `StateChange` 并映射；两端事件名对齐。 |
| V7 | 电池状态语义打架 | `src/ZTR.Api/Mappers/HardwareMapper.cs:37` vs `src/ZTR.Api/Hubs/SensorSignalRBridge.cs:97` | 因果 | REST 路径 `IsCharging?"Charging":"AC"`；SignalR 路径 `IsCharging?"AC":"DC"`。同一字段两链路含义相反。 | 抽共享 `MapBatteryStatus(bool)` 供两处共用，消除分叉。 |
| V8 | 协议实例未进 DI | `src/ZTR.Desktop/App.xaml.cs:71,96` + `src/ZTR.Desktop/BootSequence.cs:27,51` | 因果/返回 | `App.xaml.cs:71` `new UserAgreementService()` 已同意，传给 `boot.RunAsync`；但 `RunAsync` 形参 `agreementService` 在方法体（`:27-47`）**从未使用**，且 `:51` 又注册一个全新（未同意）`UserAgreementService`。日后从 DI 取 `IUserAgreementService` 得到"未同意"实例，绕过协议门禁。 | 用已同意实例 `services.AddSingleton<IUserAgreementService>(agreementService)`，或在 `RunAsync` 内真正使用传入实例。 |
| V9 | 4 个桌面服务注册后零调用（死代码） | `src/ZTR.Desktop/BootSequence.cs:52-56` + 全仓 grep | 执行 | `IThemeService`/`IConfigurationService`/`IWindowEffectsService`/`IProcessManagerService` 注册，但全仓无外部 `GetService<>`，`ApplyTheme/ApplyMica/SetAffinity` 永不执行 → 主题/窗口特效/进程管控全部失效。 | 在 `MainWindow.Loaded` 解析并调用，或删除注册与实现。 |
| V10 | 核数列表截断与 `CoreCount` 不一 | `src/ZTR.Api/Mappers/HardwareMapper.cs:51` vs `:22` | 因果 | `Cores` 列表 `Math.Min(coreCount,16)`，但 `CoreCount` 取真实核数可 >16 → 前端按 `CoreCount` 渲染越界/错位。 | `Cores` 与 `CoreCount` 用同一上限，或前端按列表长度渲染。 |

---

## 2. ZTR.Api 模块（🔴 高危）

| 代码点 | 文件:行号 | 链 | 问题 | 修复建议 |
|--------|-----------|----|------|----------|
| CORS 全开 | `Program.cs:32-40` | 执行(安全) | `AllowAnyOrigin()`+`AllowAnyMethod()`+`AllowAnyHeader()`，本地硬件控制 API 可被任意站点 CSRF。 | 限定前端来源白名单；写操作配凭据策略。 |
| 未知 `/api/*` 被 SPA fallback 吞成 200 HTML | `Program.cs:85` | 返回 | `MapFallbackToFile("index.html")` 在 `MapControllers` 之后，拼错路径（如 `/api/hardwar`）返回 `index.html`+200，前端拿 HTML 当 JSON。 | 用 `MapFallbackToController` 或把 fallback 排除 `/api` 路径。 |
| 训练状态因生命周期丢失 | `MlpController.cs:16,42,138` | 返回/因果 | 见 V5。 | 状态移到单例。 |
| 电池状态语义相反 | `HardwareMapper.cs:37` vs `SensorSignalRBridge.cs:97` | 因果 | 见 V7。 | 共享 helper。 |
| `IntegrationMiddleware` 错杀合法 speed | `IntegrationMiddleware.cs:129-139` | 返回 | 含 "speed" 非风扇字段一律套 `fanspeed`(0–100) 区间，`clockSpeed`>100 被拒 400。 | 仅对 `fanSpeed`/`fanRpm` 精确匹配做区间校验。 |
| `AutomationController` 用 `Enum.Parse` 解析用户输入 | `AutomationController.cs:68,101` | 返回 | 非法枚举串抛 `ArgumentException` → 被中间件转 500 且回吐错误。 | 改 `Enum.TryParse`，失败 `BadRequest`。 |
| `BindingController.RemoveBinding` 忽略结果恒成功 | `BindingController.cs:77-81` | 返回(假成功) | `SetAffinity(...,-1)` 结果丢弃，直接 `Ok(true)`，删不存在/失败也报成功。 | 返回真实结果，不存在时 `NotFound`。 |
| `BindingController.SetBinding` 未判空 | `BindingController.cs:53` | 执行/返回 | `request.Affinity` 为 `List<int>`，缺省 null → `foreach` 抛 NRE→500。 | 入口 `if (request.Affinity==null) return BadRequest(...)`。 |
| `PerformanceController.UpdateSettings` 忽略写入结果 | `PerformanceController.cs:104-126` | 返回(假成功) | `SetMode/SetCpuFanCurve/SetGpuFanCurve` 返回值全忽略，恒 `Ok(true)`。 | 收集各 bool，任一失败 `BadRequest`。 |
| `SettingsController.UpdateSettings` 同病+缺空值守卫 | `SettingsController.cs:104-126` | 返回(假成功) | 同上；`config` 未判空。 | 校验非空、返回真实状态。 |
| `DiagnosticsController.ModeControlActive` 硬编码 true | `DiagnosticsController.cs:54` | 返回(假数据) | 诊断报告永远显示 ModeControl 活跃。 | 改用 `_modeControl` 真实状态属性。 |
| `StateHub` 无服务端推送方 | `StateHub.cs` 全文件 | 执行 | 仅客户端可调用 `SendStateChange`，无组件注入 `IHubContext<StateHub>` 推送，服务端状态变更不实时下发。 | 在需广播处注入 `IHubContext<StateHub>` 推送，否则删 Hub。 |
| `HardwareDataHub.Join/LeaveGroup` 死功能 | `HardwareDataHub.cs:8-16` | 执行 | 桥接器一律 `Clients.All`，Join/Leave 无效。 | 按 group 推送或移除暴露。 |
| `AuraController` 预设存静态内存无上限 | `AuraController.cs:13-17,245` | 返回(持久性) | 进程内列表，重启即丢，多实例不一致，无上限。 | 持久化 + 容量上限。 |
| 写操作普遍 200+`success:false` | `GpuController.cs:43` 等 | 返回(状态码失真) | 业务失败仍 200，调用方只看状态码误判成功。 | 失败统一 4xx。 |
| `GpuSensorService.Initialize()` 在 DI 工厂内做硬件 I/O | `ServiceCollectionExtensions.cs:32-38` | 执行 | 首次解析才 `Initialize()`，抛异常则首个打到依赖链的请求 500，且每次请求重复抛。 | 移入 `IHostedService.StartAsync` 或容错包装。 |
| `SensorPipeline` 工厂传 null 队列 | `ServiceCollectionExtensions.cs:47-55` | 执行/因果 | `new SensorPipeline(..., null,null,null, ...)` → 内部自建 `SensorQueue(1000)`，与 DI 单例 `SensorQueue` 是两套实例，内部队列填充后无人消费；其 `Timer` 以 `Timeout.Infinite` 创建且 `Start()` 从未调用 → 定时器永不开火。 | 传入注入队列并 `Start()`，或删除冗余 Timer/内部队列统一走 `SensorBackgroundService`。 |

---

## 3. ZTR.Desktop 模块（🔴 差）

| 代码点 | 文件:行号 | 链 | 问题 | 修复建议 |
|--------|-----------|----|------|----------|
| WebView2 桥接从未挂载 | `WebView2BridgeService.cs:14` | 执行/因果 | 见 V4。 | 见 V4。 |
| `StartupUri` 双主窗口 | `App.xaml:4` + `App.xaml.cs:100` | 执行 | 见 V3。 | 删 `StartupUri`。 |
| 协议实例未进 DI | `App.xaml.cs:71` + `BootSequence.cs:27,51` | 因果/返回 | 见 V8。 | 见 V8。 |
| `GlobalExceptionHandler.Setup` 未调用 | `GlobalExceptionHandler.cs:18` | 执行 | `Setup()` 全仓无调用 → 崩溃转储 `ForceCrashDump` 永不写；`App` 静态构造只注册无转储 handler。 | `OnStartup` 开头调 `GlobalExceptionHandler.Setup()`。 |
| `BootGuard` 整类未调用 | `BootGuard.cs:15,32` | 执行 | `VerifyEnvironment`/`ConfigureEarlyStartup` 死；WebView2 运行时缺失无友好检测。 | `OnStartup` 早期调用 `VerifyEnvironment()`。 |
| `ServiceRegistration` 整文件死代码 | `ServiceRegistration.cs:7-51` | 执行 | 三方法全无调用。 | 删或统一改用它。 |
| `ThemeService`/`WindowEffectsService`/`ProcessManagerService`/`ConfigurationService` 死代码 | `BootSequence.cs:52-56` | 执行 | 见 V9。 | 接线或删除。 |
| `ConfigurationService.Get<T>` 恒 null | `ConfigurationService.cs:14-19,36` | 返回 | `Load()` 用 `JsonSerializer.Deserialize<object>` 存 `JsonElement`，`Get<T>` 的 `value is T` 对对象/数组永 false → 必返 null，调用方误判未配置。 | `Get<T>` 内用 `GetValue<T>()`。 |
| `MainWindow` 资源未释放 | `MainWindow.xaml.cs:9,168` | 执行(资源) | `ApiServerHost`/`CoreWebView2` 关闭即泄漏，`Window_Closing` 只 `StopAsync().Wait` 未 `Dispose`。 | `Window_Closing` 中 `Dispose`；`MainWindow` 实现 `IDisposable`。 |
| 关闭时同步阻塞 `Wait` | `MainWindow.xaml.cs:168` | 执行(异步纪律) | UI 线程 `.Wait(5s)` 同步等异步，潜在死锁。 | 改 `async` + `await StopAsync()`。 |
| `ApplyMica` 实为圆角非 Mica | `WindowEffectsService.cs:21` | 返回(语义歧义) | 用 `DWMWA_WINDOW_CORNER_PREFERENCE=33` 且 `mica=3`，未应用真 Mica（26/1029）。 | 改名或补真 Mica。 |
| CORS 含非法 `file://` origin | `ApiServerHost.cs:91` | 因果(契约) | `.WithOrigins("file://")` 非合法 CORS origin，与 `AllowCredentials()` 组合无效。 | 移除。 |
| `TaskScheduler.UnobservedTaskException` 全局吞 | `App.xaml.cs:41-45` | 返回(假成功) | `e.SetObserved()` 全局吞未观察异常，掩盖 fire-and-forget 真实 bug。 | 至少记录，评估是否应上浮。 |

---

## 4. ZTR.HAL / ZTR.Intelligence 模块（🔴/🟠）

| 代码点 | 文件:行号 | 链 | 问题 | 修复建议 |
|--------|-----------|----|------|----------|
| 训练端点空跑 | `MlpController.cs:65-81`（见 V5） | 返回 | 从不调 `OnlineLearner.Train`。 | 真正接入。 |
| `ResetModel` 废网 | `MlpController.cs:100-103` | 返回 | `new double[0,0]` → `Predict` 必越界。 | 重建 `new MlpNetwork(_config)`。 |
| GPU 控制对象"释放后使用 + 双重所有权" | `GpuSensorService.cs:184-187`(NVIDIA)/`:201-204`(AMD) | 执行 | `using var probe = new NvidiaGpuControl(0); if(probe.IsValid) _gpuControls.Add(probe);` —— `using` 方法结束即 Dispose，同一实例却被加入 `_gpuControls`；`IGpuControl` 工厂（`ServiceCollectionExtensions.cs:40-45` `FirstOrDefault`）会把已释放对象当全局 `IGpuControl` 返回。当前因 `Dispose()` 是空操作未崩，但属典型 UAF。 | 去掉 `using`，由 `GpuSensorService.Dispose()` 统一持有释放。 |
| 原生资源未释放 | `NvidiaGpuControl.cs:288-294` / `AmdGpuControl.cs:322-328` | 执行 | `Dispose()` 只置 `_disposed=true`，未释放底层 `_nvApi`/`_adl2` 句柄 → 进程期泄漏。 | `Dispose()` 内 `_nvApi.Dispose()`/`_adl2.Dispose()`。 |
| 遥测伪造 GPU 功耗 | `SystemSensorFallback.cs:523-529` | 返回/因果 | `nvidia-smi` 失败时若 `temp>75 && usage>50` 返回 `new Random().Next(15,31)` 编造值流入遥测与决策。 | 与"不可用时返回 0/null"一致，不凭空生成。 |
| 异步初始化竞态 | `SystemSensorFallback.cs:44-48` | 执行 | 构造器 `_ = InitializeAsync()` 即焚即弃，`IsAvailable` 默认 false，`SensorPipeline` 构造据此判 fallback 不可用，~200ms 内采集跳过回退。 | 构造器 `await InitializeAsync()`，或读取先等 `_initialized`。 |
| `SensorPipeline` 死 Timer + 双队列 | `SensorPipeline.cs:88,94` | 执行 | 见 §2 末条。 | 见 §2。 |
| 智能层整体空转 | `MlpController.cs:14` + `PredictiveScheduler` 无调用方 | 执行 | `_scheduler` 注入但从不调 `OnSensorUpdate/Update`，无 Hub/BackgroundService 订阅 → AI 调度运行时不产生任何实时决策。 | 挂到 `SensorQueue.StateEnqueued` 或后台循环；否则移除该层与注册。 |
| 配置与网络维度不一致 | `MlpController.cs:51-63` | 因果/返回 | `UpdateConfig` 改写单例 `_config` 维度，但 `MlpNetwork` 已按原维度构建，后续 `Predict`/`Train` 维度不匹配抛异常。 | 配置变更时 `_network = new MlpNetwork(_config)` 并重置 `OnlineLearner`。 |
| 死代码 `ManualOverride` | `ZTR.Intelligence/ManualOverride.cs:8` | 执行 | 全仓无注册/引用。 | 删或接入优先级 Manual>MLP。 |
| 空项目被引用 | `ZTR.Common/ZTR.Common.csproj` | 执行 | 被 4 项目引用但零 .cs 文件，编译为空程序集。 | 删引用或迁移真实工具。 |
| 死单例 `DeviceProbe` | `DeviceProbe.cs` + `ServiceCollectionExtensions.cs:56` | 执行 | 注册单例但无消费者（仅测试 `new`）。 | 接入或移除注册。 |
| 置信度语义失真 | `PredictiveScheduler.cs:141-151` | 返回 | `ComputeConfidence` 低方差=高置信；未训练网络输出恒值被判 ~1.0 置信，误导前端。 | 置信度结合"是否训练过/样本量"。 |
| 权重组并发读写无锁 | `MlpNetwork.cs:199-204` + `OnlineLearner.cs:119-151` | 执行 | `W1..B3` 直接暴露可变数组，若 `Predict` 与 `Train` 并发读到半更新权重。 | 加读写锁或单线程约束。 |
| `async void` 事件处理 | `SensorSignalRBridge.cs:30` | 执行/返回 | `OnStateEnqueued` 为 `async void`，高频采集可能积压，异常被吞。 | 改 `async Task` + 限流/批量推送。 |
| `ZTR.Service` Worker | — | — | 仅做嵌套 `WebApplication` 接线，`ExecuteAsync` 真正启动 Kestrel+采集+SignalR，无空跑；`StopAsync` 释放到位。 | 🟢 合格，无需改。 |
| `ZTR.Models` | — | — | 契约维度（InputSize=16/OutputSize=8）与 `SensorFeatureExtractor`/`PerformanceDecisionEngine` 一致。 | 🟢 良好。 |

---

## 5. frontend 模块（🔴 高风险）

| 代码点 | 文件:行号 | 链 | 问题 | 修复建议 |
|--------|-----------|----|------|----------|
| 订阅幽灵事件 | `services/signalR.ts:101,105,109` | 执行/因果 | 见 V6。 | 后端补发或前端改订阅真实事件名。 |
| MLP 决策/状态恒空 | `pages/MlpPage.tsx:62-79` + `store/useMlpStore.ts:42-50` + `services/mlpApi.ts:37` | 返回/执行 | 依赖死事件 `mlp:decision`；`getDecisions` 从未被任何页调用。 | MlpPage 加载时 `mlpApi.getDecisions()`+`getStatus()` 填充 store。 |
| 自动化规则模式字段被静默丢弃 | `services/otherApi.ts:4-14` + `pages/Automation.tsx:171-180` | 因果 | 前端发字符串 "turbo"/"eco"，后端 `PerformanceMode`/`GpuMode` 是 `int?` → 绑定失败置 null，性能/GPU 切换全失效。 | 前端发枚举 int 值，或后端 `Enum.Parse` 接字符串；GET 返回口径对齐。 |
| Aura `setEffect` 参数被忽略 | `services/otherApi.ts:19-20` vs `AuraController.cs:294-299` | 因果 | 前端平铺 `brightness/speed/intensity`，后端只读 `request.Params?.Brightness/...`，`Params` 为 null → 全用默认(80/50/70)。 | 改发 `{ effect, color, params:{brightness,speed,intensity} }`。 |
| `getConfig()` 返回结构错误 | `services/performanceApi.ts:12-22` vs `pages/Performance.tsx:45-47` | 因果/返回 | `modeRes.data` 本身是 `{mode:string}`，导致 `config.mode` 是对象非字符串，`setMode` 把对象当模式，无高亮。 | `data:{ mode: modeRes.data?.mode, curves: curvesRes.data }`。 |
| Screen MiniLED 大小写不匹配 | `pages/Screen.tsx:56-57,237-247` vs `ScreenController.cs:55` | 因果/返回 | 后端 `"Standard"/"Advanced"/"Off"`，前端 id 小写 → 加载不高亮（设置时因 ignoreCase 成功但显示错位）。 | 前端 `String(mode).toLowerCase()` 归一。 |
| Updates 期望 `lastChecked` 后端不返回 | `pages/Updates.tsx:48-55` vs `UpdatesController.cs:23-36` | 返回/因果 | 后端只返 `{model,updates}`，`lastChecked` 为 undefined 回落 `new Date()`，显示"最后检查时间"实为当前时间。 | 后端加 `lastChecked`，明确 `updates[]` 字段对齐。 |
| `diagnostics` 服务死代码且契约错 | `services/diagnostics.ts:4-21` vs `DiagnosticsController.cs:136-158` | 执行/因果 | 全仓未 import；字段拼写 `aspiAvailable`(后端 `acpiAvailable`)、`battery.chargePercent`(后端 `Percentage`)、`fan.cpuSpeed`(后端 `Fans` 是列表) → 一旦接线即崩。 | 删或按后端真实字段重写类型。 |
| store `unsubscribe` 不移除 SignalR 监听 | `store/useHardwareStore.ts:115-121` + `useMlpStore.ts:53-55` | 执行 | `subscribe()` 注册的回调只重置标志位，从不调 `on()` 返回的 off → StrictMode 双挂载累积重复回调。 | 保存并调用 `signalRService.on(...)` 返回的 off 句柄。 |
| `performanceApi.setConfig` 死代码 + 形状错 | `services/performanceApi.ts:23-24` | 执行/因果 | 无调用；`PUT /api/settings` 发任意 config，后端要 `{Mode,CpuFanCurve,GpuFanCurve}` 形状不符。 | 删或改 `PUT /api/settings/user` 走 `{settings}` 信封。 |
| `bindingApi.getTopology` 死代码 | `services/otherApi.ts:10` | 执行 | `Binding.tsx` 从未调用；后端返 `{Cpu,Gpu}` 对象前端声明成 `Array`，接上也错位。 | 删或修正类型并真正使用。 |
| `useSignalR` 吞连接错误 | `hooks/useSignalR.ts:11` | 返回 | `connect().catch(()=>{})` 空吞，无错误态/重试提示。 | 至少记录并写 `connectionStatus='error'`。 |
| API 失败 `data:null` 静默陈旧 | `services/api.ts:27,41-43` + `useHardwareStore.fetchOnce` | 返回 | 非 2xx/解析失败返 `success:false,data:null`；`fetchOnce` 在 `success:false` 既不报错也不更新，静默保留旧数据。 | `success:false` 时写 store `error` 并触发 banner。 |
| MlpPage/Settings 空 catch 假成功 | `pages/MlpPage.tsx:105` + `Settings.tsx:44` | 返回 | 学习率/同步失败被空 catch 吞，用户见"已生效"假象。 | 至少 `setError(...)` 暴露失败或回滚。 |
| 两套 `Gauge` 实现并存 | `components/gauge/Gauge.tsx` + `components/common/Gauge.tsx` | 执行 | Dashboard 引 `gauge/Gauge`，Performance/GpuTuning 引 `common/Gauge`，Props 不同 → 维护分裂、行为易不一。 | 统一为一个 `Gauge`。 |

---

## 6. 信任但验证（核验记录）

主代理对以下坐标**逐行 Read / Grep 复核**，确认为实（非子代理转述）：
- `Program.cs:62,118,32-40,85` — https 重定向 / 仅 http / CORS 全开 / fallback 吞 API ✓
- `ServiceCollectionExtensions.cs:40-45,47-55,67` + `SensorBackgroundService.cs:35-43` — 桥接器未注入、SensorPipeline 双队列 ✓
- `SensorSignalRBridge.cs:26,30,37,40,97,78-79` — 订阅在构造、async void、事件名、电池语义、核数硬编码 0 ✓
- `App.xaml:4` + `App.xaml.cs:71,87-104,96` — 双主窗口、协议手动 new + 传参 ✓
- `WebView2BridgeService.cs:14` + 全仓 grep `\.Attach(`/`GetService<IWebView2BridgeService>` 零命中 → 桥未挂载 ✓
- `MlpController.cs:14,16,42,56-59,71,100-103,138` — 注入 `_scheduler` 零调用、实例字段、ResetModel 零维、UpdateConfig 不重建网络 ✓
- `signalR.ts:97,101,105,109` + 后端 grep `SendAsync("` 仅 `SensorUpdate`/`StateChange` → 前端订阅幽灵事件 ✓
- `HardwareMapper.cs:22,37,51` — 电池语义、CoreCount 与 Cores 截断不一 ✓
- `BootSequence.cs:27,51,52-56` + grep 无外部 `GetService<I(Theme/WindowEffects/ProcessManager/Configuration)Service>` → 4 服务死代码、协议参数未用 ✓
- `SensorHub.cs:17` / `StateHub.cs:16` — 后端仅发这两事件 ✓

> 详表其余中/低项由 4 路子代理并行产出（带精确坐标），主代理对顶层高危项已坐实；未逐条复核坐标的中低项若需进入修复排期，建议在动手前再按坐标跳读一次（遵循「边写边复核」）。

---

## 7. 构建门禁核验（本环境真实编译，非"已触发"即算过）

- 命令：`dotnet build src/ZTR.Api/ZTR.Api.csproj -c Debug`（后台阻塞至终态）。
- 结果：**EXITCODE=0，已成功生成，7 个警告，0 个错误**。依赖项目 `ZTR.Models`/`ZTR.Common`/`ZTR.Intelligence`/`ZTR.HAL` 全部还原并编译通过（NuGet 还原成功，`ZTR.Common` 空壳也能编出空程序集）。
- 含义：**因果链（依赖存在性）在 API 层成立**——所有 NuGet/项目引用真实可达。这恰恰反证本报告的结论：三链问题**全是运行期/逻辑层断链（写了没接、假定存在、假成功），而非编译期缺失**。**"能编译" ≠ "能交付"**，本项目是典型样例。
- 附带编译期信号：`SystemSensorFallback.cs(688,60)` 报 `CS8602 解引用可能出现空引用`（返回链隐患，与 §4 遥测/回退逻辑同源）；另有一批 `IL2026/IL3050`（AOT/trimming 警告，源自 `AddControllers`/`JsonStringEnumConverter()`/中间件 `JsonSerializer.Serialize`，若未来走 AOT 发布需改用源生成）。

---

## 8. 修复优先级（建议）

1. **P0（阻塞交付）**：V1 https 重定向、V2 桥接死链、V3 双主窗口、V4 桥接未挂载、V5 MLP 假训练/ResetModel 废网、V6/V8 实时与协议门禁。
2. **P1（功能实质失效）**：返回链假成功（`RemoveBinding`/`UpdateSettings`/`Performance` 忽略结果）、契约错配（自动化模式、Aura 参数、getConfig 嵌套、Screen 大小写、diagnostics 类型）、GPU 释放后使用 + 原生句柄泄漏。
3. **P2（健壮/清理）**：CORS 白名单、SPA fallback 排除 `/api`、死服务/死代码清理（Theme/WindowEffects/ProcessManager/Config/DeviceProbe/ManualOverride/ZTR.Common 空壳）、`async void` 限流、`ConfigurationService.Get<T>`、资源释放与 Dispose。

---

## 9. 一句话总评（按你的规程）

**三链是尺子，诚实是底线：执行链断在"写了没接上"（桥接/双窗口/4 死服务），因果链断在"假定存在"（事件名/字段/协议实例/空壳项目），返回链烂在"假成功与失真"（MLP 空跑、忽略写入结果、状态码恒 200）。硬件采集主干可信，但实时与 AI 两层是纸糊的——上线即穿。** 未修改任何文件，所有结论带坐标可查。
