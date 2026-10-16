using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Spine;
using Terraria;
using Terraria.ModLoader;

namespace CharacterRely.Animation
{
    /// <summary>Spine 3.8.75 共享模型资源。路径相对于资源所属模组，支持 JSON、SKEL 和多页 atlas。</summary>
    public sealed class SpineAsset : IDisposable
    {
        // 记录已加载资源，模组卸载时统一释放；同一资源创建的播放器共享骨架数据和纹理。
        /// <summary>全部已加载共享模型资源的登记集合。</summary>
        private static readonly HashSet<SpineAsset> loadedAssets = new();
        /// <summary>此模型当前存活的独立播放器集合。</summary>
        private readonly HashSet<SpinePlayer> players = new();
        /// <summary>共享模型使用的图集。</summary>
        private Atlas atlas;
        /// <summary>此对象是否已释放。</summary>
        private bool disposed;
        /// <summary>解析后的共享骨架初始数据，供各独立播放器实例化。</summary>
        internal SkeletonData Data { get; private set; }
        /// <summary>骨架文件中全部动画的名称，可传给播放器的 Play。</summary>
        public IReadOnlyList<string> AnimationNames { get; private set; }
        /// <summary>骨架文件中全部皮肤的名称，可传给 CreatePlayer 选择初始皮肤。</summary>
        public IReadOnlyList<string> SkinNames { get; private set; }

        /// <summary>创建共享模型资源，并初始化关联数据及缓冲区。</summary>
        private SpineAsset() { }

        /// <summary>
        /// 加载一次后共享。atlas 内的 PNG 路径相对于 atlas 所在目录。
        /// 图集导出时关闭 Premultiply Alpha，tModLoader 会执行预乘；不用手动加载或释放纹理。
        /// scale 是骨架导入缩放，默认 1。专用服务器不读取纹理。
        /// </summary>
        /// <param name="owner">包含模型文件的模组，不必是 CharacterRely 本身。</param>
        /// <param name="skeletonPath">相对于所属模组根目录的骨架路径，包含 .json 或 .skel 扩展名。</param>
        /// <param name="atlasPath">相对于所属模组根目录的图集路径，包含 .atlas 扩展名。</param>
        /// <param name="scale">导入时的骨架缩放倍率，必须大于 0；不会改变原始纹理尺寸。</param>
        /// <returns>可供多个独立播放器共享的模型资源；使用结束后应释放。</returns>
        public static SpineAsset Load(Mod owner, string skeletonPath, string atlasPath, float scale = 1f)
        {
            ArgumentNullException.ThrowIfNull(owner);
            ArgumentException.ThrowIfNullOrWhiteSpace(skeletonPath);
            ArgumentException.ThrowIfNullOrWhiteSpace(atlasPath);
            skeletonPath = skeletonPath.Replace('\\', '/');
            atlasPath = atlasPath.Replace('\\', '/');
            string extension = Path.GetExtension(skeletonPath);
            if (!extension.Equals(".json", StringComparison.OrdinalIgnoreCase) &&
                !extension.Equals(".skel", StringComparison.OrdinalIgnoreCase))
                throw new ArgumentException("骨架请使用 Spine 3.8.75 导出的 .json 或 .skel。", nameof(skeletonPath));
            return Create(owner.GetFileBytes(skeletonPath), extension.Equals(".skel", StringComparison.OrdinalIgnoreCase),
                Encoding.UTF8.GetString(owner.GetFileBytes(atlasPath)).TrimStart('\uFEFF'),
                Path.GetDirectoryName(atlasPath)?.Replace('\\', '/') ?? "", new ModTextureLoader(owner), scale);
        }

