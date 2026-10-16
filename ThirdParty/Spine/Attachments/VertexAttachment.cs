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
	/// <summary><para>顶点附件：顶点可由一个或多个骨骼变换，并可使用插槽变形数据。</para>>An attachment with vertices that are transformed by one or more bones and can be deformed by a slot's
	/// <see cref="Slot.Deform"/>.</summary>
	public abstract class VertexAttachment : Attachment {
		/// <summary>下一个顶点附件的唯一编号。</summary>
		static int nextID = 0;
		/// <summary>保护附件唯一编号分配的同步锁。</summary>
		static readonly Object nextIdLock = new Object();

		/// <summary>附件唯一编号。</summary>
		internal readonly int id;
		/// <summary>带权重顶点的骨骼数目和索引编码；无权重时为 null。</summary>
		internal int[] bones;
		/// <summary>局部顶点或带权重顶点记录，具体布局由 bones 是否为空决定。</summary>
		internal float[] vertices;
		/// <summary>输出世界顶点的浮点值数量，每顶点两个值。</summary>
		internal int worldVerticesLength;
		/// <summary>决定可继承哪些变形关键帧的附件。</summary>
		internal VertexAttachment deformAttachment;

		/// <summary><para>返回当前附件的唯一编号。</para>Gets a unique ID for this attachment.</summary>
		public int Id { get { return id; } }
		/// <summary>有权重顶点关联的骨骼索引编码；无权重附件为 null。</summary>
		public int[] Bones { get { return bones; } set { bones = value; } }
		/// <summary>无权重时保存 X、Y；有权重时保存各骨骼影响的 X、Y 和权重。</summary>
		public float[] Vertices { get { return vertices; } set { vertices = value; } }
		/// <summary>输出世界顶点的浮点值数量，每顶点两个值。</summary>
		public int WorldVerticesLength { get { return worldVerticesLength; } set { worldVerticesLength = value; } }
		///<summary><para>指定哪些附件的变形关键帧也可应用到此附件；null 表示不接受变形关键帧。</para>Deform keys for the deform attachment are also applied to this attachment.
		/// May be null if no deform keys should be applied.</summary>
		public VertexAttachment DeformAttachment { get { return deformAttachment; } set { deformAttachment = value; } }

		/// <summary>使用指定名称创建支持骨骼权重和插槽变形的顶点附件基类。</summary>
		public VertexAttachment (string name)
			: base(name) {

			deformAttachment = this;
			lock (VertexAttachment.nextIdLock) {
				id = (VertexAttachment.nextID++ & 65535) << 11;
			}
		}

		/// <summary>计算全部顶点的世界位置，并应用骨骼权重和插槽变形；输出数组须有足够容量。</summary>
		public void ComputeWorldVertices (Slot slot, float[] worldVertices) {
			ComputeWorldVertices(slot, 0, worldVerticesLength, worldVertices, 0);
		}

		/// <summary><para>把局部顶点转换为世界坐标，并应用插槽变形。start、count 按浮点值计数，每顶点两个值；offset 指定输出起点，stride 指定相邻坐标对步长。</para>
		/// Transforms the attachment's local <see cref="Vertices"/> to world coordinates. If the slot's <see cref="Slot.Deform"/> is
		/// not empty, it is used to deform the vertices.
		/// <para />
		/// See <a href="http://esotericsoftware.com/spine-runtime-skeletons#World-transforms">World transforms</a> in the Spine
		/// Runtimes Guide.
		/// </summary>
		/// <param name="start">首个参与转换的浮点值索引，每顶点包含 X、Y 两个值。 The index of the first <see cref="Vertices"/> value to transform. Each vertex has 2 values, x and y.</param>
		/// <param name="count">输出坐标的浮点值数量，不能超过 WorldVerticesLength - start。 The number of world vertex values to output. Must be less than or equal to <see cref="WorldVerticesLength"/> - start.</param>
		/// <param name="worldVertices">输出世界顶点数组，须有足够容量容纳指定偏移、步长和顶点数量。 The output world vertices. Must have a length greater than or equal to <paramref name="offset"/> + <paramref name="count"/>.</param>
		/// <param name="offset">输出数组开始写入的位置。 The <paramref name="worldVertices"/> index to begin writing values.</param>
		/// <param name="stride">相邻顶点坐标对之间的数组步长。 The number of <paramref name="worldVertices"/> entries between the value pairs written.</param>
		public void ComputeWorldVertices (Slot slot, int start, int count, float[] worldVertices, int offset, int stride = 2) {
			count = offset + (count >> 1) * stride;
			Skeleton skeleton = slot.bone.skeleton;
			var deformArray = slot.deform;
			float[] vertices = this.vertices;
			int[] bones = this.bones;
			if (bones == null) {
				if (deformArray.Count > 0) vertices = deformArray.Items;
				Bone bone = slot.bone;
				float x = bone.worldX, y = bone.worldY;
				float a = bone.a, b = bone.b, c = bone.c, d = bone.d;
				for (int vv = start, w = offset; w < count; vv += 2, w += stride) {
					float vx = vertices[vv], vy = vertices[vv + 1];
					worldVertices[w] = vx * a + vy * b + x;
					worldVertices[w + 1] = vx * c + vy * d + y;
				}
				return;
			}
			int v = 0, skip = 0;
			for (int i = 0; i < start; i += 2) {
				int n = bones[v];
				v += n + 1;
				skip += n;
			}
			var skeletonBones = skeleton.bones.Items;
			if (deformArray.Count == 0) {
				for (int w = offset, b = skip * 3; w < count; w += stride) {
					float wx = 0, wy = 0;
					int n = bones[v++];
					n += v;
					for (; v < n; v++, b += 3) {
						Bone bone = skeletonBones[bones[v]];
						float vx = vertices[b], vy = vertices[b + 1], weight = vertices[b + 2];
						wx += (vx * bone.a + vy * bone.b + bone.worldX) * weight;
						wy += (vx * bone.c + vy * bone.d + bone.worldY) * weight;
					}
					worldVertices[w] = wx;
					worldVertices[w + 1] = wy;
				}
			} else {
				float[] deform = deformArray.Items;
				for (int w = offset, b = skip * 3, f = skip << 1; w < count; w += stride) {
					float wx = 0, wy = 0;
					int n = bones[v++];
					n += v;
					for (; v < n; v++, b += 3, f += 2) {
						Bone bone = skeletonBones[bones[v]];
						float vx = vertices[b] + deform[f], vy = vertices[b + 1] + deform[f + 1], weight = vertices[b + 2];
						wx += (vx * bone.a + vy * bone.b + bone.worldX) * weight;
						wy += (vx * bone.c + vy * bone.d + bone.worldY) * weight;
					}
					worldVertices[w] = wx;
					worldVertices[w + 1] = wy;
				}
			}
		}

		///<summary><para>复制时不复制唯一编号（自动生成）及名称（构造时指定）。</para>Does not copy id (generated) or name (set on construction).</summary>
		internal void CopyTo (VertexAttachment attachment) {
			if (bones != null) {
				attachment.bones = new int[bones.Length];
				Array.Copy(bones, 0, attachment.bones, 0, bones.Length);
			}
			else
				attachment.bones = null;

			if (vertices != null) {
				attachment.vertices = new float[vertices.Length];
				Array.Copy(vertices, 0, attachment.vertices, 0, vertices.Length);
			}
			else
				attachment.vertices = null;

			attachment.worldVerticesLength = worldVerticesLength;
			attachment.deformAttachment = deformAttachment;
		}
	}
}
