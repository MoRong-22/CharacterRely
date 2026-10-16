using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Spine;

namespace CharacterRely.Animation
{
    /// <summary>一个可绘制部件的顶点、三角形及材质数据；缓冲区会跨帧复用。</summary>
    internal sealed class SpineDrawCommand
    {
        /// <summary>顶点数值数组。</summary>
        internal VertexPositionColorTexture[] Vertices = Array.Empty<VertexPositionColorTexture>();
        /// <summary>三角形顶点索引数组。</summary>
        internal int[] Indices = Array.Empty<int>();
        /// <summary>VertexCount：本次有效顶点数量；IndexCount：本次有效索引数量。</summary>
        internal int VertexCount, IndexCount;
        /// <summary>该部件使用的图集纹理。</summary>
        internal Texture2D Texture;
        /// <summary>骨架、插槽与附件相乘后的主颜色。</summary>
        internal Vector4 Light;
        /// <summary>双色着色暗部颜色。</summary>
        internal Vector3 Dark;
        /// <summary>部件颜色混合方式。</summary>
        internal BlendMode Blend;
        /// <summary>是否使用最近邻纹理采样。</summary>
        internal bool Nearest;
        /// <summary>该绘制命令对应的插槽名称。</summary>
        internal string SlotName;
        /// <summary>容量不足时扩容，并记录本次有效顶点和索引数量。</summary>
        internal void Allocate(int vertices, int indices)
        {
            if (Vertices.Length < vertices) Array.Resize(ref Vertices, vertices);
            if (Indices.Length < indices) Array.Resize(ref Indices, indices);
            VertexCount = vertices;
            IndexCount = indices;
        }
    }

    /// <summary>复用几何缓冲区；遵守 Skeleton.DrawOrder，使用官方运行时计算蒙皮与裁剪。</summary>
    internal sealed class SpineGeometry
    {
        /// <summary>四边形对应的两个三角形索引。</summary>
        private static readonly int[] quadIndices = { 0, 1, 2, 2, 3, 0 };
        /// <summary>可复用的骨架多边形裁剪器。</summary>
        private readonly SkeletonClipping clipper = new();
        /// <summary>复用的附件世界顶点位置数组，按 X、Y 交错存储。</summary>
        private float[] positions = new float[8];
        /// <summary>复用的部件绘制命令列表。</summary>
        internal readonly List<SpineDrawCommand> Commands = new();
        /// <summary>本次收集实际有效的绘制命令数，可能小于缓存列表长度。</summary>
        internal int Count { get; private set; }

        /// <summary>按骨架绘制顺序收集可见部件，计算蒙皮与裁剪后的三角形；不向显卡提交绘制。</summary>
        internal void Collect(Skeleton skeleton, bool[] visibility, Func<Slot, bool> filter)
        {
            Count = 0;
            clipper.ClipEnd();
            try
            {
                foreach (var slot in skeleton.DrawOrder)
                {
                    if (!slot.Bone.Active) { clipper.ClipEnd(slot); continue; }
                    // 裁剪附件定义后续部件的可见区域；即使本次只画手，也必须处理相关裁剪。
                    if (slot.Attachment is ClippingAttachment clip) { clipper.ClipStart(slot, clip); continue; }
                    if (!visibility[slot.Data.Index] || (filter != null && !filter(slot)))
                    { clipper.ClipEnd(slot); continue; }
                    float[] uvs;
                    int[] indices;
                    int floatCount;
                    Vector4 attachmentColor;
                    AtlasRegion region;
                    if (slot.Attachment is RegionAttachment image)
                    {
                        image.ComputeWorldVertices(slot.Bone, positions, 0, 2);
                        floatCount = 8;
                        uvs = image.UVs;
                        indices = quadIndices;
                        region = (AtlasRegion)image.RendererObject;
                        attachmentColor = new(image.R, image.G, image.B, image.A);
                    }
                    else if (slot.Attachment is MeshAttachment mesh)
                    {
                        floatCount = mesh.WorldVerticesLength;
                        if (positions.Length < floatCount) Array.Resize(ref positions, floatCount);
                        mesh.ComputeWorldVertices(slot, positions);
                        uvs = mesh.UVs;
                        indices = mesh.Triangles;
                        region = (AtlasRegion)mesh.RendererObject;
                        attachmentColor = new(mesh.R, mesh.G, mesh.B, mesh.A);
                    }
                    else { clipper.ClipEnd(slot); continue; }
                    float[] vertices = positions;
                    int indexCount = indices.Length;
                    if (clipper.IsClipping)
                    {
                        // 使用裁剪后的顶点、三角形和纹理坐标，不能沿用裁剪前的数量。
                        clipper.ClipTriangles(vertices, floatCount, indices, indexCount, uvs);
                        vertices = clipper.ClippedVertices.Items;
                        floatCount = clipper.ClippedVertices.Count;
                        indices = clipper.ClippedTriangles.Items;
                        indexCount = clipper.ClippedTriangles.Count;
                        uvs = clipper.ClippedUVs.Items;
                    }
                    if (floatCount > 0 && indexCount > 0)
                    {
                        if (Count == Commands.Count) Commands.Add(new SpineDrawCommand());
                        var command = Commands[Count++];
                        command.Allocate(floatCount / 2, indexCount);
                        command.Texture = region.page.rendererObject as Texture2D;
                        command.Nearest = region.page.magFilter == Spine.TextureFilter.Nearest;
                        command.SlotName = slot.Data.Name;
                        command.Blend = slot.Data.BlendMode;
                        command.Light = new Vector4(skeleton.R * slot.R, skeleton.G * slot.G,
                            skeleton.B * slot.B, skeleton.A * slot.A) * attachmentColor;
                        command.Dark = slot.HasSecondColor ? new Vector3(slot.R2, slot.G2, slot.B2) : Vector3.Zero;
                        for (int i = 0; i < command.VertexCount; i++)
                            command.Vertices[i] = new VertexPositionColorTexture(new Vector3(vertices[i * 2], vertices[i * 2 + 1], 0f),
                                Color.White, new Vector2(uvs[i * 2], uvs[i * 2 + 1]));
                        Array.Copy(indices, command.Indices, indexCount);
                    }
                    clipper.ClipEnd(slot);
                }
            }
            finally { clipper.ClipEnd(); }
        }

        /// <summary>清除缓存命令，使其引用的顶点数组和纹理可被释放或回收。</summary>
        internal void Clear() { Commands.Clear(); Count = 0; }
    }
}
