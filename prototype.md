# 2D 基础渲染：缺失的功能清单（prototype 讨论稿）

> 2026-10-04 整理。基于当前工作树（含未提交的 `ConditionalSchedule` 重构）。
> 用途：功能排期与设计的讨论底稿，不是最终设计。

## 0. 现状速览

已有：Sprite2D（Mesh/Material/Texture/UV/Tint）+ Transform2D + ZIndex → RenderQueue（Effect/Texture/Mesh/Sampler 分桶 + GPU 实例化）→ 单相机单 pass → swapchain。资源池 ref + slot0 默认资源不变量。

可以说「一个 sprite 怎么画出来」这条链路已经通了，缺的全是**游戏真正开始用之后马上会撞上的东西**。按「不做就会卡住」的程度排三档。

| # | 功能 | 优先级 | 成本 | 主要落点 |
| --- | --- | --- | --- | --- |
| 1 | 可见性开关（Visible） | P0 | 低 | `Sprite2D` / `SpriteRecordSystem` |
| 2 | 从文件加载纹理（PNG/BMP） | P0 | 低 | `Texture2DList` 新增 Load |
| 3 | 锚点 Anchor + 尺寸 Size | P0 | 低 | `Sprite2D` / `SpriteRecordSystem` |
| 4 | 绘制顺序模型修正（ZIndex × 批处理） | P0 | 低 | `RenderQueue.Sort` |
| 5 | 视锥剔除 | P1 | 低 | `SpriteRecordSystem` |
| 6 | ScreenToWorld / WorldToScreen | P1 | 低 | `Camera2D` / `RenderContext` |
| 7 | FlipX / FlipY | P1 | 低 | `Sprite2D` / `SpriteRecordSystem` |
| 8 | 混合模式（Additive 等） | P1 | 中 | `Material` / `PipelineCache` |
| 9 | SpriteBundle（DX） | P1 | 低 | 新文件 |
| 10 | Transform 父子层级 | P1 | 高 | 新系统 + 新组件 |
| 11 | 多相机 / 分层 + 屏幕空间 UI | P2 | 高 | 结构改动 |
| 12 | 图集 / 帧动画 / 文字 / 九宫格 | P2 | 中 | 新组件/资源 |
| 13 | Render-to-Texture / 后处理 | P2 | 高 | 管线扩展 |
| 14 | Mesh2D 强制索引（删掉非索引路径） | P1 | 低 | `Mesh2D` / 上传与提交系统 |

---

## 1. P0 详细说明

### 1.1 可见性开关

**现状**：没有任何开关。想隐藏只能把 `Tint` 的 alpha 设 0（照样上传实例、照样进批次），或者删除/添加组件（archetype 迁移，lychee README 自己说这个操作 cache 不友好）。

**方案对比**：

| 方案 | 切换成本 | 语义 | 对现有代码 |
| --- | --- | --- | --- |
| A. `Sprite2D.IsVisible` bool 字段（默认 true） | 最低 | 数据开关 | 零破坏 |
| B. `Visible` marker 组件（`All` 包含） | 高（archetype 迁移） | 显式声明才渲染 | 破坏：旧代码不加组件就不画 |
| C. `Hidden` marker 组件（`None` 排除） | 高（archetype 迁移） | 默认渲染，显式隐藏 | 零破坏 |
| D. `Visibility` 枚举字段（Visible / Hidden / Inherit） | 低 | 为层级预留 Inherit 语义 | 零破坏 |

**建议**：A 或 D。
- 如果近期就做父子层级（第 10 项），直接上 D——以后「父节点隐藏 → 子节点跟着隐藏」不需要推倒重来；
- 否则 A 最简单。B/C 的 marker 方案适合编辑器/低频操作，但受击闪白、粒子池这类高频开关每次都要动 archetype，不合适。

**实现**：`SpriteRecordSystem.Execute` 里提前 return（检查顺序放在算矩阵之前）。注意与剔除（1.5）区分：**可见性是逻辑状态，剔除是纯性能优化**，两者都 return 但统计要分开。

**落点**：`lychee_game/components/2d/Sprite2D.cs`、`lychee_game/systems/2d/SpriteRecordSystem.cs:18`

---

### 1.2 从文件加载纹理

**现状**：`Texture2D` 只能从内存 `byte[]` 构造（`Texture2DDesc.Data`），整个工程唯一一张图是 1×1 白纹理。没有任何按路径加载图片的入口——**美术资源进不来**，这是当前最硬的一个缺口。

