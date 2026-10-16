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
	/// <summary>根据模型定义创建各类附件的接口。</summary>
	public interface AttachmentLoader {
		/// <summary>返回 null 可跳过该附件的加载。</summary>
		/// <return>可以返回 null，表示不加载该附件。 May be null to not load any attachment.</return>
		RegionAttachment NewRegionAttachment (Skin skin, string name, string path);

		/// <summary>返回 null 可跳过该附件的加载。</summary>
		/// <return>可以返回 null，表示不加载该附件。 May be null to not load any attachment.</return>
		MeshAttachment NewMeshAttachment (Skin skin, string name, string path);

		/// <summary>返回 null 可跳过该附件的加载。</summary>
		/// <return>可以返回 null，表示不加载该附件。 May be null to not load any attachment.</return>
		BoundingBoxAttachment NewBoundingBoxAttachment (Skin skin, string name);

		/// <summary>返回 null 可跳过该附件的加载。</summary>
		/// <returns>可以返回 null，表示不加载该附件。 May be null to not load any attachment</returns>
		PathAttachment NewPathAttachment (Skin skin, string name);

		/// <summary>创建提供局部位置和角度的点挂件，可用于粒子或武器挂点，供皮肤和骨架使用。</summary>
		PointAttachment NewPointAttachment (Skin skin, string name);

		/// <summary>创建限定后续插槽可见范围的多边形裁剪附件，供皮肤和骨架使用。</summary>
		ClippingAttachment NewClippingAttachment (Skin skin, string name);
	}
}
