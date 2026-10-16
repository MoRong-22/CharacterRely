using System;
using System.Collections.Generic;
using System.Text;
using CharacterRely.Animation;
using CharacterRely.Content.SkillSystem;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.GameContent;
using Terraria.GameContent.UI.Elements;
using Terraria.GameContent.UI.States;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.UI;

namespace CharacterRely.Content.ModCharacter;

/// <summary>在原版新建玩家界面添加角色入口，选择结果随新玩家一起保存。</summary>
public sealed class CharacterSelectionUISystem : ModSystem
{
    public override void Load()
    {
        if (Main.dedServ) return;
        On_UICharacterCreation.ctor += AddSelectionButton;
    }

    public override void Unload()
    {
        if (!Main.dedServ) On_UICharacterCreation.ctor -= AddSelectionButton;
    }

    private static void AddSelectionButton(On_UICharacterCreation.orig_ctor orig,
        UICharacterCreation self, Player player)
    {
        orig(self, player);
        if (player == null || CharacterRegistry.Entries.Count == 0) return;
        // 原版根容器底部留有空行；入口放在“返回 / 创建”下方，并跟随容器布局。
        foreach (UIElement container in self.Children)
        {
            container.Append(new CharacterSelectionEntryButton(self, player));
            break;
        }
    }

    internal static void Open(UICharacterCreation creationState, Player player)
    {
        if (Main.MenuUI == null || player == null || CharacterRegistry.Entries.Count == 0) return;
        // 此时玩家还没有存档；只确认 ID，由原版“创建”流程统一写入新存档。
        Main.MenuUI.SetState(new CharacterSelectionState(player,
            id => player.GetModPlayer<CharacterSelectionPlayer>().SelectCharacter(id),
            () => Main.MenuUI.SetState(creationState)));
    }

}

/// <summary>新建玩家界面的入口；显示该新玩家当前已确认的模组角色。</summary>
internal sealed class CharacterSelectionEntryButton : UIElement
{
    private readonly Player player;

    internal CharacterSelectionEntryButton(UICharacterCreation creationState, Player player)
    {
        this.player = player;
        Width.Set(0f, 1f);
        Height.Set(38f, 0f);
        VAlign = 1f;
        OnLeftClick += (_, _) => CharacterSelectionUISystem.Open(creationState, this.player);
    }

    protected override void DrawSelf(SpriteBatch spriteBatch)
    {
        Rectangle box = GetDimensions().ToRectangle();
        Texture2D pixel = TextureAssets.MagicPixel.Value;
        spriteBatch.Draw(pixel, box, IsMouseHovering ? new Color(225, 156, 75) : new Color(91, 111, 142));
        spriteBatch.Draw(pixel, new Rectangle(box.X + 2, box.Y + 2, box.Width - 4, box.Height - 4),
            new Color(26, 31, 45, 235));
        string selectedId = player.GetModPlayer<CharacterSelectionPlayer>().SelectedCharacterId;
        string roleName = CharacterRegistry.TryGet(selectedId, out CharacterDefinition definition)
            ? definition.Name : Text("None");
        string label = Text("CardButton", roleName);
        float scale = MathF.Min(0.72f, (box.Width - 8f) / MathF.Max(1f,
            FontAssets.MouseText.Value.MeasureString(label).X));
        Utils.DrawBorderString(spriteBatch, label, box.Center.ToVector2(), Color.White, scale, 0.5f, 0.5f);
    }

    private static string Text(string key, params object[] args)
        => Language.GetTextValue("Mods.CharacterRely.CharacterSelect." + key, args);
}

/// <summary>候选角色只在此界面预览；点“确认使用”后更新待创建玩家的角色 ID。</summary>
internal sealed class CharacterSelectionState : UIState
{
    private readonly Player player;
    private readonly Action<string> onConfirmed;
    private readonly Action onBack;
    private CharacterDefinition candidate;
    private UIList detailsList;
    private UIText details;
    private CharacterSelectionPreviewElement preview;
    private CharacterActionButton confirmButton;

