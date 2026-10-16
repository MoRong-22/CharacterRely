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
	/// <summary>插槽初始颜色、附件名称、骨骼归属和混合模式。</summary>
	public class SlotData {
		/// <summary>在所属集合中的索引。</summary>
		internal int index;
		/// <summary>名称。</summary>
		internal string name;
		/// <summary>所属骨骼初始配置。</summary>
		internal BoneData boneData;
		/// <summary>r：红色分量；g：绿色分量；b：蓝色分量；a：透明度分量。</summary>
		internal float r = 1, g = 1, b = 1, a = 1;
		/// <summary>r2：双色着色的暗部红色分量；g2：双色着色的暗部绿色分量；b2：双色着色的暗部蓝色分量。</summary>
		internal float r2 = 0, g2 = 0, b2 = 0;
		/// <summary>是否启用双色着色的暗部 RGB。</summary>
		internal bool hasSecondColor = false;
		/// <summary>初始姿势所用附件的名称。</summary>
		internal string attachmentName;
		/// <summary>附件绘制的颜色混合方式。</summary>
		internal BlendMode blendMode;

		/// <summary><para>插槽在骨架插槽集合中的索引。</para>The index of the slot in <see cref="Skeleton.Slots"/>.</summary>
		public int Index { get { return index; } }
		/// <summary><para>插槽名称，在同一骨架内唯一。</para>The name of the slot, which is unique across all slots in the skeleton.</summary>
		public string Name { get { return name; } }
		/// <summary><para>此插槽所属骨骼的配置数据。</para>The bone this slot belongs to.</summary>
		public BoneData BoneData { get { return boneData; } }
		/// <summary>红色分量。</summary>
		public float R { get { return r; } set { r = value; } }
		/// <summary>绿色分量。</summary>
		public float G { get { return g; } set { g = value; } }
		/// <summary>蓝色分量。</summary>
		public float B { get { return b; } set { b = value; } }
		/// <summary>透明度分量。</summary>
		public float A { get { return a; } set { a = value; } }

		/// <summary>双色着色的暗部红色分量。</summary>
		public float R2 { get { return r2; } set { r2 = value; } }
		/// <summary>双色着色的暗部绿色分量。</summary>
		public float G2 { get { return g2; } set { g2 = value; } }
		/// <summary>双色着色的暗部蓝色分量。</summary>
		public float B2 { get { return b2; } set { b2 = value; } }
		/// <summary>是否启用双色着色的暗部 RGB。</summary>
		public bool HasSecondColor { get { return hasSecondColor; } set { hasSecondColor = value; } }

		/// <summary><para>初始姿势中可见的附件名称，null 表示初始不显示附件。</para>The name of the attachment that is visible for this slot in the setup pose, or null if no attachment is visible.</summary>
		public String AttachmentName { get { return attachmentName; } set { attachmentName = value; } }
		/// <summary><para>绘制插槽附件时采用的混合模式。</para>The blend mode for drawing the slot's attachment.</summary>
		public BlendMode BlendMode { get { return blendMode; } set { blendMode = value; } }

		/// <summary>使用指定名称创建插槽初始颜色、附件名称、骨骼归属和混合模式。</summary>
		public SlotData (int index, String name, BoneData boneData) {
			if (index < 0) throw new ArgumentException ("index must be >= 0.", "index");
			if (name == null) throw new ArgumentNullException("name", "name cannot be null.");
			if (boneData == null) throw new ArgumentNullException("boneData", "boneData cannot be null.");
			this.index = index;
			this.name = name;
			this.boneData = boneData;
		}

		/// <summary>返回便于识别当前对象的名称或文本。</summary>
		override public string ToString () {
			return name;
		}
	}
}
