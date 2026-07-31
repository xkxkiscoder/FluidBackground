# FluidBackground

.NET 8 流体背景渲染库，双渲染后端（SkiaSharp 2D + Silk.NET OpenGL 3D），提供 WPF、WinForms、WinUI 3、Avalonia 11、Avalonia 12 五套 UI 适配层，每套均附可运行示例。

## 项目

- 技术栈：C# 12、.NET 8（`net8.0` / `net8.0-windows`）、SkiaSharp 3.119.0、Silk.NET.OpenGL 2.22.0、Avalonia 11.3.2、WindowsAppSDK 2.3.1。
- **仓库根目录是 `FluidBackground/`**（包含 `FluidBackground.slnx` 与 `.sln`，也是 git 仓库所在），不是工作区根目录 `D:\Projects\FluidBackground`。
- `FluidBackground.Core` 不依赖任何 UI 框架；适配层与示例仅面向 Windows。
- 目前无测试、无 CI、无 README。

## 命令（在 `FluidBackground/` 下执行）

- 构建全部：`dotnet build FluidBackground.slnx` —— 已验证 0 错误；24 个 NU1701 警告来自 `SkiaSharp.Views.*` 传递引入的 OpenTK，属预期。
- 构建单个项目：`dotnet build FluidBackground.Core/FluidBackground.Core.csproj`
- 运行示例（GUI）：`dotnet run --project Samples/FluidBackground.Sample.WPF`（WinUI 需加 `-p:Platform=x64`）
- 项目没有测试工程，无测试命令。

## 架构

- `FluidBackground.Core/` —— 渲染核心，无 UI 依赖：
  - `FluidRenderer.cs` —— 门面：`Create` / `CreateWithOpenGL` 工厂；`RenderFrame` / `RenderToBitmap` / `RenderToCanvas` / `SetPointerPosition`；内部用 `lock` 保证线程安全；`RenderMode.Auto` 在 OpenGL 初始化失败时自动回退到 Skia。
  - `Renderers/IFluidRenderer.cs` —— 渲染器接口（`Name`、`IsAvailable`、`Initialize`、`Render*`、`UpdateConfig`、`SetPointerPosition`）。
  - `Renderers/SkiaRenderer.cs` —— 2D 后端（SKSL 着色器）。
  - `Renderers/OpenGLRenderer.cs` —— 3D 后端（Silk.NET GL；内嵌顶点着色器，加载嵌入资源 `FluidBackground.Core.Shaders.fluid.frag`）。
  - `Models/` —— `FluidConfig`（Colors/Speed/Density/Mode/RenderQuality/EnableMeteor/EnableNebula/EnablePointerInteraction/PointerRadius）、`FluidColor`、`FluidMode`（Fluid/Starfield）、`RenderMode`（Auto/Force2D/Force3D）。
  - `Shaders/*.sksl, *.frag` —— 嵌入资源（在 csproj 中声明）。
- `FluidBackground.{WPF,WinForms,WinUI,Avalonia11,Avalonia12}/` —— 每个适配层提供同名 `FluidBackgroundControl` 控件，含 `Config` / `IsAnimationEnabled` / `MaxFps` 依赖属性（Avalonia 为 AvaloniaProperty）。
- `Samples/FluidBackground.Sample.*/` —— 各平台可运行示例，引用 Core 与对应适配层。

## 约定

- 公开 API 的 XML 文档注释一律使用**简体中文**；标识符使用英文。
- csproj 基线（Core 与适配层）：`Nullable` enable、`ImplicitUsings` enable、`LangVersion` 12.0、`AllowUnsafeBlocks` true。
- `FluidConfig.Clone()` 深拷贝配置；渲染器内部持有私有副本。
- 门面与渲染器的公开方法一律先 `ObjectDisposedException.ThrowIf` + 参数校验，再在 `lock` 内变更状态。
- 新增后端：实现 `IFluidRenderer`，并在 `FluidRenderer.InitializeRenderer` 的 `RenderMode` 分支中注册。
- 修改着色器时需同步更新 `fluid.frag`（OpenGL）与 `fluid.sksl`（Skia）两处。
- 新增适配层：新建 `FluidBackground.<框架>` 项目并实现 `FluidBackgroundControl`，增加对应 Sample，并把两者注册进 `FluidBackground.slnx`（WinUI 条目需 `Platform="x64"`）。

## 备注

-
