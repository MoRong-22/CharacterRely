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
	/// <summary>可放入骨架插槽的附件基类，保存名称并支持复制。</summary>
	abstract public class Attachment {
		/// <summary>名称。</summary>
		public string Name { get; private set; }

		/// <summary>使用指定名称创建可放入骨架插槽的附件基类，保存名称并支持复制。</summary>
		protected Attachment (string name) {
			if (name == null) throw new ArgumentNullException("name", "name cannot be null");
			Name = name;
		}

		/// <summary>返回便于识别当前对象的名称或文本。</summary>
		override public string ToString () {
			return Name;
		}

		///<summary><para>返回附件的副本。</para>Returns a copy of the attachment.</summary>
		public abstract Attachment Copy ();
	}

	/// <summary>允许附件携带渲染后端对象的接口。</summary>
	public interface IHasRendererObject {
		/// <summary>渲染后端关联对象；图集页通常为纹理，附件通常为 AtlasRegion。</summary>
		object RendererObject { get; set; }
	}
}
