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
	/// <summary>路径约束的初始目标、骨骼集合及位置、间距和旋转参数。</summary>
	public class PathConstraintData : ConstraintData {
		/// <summary>关联骨骼集合。</summary>
		internal ExposedList<BoneData> bones = new ExposedList<BoneData>();
		/// <summary>约束目标。</summary>
		internal SlotData target;
		/// <summary>路径位置的固定长度或比例计量模式。</summary>
		internal PositionMode positionMode;
		/// <summary>路径骨骼间距计量模式。</summary>
		internal SpacingMode spacingMode;
		/// <summary>沿路径的旋转及拉伸模式。</summary>
		internal RotateMode rotateMode;
		/// <summary>相对目标的旋转偏移（度）。</summary>
		internal float offsetRotation;
		/// <summary>position：沿路径的位置；spacing：沿路径的骨骼间距；rotateMix：旋转混合权重；translateMix：位移混合权重。</summary>
		internal float position, spacing, rotateMix, translateMix;

		/// <summary>使用指定名称创建路径约束的初始目标、骨骼集合及位置、间距和旋转参数。</summary>
		public PathConstraintData (string name) : base(name) {
		}

		/// <summary>关联骨骼集合。</summary>
		public ExposedList<BoneData> Bones { get { return bones; } }
		/// <summary>约束目标。</summary>
		public SlotData Target { get { return target; } set { target = value; } }
		/// <summary>路径位置的固定长度或比例计量模式。</summary>
		public PositionMode PositionMode { get { return positionMode; } set { positionMode = value; } }
		/// <summary>路径骨骼间距计量模式。</summary>
		public SpacingMode SpacingMode { get { return spacingMode; } set { spacingMode = value; } }
		/// <summary>沿路径的旋转及拉伸模式。</summary>
		public RotateMode RotateMode { get { return rotateMode; } set { rotateMode = value; } }
		/// <summary>相对目标的旋转偏移（度）。</summary>
		public float OffsetRotation { get { return offsetRotation; } set { offsetRotation = value; } }
		/// <summary>沿路径的位置。</summary>
		public float Position { get { return position; } set { position = value; } }
		/// <summary>沿路径的骨骼间距。</summary>
		public float Spacing { get { return spacing; } set { spacing = value; } }
		/// <summary>旋转混合权重。</summary>
		public float RotateMix { get { return rotateMix; } set { rotateMix = value; } }
		/// <summary>位移混合权重。</summary>
		public float TranslateMix { get { return translateMix; } set { translateMix = value; } }
	}

	/// <summary>路径位置的计量方式。</summary>
	public enum PositionMode {
		/// <summary>使用固定长度单位。</summary>
		Fixed, 
			/// <summary>使用相对路径总长的比例。</summary>
			Percent
	}

	/// <summary>路径上骨骼间距的计量方式。</summary>
	public enum SpacingMode {
		/// <summary>间距包含骨骼原始长度。</summary>
		Length, 
			/// <summary>使用固定长度单位。</summary>
			Fixed, 
			/// <summary>使用相对路径总长的比例。</summary>
			Percent
	}

	/// <summary>骨骼沿路径旋转或拉伸的方式。</summary>
	public enum RotateMode {
		/// <summary>使骨骼方向跟随路径切线。</summary>
		Tangent, 
			/// <summary>使骨骼沿相邻路径采样点连成链。</summary>
			Chain, 
			/// <summary>沿路径连成链，并调整骨骼长度。</summary>
			ChainScale
	}
}
