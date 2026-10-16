using System;
using System.Collections.Generic;
using CharacterRely.Content.ModCharacter;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.GameContent;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.ModLoader.UI;
using Terraria.UI;

namespace CharacterRely.Content.SkillSystem;

/// <summary>屏幕底部的五槽技能栏；只读取技能状态，计时仍由角色基类负责。</summary>
public sealed class SkillBarSystem : ModSystem
{
    private UserInterface userInterface;
    private SkillBarState state;
    private GameTime lastUpdate = new();

    public override void Load()
    {
        if (Main.dedServ) return;
        userInterface = new UserInterface();
        state = new SkillBarState();
        userInterface.SetState(state);
    }

    public override void UpdateUI(GameTime gameTime)
    {
        if (state == null) return;
        lastUpdate = gameTime;
        Player player = Main.LocalPlayer;
        state.Character = Main.gameMenu || player == null || !player.active || player.dead
            ? null : SkillKeybindSystem.GetInputCharacter(player);
        if (state.Character != null) userInterface.Update(gameTime);
    }

    public override void ModifyInterfaceLayers(List<GameInterfaceLayer> layers)
    {
        int index = layers.FindIndex(layer => layer.Name == "Vanilla: Mouse Text");
        if (index < 0) index = layers.Count;
        layers.Insert(index, new LegacyGameInterfaceLayer("CharacterRely: Skills", () =>
        {
            if (!Main.gameMenu && state?.Character != null) userInterface.Draw(Main.spriteBatch, lastUpdate);
            return true;
        }, InterfaceScaleType.UI));
    }

    public override void Unload()
    {
        userInterface?.SetState(null);
        userInterface = null;
        state = null;
    }
}

/// <summary>布局跟随游戏 UI 缩放；悬停技能栏时阻止鼠标操作穿透到世界。</summary>
internal sealed class SkillBarState : UIState
{
    internal CharacterLib Character;
    private readonly SkillSlotElement[] slots = new SkillSlotElement[SkillKeybindSystem.SlotCount];
    private UIElement bar;

    public override void OnInitialize()
    {
        bar = new UIElement { HAlign = 0.5f, VAlign = 1f };
        bar.Width.Set(SkillKeybindSystem.SlotCount * 58f + (SkillKeybindSystem.SlotCount - 1) * 8f, 0f);
        bar.Height.Set(70f, 0f);
        bar.Top.Set(-20f, 0f);
        Append(bar);
        for (int i = 0; i < slots.Length; i++)
        {
            slots[i] = new SkillSlotElement(this, i);
            slots[i].Left.Set(i * 66f, 0f);
            slots[i].Width.Set(58f, 0f);
            slots[i].Height.Set(70f, 0f);
            bar.Append(slots[i]);
        }
    }

    public override void Update(GameTime gameTime)
    {
        base.Update(gameTime);
        if (Character != null && bar.ContainsPoint(Main.MouseScreen)) Main.LocalPlayer.mouseInterface = true;
    }

    public override void Draw(SpriteBatch spriteBatch)
    {
        if (Character == null) return;
        base.Draw(spriteBatch);
        foreach (var slot in slots)
            if (slot.IsMouseHovering) { slot.DrawTooltip(); break; }
    }
}

/// <summary>显示图标、当前/最大充能、剩余 CD 和实际绑定按键；空槽不伪造技能。</summary>
internal sealed class SkillSlotElement : UIElement
{
    private readonly SkillBarState owner;
    private readonly int slot;
    private string iconPath;
    private Asset<Texture2D> icon;

    internal SkillSlotElement(SkillBarState owner, int slot) { this.owner = owner; this.slot = slot; }

    private Skill Skill => owner.Character != null && slot < owner.Character.Skills.Count
        ? owner.Character.Skills[slot] : null;

    private string KeyName
    {
        get
        {
            if (slot >= SkillKeybindSystem.SlotKeys.Count) return "—";
            var keys = SkillKeybindSystem.SlotKeys[slot].GetAssignedKeys();
            return keys.Count == 0 ? "—" : string.Join(" / ", keys);
        }
    }

    private static string Text(string key, params object[] args)
        => Language.GetTextValue("Mods.CharacterRely.SkillUI." + key, args);

