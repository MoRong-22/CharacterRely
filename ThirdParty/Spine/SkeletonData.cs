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

	/// <summary><para>保存骨架初始姿势及可共享的无实例状态数据，包括骨骼、皮肤、附件和动画。</para>Stores the setup pose and all of the stateless data for a skeleton.</summary>
	public class SkeletonData {
		/// <summary>名称。</summary>
		internal string name;
		/// <summary>关联骨骼集合。</summary>
		internal ExposedList<BoneData> bones = new ExposedList<BoneData>(); // 父骨骼排在子骨骼之前。 原注： Ordered parents first
		/// <summary>骨架插槽集合。</summary>
		internal ExposedList<SlotData> slots = new ExposedList<SlotData>(); // 初始姿势的插槽绘制顺序。 原注： Setup pose draw order.
		/// <summary>全部皮肤集合。</summary>
		internal ExposedList<Skin> skins = new ExposedList<Skin>();
		/// <summary>默认皮肤。</summary>
		internal Skin defaultSkin;
		/// <summary>模型事件定义集合。</summary>
		internal ExposedList<EventData> events = new ExposedList<EventData>();
		/// <summary>模型动作集合。</summary>
		internal ExposedList<Animation> animations = new ExposedList<Animation>();
		/// <summary>IK 约束集合。</summary>
		internal ExposedList<IkConstraintData> ikConstraints = new ExposedList<IkConstraintData>();
		/// <summary>变换约束集合。</summary>
		internal ExposedList<TransformConstraintData> transformConstraints = new ExposedList<TransformConstraintData>();
		/// <summary>路径约束集合。</summary>
		internal ExposedList<PathConstraintData> pathConstraints = new ExposedList<PathConstraintData>();
		/// <summary>x：X 位置；y：Y 位置；width：宽度；height：高度。</summary>
		internal float x , y, width, height;
		/// <summary>version：模型导出版本；hash：模型导出内容哈希。</summary>
		internal string version, hash;

		// 非运行必需的辅助数据。 原注： Nonessential.
		/// <summary>Spine 编辑器动画时间表的帧率。</summary>
		internal float fps;
		/// <summary>imagesPath：导出时记录的图片目录；audioPath：事件音频资源路径。</summary>
		internal string imagesPath, audioPath;

		/// <summary>名称。</summary>
		public string Name { get { return name; } set { name = value; } }

		/// <summary><para>按父骨骼在前排序的骨骼配置，根骨骼始终为第一项。</para>The skeleton's bones, sorted parent first. The root bone is always the first bone.</summary>
		public ExposedList<BoneData> Bones { get { return bones; } }

		/// <summary>骨架插槽集合。</summary>
		public ExposedList<SlotData> Slots { get { return slots; } }

		/// <summary><para>所有皮肤，包括默认皮肤。</para>All skins, including the default skin.</summary>
		public ExposedList<Skin> Skins { get { return skins; } set { skins = value; } }

		/// <summary><para>骨架默认皮肤，通常包含编辑器中未放入自定义皮肤的附件；可以为 null。</para>
		/// The skeleton's default skin.
		/// By default this skin contains all attachments that were not in a skin in Spine.
		/// </summary>
		/// <return>允许返回 null，表示没有对应对象。 May be null.</return>
		public Skin DefaultSkin { get { return defaultSkin; } set { defaultSkin = value; } }

		/// <summary>模型事件定义集合。</summary>
		public ExposedList<EventData> Events { get { return events; } set { events = value; } }
		/// <summary>模型动作集合。</summary>
		public ExposedList<Animation> Animations { get { return animations; } set { animations = value; } }
		/// <summary>IK 约束集合。</summary>
		public ExposedList<IkConstraintData> IkConstraints { get { return ikConstraints; } set { ikConstraints = value; } }
		/// <summary>变换约束集合。</summary>
		public ExposedList<TransformConstraintData> TransformConstraints { get { return transformConstraints; } set { transformConstraints = value; } }
		/// <summary>路径约束集合。</summary>
		public ExposedList<PathConstraintData> PathConstraints { get { return pathConstraints; } set { pathConstraints = value; } }

		/// <summary>X 位置。</summary>
		public float X { get { return x; } set { x = value; } }
		/// <summary>Y 位置。</summary>
		public float Y { get { return y; } set { y = value; } }
		/// <summary>宽度。</summary>
		public float Width { get { return width; } set { width = value; } }
		/// <summary>高度。</summary>
		public float Height { get { return height; } set { height = value; } }
		/// <summary><para>导出此骨架所用的 Spine 版本，未提供时为 null。</para>The Spine version used to export this data, or null.</summary>
		public string Version { get { return version; } set { version = value; } }
		/// <summary>模型导出内容哈希。</summary>
		public string Hash { get { return hash; } set { hash = value; } }

		/// <summary><para>编辑器中设置的图片目录，仅导出辅助数据时可用；可以为 null。</para>The path to the images directory as defined in Spine. Available only when nonessential data was exported. May be null</summary>
		public string ImagesPath { get { return imagesPath; } set { imagesPath = value; } }

		/// <summary><para>编辑器中设置的音频目录，仅导出辅助数据时可用；可以为 null。</para>The path to the audio directory defined in Spine. Available only when nonessential data was exported. May be null.</summary>
		public string AudioPath { get { return audioPath; } set { audioPath = value; } }

		/// <summary><para>编辑器动画时间表的帧率，仅导出辅助数据时可用。</para>
		/// The dopesheet FPS in Spine. Available only when nonessential data was exported.</summary>
		public float Fps { get { return fps; } set { fps = value; } }

		// 骨骼查询相关方法。 原注： --- Bones.

		/// <summary><para>按名称线性查找骨骼配置，未找到返回 null；频繁调用时应缓存结果。</para>
		/// Finds a bone by comparing each bone's name.
		/// It is more efficient to cache the results of this method than to call it multiple times.</summary>
		/// <returns>允许返回 null，表示没有对应对象。 May be null.</returns>
		public BoneData FindBone (string boneName) {
			if (boneName == null) throw new ArgumentNullException("boneName", "boneName cannot be null.");
			var bones = this.bones;
			var bonesItems = bones.Items;
			for (int i = 0, n = bones.Count; i < n; i++) {
				BoneData bone = bonesItems[i];
				if (bone.name == boneName) return bone;
			}
			return null;
		}

		/// <summary>骨骼未找到时返回 -1。</summary>
		/// <returns>未找到骨骼时返回 -1。 -1 if the bone was not found.</returns>
		public int FindBoneIndex (string boneName) {
			if (boneName == null) throw new ArgumentNullException("boneName", "boneName cannot be null.");
			var bones = this.bones;
			var bonesItems = bones.Items;
			for (int i = 0, n = bones.Count; i < n; i++)
				if (bonesItems[i].name == boneName) return i;
			return -1;
		}

		// 插槽查询相关方法。 原注： --- Slots.

		/// <summary>允许为 null。</summary>
		/// <returns>允许返回 null，表示没有对应对象。 May be null.</returns>
		public SlotData FindSlot (string slotName) {
			if (slotName == null) throw new ArgumentNullException("slotName", "slotName cannot be null.");
			ExposedList<SlotData> slots = this.slots;
			for (int i = 0, n = slots.Count; i < n; i++) {
				SlotData slot = slots.Items[i];
				if (slot.name == slotName) return slot;
			}
			return null;
		}

		/// <summary>插槽未找到时返回 -1。</summary>
		/// <returns>未找到插槽时返回 -1。 -1 if the slot was not found.</returns>
		public int FindSlotIndex (string slotName) {
			if (slotName == null) throw new ArgumentNullException("slotName", "slotName cannot be null.");
			ExposedList<SlotData> slots = this.slots;
			for (int i = 0, n = slots.Count; i < n; i++)
				if (slots.Items[i].name == slotName) return i;
			return -1;
		}

		// 皮肤查询相关方法。 原注： --- Skins.

		/// <summary>允许为 null。</summary>
		/// <returns>允许返回 null，表示没有对应对象。 May be null.</returns>
		public Skin FindSkin (string skinName) {
			if (skinName == null) throw new ArgumentNullException("skinName", "skinName cannot be null.");
			foreach (Skin skin in skins)
				if (skin.name == skinName) return skin;
			return null;
		}

		// 事件查询相关方法。 原注： --- Events.

		/// <summary>允许为 null。</summary>
		/// <returns>允许返回 null，表示没有对应对象。 May be null.</returns>
		public EventData FindEvent (string eventDataName) {
			if (eventDataName == null) throw new ArgumentNullException("eventDataName", "eventDataName cannot be null.");
			foreach (EventData eventData in events)
				if (eventData.name == eventDataName) return eventData;
			return null;
		}

		// 动画查询相关方法。 原注： --- Animations.

		/// <summary>允许为 null。</summary>
		/// <returns>允许返回 null，表示没有对应对象。 May be null.</returns>
		public Animation FindAnimation (string animationName) {
			if (animationName == null) throw new ArgumentNullException("animationName", "animationName cannot be null.");
			ExposedList<Animation> animations = this.animations;
			for (int i = 0, n = animations.Count; i < n; i++) {
				Animation animation = animations.Items[i];
				if (animation.name == animationName) return animation;
			}
			return null;
		}

		// IK 约束查询相关方法。 原注： --- IK constraints.

		/// <summary>允许为 null。</summary>
		/// <returns>允许返回 null，表示没有对应对象。 May be null.</returns>
		public IkConstraintData FindIkConstraint (string constraintName) {
			if (constraintName == null) throw new ArgumentNullException("constraintName", "constraintName cannot be null.");
			ExposedList<IkConstraintData> ikConstraints = this.ikConstraints;
			for (int i = 0, n = ikConstraints.Count; i < n; i++) {
				IkConstraintData ikConstraint = ikConstraints.Items[i];
				if (ikConstraint.name == constraintName) return ikConstraint;
			}
			return null;
		}

		// 变换约束查询相关方法。 原注： --- Transform constraints.

		/// <summary>允许为 null。</summary>
		/// <returns>允许返回 null，表示没有对应对象。 May be null.</returns>
		public TransformConstraintData FindTransformConstraint (string constraintName) {
			if (constraintName == null) throw new ArgumentNullException("constraintName", "constraintName cannot be null.");
			ExposedList<TransformConstraintData> transformConstraints = this.transformConstraints;
			for (int i = 0, n = transformConstraints.Count; i < n; i++) {
				TransformConstraintData transformConstraint = transformConstraints.Items[i];
				if (transformConstraint.name == constraintName) return transformConstraint;
			}
			return null;
		}

		// 路径约束查询相关方法。 原注： --- Path constraints.

		/// <summary>允许为 null。</summary>
		/// <returns>允许返回 null，表示没有对应对象。 May be null.</returns>
		public PathConstraintData FindPathConstraint (string constraintName) {
			if (constraintName == null) throw new ArgumentNullException("constraintName", "constraintName cannot be null.");
			ExposedList<PathConstraintData> pathConstraints = this.pathConstraints;
			for (int i = 0, n = pathConstraints.Count; i < n; i++) {
				PathConstraintData constraint = pathConstraints.Items[i];
				if (constraint.name.Equals(constraintName)) return constraint;
			}
			return null;
		}

		/// <summary>路径约束未找到时返回 -1。</summary>
		/// <returns>未找到路径约束时返回 -1。 -1 if the path constraint was not found.</returns>
		public int FindPathConstraintIndex (string pathConstraintName) {
			if (pathConstraintName == null) throw new ArgumentNullException("pathConstraintName", "pathConstraintName cannot be null.");
			ExposedList<PathConstraintData> pathConstraints = this.pathConstraints;
			for (int i = 0, n = pathConstraints.Count; i < n; i++)
				if (pathConstraints.Items[i].name.Equals(pathConstraintName)) return i;
			return -1;
		}

		// ---

		/// <summary>返回便于识别当前对象的名称或文本。</summary>
		override public string ToString () {
			return name ?? base.ToString();
		}
	}
}
