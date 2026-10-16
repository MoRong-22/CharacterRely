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
	/// <summary><para>一个关键帧事件实例的当前参数值。</para>Stores the current pose values for an Event.</summary>
	public class Event {
		/// <summary>初始配置数据。</summary>
		internal readonly EventData data;
		/// <summary>累计时间（秒）。</summary>
		internal readonly float time;
		/// <summary>事件携带的整数参数。</summary>
		internal int intValue;
		/// <summary>事件携带的浮点参数。</summary>
		internal float floatValue;
		/// <summary>事件携带的字符串参数。</summary>
		internal string stringValue;
		/// <summary>事件音频音量。</summary>
		internal float volume;
		/// <summary>事件音频左右声道平衡。</summary>
		internal float balance;

		/// <summary>初始配置数据。</summary>
		public EventData Data { get { return data; } }
		/// <summary><para>事件关键帧所在的动画时间，单位为秒。</para>The animation time this event was keyed.</summary>
		public float Time { get { return time; } }

		/// <summary>事件整数参数。</summary>
		public int Int { get { return intValue; } set { intValue = value; } }
		/// <summary>事件浮点参数。</summary>
		public float Float { get { return floatValue; } set { floatValue = value; } }
		/// <summary>事件字符串参数。</summary>
		public string String { get { return stringValue; } set { stringValue = value; } }

		/// <summary>事件音频音量。</summary>
		public float Volume { get { return volume; } set { volume = value; } }
		/// <summary>事件音频左右声道平衡。</summary>
		public float Balance { get { return balance; } set { balance = value; } }

		/// <summary>创建事件关键帧的时间和参数实例，并初始化关联数据及缓冲区。</summary>
		public Event (float time, EventData data) {
			if (data == null) throw new ArgumentNullException("data", "data cannot be null.");
			this.time = time;
			this.data = data;
		}

		/// <summary>返回便于识别当前对象的名称或文本。</summary>
		override public string ToString () {
			return this.data.Name;
		}
	}
}
