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

namespace Spine {
	/// <summary><para>包含单个位置和角度的点附件，可用作弹幕、粒子挂点；比骨骼计算更轻量，能放入皮肤并切换显示。</para>
	/// An attachment which is a single point and a rotation. This can be used to spawn projectiles, particles, etc. A bone can be
	/// used in similar ways, but a PointAttachment is slightly less expensive to compute and can be hidden, shown, and placed in a
	/// skin.
	/// <para>
	/// See <a href="http://esotericsoftware.com/spine-point-attachments">Point Attachments</a> in the Spine User Guide.
	/// </para>
	/// </summary>
	public class PointAttachment : Attachment {
		/// <summary>x：X 位置；y：Y 位置；rotation：局部旋转角度（度）。</summary>
		internal float x, y, rotation;
		/// <summary>X 位置。</summary>
		public float X { get { return x; } set { x = value; } }
		/// <summary>Y 位置。</summary>
		public float Y { get { return y; } set { y = value; } }
		/// <summary>局部旋转角度（度）。</summary>
		public float Rotation { get { return rotation; } set { rotation = value; } }

		/// <summary>使用指定名称创建提供局部位置和角度的点挂件，可用于粒子或武器挂点。</summary>
		public PointAttachment (string name)
			: base(name) {
		}

		/// <summary>结合所属骨骼的世界矩阵，计算点附件的世界位置。</summary>
		public void ComputeWorldPosition (Bone bone, out float ox, out float oy) {
			bone.LocalToWorld(this.x, this.y, out ox, out oy);
		}

		/// <summary>结合所属骨骼的世界矩阵，计算点附件的世界角度（度）。</summary>
		public float ComputeWorldRotation (Bone bone) {
			float cos = MathUtils.CosDeg(rotation), sin = MathUtils.SinDeg(rotation);
			float ix = cos * bone.a + sin * bone.b;
			float iy = cos * bone.c + sin * bone.d;
			return MathUtils.Atan2(iy, ix) * MathUtils.RadDeg;
		}

		/// <summary>复制此附件及其可变数据，按附件类型保留共享资源引用。</summary>
		public override Attachment Copy () {
			PointAttachment copy = new PointAttachment(this.Name);
			copy.x = x;
			copy.y = y;
			copy.rotation = rotation;
			return copy;
		}
	}
}