查过依赖：FNA 内嵌的 SDL3-CS 只绑了 `SDL_LoadBMP`（`lib/SDL3-CS/SDL3/SDL3.Core.cs:1712`），没有 SDL3_image。

**方案**：
- a. `SDL_LoadBMP`：零新依赖，立刻可用，但只有 BMP（美术流程难受），适合当第一步；
- b. **StbImageSharp**（纯托管 NuGet，PNG/JPG/BMP/TGA，输出 RGBA8 正好匹配现有上传路径）——推荐；
- c. 绑 SDL3_image：功能最全，但多一个 native dll 要部署，后置。

**建议**：先定接口 `Texture2DList.Load(string path)`（内部换成 a 实现打通链路），随后把实现换成 b，接口不变。顺带让加载结果带上宽高（1.3 的默认 Size 需要）。

**落点**：`lychee_game/resources/2d/Texture2D.cs`

> 顺带：大图同步解码/上传会卡帧，异步加载可以更晚再做，但接口命名别堵死（`Load` vs `LoadAsync`）。

---

### 1.3 锚点 Anchor + 尺寸 Size

**现状**：UnitQuad 是中心原点、边长 1 的 quad，尺寸靠 `Scale`（Demo 里 `Scale=(100,100)` 表示 100 像素，隐含「世界单位≈像素」的约定）。没有锚点——UI 想要左上角对齐，要手动补半个尺寸的偏移，稍微旋转/缩放就不好算。

**建议**：
- `Sprite2D` 加 `Vector2 Size`（默认取纹理像素尺寸，UnitQuad/白纹理时退回 (1,1)）；`Scale` 保持为乘数，语义不重叠；
- `Sprite2D` 加 `Vector2 Anchor`（0..1，或 enum 九宫格）。实现上作为局部平移乘在矩阵最右侧：

```csharp
// 顶点在 [-0.5, 0.5]^2；把锚点 a 对齐到原点：v' = v + (0.5 - a)
world = T(pos) * R * S(scale) * S(flip) * T(0.5 - anchor)
```

这样锚点在旋转/缩放下的行为天然正确（绕锚点旋转），**不需要改 mesh、不增加 draw call**。

**落点**：`lychee_game/components/2d/Sprite2D.cs`、`SpriteRecordSystem.cs`（组装 world 时）

---

### 1.4 绘制顺序模型修正：ZIndex × 批处理的矛盾

**现状**（`RenderQueue.Sort`，`lychee_game/resources/2d/RenderQueue.cs:91`）：

```
排序键 = Effect.Index → Texture.Index → ZIndex
```

问题不止一个：

1. **ZIndex 只在同 Effect + 同 Texture 的组内有效**。两个不同纹理的 sprite，无论 Z 怎么设，先后由纹理索引决定——对 painter's algorithm 的 2D 引擎这是正确性问题，而且是那种「某天换了张图顺序突然就错了」的隐蔽问题。
2. **排序键不完整**：没比 `Generation`（同索引新老资源混排）；`Mesh` / `Sampler` 没参与排序但参与 `SameBatch`，相同键的项交错时会多拆批。
3. **`List<T>.Sort` 不稳定**：Z 相同、状态相同的两个 sprite 相对顺序无定义，重叠时可能出现帧间闪烁（取决于 archetype 迭代顺序）。

**方向选择**：

| 模型 | 键 | 效果 |
| --- | --- | --- |
| z 主导 | (ZIndex, Effect, Texture, Mesh, Sampler, 入队序号) | 正确性优先；z 交错时碎批 |
| 层主导 | (SortingLayer, ZIndex, 状态键, 序号) | 正确 + 分层；层内允许碎批 |
| 状态主导（现状） | (Effect, Texture, ZIndex) | 批处理最优，有坑 |

**建议**：短期切到 **z 主导**（顺带把序号 tie-break 加上，保证确定性；序号在 `Enqueue` 时分配即可）；等第 11 项（UI 分层）时升级为「层主导」——加一个 `SortingLayer`（byte）做主键，UI/世界自然分开，层内仍能吃到批处理。Demo 想保持单批就给同 z。

**落点**：`RenderQueue.cs`（`DrawRecord` 加 Seq 字段 / `Sort()` 重写）

---

## 2. P1 详细说明

### 2.1 视锥剔除

