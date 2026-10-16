/******************************************************************************
 * Spine Runtimes License Agreement
 * Last updated May 1, 2019. Replaces all prior versions.
 *
 * Copyright (c) 2013-2019, Esoteric Software LLC
 *
 * Integration of the Spine Runtimes into software or otherwise creating
 * derivative works of the Spine Runtimes is permitted under the terms and
 * conditions of Section 2 of the Spine Editor License Agreement:
 * http://esotericsoftware.com/spine-editor-license
 *
 * Otherwise, it is permitted to integrate the Spine Runtimes into software
 * or otherwise create derivative works of the Spine Runtimes (collectively,
 * "Products"), provided that each user of the Products must obtain their own
 * Spine Editor license and redistribution of the Products in any form must
 * include this license and copyright notice.
 *
 * THIS SOFTWARE IS PROVIDED BY ESOTERIC SOFTWARE LLC "AS IS" AND ANY EXPRESS
 * OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES
 * OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE DISCLAIMED. IN
 * NO EVENT SHALL ESOTERIC SOFTWARE LLC BE LIABLE FOR ANY DIRECT, INDIRECT,
 * INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING,
 * BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES, BUSINESS
 * INTERRUPTION, OR LOSS OF USE, DATA, OR PROFITS) HOWEVER CAUSED AND ON ANY
 * THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING
 * NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE,
 * EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
 *****************************************************************************/

using System;

namespace Spine {
	/// <summary><para>通过网格显示纹理区域的附件，支持骨骼权重和顶点变形。</para>Attachment that displays a texture region using a mesh.</summary>
	public class MeshAttachment : VertexAttachment, IHasRendererObject {
		/// <summary>regionOffsetX：区域裁剪产生的 X 偏移；regionOffsetY：区域裁剪产生的 Y 偏移；regionWidth：图集区域裁剪后宽度；regionHeight：图集区域裁剪后高度；regionOriginalWidth：图集区域裁剪前宽度；regionOriginalHeight：图集区域裁剪前高度。</summary>
		internal float regionOffsetX, regionOffsetY, regionWidth, regionHeight, regionOriginalWidth, regionOriginalHeight;
		/// <summary>链接网格共享数据的父网格引用。</summary>
		private MeshAttachment parentMesh;
		/// <summary>uvs：映射到整张图集的顶点 UV 数组；regionUVs：相对于附件区域的归一化顶点 UV 数组。</summary>
		internal float[] uvs, regionUVs;
		/// <summary>三角形顶点索引，每三个索引组成一个三角形。</summary>
		internal int[] triangles;
		/// <summary>r：红色分量；g：绿色分量；b：蓝色分量；a：透明度分量。</summary>
		internal float r = 1, g = 1, b = 1, a = 1;
		/// <summary>网格凸包顶点坐标值数量，每顶点占两个值。</summary>
		internal int hulllength;

		/// <summary>网格凸包顶点坐标值数量，每顶点占两个值。</summary>
		public int HullLength { get { return hulllength; } set { hulllength = value; } }
		/// <summary>相对于附件区域的归一化顶点 UV 数组。</summary>
		public float[] RegionUVs { get { return regionUVs; } set { regionUVs = value; } }
		/// <summary><para>每个顶点的 UV 坐标对，相对于整张图集纹理归一化。</para>The UV pair for each vertex, normalized within the entire texture. <seealso cref="MeshAttachment.UpdateUVs"/></summary>
		public float[] UVs { get { return uvs; } set { uvs = value; } }
		/// <summary>三角形顶点索引，每三个索引组成一个三角形。</summary>
		public int[] Triangles { get { return triangles; } set { triangles = value; } }

		/// <summary>红色分量。</summary>
		public float R { get { return r; } set { r = value; } }
		/// <summary>绿色分量。</summary>
		public float G { get { return g; } set { g = value; } }
		/// <summary>蓝色分量。</summary>
		public float B { get { return b; } set { b = value; } }
		/// <summary>透明度分量。</summary>
		public float A { get { return a; } set { a = value; } }

