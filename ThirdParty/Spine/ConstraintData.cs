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
using System.Collections.Generic;

namespace Spine
{
	/// <summary><para>各类约束的共享配置数据基类。</para>The base class for all constraint datas.</summary>
	public abstract class ConstraintData {
		/// <summary>名称。</summary>
		internal readonly string name;
		/// <summary>约束应用顺序。</summary>
		internal int order;
		/// <summary>是否仅在当前皮肤包含此项时激活。</summary>
		internal bool skinRequired;

		/// <summary>使用指定名称创建各约束类型共享的名称、顺序和皮肤依赖配置。</summary>
		public ConstraintData (string name) {
			if (name == null) throw new ArgumentNullException("name", "name cannot be null.");
			this.name = name;
		}

		/// <summary><para>约束名称，在同一骨架的同类约束中唯一。</para> The constraint's name, which is unique across all constraints in the skeleton of the same type.</summary>
		public string Name { get { return name; } }

		///<summary><para>骨架更新时应用该约束的顺序编号。</para>The ordinal of this constraint for the order a skeleton's constraints will be applied by
		/// <see cref="Skeleton.UpdateWorldTransform()"/>.</summary>
		public int Order { get { return order; } set { order = value; } }

		///<summary><para>true 时，只有当前皮肤包含此约束才更新它。</para>When true, <see cref="Skeleton.UpdateWorldTransform()"/> only updates this constraint if the <see cref="Skeleton.Skin"/> contains
		/// this constraint.</summary>
		///<seealso cref="Skin.Constraints"/>
		public bool SkinRequired { get { return skinRequired; } set { skinRequired = value; } }

		/// <summary>返回便于识别当前对象的名称或文本。</summary>
		override public string ToString () {
			return name;
		}
	}
}
