# 潮汐微光 / Shallow Sea Dream

Godot 4.7 Mono / C# 的三章节海洋保护作品集 Demo。玩家扮演微光水母，聆听受污染海域居民的线索，在迷宫中用水、冰、电恢复潮流、解冻育场、重连安全回路，并净化而非击杀受困的海洋守护者。

## 当前完成度

- 第一章「潮湾苗圃」保留三段有机迷宫；第二章「霜骨海沟」加入三座解冻锚与冰元素；第三章「断流灯塔」加入三座潮汐继电器、电元素与完整结局。
- 每章 3 个渐进海域、12 个唯一元素拾取（目标 8 个）、可恢复捷径、章节守护者与可见的污染前后对比。
- 8 枚跨区域潮汐记忆、10 段记忆文本、8 个环境残片、6 位可交谈角色。
- 污染生物与遮罩会随浅滩、沉积带、巢母深处的净化进度依次退场。
- 三位守护者各需要 4 次对应元素引导：水 → 冰 → 电；第一章解锁冰，第二章解锁电，第三章点亮海底灯塔。
- 修复日志遮罩常驻、HUD 锚点警告、碎片累计漂移、精灵图集网格错误、过冲/弹跳动效和全屏暗化。
- 标题、教程、HUD、角色气泡、日志与结算现已统一为纸张纤维、手绘墨边和笔刷进度条；地图能量线与目标环也采用非规则笔触。
- 文字系统已分为手绘展示标题、清晰加重信息、正文与元数据四级；英文长文案使用智能换行，任务卡、位置卡、技能栏、角色气泡、日志、失败页与结算页均预留了安全宽高，不再溢出容器。
- 游戏内所有可见文案已统一为简洁自然的英文，面向艺术院校作品集评审；三章依次传递海洋垃圾、营养物污染与缺氧、海洋升温与珊瑚白化知识，并把信息放在对应修复动作之后。
- 当前 50 张运行时图片全部来自项目内 `source_art/` 保存的用户手绘素材副本，或由这些原图做可复现的裁切、组合和调色；没有保留旧 Gemini 运行时图片，也不再依赖其他项目目录。

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
| 世界 | `zone_shallows.png`, `zone_sediment.png`, `zone_depths.png`, `wall_tile.png` | 从 `资源/IMG_2030.PNG` 连续裁切的三段海域与手绘礁石纹理 |
| 玩家 | `_raw/jellyfish_base.png`, `_raw/jellyfish_water.png`, `_raw/jellyfish_ice.png`, `_raw/jellyfish_electric.png` | `游戏素材库` 与旧项目手绘 3×2 动画序列 |
| 污染 | `invader_1.png`, `invader_2.png`, `invader_3.png` | `游戏人物` 中塑料瓶、化学气、汽油小怪 |
| 第一章守护者 | `brood_mother.png` | 旧项目手绘焦油怪序列，作为需净化的污染守护者 |
| 后续守护者 | `legacy/ice_monster_idle_sheet.png`, `legacy/factory_monster_idle_sheet.png` | 第二章霜壳守望者、第三章废热炉心原始 3×2 手绘序列 |
| 角色 | `npc_npc1giving.png`, `npc_starfish.png`, `npc_seaweed.png`, `npc_hermit.png`, `npc_shoal.png`, `npc_lantern.png` | 从 `资源` 与 `游戏人物` 选择、裁切或组合的手绘角色 |
| 叙事物件 | `npc_barrel.png`, `icon_memory.png`, `icon_note.png`, `water_shard.png` | 原文件夹汽油桶、水元素球与沙水雕刻残片 |
| 界面 | `title_bg.png`, `AaShuiyu.ttf` | `IMG_2030.PNG` 手绘主视觉裁切与展示标题字体；正文使用系统已有的 Avenir/Helvetica 安全回退，不新增图片或字体文件 |

所有运行时图片均可通过 `./tools/sync_folder_art.sh` 从项目内 `source_art/` 重新构建；脚本只做本地复制、裁切、缩放、组合和调色，不调用生成服务、不删除项目文件，也没有混入像素洞穴砖块。当前没有音频文件；`AudioManager` 会安全跳过缺失音效，因此录制阶段可后期配乐，不影响游戏流程。

三个版本恢复点及无覆盖回退方法见 [RESTORE_GUIDE.md](RESTORE_GUIDE.md)。

## 尚可继续

- 如需最终视频成片：从三章各录一个恢复前后镜头，并在后期添加统一环境音乐与按键音。
- 如需发布构建：补做导出预设、版权清单、音频资源和无障碍设置页；本作品集 Demo 暂不包含这些发行工作。

详细审查见 [AUDIT_REPORT.md](AUDIT_REPORT.md)，叙事知识架构见 [NARRATIVE_ARCHITECTURE.md](NARRATIVE_ARCHITECTURE.md)，设计约束见 [.impeccable.md](.impeccable.md)。
