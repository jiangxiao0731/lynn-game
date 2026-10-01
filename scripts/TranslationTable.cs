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
        ["STATUS_NEED_ENERGY"] = ("先处理更多污染物，微光才够强。", "Clean more pollution first. Shimmer needs more cleanup power."),
        ["STATUS_NEED_TARGET"] = ("附近没有可净化的目标。", "No polluted creature is close enough."),
        ["STATUS_FIRST_WATER"] = ("处理掉第一件塑料污染。净化力增加，遇到大怪时按 1 反击。", "First plastic pollutant cleaned. Cleanup power is building—press 1 near the monster."),
        ["STATUS_FORM_WATER"] = ("水流净化已准备好。", "Water cleanup is ready."),
        ["STATUS_BOSS_GATE"] = ("先处理 8 件塑料污染，再去压制塑料怪物。", "Clean 8 plastic pollutants before you face the Plastic Monster."),

        // Skill feedback templates ({0} = form label / amount)
        ["SKILL_RELEASED_TEMPLATE"] = ("{0}已释放，净化生效。", "{0} released. The cleanup worked."),
        ["SKILL_INEFFECTIVE_TEMPLATE"] = ("{0}效果有限。", "{0} had little effect here."),
        ["DAMAGE_RECEIVED_TEMPLATE"] = ("水母受到 {0} 点污染伤害。", "Shimmer took {0} pollution damage."),

        // Form labels
        ["FORM_BASE"] = ("基础水母", "Shimmer"),
        ["FORM_WATER"] = ("水流净化", "Water Flow"),
        ["FORM_ICE"] = ("冰封净化", "Ice Seal"),
        ["FORM_ELECTRIC"] = ("电流净化", "Electric Pulse"),

        // Objective checklist (6 stages)
        ["OBJ_FIND_NPC"] = ("找岚婆婆：了解这片海怎么坏掉的", "Find Granny Lan: learn what happened here"),
        ["OBJ_TALK_STARFISH"] = ("问海星：塑料为什么会伤害动物", "Ask Starfish why plastic hurts animals"),
        ["OBJ_TALK_SEAWEED"] = ("沿路听居民说完，再问海草怎么接近垃圾堆", "Follow the residents' story, then ask Seaweed about the trash pile"),
        ["OBJ_COLLECT_SHARDS"] = ("处理塑料瓶和垃圾，让微光变强", "Clean plastic bottles and trash so Shimmer can fight"),
        ["OBJ_DEFEAT_BOSS"] = ("保持距离，按 1 用净化力压制塑料怪物", "Keep distance. Press 1 to push back the Plastic Monster"),
        ["OBJ_EXIT_LEVEL"] = ("带着新的洋流前往下一片海", "Follow the new current to the next sea"),
        ["OBJ_COMPLETE"] = ("章节完成", "Chapter complete"),

        // Minimap legend
        ["MINIMAP_LEGEND"] = ("🟢微光 🟡岚婆婆 🔵污染物 🔴巢母 🟤汽油桶 ⚪出口",
                              "🟢Shimmer  🟡Granny Lan  🔵Pollution  🔴Boss  🟤Barrel  ⚪Exit"),

        // Monster codex
        ["MONSTER_TITLE"] = ("怪物图鉴", "Creature Guide"),
        ["MONSTER_DEFAULT_BODY"] = ("靠近污染体可查看其档案。", "Move closer to a polluted creature to learn about it."),
        ["MONSTER_INFO_TEMPLATE"] = ("{0}\n生命 {1}/{2}\n弱点：水", "{0}\nHP {1}/{2}\nWeakness: Water"),

        // Panels
        ["UI_PAUSE_LABEL"] = ("游戏已暂停", "Paused"),
        ["RESTART_HINT"] = ("按 T 重新开始", "Press T to restart"),
        ["SETTLEMENT_TITLE"] = ("净化完成", "Area Restored"),
        ["SETTLEMENT_SUMMARY"] = ("塑料怪物已被清理，干净洋流浮现。\n更冷的水流正从下方呼唤。",
                                  "The Plastic Monster has been cleared away, and a clean current has surfaced.\nA colder current is calling from below."),
        ["FAILURE_LABEL"] = ("微光暗淡了…", "Shimmer Faded"),

        // Resume notice
        ["RESUME_NOTICE_TEMPLATE"] = ("进度已恢复…已处理污染物:{0}", "Progress restored. Pollution cleaned: {0}"),

        // HP label
        ["HP_LABEL_TEMPLATE"] = ("生命 {0} / {1}", "HP {0} / {1}"),

        // Memory log / 图鉴 (item 5)
        ["LOG_TITLE"] = ("记忆日志 · 图鉴", "Memory Journal"),
        ["LOG_HINT"] = ("按 J / Tab 关闭", "Press J / Tab to close"),
        ["LOG_SEC_MEMORIES"] = ("◆ 海洋记忆  {0}/{1}", "◆ Ocean Memories  {0}/{1}"),
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
        ["ZONE_DEPTHS"] = ("缠结深处", "Tangled Depths"),
        ["ZONE_ENTER_TEMPLATE"] = ("进入：{0}", "Entering: {0}"),

        // Soft death / rest (item 4)
        ["SOFT_RESPAWN"] = ("微光黯淡了片刻……潮水把你托回安全处。", "Shimmer fades for a moment. The tide carries you back to safety."),
        ["REST_PROMPT"] = ("在此小憩，恢复微光？", "Rest here and restore Shimmer's light?"),
        ["BOSS_PURIFY_HINT"] = ("塑料怪物开始攻击了。后退，按 1 释放刚积攒的净化力。", "The Plastic Monster is attacking. Back up, then press 1 with your cleanup power."),
        ["BOSS_PURIFIED"] = ("塑料怪物散开了。海里的塑料污染被清走，被困动物回到水流中。", "The Plastic Monster broke apart. The plastic pollution is gone, and trapped animals return to the current."),

        // Controls sheet (verbatim from brief §5)
        ["CONTROLS_SHEET"] = ("←↑→↓ 移动 / 1·2·3 释放 水·冰·电 / E 互动 / R 收回当前形态 / T 重新开始当前关卡 / Esc 暂停·继续",
                              "Move / E Interact / 123 Use Water·Ice·Electric / R Return to base form / T Restart / Esc Pause"),
    };

    public static string Tr(string key)
    {
        if (!Table.TryGetValue(key, out var v)) return key;
        return Current == Locale.En && !string.IsNullOrEmpty(v.En) ? v.En : v.Zh;
    }
}
