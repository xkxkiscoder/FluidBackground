# FluidBackground

一个 .NET 8 流体背景渲染库：在任意 UI 框架中嵌入一块流动的渐变动画背景，支持**流体**与**星空**两种效果模式与鼠标指针交互。

内置双渲染后端：

| 后端 | 技术 | 说明 |
|---|---|---|
| 2D | SkiaSharp + SKSL | 纯 CPU 逐像素计算，兼容性最好，无任何环境依赖 |
| 3D | Silk.NET.OpenGL | GPU 着色器渲染（GLSL 330），效果带体积感，需 OpenGL 3.3+ 上下文 |

同一套效果算法分别用 SKSL 与 GLSL 实现，两套着色器行为保持一致。

## 快速开始

```bash
# 构建全部（含 6 个库项目 + 5 个示例）
dotnet build FluidBackground.slnx

# 运行任意示例（GUI 应用）
dotnet run --project Samples/FluidBackground.Sample.WPF
dotnet run --project Samples/FluidBackground.Sample.Avalonia11
dotnet run --project Samples/FluidBackground.Sample.WinForms
dotnet run --project Samples/FluidBackground.Sample.WinUI -p:Platform=x64
```

> 说明：`dotnet build` 会产生 24 个 `NU1701` 警告，来自 `SkiaSharp.Views.*` 传递引入的 OpenTK 包，属预期行为，不影响构建（0 错误）。

## 架构总览

```
┌──────────────────────────── 适配层（每平台一个项目）────────────────────────────┐
│  FluidBackground.{WPF, WinForms, WinUI, Avalonia11, Avalonia12}               │
│  每平台提供一个 FluidBackgroundControl 控件：                                  │
│  · 依赖属性（Config / IsAnimationEnabled / MaxFps）                            │
│  · 渲染循环（跟随 UI 帧率）                                                    │
│  · 把渲染结果拷贝到平台位图并绘制                                              │
│  · 鼠标移动 → 归一化坐标 (0~1) → SetPointerPosition                            │
└──────────────────────────────────────┬───────────────────────────────────────┘
                                        ▼
┌────────────────────────────── 核心库 FluidBackground.Core ─────────────────────┐
│  FluidRenderer（门面）                                                         │
│   ├─ 工厂：Create() / CreateWithOpenGL(gl)                                     │
│   ├─ 按 RenderMode 选择后端，Auto 失败自动回退 Skia                             │
│   ├─ 线程安全：所有操作在 lock 内执行                                          │
│   └─ 统一入口：RenderFrame / RenderToBitmap / RenderToCanvas                   │
│        │                                                                      │
│        ├──► SkiaRenderer（2D，CPU 逐像素）                                     │
│        │     着色器算法内嵌在 C#：Noise / FluidEffect / ...                   │
│        │                                                                      │
│        └──► OpenGLRenderer（3D，GPU 着色器）                                   │
│              顶点着色器内嵌 + 片元着色器读取嵌入资源 Shaders/fluid.frag        │
│              全屏三角形 + 离屏 FBO 渲染 + ReadPixels 回读像素                   │
└───────────────────────────────────────────────────────────────────────────────┘
```

### 渲染门面：`FluidRenderer`

`FluidRenderer.cs` 是唯一的对外入口，封装了后端选择与线程安全：

- `Create(config)`：按 `RenderMode` 选择后端——`Force2D` 走 Skia、`Force3D` 走 OpenGL、`Auto` 先尝试 OpenGL，初始化失败则自动回退到 Skia（保证任何环境下都能渲染）。
- `CreateWithOpenGL(gl)`：接收外部提供的 Silk.NET `GL` 上下文（由宿主 UI 框架管理），并把 `RenderMode` 强制为 `Force3D`。
- 所有公开方法先做 `ObjectDisposedException.ThrowIf` + 参数校验，再在 `lock` 内调用后端渲染器，可安全跨线程调用。
- `RenderToCanvas(canvas, ...)`：适配层专用捷径，Skia 后端直接画到目标 `SKCanvas`；OpenGL 后端则先回读像素再画位图。

