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

	/// <summary><para>根据图集区域配置附件的加载器；负责为附件绑定纹理区域，详见运行时指南的骨架数据加载章节。</para>
	/// An AttachmentLoader that configures attachments using texture regions from an Atlas.
	/// See <a href='http://esotericsoftware.com/spine-loading-skeleton-data#JSON-and-binary-data'>Loading Skeleton Data</a> in the Spine Runtimes Guide.
	/// </summary>
	public class AtlasAttachmentLoader : AttachmentLoader {
		/// <summary>查找附件纹理区域时按顺序遍历的图集数组。</summary>
		private Atlas[] atlasArray;

		/// <summary>创建利用一个或多个图集为附件绑定纹理区域的加载器，并初始化关联数据及缓冲区。</summary>
		public AtlasAttachmentLoader (params Atlas[] atlasArray) {
			if (atlasArray == null) throw new ArgumentNullException("atlas array cannot be null.");
			this.atlasArray = atlasArray;
		}

		/// <summary>创建把单个图集区域绘制为四边形的附件，供皮肤和骨架使用。</summary>
		public RegionAttachment NewRegionAttachment (Skin skin, string name, string path) {
			AtlasRegion region = FindRegion(path);
			if (region == null) throw new ArgumentException(string.Format("Region not found in atlas: {0} (region attachment: {1})", path, name));
			RegionAttachment attachment = new RegionAttachment(name);
			attachment.RendererObject = region;
			attachment.SetUVs(region.u, region.v, region.u2, region.v2, region.rotate);
			attachment.regionOffsetX = region.offsetX;
			attachment.regionOffsetY = region.offsetY;
			attachment.regionWidth = region.width;
			attachment.regionHeight = region.height;
			attachment.regionOriginalWidth = region.originalWidth;
			attachment.regionOriginalHeight = region.originalHeight;
			return attachment;
		}

		/// <summary>创建支持骨骼权重及顶点变形的纹理网格附件，供皮肤和骨架使用。</summary>
		public MeshAttachment NewMeshAttachment (Skin skin, string name, string path) {
			AtlasRegion region = FindRegion(path);
			if (region == null) throw new ArgumentException(string.Format("Region not found in atlas: {0} (region attachment: {1})", path, name));
			MeshAttachment attachment = new MeshAttachment(name);
			attachment.RendererObject = region;
			attachment.RegionU = region.u;
			attachment.RegionV = region.v;
			attachment.RegionU2 = region.u2;
			attachment.RegionV2 = region.v2;
			attachment.RegionRotate = region.rotate;
			attachment.RegionDegrees = region.degrees;
			attachment.regionOffsetX = region.offsetX;
			attachment.regionOffsetY = region.offsetY;
			attachment.regionWidth = region.width;
			attachment.regionHeight = region.height;
			attachment.regionOriginalWidth = region.originalWidth;
			attachment.regionOriginalHeight = region.originalHeight;
			return attachment;
		}

		/// <summary>创建用于边界或碰撞检测的多边形附件，供皮肤和骨架使用。</summary>
		public BoundingBoxAttachment NewBoundingBoxAttachment (Skin skin, string name) {
			return new BoundingBoxAttachment(name);
		}

		/// <summary>创建由贝塞尔曲线组成、供路径约束使用的路径附件，供皮肤和骨架使用。</summary>
		public PathAttachment NewPathAttachment (Skin skin, string name) {
			return new PathAttachment(name);
		}

		/// <summary>创建提供局部位置和角度的点挂件，可用于粒子或武器挂点，供皮肤和骨架使用。</summary>
		public PointAttachment NewPointAttachment (Skin skin, string name) {
			return new PointAttachment(name);
		}

		/// <summary>创建限定后续插槽可见范围的多边形裁剪附件，供皮肤和骨架使用。</summary>
		public ClippingAttachment NewClippingAttachment(Skin skin, string name) {
			return new ClippingAttachment(name);
		}

		/// <summary>在配置的图集中按名称查找纹理区域，未找到返回 null。</summary>
		public AtlasRegion FindRegion (string name) {
			AtlasRegion region;

			for (int i = 0; i < atlasArray.Length; i++) {
				region = atlasArray[i].FindRegion(name);
				if (region != null)
					return region;
			}

			return null;
		}
	}
}
