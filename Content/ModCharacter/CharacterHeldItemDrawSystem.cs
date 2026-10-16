using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace CharacterRely.Content.ModCharacter;

/// <summary>将原版手持物品绘制平移到角色提供的握点，保留原版纹理、握柄偏移与发光层。</summary>
public sealed class CharacterHeldItemDrawSystem : ModSystem
{
    /// <summary>客户端接入原版手持物品绘制入口。</summary>
    public override void Load()
    {
        if (!Main.dedServ)
            On_PlayerDrawLayers.DrawPlayer_27_HeldItem += DrawHeldItem;
    }

    /// <summary>卸载时移除钩子。</summary>
    public override void Unload()
    {
        if (!Main.dedServ)
            On_PlayerDrawLayers.DrawPlayer_27_HeldItem -= DrawHeldItem;
    }

    /// <summary>先让原版生成物品绘制数据，再只平移本层新增的数据。</summary>
    private static void DrawHeldItem(On_PlayerDrawLayers.orig_DrawPlayer_27_HeldItem orig,
        ref PlayerDrawSet drawInfo)
    {
        int firstItem = drawInfo.DrawDataCache.Count;
        orig(ref drawInfo);
        if (drawInfo.DrawDataCache.Count == firstItem) return;

        foreach (var component in drawInfo.drawPlayer.ModPlayers)
        {
            if (component is CharacterLib character && character.IsSelected && character.IsDrawEnabled
                && character.TryGetHeldItemDrawPosition(out Vector2 hand))
            {
                MoveHeldItem(ref drawInfo, firstItem, hand);
                break;
            }
        }
    }

    /// <summary>以原版握点为基准整体平移，物品自身的旋转、大小、染色和原点均由原版处理。</summary>
    internal static void MoveHeldItem(ref PlayerDrawSet drawInfo, int firstItem, Vector2 worldHand)
    {
        // Rapier/短剑的原版贴图以 ItemLocation 为握点；复合前手位置与 ItemLocation
        // 并不重合，拿 GetFrontHandPosition 作基准会把短剑额外错开一段距离。
        Vector2 vanillaHand;
        if (drawInfo.heldItem?.useStyle is ItemUseStyleID.Thrust or ItemUseStyleID.Rapier)
            vanillaHand = drawInfo.ItemLocation;
        else if (!TryGetVanillaHand(drawInfo.drawPlayer, out vanillaHand))
            return;
        Vector2 pivot = drawInfo.drawPlayer.position + drawInfo.rotationOrigin;
        // 原版稍后还会绕玩家旋转；先逆变换目标，避免物品挂点被重复旋转。
        Vector2 beforeRotation = pivot + (worldHand - pivot).RotatedBy(-drawInfo.rotation);
        Vector2 offset = beforeRotation - vanillaHand;
        for (int i = firstItem; i < drawInfo.DrawDataCache.Count; i++)
        {
            DrawData item = drawInfo.DrawDataCache[i];
            item.position += item.ignorePlayerRotation ? worldHand - vanillaHand : offset;
            drawInfo.DrawDataCache[i] = item;
        }
    }

    /// <summary>读取尚未应用整体旋转的原版前手，与通用手臂适配器使用同一姿势来源。</summary>
    internal static bool TryGetVanillaHand(Player player, out Vector2 hand)
    {
        var arm = player.compositeFrontArm;
        if (arm.enabled)
        {
            hand = player.GetFrontHandPosition(arm.stretch, arm.rotation);
            return true;
        }

        int frame = player.bodyFrame.Height > 0 ? player.bodyFrame.Y / player.bodyFrame.Height : 0;
        if (Main.OffsetsPlayerOnhand == null || (uint)frame >= (uint)Main.OffsetsPlayerOnhand.Length)
        {
            hand = default;
            return false;
        }
        // 与原版 HandPosition 相同的身体帧握点，保留坐骑中心与倒置重力的处理。
        Vector2 offset = Main.OffsetsPlayerOnhand[frame] * 2f;
        if (player.direction != 1) offset.X = player.bodyFrame.Width - offset.X;
        if (player.gravDir != 1f) offset.Y = player.bodyFrame.Height - offset.Y;
        offset -= new Vector2(player.bodyFrame.Width - player.width, player.bodyFrame.Height - 42f) * 0.5f;
        hand = player.MountedCenter + offset - new Vector2(10f, 21f);
        return true;
    }
}