全屏可见的 sprite 才进队列。2D 实现很便宜：
- 相机世界矩形：`SwapchainWidth/2/Zoom × SwapchainHeight/2/Zoom`（有旋转取外接 AABB，或者干脆 OBB 测试）；
- sprite 包围盒：局部 AABB（mesh bounds，UnitQuad = [-0.5,0.5]²）经 world 变换四角后取 min/max；
- `SpriteRecordSystem` 提前 return；`RenderContext` 加 `CulledCount` 统计便于调优。

注意：`Mesh2D` 目前没有 bounds 字段，需要就地算一次并缓存（或 UnitQuad 特判）。剔除与可见性分开统计。

**落点**：`SpriteRecordSystem.cs`、`Mesh2D.cs`（加 Bounds）

### 2.2 ScreenToWorld / WorldToScreen

鼠标拾取（点选 sprite、放置、拖拽）没有可用 API。`RenderContext.ViewProjection` 已有，缺的是逆变换工具：

```csharp
public Vector2 ScreenToWorld(Vector2 screenPx, uint viewportW, uint viewportH);
public Vector2 WorldToScreen(Vector2 world, uint viewportW, uint viewportH);
```

两个坑写进注释：SDL 窗口坐标 y 向下 vs 世界 y 向上；high-DPI 下窗口逻辑坐标与 swapchain 像素坐标的比例（`SDL_GetWindowSize` vs `SDL_GetWindowSizeInPixels`）。顺手把「相机可视世界矩形」暴露出来，2.1 直接复用。

**落点**：`Camera2D.cs`

### 2.3 FlipX / FlipY

负 Scale 能做但语义混乱（旋转后镜像方向不对、和图集/锚点组合要小心）。给 `Sprite2D` 加两个 bool，在矩阵里乘 `S(flip)`（位置见 1.3 的公式，在锚点平移之前），镜像自然发生在锚点处。

### 2.4 混合模式（Additive 等）

`PipelineKey.BlendEnabled` 恒 true、blend state 写死标准 alpha 混合（`PipelineCache.cs:188-201`），没有 additive——发光、粒子、命中闪烁这些常见效果做不出来。
- `Material` 加 `BlendMode { Alpha(默认), Additive, Opaque }`；
- `PipelineKey` 把 `bool BlendEnabled` 换成 `BlendMode`，`PipelineCache` 按枚举生成 blend_state（Additive: `SRC_ALPHA`/`ONE`）；
- `DrawRecord` 解包时带上，进 `SameBatch` 和排序键。

shader 不用动（固定功能混合）。

### 2.5 SpriteBundle（DX）

现在生成一个 sprite 要手动加 3 个组件（以后 4 个），漏一个就不渲染，而且不报错。lychee 已有 `IComponentBundle`，包一个：

```csharp
public struct SpriteBundle : IComponentBundle
{
    public Sprite2D Sprite;
    public Transform2D Transform;
    public ZIndex ZIndex;
}
```

**落点**：`lychee_game/components/2d/` 新文件。

### 2.6 Transform 父子层级

**为什么重要**：组合对象（角色带武器、UI 面板整组移动）、整树显隐/缩放都依赖它，也是 UI 系统的根基。当前 Transform2D 只算局部矩阵，没有 parent 概念。

**可行性**：`lychee.EntityRef` 是纯 unmanaged struct（`Entity.cs:165`，只有 ID + Generation），可以直接塞进 `[Component]`。设计草稿：
- `Parent { EntityRef Value }`（可选组件）；`Transform2D` 保持局部；
- 新增计算产物 `WorldTransform2D { Matrix4x4 Value }`；
- 新增 `TransformHierarchySystem`，排在 `SpriteRecordSystem` 之前：无父节点先算，再自顶向下传播。

**难点**：lychee 没有内置 children/关系存储，且 `[Component]` 要求 unmanaged（普通 `List<EntityRef>` 放不进去）。两个可选实现：
- a. 只存 `Parent`：每帧构建一次「父 → 子列表」的临时映射（托管字典，注意分配），或每实体沿 parent 链向上累乘（带缓存，浅树够用）；
- b. 在 lychee 框架层加动态数组组件/关系支持（大活，跨仓库）。

**建议**：先做 a 的原型验证成本；这项独立立项，别混在渲染小修里做。它同时解锁 1.1 里 `Visibility.Inherit` 的完整语义。

---

## 3. P2（迟早要做，先列着）