        /// <summary>从骨架字节和图集文本创建资源；先核对导出版本，再解析骨架及纹理区域。</summary>
        internal static SpineAsset Create(byte[] skeleton, bool binary, string atlasText, string imageDirectory,
            TextureLoader loader, float scale = 1f)
        {
            if (!float.IsFinite(scale) || scale <= 0f) throw new ArgumentOutOfRangeException(nameof(scale));
            string json = binary ? null : Encoding.UTF8.GetString(skeleton).TrimStart('\uFEFF');
            string version;
            if (binary)
            {
                using var stream = new MemoryStream(skeleton, false);
                version = SkeletonBinary.GetVersionString(stream);
            }
            else
            {
                using var document = JsonDocument.Parse(json);
                version = document.RootElement.GetProperty("skeleton").GetProperty("spine").GetString();
            }
            if (version == null || !version.StartsWith("3.8.", StringComparison.Ordinal))
                throw new InvalidDataException($"骨架导出版本为 '{version}'，当前播放器使用 Spine 3.8.75。请用对应版本重新导出。");

            var asset = new SpineAsset();
            try
            {
                using var atlasReader = new StringReader(atlasText);
                asset.atlas = new Atlas(atlasReader, imageDirectory, loader);
                if (!asset.atlas.Any()) throw new InvalidDataException("Atlas 没有可用区域。");
                if (binary)
                {
                    using var stream = new MemoryStream(skeleton, false);
                    asset.Data = new SkeletonBinary(asset.atlas) { Scale = scale }.ReadSkeletonData(stream);
                }
                else
                {
                    using var reader = new StringReader(json);
                    asset.Data = new SkeletonJson(asset.atlas) { Scale = scale }.ReadSkeletonData(reader);
                }
                if (asset.Data == null || asset.Data.Bones.Count == 0) throw new InvalidDataException("骨架没有可用骨骼。");
                asset.AnimationNames = Array.AsReadOnly(asset.Data.Animations.Select(a => a.Name).ToArray());
                asset.SkinNames = Array.AsReadOnly(asset.Data.Skins.Select(s => s.Name).ToArray());
                loadedAssets.Add(asset);
                return asset;
            }
            catch { asset.atlas?.Dispose(); throw; }
        }

        /// <summary>每个玩家或预览创建一个独立实例；不传 skinName 使用导出的默认皮肤。</summary>
        /// <param name="skinName">初始皮肤名；传 null 使用骨架默认皮肤。</param>
        /// <returns>拥有独立进度、骨骼姿势和部件显隐状态的播放器。</returns>
        public SpinePlayer CreatePlayer(string skinName = null)
        {
            ObjectDisposedException.ThrowIf(disposed, this);
            if (skinName != null && Data.FindSkin(skinName) == null)
                throw new ArgumentException($"皮肤 '{skinName}' 不存在。可用：{string.Join(", ", SkinNames)}", nameof(skinName));
            var player = new SpinePlayer(this, skinName);
            players.Add(player);
            return player;
        }

        // 播放器释放时解除登记，不影响同一资源下的其他播放器。
        /// <summary>从共享资源的存活实例集合中移除此播放器，不影响其他实例。</summary>
        internal void Release(SpinePlayer player) => players.Remove(player);
        /// <summary>释放资源及其全部播放器；由 ModContent 管理的纹理不会在这里销毁。</summary>
        public void Dispose()
        {
            if (disposed) return;
            disposed = true;
            foreach (var player in new List<SpinePlayer>(players)) player.Dispose();
            atlas.Dispose();
            atlas = null;
            Data = null;
            loadedAssets.Remove(this);
        }

        /// <summary>模组卸载入口：释放全部已登记模型及渲染器自建的混合状态。</summary>
        internal static void UnloadAll()
        {
            foreach (var asset in new List<SpineAsset>(loadedAssets)) asset.Dispose();
            SpineRenderer.Unload();
        }

        /// <summary>将 Spine 图集页面映射到所属模组的纹理资源；专用服务器只保留尺寸信息。</summary>
        private sealed class ModTextureLoader(Mod owner) : TextureLoader
        {
            /// <summary>从模组资源中加载图集纹理，并回填页面尺寸；专用服务器仅检查尺寸。</summary>
            public void Load(AtlasPage page, string path)
            {
                if (Main.dedServ)
                {
                    if (page.width <= 0 || page.height <= 0) throw new InvalidDataException("服务器加载 atlas 需要 size 字段。");
                    return;
                }
                path = path.Replace('\\', '/');
                if (path.EndsWith(".png", StringComparison.OrdinalIgnoreCase)) path = path[..^4];
                var texture = ModContent.Request<Texture2D>(owner.Name + "/" + path, AssetRequestMode.ImmediateLoad).Value;
                page.rendererObject = texture;
                page.width = texture.Width;
                page.height = texture.Height;
            }
            // 纹理由 ModContent 统一管理；多个模型可能共用同一张 PNG，因此这里不主动释放。
            /// <summary>纹理由 ModContent 共享管理，此方法不主动销毁纹理。</summary>
            public void Unload(object texture) { }
        }
    }
}
