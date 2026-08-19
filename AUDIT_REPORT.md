# 潮汐微光 / Shallow Sea Dream — 现状审计

审计日期：2026-08-18  
范围：`shallow-sea-dream-cs` 的构建、运行、视觉、交互、素材、可访问性和发布准备度。

> 2026-08-18 修复复核：C1、C2、H1、H2、H4 及动画切片/漂移问题已处理。C2 的根因是 `LogPanel` 只隐藏日志卡片、未隐藏 78% 全屏遮罩；现已同步控制遮罩可见性。当前 `dotnet build` 为 0 errors / 0 warnings，Godot 导入与窗口化录像均通过。H3（音频）、H5（完整设置/无障碍）、文档与 builder 漂移仍属于后续非发行型作品集工作。

## Anti-Patterns Verdict

**未通过 AI Slop Test。** 标题画面有完成度，但整套 UI 仍大量使用“深色背景 + 青色霓虹 + 玻璃面板 + 胶囊按钮 + 居中排版”。这些元素被同时、重复地用在标题、教程、HUD、对话和弹窗上，使各界面缺少叙事身份，看起来像同一个 AI UI 模板的不同尺寸版本。`UiTheme.cs` 甚至把 glass/pill/glow 作为全局设计语言固化。

应该保留海底绘本气质和圆润中文字体，但把视觉重心从发光玻璃框转向手绘素材、沉积层次、纸张/贝壳形状、潮汐轨迹与更安静的留白。

## Executive Summary

- 总计：**2 Critical / 5 High / 5 Medium / 3 Low**。
- 当前质量分：**52/100**。架构和叙事基础强，但关卡可读性、相机构图、素材使用和发布完整性未达可交付标准。
- 最严重问题：
  1. 起始相机显示大面积地图外空区，关卡主体只占画面右侧。
  2. 实际关卡与对话 HUD 严重偏暗，文字、角色、目标难以识别。
  3. HUD 启动时产生 Godot 布局警告，部分尺寸会被锚点覆盖。
  4. 已有 27 个最终运行素材、外部 141+ 个媒体素材，但大量仍未进入游戏。
  5. 项目素材被 `.gitignore` 整体忽略，当前仓库没有可复现的提交基线。

## Detailed Findings by Severity

### Critical

#### C1 — 起始相机未限制在 3600×1080 地图内

- **位置：** `scenes/game_scene1.tscn:55`、`scripts/GameSceneController.cs:19-22`
- **类别：** Gameplay / Responsive / Composition
- **描述：** 玩家出生在 `(220, 540)`，Camera2D 没有 `LimitLeft/Top/Right/Bottom`。1920×1080 下相机向地图左侧露出约 740px 的地图外区域。
- **影响：** 玩家第一眼看到的关卡主体被挤到右侧，任务、NPC、碎片和环境空间关系失真；地图外空区看起来像严重渲染错误。
- **建议：** 设置相机边界并开启 position smoothing；出生时把相机钳制到左上区域，同时检查 16:9、16:10、超宽屏。
- **建议命令：** `/adapt`、`/arrange`、`/harden`

#### C2 — 实际关卡和对话状态对比度不足

- **位置：** `scripts/GameSceneController.cs:122-211`、`scripts/HudController.cs:80-335`、`scripts/DialogueRunner.cs:50-110`
- **类别：** Accessibility / Visual hierarchy
- **描述：** 实际 1920×1080 录像中，世界、HUD、对话文字整体接近深海背景亮度；任务和对话内容难以快速读取。标题画面在同一录制路径下正常，因此不是单纯的录像工具问题。
- **影响：** 核心故事、任务进度和角色互动不可读，阻断关卡推进；不满足普通文本 WCAG AA 4.5:1 的目标。
- **建议：** 移除或校准全局 CanvasModulate；把对话/关键 HUD 提升到独立高对比层；减少同时可见的 HUD 面板；用暖灰蓝文字而不是低透明青色；加入运行时对比度截图检查。
- **建议命令：** `/colorize`、`/typeset`、`/quieter`

### High

#### H1 — HUD 启动产生锚点/尺寸冲突警告

- **位置：** `scripts/HudController.cs:64-72`，运行时堆栈指向 `MakePanel()`
- **类别：** Responsive / Reliability
- **描述：** 对非等值对向锚点的 Control 直接设置 `Size`，Godot 明确警告 `_Ready()` 后尺寸会被覆盖。
- **影响：** HUD 在不同分辨率和 stretch 模式下可能错位、裁切或覆盖。
- **建议：** 对 Wide preset 使用 offset 控制，对定宽角落面板使用同侧 anchors；不要混用互斥的 `Size` 与 stretch anchors。
- **建议命令：** `/adapt`、`/arrange`

