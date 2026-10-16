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
	/// <summary><para>显示单个图集纹理区域的四边形附件。</para>Attachment that displays a texture region.</summary>
	public class RegionAttachment : Attachment, IHasRendererObject {
		/// <summary>下左角顶点的 X 坐标在数组中的索引。</summary>
		public const int BLX = 0;
		/// <summary>下左角顶点的 Y 坐标在数组中的索引。</summary>
		public const int BLY = 1;
		/// <summary>上左角顶点的 X 坐标在数组中的索引。</summary>
		public const int ULX = 2;
		/// <summary>上左角顶点的 Y 坐标在数组中的索引。</summary>
		public const int ULY = 3;
		/// <summary>上右角顶点的 X 坐标在数组中的索引。</summary>
		public const int URX = 4;
		/// <summary>上右角顶点的 Y 坐标在数组中的索引。</summary>
		public const int URY = 5;
		/// <summary>下右角顶点的 X 坐标在数组中的索引。</summary>
		public const int BRX = 6;
		/// <summary>下右角顶点的 Y 坐标在数组中的索引。</summary>
		public const int BRY = 7;

		/// <summary>x：X 位置；y：Y 位置；rotation：局部旋转角度（度）；scaleX：X 方向缩放倍率；scaleY：Y 方向缩放倍率；width：宽度；height：高度。</summary>
		internal float x, y, rotation, scaleX = 1, scaleY = 1, width, height;
		/// <summary>regionOffsetX：区域裁剪产生的 X 偏移；regionOffsetY：区域裁剪产生的 Y 偏移；regionWidth：图集区域裁剪后宽度；regionHeight：图集区域裁剪后高度；regionOriginalWidth：图集区域裁剪前宽度；regionOriginalHeight：图集区域裁剪前高度。</summary>
		internal float regionOffsetX, regionOffsetY, regionWidth, regionHeight, regionOriginalWidth, regionOriginalHeight;
		/// <summary>offset：四边形顶点的局部偏移数组；uvs：映射到整张图集的顶点 UV 数组。</summary>
		internal float[] offset = new float[8], uvs = new float[8];
		/// <summary>r：红色分量；g：绿色分量；b：蓝色分量；a：透明度分量。</summary>
		internal float r = 1, g = 1, b = 1, a = 1;

		/// <summary>X 位置。</summary>
		public float X { get { return x; } set { x = value; } }
		/// <summary>Y 位置。</summary>
		public float Y { get { return y; } set { y = value; } }
		/// <summary>局部旋转角度（度）。</summary>
		public float Rotation { get { return rotation; } set { rotation = value; } }
		/// <summary>X 方向缩放倍率。</summary>
		public float ScaleX { get { return scaleX; } set { scaleX = value; } }
		/// <summary>Y 方向缩放倍率。</summary>
		public float ScaleY { get { return scaleY; } set { scaleY = value; } }
		/// <summary>宽度。</summary>
		public float Width { get { return width; } set { width = value; } }
		/// <summary>高度。</summary>
		public float Height { get { return height; } set { height = value; } }

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

		/// <summary>四边形顶点的局部偏移数组。</summary>
		public float[] Offset { get { return offset; } }
		/// <summary>四角纹理坐标，映射到整张图集纹理的归一化 UV。</summary>
		public float[] UVs { get { return uvs; } }

		/// <summary>使用指定名称创建把单个图集区域绘制为四边形的附件。</summary>
		public RegionAttachment (string name)
			: base(name) {
		}

		/// <summary>根据局部位置、旋转、缩放与区域裁剪信息重算四角偏移。</summary>
		public void UpdateOffset () {
			float width = this.width;
			float height = this.height;
			float localX2 = width * 0.5f;
			float localY2 = height * 0.5f;
			float localX = -localX2;
			float localY = -localY2;
			if (regionOriginalWidth != 0) { // 保留的参考条件：图集区域存在时才处理。 原注： if (region != null)
				localX += regionOffsetX / regionOriginalWidth * width;
				localY += regionOffsetY / regionOriginalHeight * height;
				localX2 -= (regionOriginalWidth - regionOffsetX - regionWidth) / regionOriginalWidth * width;
				localY2 -= (regionOriginalHeight - regionOffsetY - regionHeight) / regionOriginalHeight * height;
			}
			float scaleX = this.scaleX;
			float scaleY = this.scaleY;
			localX *= scaleX;
			localY *= scaleY;
			localX2 *= scaleX;
			localY2 *= scaleY;
			float rotation = this.rotation;
			float cos = MathUtils.CosDeg(rotation);
			float sin = MathUtils.SinDeg(rotation);
			float x = this.x;
			float y = this.y;
			float localXCos = localX * cos + x;
			float localXSin = localX * sin;
			float localYCos = localY * cos + y;
			float localYSin = localY * sin;
			float localX2Cos = localX2 * cos + x;
			float localX2Sin = localX2 * sin;
			float localY2Cos = localY2 * cos + y;
			float localY2Sin = localY2 * sin;
			float[] offset = this.offset;
			offset[BLX] = localXCos - localYSin;
			offset[BLY] = localYCos + localXSin;
			offset[ULX] = localXCos - localY2Sin;
			offset[ULY] = localY2Cos + localXSin;
			offset[URX] = localX2Cos - localY2Sin;
			offset[URY] = localY2Cos + localX2Sin;
			offset[BRX] = localX2Cos - localYSin;
			offset[BRY] = localYCos + localX2Sin;
		}

		/// <summary>设置图集 UV 范围，并按旋转标记排列四角纹理坐标。</summary>
		public void SetUVs (float u, float v, float u2, float v2, bool rotate) {
			float[] uvs = this.uvs;
			// UV 数组布局与 Java 的 RegionAttachment 参考实现不同。 原注： UV values differ from RegionAttachment.java
			if (rotate) {
				uvs[URX] = u;
				uvs[URY] = v2;
				uvs[BRX] = u;
				uvs[BRY] = v;
				uvs[BLX] = u2;
				uvs[BLY] = v;
				uvs[ULX] = u2;
				uvs[ULY] = v2;
			} else {
				uvs[ULX] = u;
				uvs[ULY] = v2;
				uvs[URX] = u;
				uvs[URY] = v;
				uvs[BRX] = u2;
				uvs[BRY] = v;
				uvs[BLX] = u2;
				uvs[BLY] = v2;
			}
		}

		/// <summary><para>将附件四个顶点转换到世界坐标；bone 为父骨骼，输出数组应至少容纳 offset 后的 8 个值，stride 为相邻坐标对的写入步长。</para>Transforms the attachment's four vertices to world coordinates.</summary>
		/// <param name="bone">附件所属的父骨骼。 The parent bone.</param>
		/// <param name="worldVertices">输出世界坐标数组；默认步长下长度至少为 offset + 8。 The output world vertices. Must have a length greater than or equal to offset + 8.</param>
		/// <param name="offset">开始写入输出数组的索引。 The worldVertices index to begin writing values.</param>
		/// <param name="stride">相邻顶点坐标对在输出数组中的步长。 The number of worldVertices entries between the value pairs written.</param>
		public void ComputeWorldVertices (Bone bone, float[] worldVertices, int offset, int stride = 2) {
			float[] vertexOffset = this.offset;
			float bwx = bone.worldX, bwy = bone.worldY;
			float a = bone.a, b = bone.b, c = bone.c, d = bone.d;
			float offsetX, offsetY;

			// 顶点顺序与 Java 的 RegionAttachment 参考实现不同。 原注： Vertex order is different from RegionAttachment.java
			offsetX = vertexOffset[BRX]; // 0
			offsetY = vertexOffset[BRY]; // 1
			worldVertices[offset] = offsetX * a + offsetY * b + bwx; // 左下角顶点。 原注： bl
			worldVertices[offset + 1] = offsetX * c + offsetY * d + bwy;
			offset += stride;

			offsetX = vertexOffset[BLX]; // 2
			offsetY = vertexOffset[BLY]; // 3
			worldVertices[offset] = offsetX * a + offsetY * b + bwx; // 左上角顶点。 原注： ul
			worldVertices[offset + 1] = offsetX * c + offsetY * d + bwy;
			offset += stride;

			offsetX = vertexOffset[ULX]; // 4
			offsetY = vertexOffset[ULY]; // 5
			worldVertices[offset] = offsetX * a + offsetY * b + bwx; // 右上角顶点。 原注： ur
			worldVertices[offset + 1] = offsetX * c + offsetY * d + bwy;
			offset += stride;

			offsetX = vertexOffset[URX]; // 6
			offsetY = vertexOffset[URY]; // 7
			worldVertices[offset] = offsetX * a + offsetY * b + bwx; // 右下角顶点。 原注： br
			worldVertices[offset + 1] = offsetX * c + offsetY * d + bwy;
			// 保留的参考代码：推进输出偏移一个步长。 原注：offset += stride;
		}

		/// <summary>复制此附件及其可变数据，按附件类型保留共享资源引用。</summary>
		public override Attachment Copy () {
			RegionAttachment copy = new RegionAttachment(this.Name);
			copy.RendererObject = RendererObject;
			copy.regionOffsetX = regionOffsetX;
			copy.regionOffsetY = regionOffsetY;
			copy.regionWidth = regionWidth;
			copy.regionHeight = regionHeight;
			copy.regionOriginalWidth = regionOriginalWidth;
			copy.regionOriginalHeight = regionOriginalHeight;
			copy.Path = Path;
			copy.x = x;
			copy.y = y;
			copy.scaleX = scaleX;
			copy.scaleY = scaleY;
			copy.rotation = rotation;
			copy.width = width;
			copy.height = height;
			Array.Copy(uvs, 0, copy.uvs, 0, 8);
			Array.Copy(offset, 0, copy.offset, 0, 8);
			copy.r = r;
			copy.g = g;
			copy.b = b;
			copy.a = a;
			return copy;
		}
	}
}
