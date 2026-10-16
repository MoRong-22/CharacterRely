using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Spine;
using Terraria.ModLoader;

namespace CharacterRely.Animation
{
    /// <summary>把骨架几何数据提交给图形设备，处理预乘透明度、双色着色和插槽混合模式。</summary>
    internal static class SpineRenderer
    {
        /// <summary>由模组资源系统管理的共享渲染着色器。</summary>
        private static Asset<Effect> shader;
        /// <summary>additive：加法混合状态；multiply：正片叠底混合状态；screen：滤色混合状态。</summary>
        private static BlendState additive, multiply, screen;
        /// <summary>菜单列表预览沿用 UI 裁剪区域，避免滚动时绘制到卡片外。</summary>
        private static RasterizerState clippedRasterizer;
        /// <summary>按指定源和目标颜色系数创建预乘透明度混合状态。</summary>
        private static BlendState CreateBlend(Blend source, Blend destination) => new()
        {
            ColorSourceBlend = source, ColorDestinationBlend = destination,
            AlphaSourceBlend = Blend.One, AlphaDestinationBlend = Blend.InverseSourceAlpha
        };
        // 对应 Spine 的正常、叠加、正片叠底和滤色；自建状态复用至模组卸载。
        /// <summary>返回对应 Spine 混合模式的图形状态，按需创建并缓存。</summary>
        private static BlendState GetBlend(BlendMode mode) => mode switch
        {
            BlendMode.Normal => BlendState.AlphaBlend,
            BlendMode.Additive => additive ??= CreateBlend(Blend.One, Blend.One),
            BlendMode.Multiply => multiply ??= CreateBlend(Blend.DestinationColor, Blend.InverseSourceAlpha),
            BlendMode.Screen => screen ??= CreateBlend(Blend.One, Blend.InverseSourceColor),
            _ => throw new NotSupportedException($"未知 Spine 混合模式：{mode}")
        };

        /// <summary>绘制已经计算好的部件几何；结束后恢复图形设备状态，但不重启调用方的 SpriteBatch。</summary>
        /// <param name="device">当前客户端使用的图形设备。</param>
        /// <param name="geometry">本次有效部件的三角形、纹理和颜色数据。</param>
        /// <param name="transform">包含模型位置、缩放、旋转及视图变换的矩阵，不含投影矩阵。</param>
        /// <param name="tint">应用于全部部件的整体颜色及透明度。</param>
        /// <param name="suppliedEffect">可选的同参数布局着色器；不传时使用模组内置着色器。</param>
        internal static void Draw(GraphicsDevice device, SpineGeometry geometry, Matrix transform, Color tint, Effect suppliedEffect = null)
        {
            if (geometry.Count == 0) return;
            for (int i = 0; i < geometry.Count; i++)
                if (geometry.Commands[i].Texture == null)
                    throw new InvalidOperationException($"插槽 '{geometry.Commands[i].SlotName}' 没有图集纹理，专用服务器不可调用 Draw。");
            Effect effect = suppliedEffect;
            if (effect == null)
            {
                shader ??= ModContent.Request<Effect>("CharacterRely/Animation/SpineRender", AssetRequestMode.ImmediateLoad);
                effect = shader.Value;
            }
            // 暂存当前绘制状态，避免影响本次骨架绘制前后的其他游戏内容。
            var oldBlend = device.BlendState;
            var oldDepth = device.DepthStencilState;
            var oldRasterizer = device.RasterizerState;
            var oldSampler = device.SamplerStates[0];
            var oldTexture = device.Textures[0];
            var oldIndices = device.Indices;
            var oldVertices = device.GetVertexBuffers();
            try
            {
                device.DepthStencilState = DepthStencilState.None;
                device.RasterizerState = oldRasterizer.ScissorTestEnable
                    ? clippedRasterizer ??= new RasterizerState { CullMode = CullMode.None, ScissorTestEnable = true }
                    : RasterizerState.CullNone;
                // 将左上角为原点、Y 向下的屏幕坐标转换到显卡使用的裁剪空间。
                var projection = Matrix.CreateOrthographicOffCenter(0f, device.Viewport.Width, device.Viewport.Height, 0f, 0f, 1f);
                effect.Parameters["uTransform"].SetValue(transform * projection);
                effect.Parameters["uTint"].SetValue(tint.ToVector4());
                for (int i = 0; i < geometry.Count; i++)
                {
                    var command = geometry.Commands[i];
                    device.BlendState = GetBlend(command.Blend);
                    effect.Parameters["uTexture"].SetValue(command.Texture);
                    effect.Parameters["uLight"].SetValue(command.Light);
                    effect.Parameters["uDark"].SetValue(command.Dark);
                    effect.CurrentTechnique.Passes[0].Apply();
                    device.SamplerStates[0] = command.Nearest ? SamplerState.PointClamp : SamplerState.LinearClamp;
                    device.DrawUserIndexedPrimitives(PrimitiveType.TriangleList, command.Vertices, 0, command.VertexCount,
                        command.Indices, 0, command.IndexCount / 3);
                }
            }
            finally
            {
                device.BlendState = oldBlend;
                device.DepthStencilState = oldDepth;
                device.RasterizerState = oldRasterizer;
                device.SamplerStates[0] = oldSampler;
                device.Textures[0] = oldTexture;
                device.Indices = oldIndices;
                device.SetVertexBuffers(oldVertices);
            }
        }

        /// <summary>清除着色器引用并释放自建混合状态；着色器资源本身由 ModContent 管理。</summary>
        internal static void Unload()
        {
            shader = null;
            additive?.Dispose(); additive = null;
            multiply?.Dispose(); multiply = null;
            screen?.Dispose(); screen = null;
            clippedRasterizer?.Dispose(); clippedRasterizer = null;
        }
    }
}