		/// <summary>图集区域查找路径。</summary>
		public string Path { get; set; }
		/// <summary>渲染后端关联对象；图集页通常为纹理，附件通常为 AtlasRegion。</summary>
		public object RendererObject { get; set; }
		/// <summary>图集区域起始 U 坐标。</summary>
		public float RegionU { get; set; }
		/// <summary>图集区域起始 V 坐标。</summary>
		public float RegionV { get; set; }
		/// <summary>图集区域结束 U 坐标。</summary>
		public float RegionU2 { get; set; }
		/// <summary>图集区域结束 V 坐标。</summary>
		public float RegionV2 { get; set; }
		/// <summary>图集区域是否旋转 90 度。</summary>
		public bool RegionRotate { get; set; }
		/// <summary>图集区域存储旋转角度。</summary>
		public int RegionDegrees { get; set; }
		/// <summary>区域裁剪产生的 X 偏移。</summary>
		public float RegionOffsetX { get { return regionOffsetX; } set { regionOffsetX = value; } }
		/// <summary>区域裁剪产生的 Y 偏移。</summary>
		public float RegionOffsetY { get { return regionOffsetY; } set { regionOffsetY = value; } } // 未旋转情况下，从图像左下侧裁掉的像素量。 原注： Pixels stripped from the bottom left, unrotated.
		/// <summary>图集区域裁剪后宽度。</summary>
		public float RegionWidth { get { return regionWidth; } set { regionWidth = value; } }
		/// <summary>图集区域裁剪后高度。</summary>
		public float RegionHeight { get { return regionHeight; } set { regionHeight = value; } } // 未旋转的裁剪后尺寸。 原注： Unrotated, stripped size.
		/// <summary>图集区域裁剪前宽度。</summary>
		public float RegionOriginalWidth { get { return regionOriginalWidth; } set { regionOriginalWidth = value; } }
		/// <summary>图集区域裁剪前高度。</summary>
		public float RegionOriginalHeight { get { return regionOriginalHeight; } set { regionOriginalHeight = value; } } // 未旋转的原始完整尺寸。 原注： Unrotated, unstripped size.

		/// <summary>共享几何数据的父网格。</summary>
		public MeshAttachment ParentMesh {
			get { return parentMesh; }
			set {
				parentMesh = value;
				if (value != null) {
					bones = value.bones;
					vertices = value.vertices;
					worldVerticesLength = value.worldVerticesLength;
					regionUVs = value.regionUVs;
					triangles = value.triangles;
					HullLength = value.HullLength;
					Edges = value.Edges;
					Width = value.Width;
					Height = value.Height;
				}
			}
		}

		// 非运行必需的辅助数据。 原注： Nonessential.
		/// <summary>网格编辑器边界边索引，属于非运行必需的辅助数据。</summary>
		public int[] Edges { get; set; }
		/// <summary>宽度。</summary>
		public float Width { get; set; }
		/// <summary>高度。</summary>
		public float Height { get; set; }

		/// <summary>使用指定名称创建支持骨骼权重及顶点变形的纹理网格附件。</summary>
		public MeshAttachment (string name)
			: base(name) {
		}

		/// <summary>依据区域范围、裁剪和旋转信息重算网格图集 UV。</summary>
		public void UpdateUVs () {
			float[] regionUVs = this.regionUVs;
			if (this.uvs == null || this.uvs.Length != regionUVs.Length) this.uvs = new float[regionUVs.Length];
			float[] uvs = this.uvs;
			float u = RegionU, v = RegionV, width = 0, height = 0;

			if (RegionDegrees == 90) {
				float textureHeight = this.regionWidth / (RegionV2 - RegionV);
				float textureWidth = this.regionHeight / (RegionU2 - RegionU);
				u -= (RegionOriginalHeight - RegionOffsetY - RegionHeight) / textureWidth;
				v -= (RegionOriginalWidth - RegionOffsetX - RegionWidth) / textureHeight;
				width = RegionOriginalHeight / textureWidth;
				height = RegionOriginalWidth / textureHeight;

				for (int i = 0, n = uvs.Length; i < n; i += 2) {
					uvs[i] = u + regionUVs[i + 1] * width;
					uvs[i + 1] = v + (1 - regionUVs[i]) * height;
				}
			} else if (RegionDegrees == 180) {
				float textureWidth = this.regionWidth / (RegionU2 - RegionU);
				float textureHeight = this.regionHeight / (RegionV2 - RegionV);
				u -= (RegionOriginalWidth - RegionOffsetX - RegionWidth) / textureWidth;
				v -= RegionOffsetY / textureHeight;
				width = RegionOriginalWidth / textureWidth;
				height = RegionOriginalHeight / textureHeight;

				for (int i = 0, n = uvs.Length; i < n; i += 2) {
					uvs[i] = u + (1 - regionUVs[i]) * width;
					uvs[i + 1] = v + (1 - regionUVs[i + 1]) * height;
				}
			} else if (RegionDegrees == 270) {
				float textureWidth = this.regionWidth / (RegionU2 - RegionU);
				float textureHeight = this.regionHeight / (RegionV2 - RegionV);
				u -= RegionOffsetY / textureWidth;
				v -= RegionOffsetX / textureHeight;
				width = RegionOriginalHeight / textureWidth;
				height = RegionOriginalWidth / textureHeight;

				for (int i = 0, n = uvs.Length; i<n; i += 2) {
					uvs[i] = u + (1 - regionUVs[i + 1]) * width;
					uvs[i + 1] = v + regionUVs[i] * height;
				}
			} else {
				float textureWidth = this.regionWidth / (RegionU2 - RegionU);
				float textureHeight = this.regionHeight / (RegionV2 - RegionV);
				u -= RegionOffsetX / textureWidth;
				v -= (RegionOriginalHeight - RegionOffsetY - RegionHeight) / textureHeight;
				width = RegionOriginalWidth / textureWidth;
				height = RegionOriginalHeight / textureHeight;

				for (int i = 0, n = uvs.Length; i < n; i += 2) {
					uvs[i] = u + regionUVs[i] * width;
					uvs[i + 1] = v + regionUVs[i + 1] * height;
				}
			}
		}