#### H2 — 素材存在但没有形成关卡视觉系统

- **位置：** `scripts/GameSceneController.cs:122-211`、`scripts/GameSceneController.cs:270-330`、`scripts/BossController.cs:110-123`
- **类别：** Visual design / Asset utilization
- **描述：** `zone_shallows/sediment/depths.png`、`maze_map.png`、`wall_tile.png`、三种 invader 等素材未用于主关卡；汽油桶和最终电元素掉落仍是 procedural blob。三个区域目前主要是色块和矩形墙。
- **影响：** 76 张项目图片和外部素材库没有转化为世界细节，关卡比标题画面低一个明显完成度等级。
- **建议：** 以三张 zone background 为每区主景；wall tile/洞穴 tiles 做碰撞边缘；invader 做无伤害环境污染体；真实 barrel/boss/NPC/memory/note 全部接入；程序图形只保留为引导和反馈层。
- **建议命令：** `/normalize`、`/colorize`、`/delight`

#### H3 — 音频系统完整但运行素材为零

- **位置：** `scripts/AudioManager.cs`、`ASSET_MANIFEST.md:69-70`
- **类别：** UX / Feedback
- **描述：** 代码覆盖标题音乐、环境音乐和 14+ SFX，但 `assets/` 下没有任何 OGG/WAV/MP3。
- **影响：** 收集、净化、受伤、任务推进和剧情切换都缺少反馈，体验显得未完成。
- **建议：** 优先加入一条可循环环境音、标题主题、收集/互动/净化/受伤/UI 五组短音效；记录许可证并保留无音频时的 graceful fallback。
- **建议命令：** `/delight`、`/polish`

#### H4 — 发布仓库不可复现

- **位置：** `.gitignore:1-8`、Git 状态
- **类别：** Ship readiness
- **描述：** C# 工程当前没有提交，所有源文件均为 untracked；`.gitignore` 还忽略整个 `assets`，导致即使提交代码也不会包含游戏素材。
- **影响：** 换机、归档或构建导出时会得到缺素材项目，无法回滚改造。
- **建议：** 先建立安全基线提交；只忽略 `_raw`、生成帧、导入缓存和 screenshots，不要忽略最终 runtime assets。
- **建议命令：** `/harden`

#### H5 — 设计稿承诺的可访问性设置没有移植

- **位置：** `DESIGN_BRIEF.md:273-275`；`scripts/` 中无对应实现
- **类别：** Accessibility
- **描述：** C# 版缺少 text scale、reduced motion、colorblind mode 和可持久化设置界面。
- **影响：** 小字、动态效果和以颜色区分元素的机制对部分玩家不可用。
- **建议：** 实现 SettingsManager + 设置页；至少完成字号缩放、降低动态和高对比模式，并让键盘/手柄可完整操作。
- **建议命令：** `/harden`、`/adapt`

### Medium

#### M1 — UI 设计语言过度依赖 glass/pill/glow

- **位置：** `scripts/UiTheme.cs:44-137`
- **类别：** Theming / Anti-pattern
- **描述：** 所有面板和按钮共享玻璃、霓虹描边和胶囊外形，层级主要靠发光强度而不是构图、字体和内容密度建立。
- **影响：** 标题、教程、HUD、对话和日志缺少场景区分，故事书气质被“科技面板”压过。
- **建议：** 保留少量玻璃用于暂停/日志；任务采用水流标签，对话采用非对称贝壳/纸片轮廓，技能栏采用实体图标槽。
- **建议命令：** `/normalize`、`/distill`、`/bolder`

#### M2 — 使用 Bounce/Back 动画

- **位置：** `scripts/HudController.cs:308-313`、`scripts/UiFx.cs:73-90`、`scripts/TutorialController.cs:177`
- **类别：** Motion
- **描述：** 完成、按钮与变形使用 bounce/back easing，与温柔沉静的环保寓言气质不一致。
- **影响：** 动效显得玩具化、模板化；也没有 reduced-motion 路径。
- **建议：** 改为 160–280ms ease-out-quart/quint，重要净化时刻使用一次缓慢扩散而不是弹跳。
- **建议命令：** `/animate`、`/quieter`

#### M3 — 调试快捷键和绝对路径仍在运行代码

- **位置：** `scripts/GameSceneController.cs:48-61`
- **类别：** Code quality / Portability
- **描述：** F12 会写死到 `/Users/shawj/Desktop/.../_capture.png`。
- **影响：** 换机失效，生产输入路径中残留诊断逻辑。
- **建议：** 移到 `#if DEBUG`，写入 `user://captures/` 或专用 test scene。
- **建议命令：** `/harden`、`/polish`