- **多相机 / 分层 + 屏幕空间 UI**：目前单 MainCamera、单 pass，UI 只能跟世界相机（左上角原点很别扭）。`RenderUI` schedule 已有占位（`EndFrameSystem` 挂在那里），说明方向已经留好了。做法：`SortingLayer` + 相机 culling mask，或独立的 UI pass。
- **图集**：`UVRect` 已有，缺「加载图集 + 按帧名查询」的 helper。批处理按纹理拆批，图集直接决定 draw call 数量，是 2D 性能的关键前置。
- **帧动画**：`UVRect` 帧序列 + fps + 循环的组件 + Update 系统推进，依赖图集。
- **文字**：bitmap font（内置 ASCII 起步）→ SDL_ttf。做游戏绕不开。
- **九宫格**：UI 必备。
- **Render-to-Texture / 后处理**：像素风「低分辨率渲染再放大」、Bloom、截图都依赖它。当前直接渲染到 swapchain，属于管线扩展。
- **调试可视化**：sprite 包围盒、UV、被剔除对象高亮——调试剔除/排序问题的效率工具。

---

## 4. 顺带发现的小问题（非功能）

1. `EndFrameSystem` 每帧 `Console.WriteLine`（`EndFrameSystem.cs:25`），正式跑应受 `DebugMode` / 日志等级控制。
2. `RenderQueue.Sort` 排序键缺 `Generation`（1.4 一并修）。
3. `SpriteRecordSystem` 中 Material 解析失败静默 `return`（`SpriteRecordSystem.cs:24-27`）——sprite 凭空消失且无任何提示，建议 `Debug.Assert` 或计数警告。
4. 无剔除时 `SpriteInstanceBuffer` 的扩容策略值得看一眼：实体数上万时是每帧全量上传 96B/个。

---

## 5. 设计修正 / API 简化

### 5.1 Mesh2D 必须带索引（`Indexed` 恒为 true）

**想法**：`Mesh2DDesc` 强制要求提供 `Indices` 且长度 > 0，为空直接抛异常。即非索引绘制路径整体删掉。

**现状**：`Mesh2DDesc.Indices` 默认为空数组，注释写明「Null or empty means non-indexed drawing」（`Mesh2D.cs:19-21`）；`Mesh2D.Indexed` 是运行时才确定的开关（`Mesh2D.cs:46`）。上传与提交侧为了这条路径各留了一个分支：

- `BeginFrameSystem.cs:102,151`：`if (mesh.Indexed && mesh.IndexData != null)` 才建并上传索引缓冲；
- `SpriteSubmitSystem.cs:215,244`：索引 / 非索引两条绑定与绘制分支（`DrawGPUIndexedPrimitives` vs `DrawGPUPrimitives`）。

**理由**：

- 2D 网格几乎总是索引的（quad = 4 顶点 + 6 索引），非索引意味着顶点重复，没有保留价值；
- 强制索引后 `Mesh2D` 的 API 变成全函数：`IndexData` 非空、`IndexCount > 0`、`Indexed` 属性可以整个删掉；
- 一次消除三处分支和一类「忘了写 Indices → 静默画错」的坑。唯一可想象的例外（fullscreen triangle 之类）在 2D 场景不值得为它保留整条路径。

**影响面**（现有调用方全都已提供索引，实际无行为变化）：

- `Mesh2D.cs`：构造器改为空 `Indices` 抛 `ArgumentException`（null 本来就抛）；删除 `Indexed` 属性；`IndexData` 改非空类型、去掉 `?`；
- `BeginFrameSystem.cs` / `SpriteSubmitSystem.cs`：删掉非索引分支；
- `lychee_game.Tests/Program.cs:107-109`：打印 `Indexed` 和 `IndexData?.Length ?? 0` 的两行跟着调整。

**成本**：低。趁调用方还少（只有 UnitQuad 和 Tests），越早收紧越省事。

---

## 6. 建议落地顺序

1. **第一波（小而高价值）**：可见性开关 → 排序修正 → 纹理文件加载 → Anchor/Size。做完这四件，做一个「有真实美术资源的游戏原型」就不缺零件了。
2. **第二波（表现力/性能）**：剔除 → ScreenToWorld → Flip → 混合模式 → SpriteBundle。
3. **第三波（结构性）**：父子层级 → 多相机/UI 分层。
4. **第四波（内容工具）**：图集 → 帧动画 → 文字 → 九宫格 → 后处理。
5. **顺手整理**：Mesh2D 强制索引（5.1）成本低，跟着第一或第二波一起做掉。
