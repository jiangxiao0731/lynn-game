# 潮汐微光 / Shallow Sea Dream

Godot 4.7 Mono / C# 的三章节海洋保护作品集 Demo。玩家扮演微光水母，聆听受污染海域居民的线索，在迷宫中用水、冰、电恢复潮流、解冻育场、重连安全回路，并净化而非击杀受困的海洋守护者。

## 当前完成度

- 第一章「潮湾苗圃」保留三段有机迷宫；第二章「霜骨海沟」加入三座解冻锚与冰元素；第三章「断流灯塔」加入三座潮汐继电器、电元素与完整结局。
- 每章 3 个渐进海域、12 个唯一元素拾取（目标 8 个）、可恢复捷径、章节守护者与可见的污染前后对比。
- 8 枚跨区域潮汐记忆、10 段记忆文本、8 个环境残片、6 位可交谈角色。
- 污染生物与遮罩会随浅滩、沉积带、巢母深处的净化进度依次退场。
- 三位守护者各需要 4 次对应元素引导：水 → 冰 → 电；第一章解锁冰，第二章解锁电，第三章点亮海底灯塔。
- 修复日志遮罩常驻、HUD 锚点警告、碎片累计漂移、精灵图集网格错误、过冲/弹跳动效和全屏暗化。
- 现有水母原始图集在运行时做非破坏式柔和色键；没有生成或改写新素材。

目标完整游玩约 12–18 分钟；建议剪成 2–3 分钟作品集视频。录制时依次展示第一章水净化、第二章解冻锚、第三章回路点亮、三种水母形态、角色附近气泡与最终海底星河。

## 运行与验证

```bash
dotnet build
/Users/shawj/Applications/Godot_mono.app/Contents/MacOS/Godot --headless --path . --import --quit
/Users/shawj/Applications/Godot_mono.app/Contents/MacOS/Godot --path .
```

录制无开场对话的净画面时，可在 Godot 参数后追加 `-- --skip-opening`。

自动章节链验证：

```bash
/Users/shawj/Applications/Godot_mono.app/Contents/MacOS/Godot --headless --path . res://scenes/game_scene2.tscn --quit-after 120 -- --smoke-complete
/Users/shawj/Applications/Godot_mono.app/Contents/MacOS/Godot --headless --path . res://scenes/game_scene3.tscn --quit-after 120 -- --smoke-complete
```

## 素材使用表

| 类别 | 已使用素材 | 用途 |
|---|---|---|
| 世界 | `zone_shallows.png`, `zone_sediment.png`, `zone_depths.png` | 三段海域主背景与净化前后对比 |
| 玩家 | `_raw/jellyfish_base.png`, `_raw/jellyfish_water.png`, `_raw/jellyfish_ice.png`, `_raw/jellyfish_electric.png` | 正确网格切片后的四形态动画 |
| 污染 | `invader_1.png`, `invader_2.png`, `invader_3.png` | 三海域污染残影，净化后淡出 |
| 巢母 | `brood_mother.png` | 最终净化对象 |
| 后续守护者 | `legacy/ice_monster_idle_sheet.png`, `legacy/factory_monster_idle_sheet.png` | 第二章霜壳守望者、第三章废热炉心；3×2 原画在运行时柔和色键并切片 |
| 复苏生物 | `legacy/electric_jellyfish_idle_sheet.png`, `legacy/tar_monster_idle_sheet.png` | 已迁入的旧项目绘本素材，为第三章生态/污染视觉储备 |
| 角色 | `npc_npc1giving.png`, `npc_starfish.png`, `npc_seaweed.png`, `npc_hermit.png`, `npc_shoal.png`, `npc_lantern.png` | 世界角色与对话肖像 |
| 叙事物件 | `npc_barrel.png`, `icon_memory.png`, `icon_note.png`, `water_shard.png` | 汽油桶、记忆、残片、净化弹体与潮汐出口 |
| 界面 | `title_bg.png`, `AaShuiyu.ttf` | 标题背景与全局中文字体 |

后续章节只迁入旧项目已有的手绘怪物原画，没有生成新素材，也没有混入旧版像素洞穴砖块。当前没有音频文件；`AudioManager` 会安全跳过缺失音效，因此录制阶段可后期配乐，不影响游戏流程。

## 尚可继续

- 如需最终视频成片：从三章各录一个恢复前后镜头，并在后期添加统一环境音乐与按键音。
- 如需发布构建：补做导出预设、版权清单、音频资源和无障碍设置页；本作品集 Demo 暂不包含这些发行工作。

详细审查见 [AUDIT_REPORT.md](AUDIT_REPORT.md)，设计约束见 [.impeccable.md](.impeccable.md)。