#### M4 — Builder、场景和文档存在事实漂移

- **位置：** `scenes/Build*.cs`、`STRUCTURE.md`、`ShallowSeaDream.csproj`
- **类别：** Maintainability
- **描述：** Builder 注释仍称 placeholder，当前结构文档称 `net10.0`，csproj 实际为 `net8.0`；文档又称 scene builder 在本机不能 headless 执行。
- **影响：** 后续 Godogen 可能从错误来源重建并覆盖现状。
- **建议：** 明确 `.tscn` 或 builder 哪个是 source of truth；修正文档和 TFM；给生成流程加单一脚本。
- **建议命令：** `/extract`、`/harden`

#### M5 — 关卡素材尺寸和加载成本偏高

- **位置：** `assets/img/*.png`、`assets/fonts/AaShuiyu.ttf`
- **类别：** Performance
- **描述：** 多张 2752×1536 PNG 为 4–6MB，字体 5.7MB，项目还保存 `_raw` 和逐帧中间产物。
- **影响：** 导入、启动、内存和最终包体不必要地增大。
- **建议：** runtime 与 source assets 分离；生成 WebP/优化 PNG；为背景设置合理导入压缩；中间帧不进入导出。
- **建议命令：** `/optimize`

### Low

#### L1 — 标题副标题在 1920×1080 下过小

- **位置：** `scripts/TitleController.cs`
- **类别：** Typography
- **影响：** 中英文副标题无法在普通观看距离快速读取。
- **建议：** 提高副标题尺寸/字距，减少厚描边字体在正文中的使用。

#### L2 — 任务、图鉴、状态同时常驻，信息密度偏高

- **位置：** `scripts/HudController.cs`
- **类别：** Information architecture
- **影响：** 探索和环境素材被四角 HUD 包围，对话期间更加拥挤。
- **建议：** 常驻只保留当前目标、生命和当前技能；图鉴/日志按需展开。

#### L3 — 缺少 C# 自动化测试

- **位置：** `shallow-sea-dream-cs` 全局
- **类别：** Reliability
- **影响：** 目标链、存档迁移和剧情 flag 容易在改造时回归。
- **建议：** 先为 ObjectiveManager、SaveManager、MemoryLog 和技能伤害规则建立无渲染测试。

## Patterns & Systemic Issues

- 视觉系统和素材系统脱节：标题使用完整插画，游戏内却大量 procedural rectangle/blob。
- HUD 依赖固定像素和预设锚点混用，响应式基础不可靠。
- “温柔绘本”叙事与“深色霓虹玻璃”UI 表达冲突。
- 文档、builder、直接维护的 `.tscn` 三套来源并存，长期会漂移。
- 可访问性在原设计中被记录，但未进入当前 C# 可运行路径。

## Positive Findings

- `dotnet build` 当前为 **0 errors / 0 warnings**。
- 事件总线、Player、SkillSystem、ObjectiveManager、HUD、DialogueRunner、MemoryLog 已合理拆分，优于原版 god-class。
- 标题背景、角色、NPC、Boss、三种区域、记忆和残片素材已经具备较完整的手绘视觉基础。
- 缺音频和缺图时有 graceful fallback，不会因为单个资源缺失而崩溃。
- 键盘和手柄输入均已定义，标题按钮有 focus 状态。
- 叙事主题清晰，“净化而非击杀”的用词系统值得保留。

## Recommendations by Priority

1. **Immediate：** 修相机边界、关卡亮度、对话对比度和 HUD 布局警告；建立可回滚基线。
2. **Short-term：** 接入三张区域背景、真实桶/NPC/Boss/污染体/元素图标；重新组织 HUD；补齐核心音频。
3. **Medium-term：** 实现设置和可访问性、清理 debug 与中间素材、统一 builder/tscn 来源、补自动化测试。
4. **Long-term：** 完整通关录制、手柄流程验证、导出包体优化和第二海域扩展。

## Suggested Commands for Fixes

- `/normalize`：把标题的手绘美术语言延伸到关卡和所有 UI。
- `/colorize` + `/typeset`：解决对比度和正文可读性。
- `/arrange` + `/adapt`：修相机、HUD 层级和多分辨率布局。
- `/harden`：修复布局警告、存档、设置、绝对路径和发布复现问题。
- `/optimize`：分离 runtime/source assets 并降低贴图/字体开销。
- `/polish`：完成统一动效、反馈、文案、音频和最终逐屏检查。