### 2D 后端：`SkiaRenderer`（CPU 逐像素）

`SkiaRenderer.cs` 不依赖 GPU，把着色器算法用 C# 重新实现：

1. 按 `RenderQuality` 缩放渲染分辨率（`quality=1` 为 1:1，越大越省 CPU，最终拉伸回目标尺寸）。
2. 用 `unsafe` 指针遍历每个像素，调用 `CalculatePixel(u, v, time)` 按 `Mode` 分发到 `FluidEffect` / `StarfieldEffect` 计算 RGB 颜色，结果打包为 ARGB `uint` 直接写 `SKBitmap` 内存。
3. `canvas.DrawBitmap(小图, 目标矩形)` 拉伸到控件大小。

算法核心（与着色器一一对应）：

- `Hash` + `Noise`：值噪声，整数格点哈希后做 smoothstep 双线性插值；
- `FluidEffect`：三层噪声以不同频率/速度叠加成 `blend`，在 N 个颜色之间线性插值（域扭曲的简化版）；
- `StarfieldEffect`：网格哈希生成随机星点（两层、闪烁、缓慢漂移），fbm 噪声叠加星云色块，周期性流星划过；
- 指针交互：指针周围 `SmoothStep(PointerRadius, 0, dist)` 决定影响强度，对该处颜色提亮。

### 3D 后端：`OpenGLRenderer`（GPU 着色器）

`OpenGLRenderer.cs` 用 Silk.NET 绑定 OpenGL 3.3，流程：

1. **着色器**：顶点着色器内嵌（全屏两个三角形），片元着色器从嵌入资源加载 `FluidBackground.Core.Shaders.fluid.frag`（GLSL 330）。
2. **离屏渲染**：`EnsureFramebuffer` 按目标尺寸创建 FBO + 纹理；每帧把配置通过 uniform（`iTime`、`iSpeed`、`iDensity`、`iMode`、`iPointer`、`iColor0~3` 等）传入，绘制全屏三角形。
3. **回读**：`glReadPixels` 把 RGBA 像素读回 `byte[]`，由于 OpenGL 原点在左下，需 `FlipImageVertically` 垂直翻转，再转成 `SKBitmap` 或直接返回字节数组。

`fluid.frag` 比 SKSL 版多了 3D 噪声（`noise3D` / `fbm3D`，把 z 轴作为时间维度折叠进 2D 噪声），三种效果均加入体积扰动，观感更立体。**修改效果时必须同步维护两套着色器**（`fluid.sksl` 与 `fluid.frag`）。

### 适配层：`FluidBackgroundControl`

每个 UI 框架一个项目，控件职责相同，机制各异：

- **WPF**（`FluidBackground.WPF/FluidBackgroundControl.cs`）：
  - 继承 `FrameworkElement`，重写 `OnRender` 绘制。
  - 渲染循环挂在 `CompositionTarget.Rendering` 事件上（跟随 WPF 渲染线程），`MaxFps` 用于节流。
  - `OnRender` 里复用 `SKBitmap` + `WriteableBitmap`，用不安全指针把 RGBA 逐行拷贝进 `BackBuffer`（同时交换 R/B 匹配 `Pbgra32`），`AddDirtyRect` 后 `DrawImage`。
  - 控件加载时创建渲染器，卸载时释放；`OnMouseMove` 把鼠标位置归一化后传给 `SetPointerPosition`。
- **Avalonia 11 / 12**：继承 `Control`，重写 `Render(DrawingContext)`，Avalonia 版本分别针对 11.3.2 与 12.x 的 API 差异维护两份。
- **WinForms**：继承 `Control`，用 `System.Windows.Forms.Timer` 驱动 `Invalidate()`，在 `OnPaint` 中用 `Graphics.DrawImage` 绘制位图。
- **WinUI 3**：继承 `ContentControl`（WindowsAppSDK 2.3.1），帧回调挂在 `CompositionTarget.Rendering` 上。

