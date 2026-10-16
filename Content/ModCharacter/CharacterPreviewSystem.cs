using System;
using CharacterRely.Animation;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent.UI.Elements;
using Terraria.ModLoader;
using Terraria.UI;

namespace CharacterRely.Content.ModCharacter;

/// <summary>给原版选人界面的角色卡片接入 Spine 预览，保留原来的卡片和按钮。</summary>
public sealed class CharacterPreviewSystem : ModSystem
{
    public override void Load()
    {
        if (!Main.dedServ) On_UICharacter.ctor += AddPreview;
    }

    public override void Unload()
    {
        if (!Main.dedServ) On_UICharacter.ctor -= AddPreview;
    }

    private static void AddPreview(On_UICharacter.orig_ctor orig, UICharacter self, Player player,
        bool animated, bool drawsBackPanel, float characterScale, bool useAClone)
    {
        orig(self, player, animated, drawsBackPanel, characterScale, useAClone);
        self.Append(new CharacterPreviewElement(player));
    }
}

/// <summary>独立的角色预览控件。只推进预览动画，离开界面时释放，不触发玩家技能。</summary>
public sealed class CharacterPreviewElement : UIElement
{
    private readonly Player player;
    private CharacterLib character;
    private SpinePlayer preview;
    private float[] boundsBuffer;
    private Vector4 bounds;

    public CharacterPreviewElement(Player player)
    {
        this.player = player;
        Width.Set(0f, 1f);
        Height.Set(0f, 1f);
        IgnoresMouseInteraction = true;
        // 让 UIElement 为本控件建立确定的 UI 绘制状态，原始三角形绘制后可准确恢复。
        UseImmediateMode = true;
        OverrideSamplerState = SamplerState.LinearClamp;
    }

    public override void Update(GameTime gameTime)
    {
        base.Update(gameTime);
        CharacterLib selected = player?.GetModPlayer<CharacterSelectionPlayer>().SelectedCharacter;
        if (!ReferenceEquals(selected, character))
        {
            ReleasePreview();
            character = selected;
        }
        if (character == null) return;
        if (preview == null)
        {
            preview = character.CreatePreviewPlayer();
            preview.Skeleton.GetBounds(out float x, out float y, out float width, out float height, ref boundsBuffer);
            bounds = new Vector4(x, y, width, height);
        }
        preview.Update((float)gameTime.ElapsedGameTime.TotalSeconds);
    }

    /// <summary>按预览框适配模型大小，位置使用 UI 坐标，不使用游戏世界或摄像机坐标。</summary>
    internal static bool Fit(Vector4 bounds, Rectangle area, out Vector2 position, out float scale)
    {
        position = default;
        scale = 1f;
        if (!float.IsFinite(bounds.X) || !float.IsFinite(bounds.Y) || !float.IsFinite(bounds.Z)
            || !float.IsFinite(bounds.W) || bounds.Z <= 0f || bounds.W <= 0f || area.Width <= 8 || area.Height <= 8)
            return false;
        scale = MathF.Min((area.Width - 8f) / bounds.Z, (area.Height - 8f) / bounds.W);
        position = new Vector2(area.X + area.Width * 0.5f - (bounds.X + bounds.Z * 0.5f) * scale,
            area.Bottom - 4f + bounds.Y * scale);
        return true;
    }

    protected override void DrawSelf(SpriteBatch spriteBatch)
    {
        if (preview == null || !Fit(bounds, GetDimensions().ToRectangle(), out Vector2 position, out float scale)) return;
        GraphicsDevice device = spriteBatch.GraphicsDevice;
        var rasterizer = device.RasterizerState;
        var sampler = device.SamplerStates[0];
        Rectangle scissor = device.ScissorRectangle;
        // 先提交卡片背景，防止延迟 SpriteBatch 在 Spine 之后把预览盖住。
        spriteBatch.End();
        try
        {
            preview.Draw(device, position, scale: scale, viewMatrix: Main.UIScaleMatrix);
        }
        finally
        {
            device.ScissorRectangle = scissor;
            spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, sampler,
                DepthStencilState.None, rasterizer, null, Main.UIScaleMatrix);
        }
    }

    public override void OnDeactivate()
    {
        ReleasePreview();
        base.OnDeactivate();
    }

    private void ReleasePreview()
    {
        preview?.Dispose();
        preview = null;
        character = null;
        boundsBuffer = null;
    }
}
