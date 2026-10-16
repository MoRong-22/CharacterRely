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

namespace Spine {
	/// <summary><para>IK 约束的初始姿势参数。</para>Stores the setup pose for an IkConstraint.</summary>
	public class IkConstraintData : ConstraintData {
		/// <summary>关联骨骼集合。</summary>
		internal ExposedList<BoneData> bones = new ExposedList<BoneData>();
		/// <summary>约束目标。</summary>
		internal BoneData target;
		/// <summary>IK 弯曲方向，取 1 或 -1。</summary>
		internal int bendDirection = 1;
		/// <summary>compress：目标太近时是否压缩骨骼；stretch：目标太远时是否拉伸骨骼；uniform：压缩或拉伸时是否同时缩放两轴。</summary>
		internal bool compress, stretch, uniform;
		/// <summary>mix：约束混合权重；softness：接近最大可达距离时的 IK 柔化距离。</summary>
		internal float mix = 1, softness;

		/// <summary>使用指定名称创建IK 约束初始骨骼、目标和弯曲拉伸参数。</summary>
		public IkConstraintData (string name) : base(name) {
		}

		/// <summary><para>受此 IK 约束影响的骨骼配置集合。</para>The bones that are constrained by this IK Constraint.</summary>
		public ExposedList<BoneData> Bones {
			get { return bones; }
		}

		/// <summary><para>IK 目标骨骼。</para>The bone that is the IK target.</summary>
		public BoneData Target {
			get { return target; }
			set { target = value; }
		}

		/// <summary><para>约束与未约束旋转之间的混合比例，范围 0～1。</para>
		/// A percentage (0-1) that controls the mix between the constraint and unconstrained rotations.</summary>
		public float Mix {
			get { return mix; }
			set { mix = value; }
		}

		///<summary><para>双骨骼 IK 接近最大可达距离时开始减缓旋转的柔化距离。</para>For two bone IK, the distance from the maximum reach of the bones that rotation will slow.</summary>
		public float Softness {
			get { return softness; }
			set { softness = value; }
		}

		/// <summary><para>IK 弯曲方向，取 1 或 -1。</para>Controls the bend direction of the IK bones, either 1 or -1.</summary>
		public int BendDirection {
			get { return bendDirection; }
			set { bendDirection = value; }
		}

		/// <summary><para>单骨骼 IK 的压缩开关，目标太近时缩短骨骼到达目标。</para>
		/// When true, and only a single bone is being constrained,
		/// if the target is too close, the bone is scaled to reach it. </summary>
		public bool Compress {
			get { return compress; }
			set { compress = value; }
		}

		/// <summary><para>目标太远时沿 X 轴拉伸父骨骼；局部非等比缩放时不拉伸。</para>
		/// When true, if the target is out of range, the parent bone is scaled on the X axis to reach it.
		/// If the bone has local nonuniform scale, stretching is not applied.</summary>
		public bool Stretch {
			get { return stretch; }
			set { stretch = value; }
		}

		/// <summary><para>单骨骼压缩或拉伸时是否同时缩放 X、Y 两轴。</para>
		/// When true, only a single bone is being constrained and Compress or Stretch is used,
		/// the bone is scaled both on the X and Y axes.</summary>
		public bool Uniform {
			get { return uniform; }
			set { uniform = value; }
		}
	}
}
