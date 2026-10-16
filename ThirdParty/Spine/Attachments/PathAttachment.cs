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
	/// <summary>由贝塞尔曲线组成、供路径约束使用的路径附件。</summary>
	public class PathAttachment : VertexAttachment {
		/// <summary>骨骼或曲线长度缓存。</summary>
		internal float[] lengths;
		/// <summary>closed：路径首尾是否闭合；constantSpeed：路径约束是否按曲线长度均匀采样。</summary>
		internal bool closed, constantSpeed;

		/// <summary><para>初始姿势中，从路径起点到每条曲线终点的累计长度。</para>The length in the setup pose from the start of the path to the end of each curve.</summary>
		public float[] Lengths { get { return lengths; } set { lengths = value; } }
		/// <summary>路径首尾是否闭合。</summary>
		public bool Closed { get { return closed; } set { closed = value; } }
		/// <summary>路径约束是否按曲线长度均匀采样。</summary>
		public bool ConstantSpeed { get { return constantSpeed; } set { constantSpeed = value; } }

		/// <summary>使用指定名称创建由贝塞尔曲线组成、供路径约束使用的路径附件。</summary>
		public PathAttachment (String name)
			: base(name) {
		}

		/// <summary>复制此附件及其可变数据，按附件类型保留共享资源引用。</summary>
		public override Attachment Copy () {
			PathAttachment copy = new PathAttachment(this.Name);
			CopyTo(copy);
			copy.lengths = new float[lengths.Length];
			Array.Copy(lengths, 0, copy.lengths, 0, lengths.Length);
			copy.closed = closed;
			copy.constantSpeed = constantSpeed;
			return copy;
		}
	}
}
