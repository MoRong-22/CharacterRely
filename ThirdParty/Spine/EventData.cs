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
	/// <summary><para>模型中事件定义的默认参数。</para>Stores the setup pose values for an Event.</summary>
	public class EventData {
		/// <summary>名称。</summary>
		internal string name;

		/// <summary><para>事件名称，在同一骨架内唯一。</para>The name of the event, which is unique across all events in the skeleton.</summary>
		public string Name { get { return name; } }
		/// <summary>事件整数参数。</summary>
		public int Int { get; set; }
		/// <summary>事件浮点参数。</summary>
		public float Float { get; set; }
		/// <summary>事件字符串参数。</summary>
		public string @String { get; set; }

		/// <summary>事件音频资源路径。</summary>
		public string AudioPath { get; set; }
		/// <summary>事件音频音量。</summary>
		public float Volume { get; set; }
		/// <summary>事件音频左右声道平衡。</summary>
		public float Balance { get; set; }

		/// <summary>使用指定名称创建模型中事件定义的默认参数。</summary>
		public EventData (string name) {
			if (name == null) throw new ArgumentNullException("name", "name cannot be null.");
			this.name = name;
		}

		/// <summary>返回便于识别当前对象的名称或文本。</summary>
		override public string ToString () {
			return Name;
		}
	}
}