    internal CharacterSelectionState(Player player, Action<string> onConfirmed, Action onBack)
    {
        this.player = player;
        this.onConfirmed = onConfirmed;
        this.onBack = onBack;
        string savedId = player.GetModPlayer<CharacterSelectionPlayer>().SelectedCharacterId;
        if (!CharacterRegistry.TryGet(savedId, out candidate) && CharacterRegistry.Entries.Count > 0)
            candidate = CharacterRegistry.Entries[0];
    }

    public override void OnInitialize()
    {
        var panel = new UIPanel();
        panel.Width.Set(900f, 0f);
        panel.Height.Set(540f, 0f);
        panel.HAlign = 0.5f;
        panel.VAlign = 0.5f;
        panel.BackgroundColor = new Color(25, 30, 43, 245);
        panel.BorderColor = new Color(180, 135, 72);
        Append(panel);

        var heading = new UITextPanel<LocalizedText>(TextObject("Title"), 0.8f, true);
        heading.Width.Set(0f, 1f);
        heading.Height.Set(40f, 0f);
        heading.Top.Set(-4f, 0f);
        heading.BackgroundColor = new Color(54, 45, 42);
        panel.Append(heading);

        var listPanel = new UIPanel();
        listPanel.Left.Set(12f, 0f);
        listPanel.Top.Set(48f, 0f);
        listPanel.Width.Set(250f, 0f);
        listPanel.Height.Set(420f, 0f);
        panel.Append(listPanel);
        var list = new UIList { ListPadding = 6f, ManualSortMethod = elements => { } };
        list.Width.Set(-26f, 1f);
        list.Height.Set(0f, 1f);
        listPanel.Append(list);
        var scrollbar = new UIScrollbar();
        scrollbar.Height.Set(0f, 1f);
        scrollbar.Left.Set(-20f, 1f);
        listPanel.Append(scrollbar);
        list.SetScrollbar(scrollbar);

        foreach (CharacterDefinition entry in CharacterRegistry.Entries)
        {
            var option = new CharacterSelectionOption(this, entry);
            option.Width.Set(0f, 1f);
            option.Height.Set(48f, 0f);
            list.Add(option);
        }

        var detailPanel = new UIPanel();
        detailPanel.Left.Set(272f, 0f);
        detailPanel.Top.Set(48f, 0f);
        detailPanel.Width.Set(-284f, 1f);
        detailPanel.Height.Set(420f, 0f);
        panel.Append(detailPanel);

        preview = new CharacterSelectionPreviewElement(this);
        preview.Left.Set(8f, 0f);
        preview.Top.Set(8f, 0f);
        preview.Width.Set(216f, 0f);
        preview.Height.Set(205f, 0f);
        detailPanel.Append(preview);

        detailsList = new UIList { ListPadding = 0f, ManualSortMethod = elements => { } };
        detailsList.Left.Set(232f, 0f);
        detailsList.Top.Set(8f, 0f);
        detailsList.Width.Set(-260f, 1f);
        detailsList.Height.Set(-16f, 1f);
        detailPanel.Append(detailsList);
        var detailsScroll = new UIScrollbar();
        detailsScroll.Left.Set(-18f, 1f);
        detailsScroll.Top.Set(8f, 0f);
        detailsScroll.Height.Set(-16f, 1f);
        detailPanel.Append(detailsScroll);
        detailsList.SetScrollbar(detailsScroll);
        details = new UIText(string.Empty, 0.78f, false) { TextColor = Color.White };
        details.Width.Set(0f, 1f);
        detailsList.Add(details);

        confirmButton = new CharacterActionButton(Text("Confirm"), Confirm);
        confirmButton.Width.Set(210f, 0f);
        confirmButton.Height.Set(38f, 0f);
        confirmButton.Left.Set(526f, 0f);
        confirmButton.Top.Set(476f, 0f);
        panel.Append(confirmButton);

        var backButton = new CharacterActionButton(Text("Back"), Back);
        backButton.Width.Set(140f, 0f);
        backButton.Height.Set(38f, 0f);
        backButton.Left.Set(748f, 0f);
        backButton.Top.Set(476f, 0f);
        panel.Append(backButton);

        UpdateDetails();
    }