		/// <summary>复制此附件及其可变数据，按附件类型保留共享资源引用。</summary>
		public override Attachment Copy () {
			if (parentMesh != null) return NewLinkedMesh();

			MeshAttachment copy = new MeshAttachment(this.Name);
			copy.RendererObject = RendererObject;
			copy.regionOffsetX = regionOffsetX;
			copy.regionOffsetY = regionOffsetY;
			copy.regionWidth = regionWidth;
			copy.regionHeight = regionHeight;
			copy.regionOriginalWidth = regionOriginalWidth;
			copy.regionOriginalHeight = regionOriginalHeight;
			copy.RegionRotate = RegionRotate;
			copy.RegionDegrees = RegionDegrees;
			copy.RegionU = RegionU;
			copy.RegionV = RegionV;
			copy.RegionU2 = RegionU2;
			copy.RegionV2 = RegionV2;

			copy.Path = Path;
			copy.r = r;
			copy.g = g;
			copy.b = b;
			copy.a = a;

			CopyTo(copy);
			copy.regionUVs = new float[regionUVs.Length];
			Array.Copy(regionUVs, 0, copy.regionUVs, 0, regionUVs.Length);
			copy.uvs = new float[uvs.Length];
			Array.Copy(uvs, 0, copy.uvs, 0, uvs.Length);
			copy.triangles = new int[triangles.Length];
			Array.Copy(triangles, 0, copy.triangles, 0, triangles.Length);
			copy.HullLength = HullLength;

			// 非运行必需的辅助数据。 原注： Nonessential.
			if (Edges != null) {
				copy.Edges = new int[Edges.Length];
				Array.Copy(Edges, 0, copy.Edges, 0, Edges.Length);
			}
			copy.Width = Width;
			copy.Height = Height;
			return copy;
		}

		///<summary><para>创建新网格，并将当前网格设为它的父网格，共享可链接的数据。</para>Returns a new mesh with this mesh set as the <see cref="ParentMesh"/>.</summary>
		public MeshAttachment NewLinkedMesh () {
			MeshAttachment mesh = new MeshAttachment(Name);
			mesh.RendererObject = RendererObject;
			mesh.regionOffsetX = regionOffsetX;
			mesh.regionOffsetY = regionOffsetY;
			mesh.regionWidth = regionWidth;
			mesh.regionHeight = regionHeight;
			mesh.regionOriginalWidth = regionOriginalWidth;
			mesh.regionOriginalHeight = regionOriginalHeight;
			mesh.RegionDegrees = RegionDegrees;
			mesh.RegionRotate = RegionRotate;
			mesh.RegionU = RegionU;
			mesh.RegionV = RegionV;
			mesh.RegionU2 = RegionU2;
			mesh.RegionV2 = RegionV2;

			mesh.Path = Path;
			mesh.r = r;
			mesh.g = g;
			mesh.b = b;
			mesh.a = a;

			mesh.deformAttachment = deformAttachment;
			mesh.ParentMesh = parentMesh != null ? parentMesh : this;
			mesh.UpdateUVs();
			return mesh;
		}
	}
}
