using System;
using System.IO;
using CharacterRely.Animation;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.ModLoader;

namespace CharacterRely.Content.ModCharacter;

/// <summary>统一管理完整角色的游戏像素栅格和外描边，角色模组无需自行添加绘制效果。</summary>
[Autoload(false)]
public sealed class CharacterPixelRenderSystem : ModSystem
{
    private SpinePixelTarget[] targets;
    private CharacterLib[] owners;
    private SpriteBatch batch;
    private Asset<Effect> shader;
    private bool ownsShader;

    /// <summary>客户端在世界绘制前准备模型，避免在世界合成中途切换 RT。</summary>
    public override void Load()
    {
        if (Main.dedServ) return;
        targets = new SpinePixelTarget[Main.maxPlayers];
        owners = new CharacterLib[Main.maxPlayers];
        Main.OnPreDraw += PrepareCharacters;
    }

    private void PrepareCharacters(GameTime gameTime)
    {
        GraphicsDevice device = Main.instance.GraphicsDevice;
        Matrix view = Main.GameViewMatrix.TransformationMatrix;
        float screenScale = MathF.Max(new Vector2(view.M11, view.M12).Length(),
            new Vector2(view.M21, view.M22).Length());
        for (int i = 0; i < targets.Length; i++)
        {
            Player player = Main.player[i];
            CharacterLib character = !Main.gameMenu && player != null && player.active && !player.dead
                ? player.GetModPlayer<CharacterSelectionPlayer>().SelectedCharacter : null;
            if (character == null || !character.IsDrawEnabled || !character.UsePixelRendering)
            {
                ReleaseTarget(i);
                continue;
            }

            if (!ReferenceEquals(owners[i], character))
            {
                ReleaseTarget(i);
                owners[i] = character;
            }
            character.Animator.FlipX = player.direction < 0;
            (targets[i] ??= new SpinePixelTarget()).Prepare(device, character.Animator,
                character.CharacterScale, character.PixelSize, screenScale, character.OutlineWidth);
        }
    }

    private void ReleaseTarget(int index)
    {
        targets[index]?.Dispose();
        targets[index] = null;
        owners[index] = null;
    }

    /// <summary>绘制该角色本帧的合成结果；没有准备过的实例不会误用其他角色的 RT。</summary>
    internal void Draw(CharacterLib character, GraphicsDevice device)
    {
        int index = character.Player.whoAmI;
        if (targets == null || (uint)index >= targets.Length || !ReferenceEquals(owners[index], character)) return;
        SpinePixelTarget target = targets[index];
        if (target == null) return;
        EnsureShader();
        batch ??= new SpriteBatch(device);
        target.Draw(batch, shader.Value, character.Player.Bottom - Main.screenPosition,
            Main.GameViewMatrix.TransformationMatrix, character.PixelSize, character.OutlineColor, character.OutlineWidth);
    }

    private void EnsureShader()
    {
        if (shader != null) return;
        using Stream embedded = typeof(CharacterPixelRenderSystem).Assembly
            .GetManifestResourceStream("CharacterRely.CharacterPixelOutline.xnb");
        if (embedded != null)
        {
            // DLL 内置效果，依赖库的角色模组不需要复制 FX 或 XNB。
            shader = Main.Assets.CreateUntracked<Effect>(embedded, ".xnb", AssetRequestMode.ImmediateLoad);
            ownsShader = true;
        }
        else
        {
            // CharacterRely 作为普通模组运行时，从本模组资源加载。
            shader = ModContent.Request<Effect>(Mod.Name + "/Animation/CharacterPixelOutline", AssetRequestMode.ImmediateLoad);
        }
    }

    /// <summary>移除绘制钩子，并在主线程释放 RT、SpriteBatch 和 DLL 自行创建的效果资源。</summary>
    public override void Unload()
    {
        if (Main.dedServ) return;
        Main.OnPreDraw -= PrepareCharacters;
        var oldTargets = targets;
        var oldBatch = batch;
        var oldShader = ownsShader ? shader : null;
        targets = null;
        owners = null;
        batch = null;
        shader = null;
        ownsShader = false;
        Main.QueueMainThreadAction(() =>
        {
            if (oldTargets != null)
                foreach (var target in oldTargets) target?.Dispose();
            oldBatch?.Dispose();
            oldShader?.Dispose();
        });
    }
}
