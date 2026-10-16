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
	/// <summary>骨架实例，保存独立的骨骼、插槽、约束、皮肤及当前姿势。</summary>
	public class Skeleton {
		/// <summary>初始配置数据。</summary>
		internal SkeletonData data;
		/// <summary>关联骨骼集合。</summary>
		internal ExposedList<Bone> bones;
		/// <summary>骨架插槽集合。</summary>
		internal ExposedList<Slot> slots;
		/// <summary>当前插槽绘制顺序。</summary>
		internal ExposedList<Slot> drawOrder;
		/// <summary>IK 约束集合。</summary>
		internal ExposedList<IkConstraint> ikConstraints;
		/// <summary>变换约束集合。</summary>
		internal ExposedList<TransformConstraint> transformConstraints;
		/// <summary>路径约束集合。</summary>
		internal ExposedList<PathConstraint> pathConstraints;
		/// <summary>依约束依赖关系排列的更新项。</summary>
		internal ExposedList<IUpdatable> updateCache = new ExposedList<IUpdatable>();
		/// <summary>应用约束前需要重置已应用变换的骨骼。</summary>
		internal ExposedList<Bone> updateCacheReset = new ExposedList<Bone>();
		/// <summary>当前皮肤。</summary>
		internal Skin skin;
		/// <summary>r：红色分量；g：绿色分量；b：蓝色分量；a：透明度分量。</summary>
		internal float r = 1, g = 1, b = 1, a = 1;
		/// <summary>累计时间（秒）。</summary>
		internal float time;
		/// <summary>scaleX：X 方向缩放倍率；scaleY：Y 方向缩放倍率。</summary>
		private float scaleX = 1, scaleY = 1;
		/// <summary>x：X 位置；y：Y 位置。</summary>
		internal float x, y;

		/// <summary>初始配置数据。</summary>
		public SkeletonData Data { get { return data; } }
		/// <summary>关联骨骼集合。</summary>
		public ExposedList<Bone> Bones { get { return bones; } }
		/// <summary>按依赖顺序排列的骨骼及约束更新缓存。</summary>
		public ExposedList<IUpdatable> UpdateCacheList { get { return updateCache; } }
		/// <summary>骨架插槽集合。</summary>
		public ExposedList<Slot> Slots { get { return slots; } }
		/// <summary>当前插槽绘制顺序。</summary>
		public ExposedList<Slot> DrawOrder { get { return drawOrder; } }
		/// <summary>IK 约束集合。</summary>
		public ExposedList<IkConstraint> IkConstraints { get { return ikConstraints; } }
		/// <summary>路径约束集合。</summary>
		public ExposedList<PathConstraint> PathConstraints { get { return pathConstraints; } }
		/// <summary>变换约束集合。</summary>
		public ExposedList<TransformConstraint> TransformConstraints { get { return transformConstraints; } }
		/// <summary>当前皮肤。</summary>
		public Skin Skin { get { return skin; } set { SetSkin(value); } }
		/// <summary>红色分量。</summary>
		public float R { get { return r; } set { r = value; } }
		/// <summary>绿色分量。</summary>
		public float G { get { return g; } set { g = value; } }
		/// <summary>蓝色分量。</summary>
		public float B { get { return b; } set { b = value; } }
		/// <summary>透明度分量。</summary>
		public float A { get { return a; } set { a = value; } }
		/// <summary>累计时间（秒）。</summary>
		public float Time { get { return time; } set { time = value; } }
		/// <summary>X 位置。</summary>
		public float X { get { return x; } set { x = value; } }
		/// <summary>Y 位置。</summary>
		public float Y { get { return y; } set { y = value; } }
		/// <summary>X 方向缩放倍率。</summary>
		public float ScaleX { get { return scaleX; } set { scaleX = value; } }
		/// <summary>Y 方向缩放倍率。</summary>
		public float ScaleY { get { return scaleY * (Bone.yDown ? -1 : 1); } set { scaleY = value; } }

		/// <summary>已过时的水平镜像开关，应改用 ScaleX 的正负。</summary>
		[Obsolete("Use ScaleX instead. FlipX is when ScaleX is negative.")]
		public bool FlipX { get { return scaleX < 0; } set { scaleX = value ? -1f : 1f; } }

		/// <summary>已过时的垂直镜像开关，应改用 ScaleY 的正负。</summary>
		[Obsolete("Use ScaleY instead. FlipY is when ScaleY is negative.")]
		public bool FlipY { get { return scaleY < 0; } set { scaleY = value ? -1f : 1f; } }

		/// <summary>根骨骼；无骨骼时返回 null。</summary>
		public Bone RootBone {
			get { return bones.Count == 0 ? null : bones.Items[0]; }
		}

		/// <summary>创建骨架实例，保存独立的骨骼、插槽、约束、皮肤及当前姿势，并初始化关联数据及缓冲区。</summary>
		public Skeleton (SkeletonData data) {
			if (data == null) throw new ArgumentNullException("data", "data cannot be null.");
			this.data = data;

			bones = new ExposedList<Bone>(data.bones.Count);
			foreach (BoneData boneData in data.bones) {
				Bone bone;
				if (boneData.parent == null) {
					bone = new Bone(boneData, this, null);
				} else {
					Bone parent = bones.Items[boneData.parent.index];
					bone = new Bone(boneData, this, parent);
					parent.children.Add(bone);
				}
				bones.Add(bone);
			}

			slots = new ExposedList<Slot>(data.slots.Count);
			drawOrder = new ExposedList<Slot>(data.slots.Count);
			foreach (SlotData slotData in data.slots) {
				Bone bone = bones.Items[slotData.boneData.index];
				Slot slot = new Slot(slotData, bone);
				slots.Add(slot);
				drawOrder.Add(slot);
			}

			ikConstraints = new ExposedList<IkConstraint>(data.ikConstraints.Count);
			foreach (IkConstraintData ikConstraintData in data.ikConstraints)
				ikConstraints.Add(new IkConstraint(ikConstraintData, this));

			transformConstraints = new ExposedList<TransformConstraint>(data.transformConstraints.Count);
			foreach (TransformConstraintData transformConstraintData in data.transformConstraints)
				transformConstraints.Add(new TransformConstraint(transformConstraintData, this));

			pathConstraints = new ExposedList<PathConstraint> (data.pathConstraints.Count);
			foreach (PathConstraintData pathConstraintData in data.pathConstraints)
				pathConstraints.Add(new PathConstraint(pathConstraintData, this));

			UpdateCache();
			UpdateWorldTransform();
		}

		/// <summary><para>重新构建骨骼和约束更新缓存；修改骨架数据、增删骨骼约束或带权重路径附件后需调用。</para>Caches information about bones and constraints. Must be called if the <see cref="Skin"/> is modified or if bones, constraints, or
		/// constraints, or weighted path attachments are added or removed.</summary>
		public void UpdateCache () {
			var updateCache = this.updateCache;
			updateCache.Clear();
			this.updateCacheReset.Clear();

			int boneCount = this.bones.Items.Length;
			var bones = this.bones;
			for (int i = 0; i < boneCount; i++) {
				Bone bone = bones.Items[i];
				bone.sorted = bone.data.skinRequired;
				bone.active = !bone.sorted;
			}
			if (skin != null) {
				Object[] skinBones = skin.bones.Items;
				for (int i = 0, n = skin.bones.Count; i < n; i++) {
					Bone bone = (Bone)bones.Items[((BoneData)skinBones[i]).index];
					do {
						bone.sorted = false;
						bone.active = true;
						bone = bone.parent;
					} while (bone != null);
				}
			}

			int ikCount = this.ikConstraints.Count, transformCount = this.transformConstraints.Count, pathCount = this.pathConstraints.Count;
			var ikConstraints = this.ikConstraints;
			var transformConstraints = this.transformConstraints;
			var pathConstraints = this.pathConstraints;
			int constraintCount = ikCount + transformCount + pathCount;
			// 参考实现的外层循环标签。 原注：outer:
			for (int i = 0; i < constraintCount; i++) {
				for (int ii = 0; ii < ikCount; ii++) {
					IkConstraint constraint = ikConstraints.Items[ii];
					if (constraint.data.order == i) {
						SortIkConstraint(constraint);
						goto continue_outer; // 参考实现：继续外层循环。 原注：continue outer;
					}
				}
				for (int ii = 0; ii < transformCount; ii++) {
					TransformConstraint constraint = transformConstraints.Items[ii];
					if (constraint.data.order == i) {
						SortTransformConstraint(constraint);
						goto continue_outer; // 参考实现：继续外层循环。 原注：continue outer;
					}
				}
				for (int ii = 0; ii < pathCount; ii++) {
					PathConstraint constraint = pathConstraints.Items[ii];
					if (constraint.data.order == i) {
						SortPathConstraint(constraint);
						goto continue_outer; // 参考实现：继续外层循环。 原注：continue outer;
					}
				}
				continue_outer: {}
			}

			for (int i = 0; i < boneCount; i++)
				SortBone(bones.Items[i]);
		}

		/// <summary>按 IK 约束依赖的目标及骨骼安排更新顺序。</summary>
		private void SortIkConstraint (IkConstraint constraint) {
			constraint.active = constraint.target.active
				&& (!constraint.data.skinRequired || (skin != null && skin.constraints.Contains(constraint.data)));
			if (!constraint.active) return;

			Bone target = constraint.target;
			SortBone(target);

			var constrained = constraint.bones;
			Bone parent = constrained.Items[0];
			SortBone(parent);

			if (constrained.Count > 1) {
				Bone child = constrained.Items[constrained.Count - 1];
				if (!updateCache.Contains(child))
					updateCacheReset.Add(child);
			}

			updateCache.Add(constraint);

			SortReset(parent.children);
			constrained.Items[constrained.Count - 1].sorted = true;
		}

		/// <summary>按路径约束及路径加权骨骼依赖安排更新顺序。</summary>
		private void SortPathConstraint (PathConstraint constraint) {
			constraint.active = constraint.target.bone.active
				&& (!constraint.data.skinRequired || (skin != null && skin.constraints.Contains(constraint.data)));
			if (!constraint.active) return;

			Slot slot = constraint.target;
			int slotIndex = slot.data.index;
			Bone slotBone = slot.bone;
			if (skin != null) SortPathConstraintAttachment(skin, slotIndex, slotBone);
			if (data.defaultSkin != null && data.defaultSkin != skin)
				SortPathConstraintAttachment(data.defaultSkin, slotIndex, slotBone);

			Attachment attachment = slot.attachment;
			if (attachment is PathAttachment) SortPathConstraintAttachment(attachment, slotBone);

			var constrained = constraint.bones;
			int boneCount = constrained.Count;
			for (int i = 0; i < boneCount; i++)
				SortBone(constrained.Items[i]);

			updateCache.Add(constraint);

			for (int i = 0; i < boneCount; i++)
				SortReset(constrained.Items[i].children);
			for (int i = 0; i < boneCount; i++)
				constrained.Items[i].sorted = true;
		}

		/// <summary>按变换约束的局部或世界空间依赖安排更新顺序。</summary>
		private void SortTransformConstraint (TransformConstraint constraint) {
			constraint.active = constraint.target.active
				&& (!constraint.data.skinRequired || (skin != null && skin.constraints.Contains(constraint.data)));
			if (!constraint.active) return;

			SortBone(constraint.target);

			var constrained = constraint.bones;
			int boneCount = constrained.Count;
			if (constraint.data.local) {
				for (int i = 0; i < boneCount; i++) {
					Bone child = constrained.Items[i];
					SortBone(child.parent);
					if (!updateCache.Contains(child)) updateCacheReset.Add(child);
				}
			} else {
				for (int i = 0; i < boneCount; i++)
					SortBone(constrained.Items[i]);
			}

			updateCache.Add(constraint);

			for (int i = 0; i < boneCount; i++)
				SortReset(constrained.Items[i].children);
			for (int i = 0; i < boneCount; i++)
				constrained.Items[i].sorted = true;
		}

		/// <summary>遍历路径附件引用的骨骼，加入路径约束之前的更新依赖。</summary>
		private void SortPathConstraintAttachment (Skin skin, int slotIndex, Bone slotBone) {
			foreach (var entryObj in skin.Attachments.Keys) {
				var entry = (Skin.SkinEntry)entryObj;
				if (entry.SlotIndex == slotIndex) SortPathConstraintAttachment(entry.Attachment, slotBone);
			}
		}

		/// <summary>遍历路径附件引用的骨骼，加入路径约束之前的更新依赖。</summary>
		private void SortPathConstraintAttachment (Attachment attachment, Bone slotBone) {
			if (!(attachment is PathAttachment)) return;
			int[] pathBones = ((PathAttachment)attachment).bones;
			if (pathBones == null)
				SortBone(slotBone);
			else {
				var bones = this.bones;
				for (int i = 0, n = pathBones.Length; i < n;) {
					int nn = pathBones[i++];
					nn += i;
					while (i < nn)
						SortBone(bones.Items[pathBones[i++]]);
				}
			}
		}

		/// <summary>递归确保父骨骼先于子骨骼加入更新缓存。</summary>
		private void SortBone (Bone bone) {
			if (bone.sorted) return;
			Bone parent = bone.parent;
			if (parent != null) SortBone(parent);
			bone.sorted = true;
			updateCache.Add(bone);
		}

		/// <summary>重置子骨骼的排序标记，使约束后的姿势可重新计算。</summary>
		private static void SortReset (ExposedList<Bone> bones) {
			var bonesItems = bones.Items;
			for (int i = 0, n = bones.Count; i < n; i++) {
				Bone bone = bonesItems[i];
				if (!bone.active) continue;
				if (bone.sorted) SortReset(bone.children);
				bone.sorted = false;
			}
		}

		/// <summary><para>更新各骨骼世界变换并应用约束。</para>Updates the world transform for each bone and applies constraints.</summary>
		public void UpdateWorldTransform () {
			var updateCacheReset = this.updateCacheReset;
			var updateCacheResetItems = updateCacheReset.Items;
			for (int i = 0, n = updateCacheReset.Count; i < n; i++) {
				Bone bone = updateCacheResetItems[i];
				bone.ax = bone.x;
				bone.ay = bone.y;
				bone.arotation = bone.rotation;
				bone.ascaleX = bone.scaleX;
				bone.ascaleY = bone.scaleY;
				bone.ashearX = bone.shearX;
				bone.ashearY = bone.shearY;
				bone.appliedValid = true;
			}
			var updateItems = this.updateCache.Items;
			for (int i = 0, n = updateCache.Count; i < n; i++)
				updateItems[i].Update();
		}

		/// <summary><para>临时把根骨骼视为指定父骨骼的子骨骼，再更新世界变换和全部约束。</para>
		/// Temporarily sets the root bone as a child of the specified bone, then updates the world transform for each bone and applies
		/// all constraints.
	 	/// </summary>
		public void UpdateWorldTransform (Bone parent) {
			// 局部更新避免提前计算受约束骨骼的世界矩阵：骨骼未在约束之前更新。 原注： This partial update avoids computing the world transform for constrained bones when 1) the bone is not updated
			// 约束只需已应用的局部变换，并由约束本身继续更新世界矩阵。 原注： before the constraint, 2) the constraint only needs to access the applied local transform, and 3) the constraint calls
			// 调用世界变换更新。 原注： updateWorldTransform.
			var updateCacheReset = this.updateCacheReset;
			var updateCacheResetItems = updateCacheReset.Items;
			for (int i = 0, n = updateCacheReset.Count; i < n; i++) {
				Bone bone = updateCacheResetItems[i];
				bone.ax = bone.x;
				bone.ay = bone.y;
				bone.arotation = bone.rotation;
				bone.ascaleX = bone.scaleX;
				bone.ascaleY = bone.scaleY;
				bone.ashearX = bone.shearX;
				bone.ashearY = bone.shearY;
				bone.appliedValid = true;
			}

			// 把外部父骨骼的变换应用到根骨骼；根骨骼总是继承缩放、旋转和镜像。 原注： Apply the parent bone transform to the root bone. The root bone always inherits scale, rotation and reflection.
			Bone rootBone = this.RootBone;
			float pa = parent.a, pb = parent.b, pc = parent.c, pd = parent.d;
			rootBone.worldX = pa * x + pb * y + parent.worldX;
			rootBone.worldY = pc * x + pd * y + parent.worldY;

			float rotationY = rootBone.rotation + 90 + rootBone.shearY;
			float la = MathUtils.CosDeg(rootBone.rotation + rootBone.shearX) * rootBone.scaleX;
			float lb = MathUtils.CosDeg(rotationY) * rootBone.scaleY;
			float lc = MathUtils.SinDeg(rootBone.rotation + rootBone.shearX) * rootBone.scaleX;
			float ld = MathUtils.SinDeg(rotationY) * rootBone.scaleY;
			rootBone.a = (pa * la + pb * lc) * scaleX;
			rootBone.b = (pa * lb + pb * ld) * scaleX;
			rootBone.c = (pc * la + pd * lc) * scaleY;
			rootBone.d = (pc * lb + pd * ld) * scaleY;

			// 更新根骨骼之外的全部项。 原注： Update everything except root bone.
			var updateCache = this.updateCache;
			var updateCacheItems = updateCache.Items;
			for (int i = 0, n = updateCache.Count; i < n; i++) {
				var updatable = updateCacheItems[i];
				if (updatable != rootBone)
					updatable.Update();
			}
		}

		/// <summary><para>恢复骨骼、约束和插槽的初始姿势。</para>Sets the bones, constraints, and slots to their setup pose values.</summary>
		public void SetToSetupPose () {
			SetBonesToSetupPose();
			SetSlotsToSetupPose();
		}

		/// <summary><para>恢复骨骼和约束的初始姿势。</para>Sets the bones and constraints to their setup pose values.</summary>
		public void SetBonesToSetupPose () {
			var bonesItems = this.bones.Items;
			for (int i = 0, n = bones.Count; i < n; i++)
				bonesItems[i].SetToSetupPose();

			var ikConstraintsItems = this.ikConstraints.Items;
			for (int i = 0, n = ikConstraints.Count; i < n; i++) {
				IkConstraint constraint = ikConstraintsItems[i];
				constraint.mix = constraint.data.mix;
				constraint.softness = constraint.data.softness;
				constraint.bendDirection = constraint.data.bendDirection;
				constraint.compress = constraint.data.compress;
				constraint.stretch = constraint.data.stretch;
			}

			var transformConstraintsItems = this.transformConstraints.Items;
			for (int i = 0, n = transformConstraints.Count; i < n; i++) {
				TransformConstraint constraint = transformConstraintsItems[i];
				TransformConstraintData constraintData = constraint.data;
				constraint.rotateMix = constraintData.rotateMix;
				constraint.translateMix = constraintData.translateMix;
				constraint.scaleMix = constraintData.scaleMix;
				constraint.shearMix = constraintData.shearMix;
			}

			var pathConstraintItems = this.pathConstraints.Items;
			for (int i = 0, n = pathConstraints.Count; i < n; i++) {
				PathConstraint constraint = pathConstraintItems[i];
				PathConstraintData constraintData = constraint.data;
				constraint.position = constraintData.position;
				constraint.spacing = constraintData.spacing;
				constraint.rotateMix = constraintData.rotateMix;
				constraint.translateMix = constraintData.translateMix;
			}
		}

		/// <summary>恢复各插槽的初始附件、颜色与绘制顺序。</summary>
		public void SetSlotsToSetupPose () {
			var slots = this.slots;
			var slotsItems = slots.Items;
			drawOrder.Clear();
			for (int i = 0, n = slots.Count; i < n; i++)
				drawOrder.Add(slotsItems[i]);

			for (int i = 0, n = slots.Count; i < n; i++)
				slotsItems[i].SetToSetupPose();
		}

		/// <summary>允许为 null。</summary>
		/// <returns>允许返回 null，表示没有对应对象。 May be null.</returns>
		public Bone FindBone (string boneName) {
			if (boneName == null) throw new ArgumentNullException("boneName", "boneName cannot be null.");
			var bones = this.bones;
			var bonesItems = bones.Items;
			for (int i = 0, n = bones.Count; i < n; i++) {
				Bone bone = bonesItems[i];
				if (bone.data.name == boneName) return bone;
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
				if (bonesItems[i].data.name == boneName) return i;
			return -1;
		}

		/// <summary>允许为 null。</summary>
		/// <returns>允许返回 null，表示没有对应对象。 May be null.</returns>
		public Slot FindSlot (string slotName) {
			if (slotName == null) throw new ArgumentNullException("slotName", "slotName cannot be null.");
			var slots = this.slots;
			var slotsItems = slots.Items;
			for (int i = 0, n = slots.Count; i < n; i++) {
				Slot slot = slotsItems[i];
				if (slot.data.name == slotName) return slot;
			}
			return null;
		}

		/// <summary>插槽未找到时返回 -1（原英文注释的 bone 为笔误）。</summary>
		/// <returns>未找到骨骼时返回 -1。 -1 if the bone was not found.</returns>
		public int FindSlotIndex (string slotName) {
			if (slotName == null) throw new ArgumentNullException("slotName", "slotName cannot be null.");
			var slots = this.slots;
			var slotsItems = slots.Items;
			for (int i = 0, n = slots.Count; i < n; i++)
				if (slotsItems[i].data.name.Equals(slotName)) return i;
			return -1;
		}

		/// <summary><para>通过皮肤名称切换皮肤，具体行为见 SetSkin。</para>Sets a skin by name (see SetSkin).</summary>
		public void SetSkin (string skinName) {
			Skin foundSkin = data.FindSkin(skinName);
			if (foundSkin == null) throw new ArgumentException("Skin not found: " + skinName, "skinName");
			SetSkin(foundSkin);
		}

		/// <summary><para>设置当前皮肤并更新缓存；优先从新皮肤替换旧附件，无旧皮肤时用初始附件。可调用 SetSlotsToSetupPose 重置显示，再应用动画关键帧。</para>
		/// <para>Sets the skin used to look up attachments before looking in the <see cref="SkeletonData.DefaultSkin"/>. If the
		/// skin is changed, <see cref="UpdateCache()"/> is called.
		/// </para>
	 	/// <para>Attachments from the new skin are attached if the corresponding attachment from the old skin was attached.
		/// If there was no old skin, each slot's setup mode attachment is attached from the new skin.
		/// </para>
		/// <para>After changing the skin, the visible attachments can be reset to those attached in the setup pose by calling
		/// <see cref="Skeleton.SetSlotsToSetupPose()"/>.
		/// Also, often <see cref="AnimationState.Apply(Skeleton)"/> is called before the next time the
		/// skeleton is rendered to allow any attachment keys in the current animation(s) to hide or show attachments from the new skin.</para>
		/// </summary>
		/// <param name="newSkin">要切换到的皮肤，可以为 null。 May be null.</param>
		public void SetSkin (Skin newSkin) {
			if (newSkin == skin) return;
			if (newSkin != null) {
				if (skin != null)
					newSkin.AttachAll(this, skin);
				else {
					ExposedList<Slot> slots = this.slots;
					for (int i = 0, n = slots.Count; i < n; i++) {
						Slot slot = slots.Items[i];
						string name = slot.data.attachmentName;
						if (name != null) {
							Attachment attachment = newSkin.GetAttachment(i, name);
							if (attachment != null) slot.Attachment = attachment;
						}
					}
				}
			}
			skin = newSkin;
			UpdateCache();
		}

		/// <summary><para>按插槽名称和附件名称查找附件，先查当前皮肤，再查默认皮肤；未找到返回 null。</para>Finds an attachment by looking in the {@link #skin} and {@link SkeletonData#defaultSkin} using the slot name and attachment name.</summary>
		/// <returns>允许返回 null，表示没有对应对象。 May be null.</returns>
		public Attachment GetAttachment (string slotName, string attachmentName) {
			return GetAttachment(data.FindSlotIndex(slotName), attachmentName);
		}

		/// <summary><para>按插槽索引和附件名称查找，先查当前皮肤，再查默认皮肤；未找到返回 null。</para>Finds an attachment by looking in the skin and skeletonData.defaultSkin using the slot index and attachment name.First the skin is checked and if the attachment was not found, the default skin is checked.</summary>
		/// <returns>允许返回 null，表示没有对应对象。 May be null.</returns>
		public Attachment GetAttachment (int slotIndex, string attachmentName) {
			if (attachmentName == null) throw new ArgumentNullException("attachmentName", "attachmentName cannot be null.");
			if (skin != null) {
				Attachment attachment = skin.GetAttachment(slotIndex, attachmentName);
				if (attachment != null) return attachment;
			}
			return data.defaultSkin != null ? data.defaultSkin.GetAttachment(slotIndex, attachmentName) : null;
		}

		/// <summary><para>按插槽名称切换附件：先找插槽再找附件；附件名为 null 时清空该插槽。</para>A convenience method to set an attachment by finding the slot with FindSlot, finding the attachment with GetAttachment, then setting the slot's slot.Attachment.</summary>
		/// <param name="attachmentName">目标附件名称；null 清空插槽附件。 May be null to clear the slot's attachment.</param>
		public void SetAttachment (string slotName, string attachmentName) {
			if (slotName == null) throw new ArgumentNullException("slotName", "slotName cannot be null.");
			ExposedList<Slot> slots = this.slots;
			for (int i = 0, n = slots.Count; i < n; i++) {
				Slot slot = slots.Items[i];
				if (slot.data.name == slotName) {
					Attachment attachment = null;
					if (attachmentName != null) {
						attachment = GetAttachment(i, attachmentName);
						if (attachment == null) throw new Exception("Attachment not found: " + attachmentName + ", for slot: " + slotName);
					}
					slot.Attachment = attachment;
					return;
				}
			}
			throw new Exception("Slot not found: " + slotName);
		}

		/// <summary>允许为 null。</summary>
		/// <returns>允许返回 null，表示没有对应对象。 May be null.</returns>
		public IkConstraint FindIkConstraint (string constraintName) {
			if (constraintName == null) throw new ArgumentNullException("constraintName", "constraintName cannot be null.");
			ExposedList<IkConstraint> ikConstraints = this.ikConstraints;
			for (int i = 0, n = ikConstraints.Count; i < n; i++) {
				IkConstraint ikConstraint = ikConstraints.Items[i];
				if (ikConstraint.data.name == constraintName) return ikConstraint;
			}
			return null;
		}

		/// <summary>允许为 null。</summary>
		/// <returns>允许返回 null，表示没有对应对象。 May be null.</returns>
		public TransformConstraint FindTransformConstraint (string constraintName) {
			if (constraintName == null) throw new ArgumentNullException("constraintName", "constraintName cannot be null.");
			ExposedList<TransformConstraint> transformConstraints = this.transformConstraints;
			for (int i = 0, n = transformConstraints.Count; i < n; i++) {
				TransformConstraint transformConstraint = transformConstraints.Items[i];
				if (transformConstraint.data.Name == constraintName) return transformConstraint;
			}
			return null;
		}

		/// <summary>允许为 null。</summary>
		/// <returns>允许返回 null，表示没有对应对象。 May be null.</returns>
		public PathConstraint FindPathConstraint (string constraintName) {
			if (constraintName == null) throw new ArgumentNullException("constraintName", "constraintName cannot be null.");
			ExposedList<PathConstraint> pathConstraints = this.pathConstraints;
			for (int i = 0, n = pathConstraints.Count; i < n; i++) {
				PathConstraint constraint = pathConstraints.Items[i];
				if (constraint.data.Name.Equals(constraintName)) return constraint;
			}
			return null;
		}

		/// <summary>仅累计骨架时间，供附件计时使用；不会自行推进 AnimationState 或重算姿势。</summary>
		public void Update (float delta) {
			time += delta;
		}

		/// <summary><para>计算当前姿势下区域和网格附件的轴对齐包围盒；输出左下角相对骨架原点的偏移及宽高，可复用传入的顶点暂存数组。</para>Returns the axis aligned bounding box (AABB) of the region and mesh attachments for the current pose.</summary>
		/// <param name="x">骨架原点到包围盒左边界的水平偏移。 The horizontal distance between the skeleton origin and the left side of the AABB.</param>
		/// <param name="y">骨架原点到包围盒底边界的垂直偏移。 The vertical distance between the skeleton origin and the bottom side of the AABB.</param>
		/// <param name="width">轴对齐包围盒宽度。 The width of the AABB</param>
		/// <param name="height">轴对齐包围盒高度。 The height of the AABB.</param>
		/// <param name="vertexBuffer">可复用的浮点顶点数组，允许初始为 null；容量不足时会分配新数组。 Reference to hold a float[]. May be a null reference. This method will assign it a new float[] with the appropriate size as needed.</param>
		public void GetBounds (out float x, out float y, out float width, out float height, ref float[] vertexBuffer) {
			float[] temp = vertexBuffer;
			temp = temp ?? new float[8];
			var drawOrderItems = this.drawOrder.Items;
			float minX = int.MaxValue, minY = int.MaxValue, maxX = int.MinValue, maxY = int.MinValue;
			for (int i = 0, n = drawOrderItems.Length; i < n; i++) {
				Slot slot = drawOrderItems[i];
				if (!slot.bone.active) continue;
				int verticesLength = 0;
				float[] vertices = null;
				Attachment attachment = slot.attachment;
				var regionAttachment = attachment as RegionAttachment;
				if (regionAttachment != null) {
					verticesLength = 8;
					vertices = temp;
					if (vertices.Length < 8) vertices = temp = new float[8];
					regionAttachment.ComputeWorldVertices(slot.bone, temp, 0);
				} else {
					var meshAttachment = attachment as MeshAttachment;
					if (meshAttachment != null) {
						MeshAttachment mesh = meshAttachment;
						verticesLength = mesh.WorldVerticesLength;
						vertices = temp;
						if (vertices.Length < verticesLength) vertices = temp = new float[verticesLength];
						mesh.ComputeWorldVertices(slot, 0, verticesLength, temp, 0);
					}
				}

				if (vertices != null) {
					for (int ii = 0; ii < verticesLength; ii += 2) {
						float vx = vertices[ii], vy = vertices[ii + 1];
						minX = Math.Min(minX, vx);
						minY = Math.Min(minY, vy);
						maxX = Math.Max(maxX, vx);
						maxY = Math.Max(maxY, vy);
					}
				}
			}
			x = minX;
			y = minY;
			width = maxX - minX;
			height = maxY - minY;
			vertexBuffer = temp;
		}
	}
}
