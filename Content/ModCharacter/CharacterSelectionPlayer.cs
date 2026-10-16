using System;
using Terraria;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace CharacterRely.Content.ModCharacter;

/// <summary>每位 Terraria 玩家只存一个已确认的角色 ID。</summary>
public sealed class CharacterSelectionPlayer : ModPlayer
{
    private const string SaveKey = "CharacterRely.SelectedCharacter";
    internal const byte SyncMessage = 1;

    public string SelectedCharacterId { get; private set; }

    /// <summary>当前选择对应的角色组件；尚未选择或角色模组未加载时返回 null。</summary>
    public CharacterLib SelectedCharacter
    {
        get
        {
            if (!CharacterRegistry.TryGet(SelectedCharacterId, out CharacterDefinition definition)) return null;
            return definition.GetForPlayer(Player);
        }
    }

    /// <summary>选择唯一的已注册角色；传 null 清空选择。游戏内切换会立即更新并同步。</summary>
    public bool SelectCharacter(string id)
    {
        if (!string.IsNullOrEmpty(id) && !CharacterRegistry.TryGet(id, out _)) return false;
        if (SelectedCharacterId == id) return true;
        SelectedCharacterId = string.IsNullOrEmpty(id) ? null : id;
        ApplySelection();
        if (!Main.gameMenu && Player.whoAmI == Main.myPlayer && Main.netMode != Terraria.ID.NetmodeID.SinglePlayer)
            SendSelection(Player.whoAmI, SelectedCharacterId, -1, -1);
        return true;
    }

    /// <summary>确认新角色时让旧角色停下技能、被动和动画；仅一个 ID 生效。</summary>
    private void ApplySelection()
    {
        if (Main.gameMenu || Player == null) return;
        foreach (ModPlayer component in Player.ModPlayers)
            if (component is CharacterLib character && !character.IsSelected)
                character.OnDeselected();
    }

    public override void SaveData(TagCompound tag)
    {
        if (!string.IsNullOrEmpty(SelectedCharacterId)) tag[SaveKey] = SelectedCharacterId;
    }

    public override void LoadData(TagCompound tag)
        => SelectedCharacterId = tag.ContainsKey(SaveKey) ? tag.GetString(SaveKey) : null;

    public override void CopyClientState(ModPlayer targetCopy)
        => ((CharacterSelectionPlayer)targetCopy).SelectedCharacterId = SelectedCharacterId;

    public override void SendClientChanges(ModPlayer clientPlayer)
    {
        if (SelectedCharacterId != ((CharacterSelectionPlayer)clientPlayer).SelectedCharacterId)
            SendSelection(Player.whoAmI, SelectedCharacterId, -1, -1);
    }

    public override void SyncPlayer(int toWho, int fromWho, bool newPlayer)
        => SendSelection(Player.whoAmI, SelectedCharacterId, toWho, fromWho);

    public override void OnEnterWorld()
    {
        if (Main.netMode == Terraria.ID.NetmodeID.MultiplayerClient && Player.whoAmI == Main.myPlayer)
            SendSelection(Player.whoAmI, SelectedCharacterId, -1, -1);
        ApplySelection();
    }

    internal void ApplySynchronizedSelection(string id)
    {
        if (!string.IsNullOrEmpty(id) && !CharacterRegistry.TryGet(id, out _)) return;
        SelectedCharacterId = string.IsNullOrEmpty(id) ? null : id;
        ApplySelection();
    }

    internal static void SendSelection(int playerIndex, string id, int toWho, int fromWho)
    {
        var packet = ModContent.GetInstance<global::CharacterRely.CharacterRely>().GetPacket();
        packet.Write(SyncMessage);
        packet.Write((byte)playerIndex);
        packet.Write(id ?? string.Empty);
        packet.Send(toWho, fromWho);
    }
}
