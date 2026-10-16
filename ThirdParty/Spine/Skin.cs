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
using System.Collections;
using System.Collections.Generic;
using Spine.Collections;

namespace Spine {
	/// <summary><para>以插槽索引和附件名为键保存皮肤附件，并包含皮肤依赖的骨骼与约束。</para>Stores attachments by slot index and attachment name.
	/// <para>See SkeletonData <see cref="Spine.SkeletonData.DefaultSkin"/>, Skeleton <see cref="Spine.Skeleton.Skin"/>, and
	/// <a href="http://esotericsoftware.com/spine-runtime-skins">Runtime skins</a> in the Spine Runtimes Guide.</para>
	/// </summary>
	public class Skin {
		/// <summary>名称。</summary>
		internal string name;
		/// <summary>按插槽索引和占位名索引的附件集合。</summary>
		private OrderedDictionary<SkinEntry, Attachment> attachments = new OrderedDictionary<SkinEntry, Attachment>(SkinEntryComparer.Instance);
		/// <summary>此皮肤依赖的骨骼配置集合。</summary>
		internal readonly ExposedList<BoneData> bones = new ExposedList<BoneData>();
		/// <summary>皮肤依赖的约束配置集合。</summary>
		internal readonly ExposedList<ConstraintData> constraints = new ExposedList<ConstraintData>();

		/// <summary>名称。</summary>
		public string Name { get { return name; } }
		/// <summary>按插槽索引和占位名索引的附件集合。</summary>
		public OrderedDictionary<SkinEntry, Attachment> Attachments { get { return attachments; } }
		/// <summary>当前皮肤激活时需要启用的骨骼配置集合。</summary>
		public ExposedList<BoneData> Bones { get { return bones; } }
		/// <summary>皮肤依赖的约束配置集合。</summary>
		public ExposedList<ConstraintData> Constraints { get { return constraints; } }

		/// <summary>使用指定名称创建按插槽和占位名组织附件及皮肤依赖的骨骼约束。</summary>
		public Skin (string name) {
			if (name == null) throw new ArgumentNullException("name", "name cannot be null.");
			this.name = name;
		}

		/// <summary><para>按插槽索引和名称设置附件；同名条目已存在时替换。</para>Adds an attachment to the skin for the specified slot index and name.
		/// If the name already exists for the slot, the previous value is replaced.</summary>
		public void SetAttachment (int slotIndex, string name, Attachment attachment) {
			if (attachment == null) throw new ArgumentNullException("attachment", "attachment cannot be null.");
			if (slotIndex < 0) throw new ArgumentNullException("slotIndex", "slotIndex must be >= 0.");
			attachments[new SkinEntry(slotIndex, name, attachment)] = attachment;
		}

		///<summary><para>将指定皮肤的附件、骨骼和约束合并到此皮肤。</para>Adds all attachments, bones, and constraints from the specified skin to this skin.</summary>
		public void AddSkin (Skin skin) {
			foreach (BoneData data in skin.bones)
				if (!bones.Contains(data)) bones.Add(data);

			foreach (ConstraintData data in skin.constraints)
				if (!constraints.Contains(data)) constraints.Add(data);

			foreach (SkinEntry entry in skin.attachments.Keys)
				SetAttachment(entry.SlotIndex, entry.Name, entry.Attachment);
		}

		///<summary><para>将指定皮肤的附件复制到此皮肤，附件使用深复制。</para>Adds all attachments from the specified skin to this skin. Attachments are deep copied.</summary>
		public void CopySkin (Skin skin) {
			foreach (BoneData data in skin.bones)
				if (!bones.Contains(data)) bones.Add(data);

			foreach (ConstraintData data in skin.constraints)
				if (!constraints.Contains(data)) constraints.Add(data);

			foreach (SkinEntry entry in skin.attachments.Keys) {
				if (entry.Attachment is MeshAttachment)
					SetAttachment(entry.SlotIndex, entry.Name,
						entry.Attachment != null ? ((MeshAttachment)entry.Attachment).NewLinkedMesh() : null);
				else
					SetAttachment(entry.SlotIndex, entry.Name, entry.Attachment != null ? entry.Attachment.Copy() : null);
			}
		}

		/// <summary><para>按插槽索引及名称查询附件，未找到返回 null。</para>Returns the attachment for the specified slot index and name, or null.</summary>
		/// <returns>允许返回 null，表示没有对应对象。 May be null.</returns>
		public Attachment GetAttachment (int slotIndex, string name) {
			var lookup = new SkinEntry(slotIndex, name, null);
			Attachment attachment = null;
			bool containsKey = attachments.TryGetValue(lookup, out attachment);
			return containsKey ? attachment : null;
		}