    internal CharacterDefinition Candidate => candidate;
    internal string CurrentId => player.GetModPlayer<CharacterSelectionPlayer>().SelectedCharacterId;

    internal void SetCandidate(CharacterDefinition value)
    {
        if (ReferenceEquals(candidate, value)) return;
        candidate = value;
        UpdateDetails();
    }

    private void UpdateDetails()
    {
        if (details == null) return;
        if (candidate == null)
        {
            SetDetailsText(Text("NoRegisteredCharacters"));
            confirmButton?.SetEnabled(false);
            return;
        }

        confirmButton?.SetEnabled(true);
        var text = new StringBuilder();
        text.AppendLine(candidate.Name);
        text.AppendLine(candidate.Description);
        text.AppendLine();
        text.AppendLine(Text("SkillsHeading"));
        IReadOnlyList<Skill> skills = candidate.Template.Skills;
        if (skills.Count == 0) text.AppendLine(Text("NoSkills"));
        foreach (Skill skill in skills)
        {
            text.Append(skill.Name).Append("  ").AppendLine(skill.Description);
            text.AppendLine(Text("SkillStats", skill.Info.MaxCount,
                (skill.Info.CooldownTicks / 60f).ToString("0.0")));
        }
        text.AppendLine();
        text.AppendLine(Text("PassivesHeading"));
        IReadOnlyList<PassiveSkill> passives = candidate.Template.PassiveSkills;
        if (passives.Count == 0) text.AppendLine(Text("NoPassives"));
        foreach (PassiveSkill passive in passives)
        {
            text.Append(passive.Name).Append("  ").AppendLine(passive.Description);
        }
        SetDetailsText(text.ToString());
    }

    private void SetDetailsText(string text)
    {
        // 文本可长于预览区域；统一换行后交给 UIList 滚动，便于显示任意数量的被动。
        int maxLines = Math.Max(1, text.Length + 1);
        string[] lines = Utils.WordwrapString(text, FontAssets.MouseText.Value, 440, maxLines, out int lastLine);
        int lineCount = Math.Max(1, lastLine + 1);
        details.SetText(string.Join("\n", lines, 0, lineCount));
        details.Height.Set(lineCount * FontAssets.MouseText.Value.LineSpacing * 0.78f + 12f, 0f);
        detailsList.Recalculate();
    }

    private void Confirm()
    {
        if (candidate == null) return;
        onConfirmed(candidate.Id);
        Back();
    }

    private void Back() => onBack();

    private static string Text(string key, params object[] args)
        => Language.GetTextValue("Mods.CharacterRely.CharacterSelect." + key, args);

    private static LocalizedText TextObject(string key)
        => Language.GetText("Mods.CharacterRely.CharacterSelect." + key);
}

internal sealed class CharacterSelectionOption : UIElement
{
    private readonly CharacterSelectionState owner;
    private readonly CharacterDefinition definition;
    private readonly Asset<Texture2D> icon;

    internal CharacterSelectionOption(CharacterSelectionState owner, CharacterDefinition definition)
    {
        this.owner = owner;
        this.definition = definition;
        if (!string.IsNullOrWhiteSpace(definition.IconPath))
            ModContent.RequestIfExists(definition.IconPath, out icon, AssetRequestMode.ImmediateLoad);
        OnLeftClick += (_, _) => owner.SetCandidate(definition);
    }