    protected override void DrawSelf(SpriteBatch spriteBatch)
    {
        Skill skill = Skill;
        Rectangle area = GetDimensions().ToRectangle();
        Rectangle box = new(area.X, area.Y, area.Width, 58);
        Texture2D pixel = TextureAssets.MagicPixel.Value;
        Color border = IsMouseHovering ? new Color(255, 215, 130)
            : skill?.IsActive == true ? new Color(255, 165, 70)
            : skill != null && skill.Count > 0 ? new Color(115, 160, 200) : new Color(75, 80, 95);
        spriteBatch.Draw(pixel, box, border);
        Rectangle inside = new(box.X + 2, box.Y + 2, box.Width - 4, box.Height - 4);
        spriteBatch.Draw(pixel, inside, new Color(22, 28, 42, 225));

        if (iconPath != skill?.IconPath)
        {
            iconPath = skill?.IconPath;
            icon = null;
            if (!string.IsNullOrWhiteSpace(iconPath))
                ModContent.RequestIfExists(iconPath, out icon, AssetRequestMode.ImmediateLoad);
        }
        if (icon != null)
        {
            Texture2D texture = icon.Value;
            float scale = MathF.Min(46f / texture.Width, 46f / texture.Height);
            spriteBatch.Draw(texture, box.Center.ToVector2(), null, Color.White, 0f,
                texture.Size() * 0.5f, scale, SpriteEffects.None, 0f);
        }
        else
        {
            string label = skill?.Name ?? Text("EmptyShort");
            float scale = MathF.Min(0.65f, 48f / MathF.Max(1f, FontAssets.MouseText.Value.MeasureString(label).X));
            Utils.DrawBorderString(spriteBatch, label, new Vector2(box.Center.X, box.Y + 9),
                skill == null ? Color.Gray : Color.White, scale, 0.5f);
        }

        if (skill != null)
        {
            if (skill.CooldownRemaining > 0)
            {
                float ratio = MathHelper.Clamp(skill.CooldownRemaining / (float)Math.Max(1, skill.Info.CooldownTicks), 0f, 1f);
                int height = (int)MathF.Ceiling(inside.Height * ratio);
                spriteBatch.Draw(pixel, new Rectangle(inside.X, inside.Bottom - height, inside.Width, height),
                    Color.Black * (skill.Count == 0 ? 0.65f : 0.3f));
                Utils.DrawBorderString(spriteBatch, (skill.CooldownRemaining / 60f).ToString("0.0"),
                    new Vector2(box.Center.X, box.Y + 25), Color.White, 0.8f, 0.5f);
            }
            Utils.DrawBorderString(spriteBatch, $"{skill.Count}/{skill.MaxCount}",
                new Vector2(box.Right - 4, box.Bottom - 15), Color.White, 0.6f, 1f);
        }
        string key = KeyName;
        float keyScale = MathF.Min(0.6f, 56f / MathF.Max(1f, FontAssets.MouseText.Value.MeasureString(key).X));
        Utils.DrawBorderString(spriteBatch, key, new Vector2(box.Center.X, box.Bottom + 2), Color.White, keyScale, 0.5f);
    }

    /// <summary>所有槽位绘制完以后再绘制说明，避免被后面的槽位盖住。</summary>
    internal void DrawTooltip()
    {
        Main.LocalPlayer.mouseInterface = true;
        Skill skill = Skill;
        string text;
        if (skill == null) text = Text("EmptySlot", slot + 1, KeyName);
        else
        {
            text = $"{skill.Name} [{KeyName}]\n{skill.Description}\n"
                + Text("Charges", skill.Count, skill.MaxCount) + "\n"
                + Text("RechargeTime", (skill.Info.CooldownTicks / 60f).ToString("0.0")) + "\n"
                + (skill.CooldownRemaining > 0
                    ? Text("Remaining", (skill.CooldownRemaining / 60f).ToString("0.0"))
                    : Text("Ready"));
            if (skill.IsActive) text += "\n" + Text("Active");
        }
        string[] lines = Utils.WordwrapString(text, FontAssets.MouseText.Value, 360, 30, out int lastLine);
        UICommon.TooltipMouseText(string.Join("\n", lines, 0, Math.Min(lines.Length, lastLine + 1)));
    }
}