		/// <summary><para>移除指定插槽和名称对应的皮肤附件，未存在时不处理。</para> Removes the attachment in the skin for the specified slot index and name, if any.</summary>
		public void RemoveAttachment (int slotIndex, string name) {
			if (slotIndex < 0) throw new ArgumentOutOfRangeException("slotIndex", "slotIndex must be >= 0");
			var lookup = new SkinEntry(slotIndex, name, null);
			attachments.Remove(lookup);
		}

		///<summary><para>返回此皮肤的全部附件条目。</para>Returns all attachments contained in this skin.</summary>
		public ICollection<SkinEntry> GetAttachments () {
			return this.attachments.Keys;
		}

		/// <summary><para>收集指定插槽索引下的全部皮肤附件；索引可通过骨架或骨架数据的 FindSlotIndex 查询。</para>Returns all attachments in this skin for the specified slot index.</summary>
		/// <param name="slotIndex">目标插槽索引，可用骨架或骨架数据的 FindSlotIndex 查询。 The target slotIndex. To find the slot index, use <see cref="Spine.Skeleton.FindSlotIndex"/> or <see cref="Spine.SkeletonData.FindSlotIndex"/>.</param>
		/// <param name="attachments">用于接收附件条目的列表，结果追加到列表末尾。</param>
		public void GetAttachments (int slotIndex, List<SkinEntry> attachments) {
			foreach (SkinEntry entry in this.attachments.Keys)
				if (entry.SlotIndex == slotIndex) attachments.Add(entry);
		}

		///<summary><para>清空皮肤中的全部附件、骨骼和约束。</para>Clears all attachments, bones, and constraints.</summary>
		public void Clear () {
			attachments.Clear();
			bones.Clear();
			constraints.Clear();
		}

		/// <summary>返回便于识别当前对象的名称或文本。</summary>
		override public string ToString () {
			return name;
		}

		/// <summary><para>如果旧皮肤对应附件当前正在使用，则替换成此皮肤中的对应附件。</para>Attach all attachments from this skin if the corresponding attachment from the old skin is currently attached.</summary>
		internal void AttachAll (Skeleton skeleton, Skin oldSkin) {
			foreach (SkinEntry entry in oldSkin.attachments.Keys) {
				int slotIndex = entry.SlotIndex;
				Slot slot = skeleton.slots.Items[slotIndex];
				if (slot.Attachment == entry.Attachment) {
					Attachment attachment = GetAttachment(slotIndex, entry.Name);
					if (attachment != null) slot.Attachment = attachment;
				}
			}
		}

		/// <summary><para>皮肤附件条目，保存插槽索引、占位名称及附件对象。</para>Stores an entry in the skin consisting of the slot index, name, and attachment.</summary>
		public struct SkinEntry {
			/// <summary>受影响插槽的索引。</summary>
			private readonly int slotIndex;
			/// <summary>名称。</summary>
			private readonly string name;
			/// <summary>当前或关联附件。</summary>
			private readonly Attachment attachment;
			/// <summary>预计算的条目哈希值。</summary>
			internal readonly int hashCode;

			/// <summary>使用指定名称创建皮肤中的插槽、占位名称与附件条目。</summary>
			public SkinEntry (int slotIndex, string name, Attachment attachment) {
				this.slotIndex = slotIndex;
				this.name = name;
				this.attachment = attachment;
				this.hashCode = this.name.GetHashCode() + this.slotIndex * 37;
			}

			/// <summary>受影响插槽的索引。</summary>
			public int SlotIndex {
				get {
					return slotIndex;
				}
			}

			/// <summary><para>附件在皮肤中登记的名称，对应 Spine 编辑器的皮肤占位名称。</para>The name the attachment is associated with, equivalent to the skin placeholder name in the Spine editor.</summary>
			public String Name {
				get {
					return name;
				}
			}

			/// <summary>此皮肤条目关联的附件对象。</summary>
			public Attachment Attachment {
				get {
					return attachment;
				}
			}
		}

		// 比较时只使用插槽索引和名称，忽略附件对象，并避免结构体装箱。 原注： Avoids boxing in the dictionary and is necessary to omit entry.attachment in the comparison.
		/// <summary>按插槽索引与占位名称比较皮肤条目的比较器。</summary>
		class SkinEntryComparer : IEqualityComparer<SkinEntry> {
			/// <summary>共享比较器实例。</summary>
			internal static readonly SkinEntryComparer Instance = new SkinEntryComparer();

			/// <summary>按比较器规则判断两个键是否表示同一逻辑条目。</summary>
			bool IEqualityComparer<SkinEntry>.Equals (SkinEntry e1, SkinEntry e2) {
				if (e1.SlotIndex != e2.SlotIndex) return false;
				if (!string.Equals(e1.Name, e2.Name, StringComparison.Ordinal)) return false;
				return true;
			}

			/// <summary>计算用于字典查找的组合哈希，与相等判断使用的字段保持一致。</summary>
			int IEqualityComparer<SkinEntry>.GetHashCode (SkinEntry e) {
				return e.Name.GetHashCode() + e.SlotIndex * 37;
			}
		}
	}
}