    protected override void DrawSelf(SpriteBatch spriteBatch)
    {
        Rectangle box = GetDimensions().ToRectangle();
        bool selected = ReferenceEquals(owner.Candidate, definition);
        Texture2D pixel = TextureAssets.MagicPixel.Value;
        spriteBatch.Draw(pixel, box, selected ? new Color(168, 112, 51) : new Color(75, 82, 99));
        spriteBatch.Draw(pixel, new Rectangle(box.X + 2, box.Y + 2, box.Width - 4, box.Height - 4),
            selected ? new Color(55, 44, 35) : new Color(29, 34, 47));
        if (icon != null)
        {
            Texture2D texture = icon.Value;
            float scale = MathF.Min(38f / texture.Width, 38f / texture.Height);
            spriteBatch.Draw(texture, new Vector2(box.X + 24, box.Center.Y), null, Color.White, 0f,
                texture.Size() * 0.5f, scale, SpriteEffects.None, 0f);
        }
        string label = definition.Name;
        if (owner.CurrentId == definition.Id)
            label += " (" + Language.GetTextValue("Mods.CharacterRely.CharacterSelect.Current") + ")";
        float left = icon == null ? 12f : 49f;
        float scaleText = MathF.Min(0.82f, (box.Width - left - 5f) /
            MathF.Max(1f, FontAssets.MouseText.Value.MeasureString(label).X));
        Utils.DrawBorderString(spriteBatch, label, new Vector2(box.X + left, box.Center.Y),
            Color.White, scaleText, 0f, 0.5f);
    }
}

internal sealed class CharacterActionButton : UIElement
{
    private readonly string label;
    private readonly Action action;
    private bool enabled = true;

    internal CharacterActionButton(string label, Action action)
    {
        this.label = label;
        this.action = action;
        OnLeftClick += (_, _) => { if (enabled) action(); };
    }

    internal void SetEnabled(bool value) => enabled = value;

    protected override void DrawSelf(SpriteBatch spriteBatch)
    {
        Rectangle box = GetDimensions().ToRectangle();
        Texture2D pixel = TextureAssets.MagicPixel.Value;
        Color color = !enabled ? new Color(64, 67, 77)
            : IsMouseHovering ? new Color(225, 156, 75) : new Color(91, 111, 142);
        spriteBatch.Draw(pixel, box, color);
        spriteBatch.Draw(pixel, new Rectangle(box.X + 2, box.Y + 2, box.Width - 4, box.Height - 4),
            enabled ? new Color(26, 31, 45, 235) : new Color(38, 40, 46));
        Utils.DrawBorderString(spriteBatch, label, box.Center.ToVector2(), enabled ? Color.White : Color.Gray,
            0.85f, 0.5f, 0.5f);
    }
}

/// <summary>独立的选人 Spine 预览，不更新存档角色的动画或技能实例。</summary>
internal sealed class CharacterSelectionPreviewElement : UIElement
{
    private readonly CharacterSelectionState owner;
    private CharacterDefinition definition;
    private SpinePlayer preview;
    private float[] boundsBuffer;
    private Vector4 bounds;

    internal CharacterSelectionPreviewElement(CharacterSelectionState owner)
    {
        this.owner = owner;
        IgnoresMouseInteraction = true;
        UseImmediateMode = true;
        OverrideSamplerState = SamplerState.LinearClamp;
    }

    public override void Update(GameTime gameTime)
    {
        base.Update(gameTime);
        if (!ReferenceEquals(definition, owner.Candidate))
        {
            ReleasePreview();
            definition = owner.Candidate;
            if (definition != null)
            {
                preview = definition.Template.CreatePreviewPlayer();
                preview.Skeleton.GetBounds(out float x, out float y, out float width, out float height, ref boundsBuffer);
                bounds = new Vector4(x, y, width, height);
            }
        }
        preview?.Update((float)gameTime.ElapsedGameTime.TotalSeconds);
    }

    protected override void DrawSelf(SpriteBatch spriteBatch)
    {
        if (preview == null || !CharacterPreviewElement.Fit(bounds, GetDimensions().ToRectangle(),
            out Vector2 position, out float scale)) return;
        GraphicsDevice device = spriteBatch.GraphicsDevice;
        RasterizerState rasterizer = device.RasterizerState;
        SamplerState sampler = device.SamplerStates[0];
        Rectangle scissor = device.ScissorRectangle;
        spriteBatch.End();
        try { preview.Draw(device, position, scale: scale, viewMatrix: Main.UIScaleMatrix); }
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
        boundsBuffer = null;
    }
}
