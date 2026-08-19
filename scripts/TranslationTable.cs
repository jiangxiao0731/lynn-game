using System.Collections.Generic;

namespace ShallowSeaDream;

/// res://scripts/TranslationTable.cs
/// Thin localization layer (DESIGN_BRIEF §12). The portfolio build runs in English;
/// the legacy Chinese column remains available as source reference. Player-facing
/// strings are routed through GameStrings.Tr(key) which delegates here.
/// Code-resident (not a .translation resource) to stay self-contained for the rebuild.
public static class TranslationTable
{
    public enum Locale { Zh, En }

    public static Locale Current { get; set; } = Locale.En;

    // key -> (zh, en). zh is authoritative; en falls back to zh when absent.
    private static readonly Dictionary<string, (string Zh, string En)> Table = new()
    {
        // Status / HUD hints
        ["STATUS_DEFAULT_HINT"] = ("探索潮湾，寻找岚婆婆。", "Explore the nursery and find Granny Lan."),
        ["STATUS_NEED_ENERGY"] = ("没有碎片，无法释放。", "You need a shard to use an element."),
        ["STATUS_NEED_TARGET"] = ("附近没有可净化的目标。", "No polluted creature is close enough."),
        ["STATUS_FIRST_WATER"] = ("拾取了第一枚水碎片，按 1 释放水元素。", "Water Shard collected. Press 1 to use Water."),
        ["STATUS_FORM_WATER"] = ("化为水元素水母。", "Water form active."),
        ["STATUS_BOSS_GATE"] = ("需要 8 枚水碎片才能净化巢母。", "Collect 8 Water Shards before you approach the Brood Mother."),

        // Skill feedback templates ({0} = form label / amount)
        ["SKILL_RELEASED_TEMPLATE"] = ("{0}已释放，净化生效。", "{0} released. Purification worked."),
        ["SKILL_INEFFECTIVE_TEMPLATE"] = ("{0}效果有限。", "{0} had little effect here."),
        ["DAMAGE_RECEIVED_TEMPLATE"] = ("水母受到 {0} 点污染伤害。", "Shimmer took {0} pollution damage."),

        // Form labels
        ["FORM_BASE"] = ("基础水母", "Shimmer"),
        ["FORM_WATER"] = ("水元素水母", "Water Form"),
        ["FORM_ICE"] = ("冰元素水母", "Ice Form"),
        ["FORM_ELECTRIC"] = ("电元素水母", "Electric Form"),

        // Objective checklist (6 stages)
        ["OBJ_FIND_NPC"] = ("寻找岚婆婆", "Find Granny Lan"),
        ["OBJ_TALK_STARFISH"] = ("与海星交谈", "Talk to the Starfish"),
        ["OBJ_TALK_SEAWEED"] = ("与海草交谈", "Talk to the Seaweed"),
        ["OBJ_COLLECT_SHARDS"] = ("收集 8 枚水碎片", "Collect 8 water shards"),
        ["OBJ_DEFEAT_BOSS"] = ("净化潮涡巢母", "Purify the Brood Mother"),
        ["OBJ_EXIT_LEVEL"] = ("抵达出口", "Reach the exit"),
        ["OBJ_COMPLETE"] = ("章节完成", "Chapter complete"),

        // Minimap legend
        ["MINIMAP_LEGEND"] = ("🟢微光 🟡岚婆婆 🔵碎片 🔴巢母 🟤汽油桶 ⚪出口",
                              "🟢Shimmer  🟡Granny Lan  🔵Shard  🔴Guardian  🟤Barrel  ⚪Exit"),

        // Monster codex
        ["MONSTER_TITLE"] = ("怪物图鉴", "Creature Guide"),
        ["MONSTER_DEFAULT_BODY"] = ("靠近污染体可查看其档案。", "Move closer to a polluted creature to learn about it."),
        ["MONSTER_INFO_TEMPLATE"] = ("{0}\n生命 {1}/{2}\n弱点：水", "{0}\nHP {1}/{2}\nWeakness: Water"),

        // Panels
        ["UI_PAUSE_LABEL"] = ("游戏已暂停", "Paused"),
        ["RESTART_HINT"] = ("按 T 重新开始", "Press T to restart"),
        ["SETTLEMENT_TITLE"] = ("净化完成", "Area Restored"),
        ["SETTLEMENT_SUMMARY"] = ("潮涡巢母已被净化，潮汐钥匙浮现。\n霜骨海沟与冰元素已经回应微光。",
                                  "The Brood Mother is free, and the Tidal Key has surfaced.\nA colder current is calling from below."),
        ["FAILURE_LABEL"] = ("微光暗淡了…", "Shimmer Faded"),

        // Resume notice
        ["RESUME_NOTICE_TEMPLATE"] = ("进度已恢复…碎片:{0}", "Progress restored. Shards: {0}"),

        // HP label
        ["HP_LABEL_TEMPLATE"] = ("生命 {0} / {1}", "HP {0} / {1}"),

        // Memory log / 图鉴 (item 5)
        ["LOG_TITLE"] = ("记忆日志 · 图鉴", "Memory Journal"),
        ["LOG_HINT"] = ("按 J / Tab 关闭", "Press J / Tab to close"),
        ["LOG_SEC_MEMORIES"] = ("◆ 微光潮汐记忆  {0}/{1}", "◆ Tidal Memories  {0}/{1}"),
        ["LOG_SEC_CONVOS"] = ("◆ 对话回忆  {0}", "◆ Conversations  {0}"),
        ["LOG_SEC_CODEX"] = ("◆ 怪物图鉴  {0}/{1}", "◆ Creature Guide  {0}/{1}"),
        ["LOG_SEC_NOTES"] = ("◆ 残片笔记  {0}/{1}", "◆ Lore Fragments  {0}/{1}"),

        // Memory vignette / lore feedback (item 3 / 5)
        ["MEMORY_UNLOCKED_TEMPLATE"] = ("唤回一段潮汐记忆：{0}", "A tidal memory returns: {0}"),
        ["NOTE_FOUND_TEMPLATE"] = ("拾得残片笔记：{0}", "Found a lore fragment: {0}"),
        ["STATUS_EXAMINE_HINT"] = ("按 E 查看", "Press E to examine"),

        // Zones (item 7)
        ["ZONE_SHALLOWS"] = ("浅滩", "The Shallows"),
        ["ZONE_SEDIMENT"] = ("沉积带", "Silt Passage"),
        ["ZONE_DEPTHS"] = ("巢母深处", "The Brood Depths"),
        ["ZONE_ENTER_TEMPLATE"] = ("进入：{0}", "Entering: {0}"),

        // Soft death / rest (item 4)
        ["SOFT_RESPAWN"] = ("微光黯淡了片刻……潮水把你托回安全处。", "Shimmer fades for a moment. The tide carries you back to safety."),
        ["REST_PROMPT"] = ("在此小憩，恢复微光？", "Rest here and restore Shimmer's light?"),
        ["BOSS_PURIFY_HINT"] = ("靠近巢母最薄的膜，按 1 用水轻轻松开。", "Move close to the thin membrane. Press 1 to release Water."),
        ["BOSS_PURIFIED"] = ("巢母被净化了，被困的微光回到水里。", "The Brood Mother is free. The trapped lights return to the current."),

        // Controls sheet (verbatim from brief §5)
        ["CONTROLS_SHEET"] = ("←↑→↓ 移动 / 1·2·3 释放 水·冰·电 / E 互动 / R 收回当前形态 / T 重新开始当前关卡 / Esc 暂停·继续",
                              "←↑→↓ Move  /  E Interact  /  1·2·3 Use Water·Ice·Electric  /  R Return to base form  /  T Restart  /  Esc Pause"),
    };

    public static string Tr(string key)
    {
        if (!Table.TryGetValue(key, out var v)) return key;
        return Current == Locale.En && !string.IsNullOrEmpty(v.En) ? v.En : v.Zh;
    }
}
