using System;
using System.Collections.Generic;
using CharacterRely.Content.ModCharacter;
using Terraria;
using Terraria.GameInput;
using Terraria.ModLoader;

namespace CharacterRely.Content.SkillSystem;

/// <summary>技能槽的通用按键。绑定槽位而非技能 ID，切换角色后沿用玩家设置的键位。</summary>
public sealed class SkillKeybindSystem : ModSystem
{
    /// <summary>技能栏和按键统一使用五个槽位；角色只配置部分技能时，其余槽位留空。</summary>
    public const int SlotCount = 5;

    /// <summary>五个技能槽，默认 Z、X、C、V、B；UI 可调用 GetAssignedKeys 显示玩家实际键位。</summary>
    public static IReadOnlyList<ModKeybind> SlotKeys { get; private set; } = Array.Empty<ModKeybind>();

    /// <summary>注册可在游戏控制菜单中修改的按键。</summary>
    public override void Load()
    {
        if (Main.dedServ) return;
        SlotKeys = Array.AsReadOnly(new[] {
            KeybindLoader.RegisterKeybind(Mod, "SkillSlot1", "Z"),
            KeybindLoader.RegisterKeybind(Mod, "SkillSlot2", "X"),
            KeybindLoader.RegisterKeybind(Mod, "SkillSlot3", "C"),
            KeybindLoader.RegisterKeybind(Mod, "SkillSlot4", "V"),
            KeybindLoader.RegisterKeybind(Mod, "SkillSlot5", "B")
        });
    }

    /// <summary>热重载时释放按键引用，实际注册由 tModLoader 清理。</summary>
    public override void Unload() => SlotKeys = Array.Empty<ModKeybind>();

    /// <summary>查找唯一接收技能输入的角色，供按键、技能栏和选人预览共用；冲突时返回 null。</summary>
    public static CharacterLib GetInputCharacter(Player player)
    {
        if (player == null) return null;
        CharacterLib target = null;
        foreach (var component in player.ModPlayers)
        {
            if (component is not CharacterLib character || !character.IsSelected
                || !character.AcceptsSkillInput || !character.SkillsEnabled) continue;
            if (target != null) return null;
            target = character;
        }
        return target;
    }
}

/// <summary>每位玩家只派发给一个已启用的角色，按住按键不会每帧重复消耗充能。</summary>
public sealed class SkillInputPlayer : ModPlayer
{
    /// <summary>仅本地玩家在正常游戏操作时处理输入；主菜单、聊天和背包中不释放技能。</summary>
    public override void ProcessTriggers(TriggersSet triggersSet)
    {
        if (Main.dedServ || Main.gameMenu || Main.gamePaused || Main.blockInput || Main.drawingPlayerChat
            || Main.editSign || Main.editChest || Main.playerInventory || Player.mouseInterface
            || Player.whoAmI != Main.myPlayer || !Player.active || Player.dead) return;

        CharacterLib target = SkillKeybindSystem.GetInputCharacter(Player);
        if (target == null) return;
        for (int slot = 0; slot < SkillKeybindSystem.SlotKeys.Count; slot++)
            if (SkillKeybindSystem.SlotKeys[slot].JustPressed) target.TryUseSkill(slot);
    }
}