所有适配层共用同一个 `FluidRenderer`，只是"如何取帧、如何呈现、如何上报指针"不同。

## 配置项

`FluidConfig`（`FluidBackground.Core/Models/FluidConfig.cs`），所有配置支持运行时通过 `UpdateConfig` 热更新：

| 属性 | 默认值 | 说明 |
|---|---|---|
| `Colors` | 深蓝→中蓝→浅蓝→紫（4 色） | 渐变颜色数组（3–6 色） |
| `Speed` | 1.0 | 动画速度（0.1–5.0） |
| `Density` | 0.3 | 图案分布浓度（0.0–1.0），越小纹理越稀疏、观感越淡雅，1.0 为最浓 |
| `Mode` | `Fluid` | 效果模式：`Fluid`（流体）/ `Starfield`（星空） |
| `RenderMode` | `Auto` | `Auto` / `Force2D` / `Force3D` |
| `RenderQuality` | 1.0 | 渲染精度（2D 后端的内部分辨率系数，1=最高；星空模式为保星点清晰始终全分辨率，忽略此项） |
| `EnableMeteor` | true | 是否显示流星（仅星空模式生效） |
| `EnableNebula` | true | 是否显示星云（仅星空模式生效） |
| `EnablePointerInteraction` | true | 是否启用指针交互 |
| `PointerRadius` | 0.3 | 指针影响半径（相对画布，0.0–1.0） |

`FluidColor` 是归一化 RGB 的 `readonly struct`，支持 `FromHex` / `FromBytes` / `FromSKColor` 等构造方式。

## 目录结构

```
FluidBackground.Core/            渲染核心（无 UI 依赖）
├─ FluidRenderer.cs              门面：后端选择 + 线程安全
├─ Renderers/                    IFluidRenderer 接口与两个后端实现
├─ Models/                       FluidConfig / FluidColor / FluidMode / RenderMode
└─ Shaders/                      fluid.sksl（Skia 版）/ fluid.frag（OpenGL 版），嵌入资源
FluidBackground.{WPF,WinForms,WinUI,Avalonia11,Avalonia12}/
└─ FluidBackgroundControl.cs     各平台控件（渲染循环 + 位图呈现 + 指针交互）
Samples/FluidBackground.Sample.*/  各平台可运行示例
```

## 如何扩展

- **新增渲染后端**：实现 `IFluidRenderer`（`Initialize` / `RenderFrame` / `RenderToBitmap` / `UpdateConfig` / `SetPointerPosition` / `Dispose`），并在 `FluidRenderer.InitializeRenderer` 的 `RenderMode` 分支注册。
- **新增 UI 平台**：新建 `FluidBackground.<框架>` 项目，提供 `FluidBackgroundControl` 控件（照抄 WPF 版的"渲染循环 + 位图呈现 + 指针上报"三个职责），配套一个 Sample，并注册进 `FluidBackground.slnx`（WinUI 条目需 `Platform="x64"`）。
- **修改效果算法**：同步更新 `Shaders/fluid.sksl`（Skia 后端直接消费）与 `Shaders/fluid.frag`（OpenGL 后端嵌入资源加载），以及 `SkiaRenderer.cs` 中对应的 C# 实现。

## 已知限制

- 3D 后端需要外部提供有效的 OpenGL 3.3+ 上下文（`CreateWithOpenGL`），无上下文时初始化失败，`Auto` 模式会自动回退到 2D。
- 2D 后端逐像素计算在超大控件 + 低 `RenderQuality` 下 CPU 占用较高，建议大尺寸场景配合 `RenderQuality` 调优或选用 3D 后端。
