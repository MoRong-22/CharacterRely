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
	/// <summary>骨骼初始姿势和父子关系等共享配置。</summary>
	public class BoneData {
		/// <summary>在所属集合中的索引。</summary>
		internal int index;
		/// <summary>名称。</summary>
		internal string name;
		/// <summary>父骨骼配置；根骨骼为 null。</summary>
		internal BoneData parent;
		/// <summary>骨骼长度。</summary>
		internal float length;
		/// <summary>x：X 位置；y：Y 位置；rotation：局部旋转角度（度）；scaleX：X 方向缩放倍率；scaleY：Y 方向缩放倍率；shearX：X 方向切变角度；shearY：Y 方向切变角度。</summary>
		internal float x, y, rotation, scaleX = 1, scaleY = 1, shearX, shearY;
		/// <summary>父骨骼变换继承方式。</summary>
		internal TransformMode transformMode = TransformMode.Normal;
		/// <summary>是否仅在当前皮肤包含此项时激活。</summary>
		internal bool skinRequired;

		/// <summary><para>骨骼在 Skeleton.Bones 中的索引。</para>The index of the bone in Skeleton.Bones</summary>
		public int Index { get { return index; } }

		/// <summary><para>骨骼名称，在同一骨架内唯一。</para>The name of the bone, which is unique across all bones in the skeleton.</summary>
		public string Name { get { return name; } }

		/// <summary><para>允许为 null。</para>May be null.</summary>
		public BoneData Parent { get { return parent; } }

		/// <summary>骨骼长度。</summary>
		public float Length { get { return length; } set { length = value; } }

		/// <summary><para>初始姿势的局部 X 位移。</para>Local X translation.</summary>
		public float X { get { return x; } set { x = value; } }

		/// <summary><para>初始姿势的局部 Y 位移。</para>Local Y translation.</summary>
		public float Y { get { return y; } set { y = value; } }

		/// <summary><para>初始姿势的局部旋转，单位为角度。</para>Local rotation.</summary>
		public float Rotation { get { return rotation; } set { rotation = value; } }

		/// <summary><para>初始姿势的局部 X 缩放。</para>Local scaleX.</summary>
		public float ScaleX { get { return scaleX; } set { scaleX = value; } }

		/// <summary><para>初始姿势的局部 Y 缩放。</para>Local scaleY.</summary>
		public float ScaleY { get { return scaleY; } set { scaleY = value; } }

		/// <summary><para>初始姿势的局部 X 切变角度。</para>Local shearX.</summary>
		public float ShearX { get { return shearX; } set { shearX = value; } }

		/// <summary><para>初始姿势的局部 Y 切变角度。</para>Local shearY.</summary>
		public float ShearY { get { return shearY; } set { shearY = value; } }

		/// <summary><para>决定此骨骼如何继承父骨骼世界变换的模式。</para>The transform mode for how parent world transforms affect this bone.</summary>
		public TransformMode TransformMode { get { return transformMode; } set { transformMode = value; } }

		///<summary><para>true 时，仅当前皮肤包含此骨骼才更新它。</para>When true, <see cref="Skeleton.UpdateWorldTransform()"/> only updates this bone if the <see cref="Skeleton.Skin"/> contains this
		/// bone.</summary>
		/// <seealso cref="Skin.Bones"/>
		public bool SkinRequired { get { return skinRequired; } set { skinRequired = value; } }

		/// <summary>允许为 null。</summary>
		/// <param name="parent">父骨骼，根骨骼时为 null。 May be null.</param>
		public BoneData (int index, string name, BoneData parent) {
			if (index < 0) throw new ArgumentException("index must be >= 0", "index");
			if (name == null) throw new ArgumentNullException("name", "name cannot be null.");
			this.index = index;
			this.name = name;
			this.parent = parent;
		}

		/// <summary>返回便于识别当前对象的名称或文本。</summary>
		override public string ToString () {
			return name;
		}
	}

	/// <summary>骨骼对父骨骼旋转、缩放和镜像的继承方式。</summary>
	[Flags]
	public enum TransformMode {
		// 参考位标记布局：翻转、缩放和旋转继承。 原注：0000 0 Flip Scale Rotation
		/// <summary>继承父骨骼位移、旋转、缩放和镜像。</summary>
		Normal = 0, // 0000
		/// <summary>只继承父骨骼位移。</summary>
		OnlyTranslation = 7, // 0111
		/// <summary>不继承父骨骼旋转和镜像。</summary>
		NoRotationOrReflection = 1, // 0001
		/// <summary>不继承父骨骼缩放。</summary>
		NoScale = 2, // 0010
		/// <summary>不继承父骨骼缩放和镜像。</summary>
		NoScaleOrReflection = 6, // 0110
	}
}
