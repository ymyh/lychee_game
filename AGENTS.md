## 项目概述

lychee_game 是基于 [lychee](../lychee) ECS 框架的 2D 游戏项目（.NET 10 / C# 14），渲染层直接调用 SDL3 GPU API，不使用现成的游戏/图形引擎。ECS 用法详见 `../lychee/README.md`。

当前处于早期阶段：

- 只有 2D 渲染管线；可运行程序为 `lychee_game.Demo`（图形 Demo）与 `lychee_game.Tests`（手写控制台验证程序，不是测试框架）
- 渲染已在 Demo 上逐帧跑通（Vulkan）；D3D12/DXIL 后端已接好，但窗口默认使用 Vulkan
- 工作树中还有未提交的重构（`ConditionalSchedule` 抽象化等）

## 依赖说明（重要）

解决方案通过相对路径引用外部项目，构建要求本机存在 `E:\C#\FNA` 与 `E:\C#\lychee`：

| 引用 | 用途 |
| --- | --- |
| `..\..\FNA\FNA.csproj` | 只使用其内嵌的 SDL3-CS binding（namespace `SDL3`），不使用 `Microsoft.Xna.Framework.*` 的任何 API |
| `..\..\lychee\lychee\lychee.csproj` | ECS 框架核心（App / System / Schedule / Resource / Entity） |
| `..\..\lychee\lychee_sg\lychee_sg.csproj` | 源生成器，作为 Analyzer 引用，负责 `[AutoImplSystem]` / `[Component]` 的代码生成 |

引用 FNA 是刻意为之：FNA 内嵌的 SDL binding 维护积极，本项目通过它使用 SDL3。不要以“代码里没用到 FNA”为由移除该引用或替换成其它 SDL3 binding。

## 构建与运行

- 构建：`dotnet build lychee_game.sln`
- 运行 Demo：`dotnet run --project lychee_game.Demo`
- 验证：`dotnet run --project lychee_game.Tests`
- 着色器：`lychee_game/shaders/compile.bat` 用 slangc 把 `default.slang` 编译为 SPIR-V（Vulkan）与 DXIL（D3D12）；产物已提交并以 EmbeddedResource 打包，不重新编译也能构建
  - DXIL 需要版本配对的 dxc / dxil.dll：脚本优先使用 Windows SDK 的配对；旧 dxc 配新 dxil.dll 会因 PSVRuntimeInfoSize 不匹配而校验失败

## 目录结构

- `lychee_game/` 主库
  - 根目录：`LycheeGamePlugin`（游戏循环 schedule）、`BasicTimePlugin`、`BasicRenderPlugin`、`Exceptions`
  - `components/2d/`、`resources/2d/`、`systems/2d/`：2D ECS 数据与渲染系统
  - `components/`、`resources/`：通用定义，如 `Transform`（3D，暂无系统）、`GpuDevice`、`Window`、`Time`
  - `schedules/`：`ConditionalSchedule` / `FireOnceSchedule` / `FixedIntervalSchedule`
  - `shaders/`：Slang 源码（`default.slang`）+ 编译脚本 + 已编译 SPIR-V / DXIL
- `lychee_game.Demo/` 图形 Demo（可运行入口程序）
- `lychee_game.Tests/` 控制台验证程序

## 架构要点

### 插件与 Schedule

`LycheeGamePlugin` 提供 10 个 schedule：StartUp / First / Input / FixedUpdate（默认 20ms，最多追帧 5 次） / Update / PostUpdate / Render / RenderTransparency / RenderUI / Last。
`BasicTimePlugin` 与 `BasicRenderPlugin` 安装时都要求 `LycheeGamePlugin` 已安装，否则抛 `PluginRequirementException`。

System 一律通过 `[AutoImplSystem]` + `Execute` 方法声明，参数由源生成器注入（组件、`[Resource]` 资源）。

### 2D 渲染管线

- `Render` schedule：BeginFrameSystem → CameraUpdateSystem → SpriteRecordSystem → SpriteInstanceUploadSystem → BeginRenderPassSystem → SpriteSubmitSystem；EndFrameSystem 挂在 `RenderUI` schedule
- BeginFrameSystem：获取 command buffer + swapchain、准备 depth texture，并上传待处理的 Mesh/Texture（copy pass 不能嵌套在 render pass 内）
- SpriteRecordSystem：对含有 Sprite2D + Transform2D + ZIndex 的实体，把 Material 解包为 Effect + SamplerState，写入 RenderQueue
- SpriteInstanceUploadSystem：把每 sprite 的实例数据（World + UvRect + Tint，96 字节）上传到 `SpriteInstanceBuffer`
- SpriteSubmitSystem：按 Effect/Texture/Mesh/Sampler 分批做 GPU 实例化绘制；PipelineCache 复用 pipeline（含 depth-stencil 状态）；每批 PushGPUVertexUniformData 推送 64 字节 ViewProjection
- BeginRenderPassSystem：按 `RenderContext.DepthStencil` 设置开始 render pass（clear 为黑色）
- EndFrameSystem：EndGPURenderPass + SubmitGPUCommandBuffer（含隐式 present）+ 清空 queue/instances
- 顶点布局：位置(FLOAT2) + UV(FLOAT2) + 颜色(UBYTE4_NORM)；实例数据走第二个 vertex buffer（INSTANCE rate，location 3-8）
- 着色器 binding 遵循 SDL3 约定：顶点 uniform 在 set 1，片元 sampler 在 set 2（DXIL 侧为 register b0/space1、t0/space2）
- 相机：原点居中，+Y 向上，正交投影

### 资源池不变量（勿破坏）

`Mesh2DList` / `Texture2DList` / `EffectList` / `MaterialList` 的 slot 0 固定为默认资源：UnitQuad / WhiteTexture / DefaultEffect / DefaultMaterial。ref 的默认值 `(Index=0, Generation=0)` 恰好命中 slot 0，因此 `Sprite2D` 等组件不显式赋值即使用默认资源。`BasicRenderPlugin.CreateDefaultResources` 中“Mesh→Texture→Effect→Material”的创建顺序是该不变量的前提。

## 代码规范

沿用 lychee 仓库 `.agent/codeStyle.md`：region 分类、私有字段不加下划线、public 成员写英文 XML 注释、条件语句必须带大括号、命名空间与目录结构一致等。

## 当前现状与注意点

- 只有 2D；`components/Transform.cs` 等 3D 组件已定义但没有对应的 3D 系统
- 窗口默认 `RenderingBackend.Vulkan`；D3D12/DXIL 路径已就绪（`DefaultShaders` 按 `device.ShaderFormat` 选择 `.spv`/`.dxil`），但暂无选择后端的入口
- 修改 shader 后需手动运行 `shaders/compile.bat` 重新生成并提交四份产物
