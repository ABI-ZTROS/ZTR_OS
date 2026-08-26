# ZTR_OS 项目规范文档

> **版本**: 1.0.0  
> **生效日期**: 2026-08-26  
> **适用范围**: 所有参与 ZTR_OS 项目开发、测试、部署的人员  
> **执行等级**: ★★★★★ (严格执行)

---

## 目录

1. [项目背景](#1-项目背景)
2. [技术规范](#2-技术规范)
3. [代码规范](#3-代码规范)
4. [架构规范](#4-架构规范)
5. [测试规范](#5-测试规范)
6. [部署规范](#6-部署规范)
7. [协作规范](#7-协作规范)
8. [验收标准](#8-验收标准)

---

## 1. 项目背景

### 1.1 项目概述

**ZTR_OS** 是面向 ASUS ROG 系列设备（笔记本、台式机、掌机）的智能硬件控制与 AI 性能优化平台。

**核心定位**:
- 为 ASUS ROG 设备提供统一的硬件抽象层（HAL）
- 实时监测 CPU/GPU/电池/风扇/RGB 等硬件状态
- 通过 MLP 神经网络实现动态性能调度
- 提供 REST API + SignalR 实时推送能力
- 支持桌面应用（WPF + WebView2）和独立 API 服务两种部署模式

### 1.2 设计目标

| 目标 | 描述 | 衡量标准 |
|------|------|----------|
| **零宕机** | 7×24 小时稳定运行 | 年可用性 ≥ 99.99% |
| **低延迟** | 传感器数据采集与推送 | 端到端延迟 ≤ 2s |
| **高精度** | 硬件状态检测准确率 | ≥ 99.5% |
| **自优化** | AI 调度系统自主决策 | 人工干预率 ≤ 5% |
| **强隔离** | 进程绑定与资源隔离 | 进程绑定准确率 100% |

### 1.3 核心功能模块

| 模块 | 功能 | 优先级 |
|------|------|--------|
| `ZTR.HAL` | 硬件抽象层（ACPI/HID/NVAPI/ADL2） | P0 |
| `ZTR.Intelligence` | MLP 神经网络决策引擎 | P0 |
| `ZTR.Api` | REST API + SignalR 实时服务 | P0 |
| `ZTR.Desktop` | WPF 桌面壳 + WebView2 嵌入 | P1 |
| `ZTR.Service` | Windows Service 独立部署 | P1 |
| `frontend` | React 19 前端界面 | P0 |

---

## 2. 技术规范

### 2.1 技术栈锁定

**后端栈（不可更改）**:
```
┌─────────────────────────────────────────────────┐
│  Runtime:   .NET 9.0 SDK (net9.0)              │
│  Language:  C# 13 (严格可空引用类型)            │
│  Web Framework: ASP.NET Core 9.0               │
│  Real-Time: Microsoft.AspNetCore.SignalR       │
│  API Docs:  Swashbuckle.AspNetCore 10.2.3      │
│  GPU APIs:  NVAPI (NVIDIA), ADL2 SDK (AMD)     │
│  Hardware:  ACPI DeviceIoControl, HID I2C      │
│  DI Container: 内置 Microsoft.Extensions.DependencyInjection │
│  Logging:    内置 Microsoft.Extensions.Logging  │
│  Serialization: System.Text.Json (CamelCase)    │
└─────────────────────────────────────────────────┘
```

**前端栈（不可更改）**:
```
┌─────────────────────────────────────────────────┐
│  Runtime:   Node.js 20+ (LTS)                  │
│  Framework: React 19.2.8                       │
│  Language:  TypeScript 6.0.2 (严格模式)        │
│  Build:     Vite 8.2.2                         │
│  State:     Zustand 5.0.15                     │
│  Routing:   React Router DOM 7.18.2            │
│  Real-Time: @microsoft/signalr 10.0.11         │
│  Testing:   Vitest 4.1.11 + Testing Library 16 │
│  Linting:   oxlint 1.79.0                      │
│  CSS:       原生 CSS (不使用 CSS-in-JS)         │
└─────────────────────────────────────────────────┘
```

### 2.2 版本锁定（强约束）

**规则**:
- 所有依赖版本在 `*.csproj` 和 `package.json` 中**必须锁定精确版本**，禁止使用 `^`、`~`、`*` 等范围符号
- 依赖升级**必须经过完整回归测试**
- 新增依赖**必须在 PR 中说明用途和替代方案**

**已锁定版本**:
| 包 | 版本 | 锁定位置 |
|----|------|----------|
| `Swashbuckle.AspNetCore` | 10.2.3 | `src/ZTR.Api/ZTR.Api.csproj` |
| `System.Management` | 10.0.11 | `src/ZTR.Api/ZTR.Api.csproj` |
| `Microsoft.Web.WebView2` | 1.0.4129.50 | `src/ZTR.Desktop/ZTR.Desktop.csproj` |
| `react` | ^19.2.8 | `frontend/package.json` |
| `@microsoft/signalr` | ^10.0.11 | `frontend/package.json` |

### 2.3 环境要求

**开发环境（强制）**:
```
✅ Windows 10/11 x64 (版本 ≥ 21H2)
✅ .NET 9.0 SDK (版本 ≥ 9.0.100)
✅ Node.js 20.x LTS
✅ npm 10.x
✅ Visual Studio 2022 / VS Code
✅ ASUS ROG 设备 + ATKACPI 驱动
✅ 管理员权限（硬件通信必需）
✅ Git 2.30+
```

**构建环境（CI/CD）**:
```
✅ GitHub Actions Runner (windows-latest)
✅ .NET 9.0 SDK
✅ Node.js 20.x
✅ 无 ASUS 硬件（使用模拟器）
```

---

## 3. 代码规范

### 3.1 C# 编码规范

#### 3.1.1 命名规范（强制）

| 类型 | 规则 | 示例 |
|------|------|------|
| 类名 | PascalCase，不使用缩写 | `SensorPipeline`, `AtkDevice` |
| 方法 | PascalCase，动词开头 | `CollectOnce()`, `TryOpenDevice()` |
| 接口 | I 前缀 + PascalCase | `IAtkDevice`, `IWmiQueryService` |
| 私有字段 | _camelCase | `_sensorPipeline`, `_logger` |
| 常量 | PascalCase | `MaxFanSpeed`, `DefaultPort` |
| 枚举值 | PascalCase | `AsusMode.Turbo`, `SensorType.Temperature` |
| DTO record | PascalCase + Response/Request 后缀 | `HardwareResponse`, `SetModeRequest` |

#### 3.1.2 代码风格（强制）

```csharp
// ✅ 正确
public class SensorPipeline
{
    private readonly ILogger<SensorPipeline> _logger;
    private readonly IAtkDevice _atkDevice;

    public SensorPipeline(ILogger<SensorPipeline> logger, IAtkDevice atkDevice)
    {
        _logger = logger;
        _atkDevice = atkDevice;
    }

    public HardwareState CollectOnce()
    {
        try
        {
            var state = new HardwareState();
            // ... 采集逻辑
            return state;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to collect sensor data");
            return new HardwareState(); // 返回空状态而非抛出
        }
    }
}

// ❌ 禁止
public class sensorPipeline  // 小写开头
{
    private ILogger log;       // 缩写
    public HardwareState collect_once() {}  // snake_case
}
```

#### 3.1.3 可空引用类型（强制）

**规则**:
- `csproj` 中必须 `<Nullable>enable</Nullable>`
- 所有引用类型属性必须标注可空性
- 使用 `!` 操作符**必须有注释说明为什么不会为 null**

```csharp
// ✅ 正确
public class HardwareState
{
    public CpuState Cpu { get; set; } = new();  // 初始化为非 null
    public BatteryState? BatteryBackup { get; set; }  // 明确可空
}

// ❌ 禁止无注释使用 !
public CpuState Cpu { get; set; } = null!;  // 必须注释解释
```

#### 3.1.4 异常处理（强制）

**规则**:
- 所有硬件通信代码**必须 try-catch**
- 捕获异常后**必须记录日志**
- 返回默认状态而非继续抛出
- 使用自定义异常类型区分错误

```csharp
// ✅ 正确
public int ReadTemperature()
{
    try
    {
        return _atkDevice.ReadTemperature();
    }
    catch (DeviceCommunicationException ex)
    {
        _logger.LogWarning(ex, "Device communication failed, returning default");
        return 0;  // 返回安全默认值
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Unexpected error reading temperature");
        return 0;
    }
}
```

#### 3.1.5 异步编程（强制）

**规则**:
- 所有 I/O 操作**必须异步**
- 使用 `ConfigureAwait(false)` 避免上下文捕获（库代码）
- 不使用 `async void`（除了事件处理器）
- 合理使用 `CancellationToken`

```csharp
// ✅ 正确
public async Task<HardwareState> CollectAsync(CancellationToken ct = default)
{
    ct.ThrowIfCancellationRequested();
    var cpuTask = _cpuSensor.ReadAsync(ct);
    var gpuTask = _gpuSensor.ReadAsync(ct);
    await Task.WhenAll(cpuTask, gpuTask).ConfigureAwait(false);
    // ...
}
```

### 3.2 TypeScript 编码规范

#### 3.2.1 类型安全（强制）

**规则**:
- 禁止使用 `any` 类型（除非有充分理由并加注释）
- 所有 API 响应**必须定义 TypeScript 接口**
- 后端 DTO 与前端类型**必须一一对应**
- 使用 `strict` 模式

```typescript
// ✅ 正确
interface HardwareState {
  cpu: CpuState
  gpu: GpuState
  fans: FanState[]
}

interface ApiResponse<T> {
  success: boolean
  data: T
  message?: string
}

// ❌ 禁止
const data: any = response.data  // 必须定义类型
```

#### 3.2.2 React 组件规范（强制）

**规则**:
- 函数组件 + Hooks
- Props 必须定义接口
- 事件处理器使用 `useCallback` 包裹
- 列表项必须使用稳定的 `key`
- 禁止在 render 中创建新对象作为 prop（会导致子组件重渲染）

```tsx
// ✅ 正确
interface GaugeProps {
  value: number
  max: number
  label: string
  unit: string
}

const Gauge = React.memo(({ value, max, label, unit }: GaugeProps) => {
  const percentage = useMemo(() => Math.min(100, (value / max) * 100), [value, max])
  const handleClick = useCallback(() => {
    console.log('clicked')
  }, [])
  
  return <svg>{label}: {percentage}%</svg>
})

// ❌ 禁止
const Gauge = ({ value, max, label, unit }) => {  // 缺少类型
  return <div style={{ width: `${value}%` }}>{label}</div>  // render 中创建对象
}
```

#### 3.2.3 状态管理规范（强制）

**规则**:
- 使用 Zustand 全局状态管理
- Store 按领域划分（`useHardwareStore`, `useMlpStore`, `useSettingsStore`）
- Store 必须提供类型化的 action
- 禁止直接 mutate state

```typescript
// ✅ 正确
export const useHardwareStore = create<HardwareStore>((set) => ({
  hardware: null,
  isConnected: false,
  setHardware: (state) => set({ hardware: state, isConnected: true }),
}))

// ❌ 禁止
const store = useHardwareStore()
store.hardware.cpu.usage = 50  // 直接修改状态
```

#### 3.2.4 API 调用规范（强制）

**规则**:
- 所有 API 调用**必须通过 `api.ts` 封装**
- 必须处理错误状态
- 必须添加类型断言
- Desktop 模式**必须使用 `window.__API_BASE_URL__`**

```typescript
// ✅ 正确
export const hardwareApi = {
  getState: () => api.get<HardwareState>('/api/hardware/state'),
}

// 使用
const response = await hardwareApi.getState()
if (response.success && response.data) {
  setHardware(response.data)
}
```

### 3.3 Git 提交规范（强制）

#### 3.3.1 提交信息格式

```
<type>(<scope>): <description>

[optional body]

[optional footer]
```

**类型（必须）**:
| 类型 | 说明 |
|------|------|
| `feat` | 新功能 |
| `fix` | 修复 bug |
| `refactor` | 重构（不改变功能） |
| `docs` | 文档更新 |
| `test` | 添加/修改测试 |
| `build` | 构建系统变更 |
| `ci` | CI/CD 配置变更 |
| `style` | 代码格式（不影响逻辑） |
| `perf` | 性能优化 |
| `revert` | 回滚提交 |

**范围（必须）**:
- `hal` - 硬件抽象层
- `intelligence` - AI 引擎
- `api` - Web API
- `desktop` - 桌面应用
- `frontend` - 前端
- `mlp` - MLP 神经网络
- `fan` - 风扇控制
- `rgb` - RGB 灯效
- `battery` - 电池管理
- `gpu` - GPU 控制
- `cpu` - CPU 控制
- `binding` - 进程绑定
- `settings` - 设置
- `docs` - 文档

**示例**:
```
fix(frontend): resolve Performance page black screen crash

- Add ErrorBoundary component for React error isolation
- Add defensive null checks in Gauge component
- Update FanResponse DTO to include TargetSpeed and Mode
- Add bounds clamping for progress bar width values

Closes #123
```

#### 3.3.2 分支策略（强制）

**分支模型**:
```
main ──────────────────────────────────────── 生产分支
  │
  ├── develop ──────────────────────────────── 开发集成分支
  │     │
  │     ├── feature/hal-acpi-rewrite ──────── 功能分支
  │     ├── fix/mlp-convergence ───────────── Bug 修复分支
  │     └── perf/sensor-pipeline ──────────── 性能优化分支
  │
  └── hotfix/critical-bug ─────────────────── 紧急修复
```

**规则**:
- `main` 分支**禁止直接推送**
- 所有改动**必须通过 Pull Request** 合入
- PR 必须关联 Issue 编号
- 必须通过 CI 检查才能合并

---

## 4. 架构规范

### 4.1 分层架构（强制）

```
┌─────────────────────────────────────────────────────────┐
│                    Presentation Layer                    │
│  ┌─────────────┐  ┌─────────────┐  ┌─────────────┐      │
│  │  Desktop    │  │  Frontend   │  │  CLI Client │      │
│  │  (WPF+WV2)  │  │  (React19)  │  │  (Python)   │      │
│  └──────┬──────┘  └──────┬──────┘  └──────┬──────┘      │
└─────────┼────────────────┼────────────────┼─────────────┘
          │                │                │
          ▼                ▼                ▼
┌─────────────────────────────────────────────────────────┐
│                    API Layer (ZTR.Api)                    │
│  ┌─────────────┐  ┌─────────────┐  ┌─────────────┐      │
│  │  Controller  │  │ SignalR Hub │  │ Middleware  │      │
│  └──────┬──────┘  └──────┬──────┘  └──────┬──────┘      │
└─────────┼────────────────┼────────────────┼─────────────┘
          │                │                │
          ▼                ▼                ▼
┌─────────────────────────────────────────────────────────┐
│              Intelligence Layer (ZTR.Intelligence)       │
│  ┌─────────────┐  ┌─────────────┐  ┌─────────────┐      │
│  │  MLP Engine  │  │ Decision    │  │ Online      │      │
│  │  (3-layer)   │  │ Engine      │  │ Learner     │      │
│  └──────┬──────┘  └──────┬──────┘  └──────┬──────┘      │
└─────────┼────────────────┼────────────────┼─────────────┘
          │                │                │
          ▼                ▼                ▼
┌─────────────────────────────────────────────────────────┐
│            Hardware Abstraction Layer (ZTR.HAL)          │
│  ┌─────────────┐  ┌─────────────┐  ┌─────────────┐      │
│  │  AsusAcpi    │  │  AsusHid    │  │  SensorPipe │      │
│  │  (DeviceIoC) │  │  (HID I2C)  │  │  (Polling)  │      │
│  └──────┬──────┘  └──────┬──────┘  └──────┬──────┘      │
└─────────┼────────────────┼────────────────┼─────────────┘
          │                │                │
          ▼                ▼                ▼
┌─────────────────────────────────────────────────────────┐
│                    Hardware Layer                         │
│  ┌─────────────┐  ┌─────────────┐  ┌─────────────┐      │
│  │  ACPI       │  │  HID        │  │  GPU APIs   │      │
│  │  (ATKACPI)  │  │  (Report 5A)│  │  (NVAPI/ADL)│      │
│  └─────────────┘  └─────────────┘  └─────────────┘      │
└─────────────────────────────────────────────────────────┘
```

### 4.2 依赖方向（强制）

**规则**:
- 依赖只能**向下流动**
- 上层不能直接引用下层实现细节
- 接口定义在下层，实现在下层，上层通过接口调用

```
Presentation → API → Intelligence → HAL → Hardware
   ↑           ↑        ↑             ↑
   └───────────┴────────┴─────────────┘
              禁止反向依赖
```

### 4.3 数据流向（强制）

#### 4.3.1 传感器数据流

```
[Hardware] → [HAL CollectOnce()] → [HardwareState Model] → [API Mapper] → [DTO Response] → [SignalR Push] → [Frontend Store] → [UI Render]
```

#### 4.3.2 控制指令流

```
[Frontend Action] → [API POST/PUT] → [Controller Validate] → [ModeControl Execute] → [HAL Command] → [Hardware Apply] → [Confirm Response] → [UI Update]
```

### 4.4 DTO 规范（强制）

**后端 DTO 设计**:
```csharp
// ZTR.Api/DTOs/FrontendDTOs.cs
public record HardwareResponse(
    CpuResponse Cpu,
    GpuResponse Gpu,
    BatteryResponse Battery,
    List<FanResponse> Fans,
    MemoryResponse Memory,
    DateTime Timestamp
);

public record FanResponse(int Id, string Name, int Speed, int TargetSpeed, string Mode);
```

**前端类型定义**（必须与后端 DTO 一一对应）:
```typescript
// frontend/src/types/index.ts
interface HardwareState {
  cpu: CpuState
  gpu: GpuState
  fans: FanState[]
}

interface FanState {
  id: number
  name: string
  speed: number
  targetSpeed: number
  mode: string
}
```

**规则**:
- 后端字段使用 PascalCase，JSON 序列化使用 CamelCase（`PropertyNamingPolicy = JsonNamingPolicy.CamelCase`）
- 前端 TypeScript 类型使用 camelCase
- 字段名必须完全对应（不允许缩写或改名）
- 新增字段**必须同步更新前后端**

---

## 5. 测试规范

### 5.1 测试覆盖要求（强制）

| 层级 | 覆盖率要求 | 测试框架 |
|------|-----------|----------|
| `ZTR.Models` | ≥ 95% | xUnit |
| `ZTR.HAL` | ≥ 90% | xUnit + Moq |
| `ZTR.Intelligence` | ≥ 90% | xUnit |
| `ZTR.Api` | ≥ 80% | xUnit + Integration Testing |
| `frontend` | ≥ 80% | Vitest + Testing Library |

### 5.2 测试类型（强制）

#### 5.2.1 单元测试

**要求**:
- 每个公共方法**必须有单元测试**
- 测试方法命名: `MethodName_Scenario_ExpectedResult`
- 禁止测试私有方法

```csharp
// ✅ 正确命名
[Fact]
public void CollectOnce_WhenHardwareAvailable_ReturnsValidState()
{
    // Arrange
    var sensor = new SensorPipeline(mockLogger, mockAtkDevice);
    
    // Act
    var result = sensor.CollectOnce();
    
    // Assert
    Assert.NotNull(result);
    Assert.True(result.Temperature > 0);
}
```

#### 5.2.2 集成测试

**要求**:
- HAL 层到 API 层的完整链路测试
- 使用 `TestWebApplicationFactory`
- 模拟硬件响应

```csharp
// ✅ 正确
public class HardwareIntegrationTests : IClassFixture<TestWebApplicationFactory<Program>>
{
    private readonly TestWebApplicationFactory<Program> _factory;
    
    [Fact]
    public async Task GetState_ReturnsCompleteHardwareData()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/api/hardware/state");
        
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<HardwareResponse>>();
        
        Assert.True(result!.Success);
        Assert.NotNull(result.Data);
        Assert.NotEmpty(result.Data.Fans);
    }
}
```

#### 5.2.3 前端测试

**要求**:
- 每个关键组件**必须有渲染测试**
- 测试用户交互行为
- Mock API 调用

```typescript
// ✅ 正确
describe('Performance Page', () => {
  it('renders fan list when data is available', async () => {
    mockApi.getState.mockResolvedValue({
      success: true,
      data: mockHardware,
    })
    
    render(<Performance />)
    
    expect(screen.getByText('CPU Fan')).toBeInTheDocument()
    expect(screen.getByText('automatic')).toBeInTheDocument()
  })
})
```

### 5.3 测试数据规范（强制）

**规则**:
- 测试数据**必须是确定的**（禁止随机）
- 测试数据**必须有语义**
- 测试 fixture 集中管理

```csharp
// ✅ 正确
public static class TestFixtures
{
    public static HardwareState CreateValidHardwareState()
    {
        return new HardwareState
        {
            Cpu = new CpuState { Temperature = 65, Usage = 45, Power = 35 },
            Gpu = new GpuState { Temperature = 55, Usage = 78, Power = 120 },
            // ...
        };
    }
}
```

---

## 6. 部署规范

### 6.1 构建流程（强制）

#### 6.1.1 本地构建

```bash
# 后端
dotnet restore
dotnet build src/ZTR.Api -c Release
dotnet test tests/

# 前端
cd frontend
npm ci
npm run lint
npm run test
npm run build
```

#### 6.1.2 CI/CD 构建（GitHub Actions）

**触发条件**:
- `push` 到 `main` 分支
- `pull_request` 到 `main` 分支
- `release` 标签创建

**构建矩阵**:
```
┌──────────────┬──────────────┬──────────────┐
│  Job         │  OS          │  Target      │
├──────────────┼──────────────┼──────────────┤
│  build-api   │  ubuntu-latest  │ net9.0    │
│  build-desktop│ windows-latest  │ win-x64  │
│  test        │  ubuntu-latest  │ all tests │
│  release     │  windows-latest │ publish   │
└──────────────┴──────────────┴──────────────┘
```

### 6.2 部署模式（强制）

#### 6.2.1 桌面应用模式（推荐）

```
┌─────────────────────────────────────────┐
│           ZTR.Desktop (WPF)             │
│  ┌─────────────────────────────────┐    │
│  │     WebView2 (http://app.local) │    │
│  │     ┌─────────────────────┐     │    │
│  │     │  React Frontend     │     │    │
│  │     │  (from wwwroot/)    │     │    │
│  │     └─────────────────────┘     │    │
│  └─────────────────────────────────┘    │
│  ┌─────────────────────────────────┐    │
│  │     Embedded Kestrel Server    │    │
│  │     (localhost:5000-5010)      │    │
│  │     ┌─────────────────────┐     │    │
│  │     │  ZTR.Api + HAL       │     │    │
│  │     └─────────────────────┘     │    │
│  └─────────────────────────────────┘    │
└─────────────────────────────────────────┘
```

**规则**:
- 使用 `SetVirtualHostNameToFolderMapping` 映射前端
- 前端通过 `window.__API_BASE_URL__` 获取后端地址
- 端口自动检测（5000-5010）
- 单 EXE 发布（self-contained）

#### 6.2.2 独立 API 服务模式

```
┌─────────────────────────────────────────┐
│        ZTR.Api (Standalone)             │
│  ┌─────────────────────────────────┐    │
│  │     Kestrel Server              │    │
│  │     (0.0.0.0:5000)             │    │
│  │     ┌─────────────────────┐     │    │
│  │     │  REST + SignalR      │     │    │
│  │     │  + Static Files      │     │    │
│  │     └─────────────────────┘     │    │
│  └─────────────────────────────────┘    │
│  Frontend served from wwwroot/           │
└─────────────────────────────────────────┘
```

**规则**:
- 支持独立部署
- 支持 Windows Service
- 支持 Docker 容器化

### 6.3 环境配置（强制）

#### 6.3.1 appsettings.json

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning",
      "ZTR.HAL": "Debug",
      "ZTR.Intelligence": "Information"
    }
  },
  "Cors": {
    "AllowedOrigins": ["http://app.local", "http://localhost:5173"]
  },
  "SensorPipeline": {
    "PollingIntervalMs": 1000,
    "TimeoutMs": 5000,
    "MaxRetries": 3
  },
  "MlpEngine": {
    "LearningRate": 0.01,
    "HiddenLayers": [64, 32, 16],
    "AutoModeSwitch": true
  }
}
```

#### 6.3.2 前端配置

```typescript
// frontend/src/config/apiConfig.ts
export function getApiBaseUrl(): string {
  // 1. Desktop mode: use injected URL
  if (window.__API_BASE_URL__) {
    return window.__API_BASE_URL__
  }
  // 2. Development mode: use env or default
  return import.meta.env.VITE_API_URL || 'http://localhost:5000'
}
```

### 6.4 发布流程（强制）

```
1. 代码合入 main 分支
2. 触发 CI/CD 构建
3. 所有检查通过
4. 创建 GitHub Release 标签
5. 生成发布产物:
   - ZTR.Desktop-win-x64.zip (桌面应用)
   - ZTR.Api-win-x64.zip (独立 API)
   - ZTR.Api-linux-x64.tar.gz (Linux API)
6. 更新 Release Notes
7. 通知测试团队
```

---

## 7. 协作规范

### 7.1 Issue 管理（强制）

#### 7.1.1 Issue 类型

| 类型 | 标签 | 描述 |
|------|------|------|
| Bug | `bug` | 功能异常 |
| Feature | `enhancement` | 新功能请求 |
| Task | `task` | 开发任务 |
| Research | `research` | 技术调研 |
| Question | `question` | 疑问 |

#### 7.1.2 Bug 报告格式

```markdown
## Bug 报告

### 严重性
- [ ] 致命（系统崩溃、数据丢失）
- [ ] 严重（核心功能不可用）
- [ ] 中等（功能异常但有替代方案）
- [ ] 轻微（UI 问题、 cosmetic）

### 重现步骤
1. 进入页面 '...'
2. 点击 '...'
3. 看到错误 '...'

### 预期行为
应该 '...'

### 实际行为
发生 '...'

### 环境信息
- OS: Windows 11 23H2
- .NET: 9.0.100
- 浏览器: Edge 120.0

### 日志
```
[错误日志]
```

### 截图
[附上截图]
```

### 7.2 Code Review 规范（强制）

#### 7.2.1 Review 检查清单

**代码质量**:
- [ ] 命名清晰、符合规范
- [ ] 方法职责单一
- [ ] 无重复代码
- [ ] 注释充分（说明 why 而非 what）

**安全性**:
- [ ] 无硬编码密钥
- [ ] 输入已验证
- [ ] 异常已处理
- [ ] 无 SQL 注入风险

**性能**:
- [ ] 无 N+1 查询
- [ ] 异步 I/O 正确
- [ ] 资源已释放
- [ ] 无内存泄漏

**测试**:
- [ ] 单元测试覆盖新代码
- [ ] 集成测试通过
- [ ] 边界条件已测试
- [ ] 异常路径已测试

#### 7.2.2 Review 反馈格式

```
[APPROVED] 认可合并

[CHANGES_REQUESTED] 需要修改:
- [ ] src/HAL/SensorPipeline.cs:45 - 建议使用 ConfigureAwait(false)
- [ ] src/Api/Controllers/HardwareController.cs:23 - 需要添加 null 检查

[COMMENT] 建议:
- 整体设计不错，建议考虑使用策略模式来处理不同设备类型
```

### 7.3 文档规范（强制）

#### 7.3.1 文档类型

| 文档 | 位置 | 更新时机 |
|------|------|----------|
| API 文档 | `docs/api/` | API 变更时 |
| 架构文档 | `docs/architecture.md` | 架构调整时 |
| 硬件协议 | `docs/hardware-protocols.md` | 新增硬件支持时 |
| MLP 引擎 | `docs/mlp-engine.md` | 算法更新时 |
| 部署文档 | `docs/deployment.md` | 部署方式变更时 |

#### 7.3.2 文档要求

- 所有 API 必须有 Swagger 注释
- 所有公共类/方法必须有 XML 文档
- 关键设计决策必须有 ADR（Architecture Decision Record）
- 文档必须与代码同步更新

---

## 8. 验收标准

### 8.1 功能验收（强制）

#### 8.1.1 硬件检测（100% 通过）

| 检测项 | 标准 | 验证方法 |
|--------|------|----------|
| CPU 温度 | 误差 ≤ ±2°C | 对比 HWMonitor 读数 |
| GPU 温度 | 误差 ≤ ±2°C | 对比 GPU-Z 读数 |
| CPU 使用率 | 误差 ≤ ±1% | 对比任务管理器 |
| GPU 使用率 | 误差 ≤ ±1% | 对比任务管理器 |
| 电池电量 | 误差 ≤ ±1% | 对比系统托盘显示 |
| 风扇转速 | 误差 ≤ ±5% | 对比 HWInfo 读数 |

#### 8.1.2 功能完整性（100% 通过）

| 功能 | 测试方法 | 通过标准 |
|------|----------|----------|
| 性能模式切换 | API 调用 + 硬件验证 | 实际切换成功 |
| 风扇曲线设置 | 手动设置 + 实时监测 | 曲线按预期执行 |
| RGB 灯效切换 | API 调用 + 目视验证 | 灯效即时生效 |
| 进程绑定 | 设置 + 任务管理器验证 | CPU 亲和性正确 |
| MLP 自动优化 | 运行 1 小时监测 | 性能不劣化 |

#### 8.1.3 稳定性测试（100% 通过）

| 测试项 | 时长 | 标准 |
|--------|------|------|
| 持续运行 | 72 小时 | 无崩溃、无内存泄漏 |
| 快速模式切换 | 1000 次 | 无死锁、无状态异常 |
| 压力测试 | 12 小时 | CPU/GPU 满载下正常运行 |
| 异常恢复 | 模拟硬件断开 | 自动重连、状态恢复 |

### 8.2 性能验收（强制）

| 指标 | 标准 | 测试方法 |
|------|------|----------|
| API 响应时间（P95） | ≤ 200ms | 压力测试工具 |
| SignalR 推送延迟 | ≤ 500ms | 端到端测试 |
| 内存占用 | ≤ 100MB | 长时间运行监测 |
| 启动时间（桌面） | ≤ 10s | 冷启动计时 |
| 构建时间 | ≤ 5min | CI/CD 计时 |

### 8.3 代码质量验收（强制）

| 指标 | 标准 | 工具 |
|------|------|------|
| 单元测试覆盖率 | ≥ 85% | dotnet test + coverage |
| 前端测试覆盖率 | ≥ 80% | vitest + coverage |
| 代码复杂度 | 圈复杂度 ≤ 15 | NDepend / SonarQube |
| 重复代码率 | ≤ 3% | SonarQube |
| 代码规范合规率 | 100% | ESLint / StyleCop |
| 安全漏洞 | 0 个高危 | OWASP Dependency Check |

---

## 附录

### A. 关键文件索引

| 文件 | 路径 | 说明 |
|------|------|------|
| 项目规范 | `PROJECT_SPEC.md` | 本文档 |
| 架构文档 | `docs/architecture.md` | 详细架构设计 |
| API 文档 | `docs/api/README.md` | API 参考 |
| 硬件协议 | `docs/hardware-protocols.md` | ACPI/HID 协议 |
| MLP 引擎 | `docs/mlp-engine.md` | 神经网络设计 |
| 部署指南 | `docs/deployment.md` | 部署与运维 |
| 快速入门 | `docs/quickstart.md` | 新手教程 |

### B. 联系方式

| 角色 | 联系人 | 职责 |
|------|--------|------|
| 项目负责人 | - | 总体架构决策 |
| 后端负责人 | - | HAL + API 开发 |
| 前端负责人 | - | React UI 开发 |
| 测试负责人 | - | 质量保证 |
| DevOps | - | CI/CD 运维 |

### C. 版本历史

| 版本 | 日期 | 变更 |
|------|------|------|
| 1.0.0 | 2026-08-26 | 初始版本 |

---

> **注意**: 本规范文档为强制性标准，所有参与者必须严格执行。任何偏离规范的行为必须经过项目负责人审批。
