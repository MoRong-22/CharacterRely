using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace CharacterRely.Animation;

/// <summary>以游戏像素栅格合成完整角色，再用点采样绘制本体和外描边。</summary>
internal sealed class SpinePixelTarget : IDisposable
{
    private RenderTarget2D target;
    private float[] boundsBuffer;
    private int gridSize;
    private float resolutionScale = 1f;
    private bool ready;

    /// <summary>骨架原点固定在 RT 中心，放大描边 RT 不改变角色脚底位置。</summary>
    private Vector2 Origin => new(target.Width / 2, target.Height / 2);

    /// <summary>只在场景绘制前调用；透明清屏后绘制当前姿势，不推进动画时间。</summary>
    internal void Prepare(GraphicsDevice device, SpinePlayer animator, float scale, int pixelSize,
        float screenScale = 1f, float outlineWidth = 2f)
    {
        ready = false;
        ArgumentOutOfRangeException.ThrowIfLessThan(pixelSize, 1);
        if (!float.IsFinite(scale) || scale <= 0f) throw new ArgumentOutOfRangeException(nameof(scale));
        if (!float.IsFinite(screenScale) || screenScale <= 0f) throw new ArgumentOutOfRangeException(nameof(screenScale));
        if (!float.IsFinite(outlineWidth) || outlineWidth < 0f) throw new ArgumentOutOfRangeException(nameof(outlineWidth));
        animator.Skeleton.GetBounds(out float x, out float y, out float width, out float height, ref boundsBuffer);
        if (!float.IsFinite(x) || !float.IsFinite(y) || !float.IsFinite(width) || !float.IsFinite(height)
            || width <= 0f || height <= 0f)
            return;

        // 每个输出像素格对应一个游戏像素；镜头放大时只放大已有像素格，不重新平滑绘制本体。
        // 默认不合并 2×2 像素，保留脸部和发丝可用的游戏像素。
        // 先以两倍精度合成，再在着色器内归并成一个游戏像素，减少细发丝和眼睛漏采样。
        resolutionScale = 2f;
        float padding = MathF.Max(pixelSize * 2, MathF.Ceiling(outlineWidth * resolutionScale) + 1f);
        int neededWidth = RoundSize((MathF.Max(MathF.Abs(x), MathF.Abs(x + width)) * scale * resolutionScale + padding) * 2, pixelSize);
        int neededHeight = RoundSize((MathF.Max(MathF.Abs(y), MathF.Abs(y + height)) * scale * resolutionScale + padding) * 2, pixelSize);
        if (target == null || target.IsDisposed || target.GraphicsDevice != device ||
            target.Width < neededWidth || target.Height < neededHeight || gridSize != pixelSize)
        {
            int newWidth = gridSize == pixelSize ? Math.Max(neededWidth, target?.Width ?? 0) : neededWidth;
            int newHeight = gridSize == pixelSize ? Math.Max(neededHeight, target?.Height ?? 0) : neededHeight;
            target?.Dispose();
            target = new RenderTarget2D(device, newWidth, newHeight, false, SurfaceFormat.Color, DepthFormat.None);
            gridSize = pixelSize;
        }

        var oldTargets = device.GetRenderTargets();
        var oldViewport = device.Viewport;
        var oldScissor = device.ScissorRectangle;
        var oldRasterizer = device.RasterizerState;
        try
        {
            device.SetRenderTarget(target);
            device.Clear(Color.Transparent);
            device.RasterizerState = RasterizerState.CullNone;
            animator.Draw(device, Origin, scale: scale * resolutionScale);
            ready = true;
        }
        finally
        {
            device.SetRenderTargets(oldTargets);
            device.Viewport = oldViewport;
            device.ScissorRectangle = oldScissor;
            device.RasterizerState = oldRasterizer;
        }
    }

    private static int RoundSize(float size, int pixelSize)
    {
        int alignment = checked(pixelSize * 16);
        return Math.Max(alignment, checked((int)MathF.Ceiling(size / alignment) * alignment));
    }

    /// <summary>统一绘制像素本体和可调宽度的外描边；不再叠加未经像素化的 Spine 本体。</summary>
    internal void Draw(SpriteBatch batch, Effect effect, Vector2 feetPosition, Matrix viewMatrix,
        int pixelSize, Color outlineColor, float outlineWidth = 2f)
    {
        if (!ready || target == null || target.IsDisposed) return;
        GraphicsDevice device = batch.GraphicsDevice;
        var oldBlend = device.BlendState;
        var oldDepth = device.DepthStencilState;
        var oldRasterizer = device.RasterizerState;
        var oldSampler = device.SamplerStates[0];
        var oldTexture = device.Textures[0];
        var oldIndices = device.Indices;
        var oldVertices = device.GetVertexBuffers();
        var oldTechnique = effect.CurrentTechnique;
        try
        {
            effect.CurrentTechnique = effect.Techniques["CharacterPixelOutline"];
            effect.Parameters["uTextureSize"].SetValue(new Vector2(target.Width, target.Height));
            effect.Parameters["uPixelSize"].SetValue(pixelSize * resolutionScale);
            effect.Parameters["uOutlineColor"].SetValue(outlineColor.ToVector4());
            effect.Parameters["uOutlineWidth"].SetValue(outlineWidth * resolutionScale);
            batch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend,
                SamplerState.PointClamp,
                DepthStencilState.None, RasterizerState.CullNone, effect, viewMatrix);
            try
            {
                batch.Draw(target, feetPosition, null, Color.White, 0f, Origin,
                    1f / resolutionScale, SpriteEffects.None, 0f);
            }
            finally { batch.End(); }
        }
        finally
        {
            effect.CurrentTechnique = oldTechnique;
            device.BlendState = oldBlend;
            device.DepthStencilState = oldDepth;
            device.RasterizerState = oldRasterizer;
            device.SamplerStates[0] = oldSampler;
            device.Textures[0] = oldTexture;
            device.Indices = oldIndices;
            device.SetVertexBuffers(oldVertices);
        }
    }

    /// <summary>离开世界、玩家离线或模组卸载时释放显存。</summary>
    public void Dispose()
    {
        target?.Dispose();
        target = null;
        boundsBuffer = null;
        ready = false;
    }
}
