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
	/// <summary>变换约束的初始目标、混合权重及偏移配置。</summary>
	public class TransformConstraintData : ConstraintData {
		/// <summary>关联骨骼集合。</summary>
		internal ExposedList<BoneData> bones = new ExposedList<BoneData>();
		/// <summary>约束目标。</summary>
		internal BoneData target;
		/// <summary>rotateMix：旋转混合权重；translateMix：位移混合权重；scaleMix：缩放混合权重；shearMix：切变混合权重。</summary>
		internal float rotateMix, translateMix, scaleMix, shearMix;
		/// <summary>offsetRotation：相对目标的旋转偏移（度）；offsetX：X 偏移；offsetY：Y 偏移；offsetScaleX：目标 X 缩放的附加偏移；offsetScaleY：目标 Y 缩放的附加偏移；offsetShearY：目标 Y 切变的附加角度。</summary>
		internal float offsetRotation, offsetX, offsetY, offsetScaleX, offsetScaleY, offsetShearY;
		/// <summary>relative：是否把目标变换作为相对增量应用；local：是否在局部空间而非世界空间施加约束。</summary>
		internal bool relative, local;

		/// <summary>关联骨骼集合。</summary>
		public ExposedList<BoneData> Bones { get { return bones; } }
		/// <summary>约束目标。</summary>
		public BoneData Target { get { return target; } set { target = value; } }
		/// <summary>旋转混合权重。</summary>
		public float RotateMix { get { return rotateMix; } set { rotateMix = value; } }
		/// <summary>位移混合权重。</summary>
		public float TranslateMix { get { return translateMix; } set { translateMix = value; } }
		/// <summary>缩放混合权重。</summary>
		public float ScaleMix { get { return scaleMix; } set { scaleMix = value; } }
		/// <summary>切变混合权重。</summary>
		public float ShearMix { get { return shearMix; } set { shearMix = value; } }

		/// <summary>相对目标的旋转偏移（度）。</summary>
		public float OffsetRotation { get { return offsetRotation; } set { offsetRotation = value; } }
		/// <summary>X 偏移。</summary>
		public float OffsetX { get { return offsetX; } set { offsetX = value; } }
		/// <summary>Y 偏移。</summary>
		public float OffsetY { get { return offsetY; } set { offsetY = value; } }
		/// <summary>目标 X 缩放的附加偏移。</summary>
		public float OffsetScaleX { get { return offsetScaleX; } set { offsetScaleX = value; } }
		/// <summary>目标 Y 缩放的附加偏移。</summary>
		public float OffsetScaleY { get { return offsetScaleY; } set { offsetScaleY = value; } }
		/// <summary>目标 Y 切变的附加角度。</summary>
		public float OffsetShearY { get { return offsetShearY; } set { offsetShearY = value; } }

		/// <summary>是否把目标变换作为相对增量应用。</summary>
		public bool Relative { get { return relative; } set { relative = value; } }
		/// <summary>是否在局部空间而非世界空间施加约束。</summary>
		public bool Local { get { return local; } set { local = value; } }

		/// <summary>使用指定名称创建变换约束的初始目标、混合权重及偏移配置。</summary>
		public TransformConstraintData (string name) : base(name) {
		}
	}
}
