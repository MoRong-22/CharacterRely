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

	/// <summary><para>动画切换配置，按“来源动作→目标动作”保存淡入淡出时长，供 AnimationState 查询。</para>Stores mix (crossfade) durations to be applied when AnimationState animations are changed.</summary>
	public class AnimationStateData {
		/// <summary>用于共享与查询动作的骨架数据。</summary>
		internal SkeletonData skeletonData;
		/// <summary>按来源动作和目标动作记录的单向混合时长字典。</summary>
		readonly Dictionary<AnimationPair, float> animationToMixTime = new Dictionary<AnimationPair, float>(AnimationPairComparer.Instance);
		/// <summary>未单独配置的动作切换时长（秒）。</summary>
		internal float defaultMix;

		/// <summary><para>用于按动作名称查找动画的共享骨架数据。</para>The SkeletonData to look up animations when they are specified by name.</summary>
		public SkeletonData SkeletonData { get { return skeletonData; } }

		/// <summary><para>没有单独配置动作对时使用的默认混合时长，单位为秒。</para>
		/// The mix duration to use when no mix duration has been specifically defined between two animations.</summary>
		public float DefaultMix { get { return defaultMix; } set { defaultMix = value; } }

		/// <summary>绑定用于按名称查找动作的骨架数据；此配置可由多个 AnimationState 共享。</summary>
		public AnimationStateData (SkeletonData skeletonData) {
			if (skeletonData == null) throw new ArgumentException("skeletonData cannot be null.", "skeletonData");
			this.skeletonData = skeletonData;
		}

		/// <summary><para>按来源和目标动作名称设置单向切换时长，单位为秒；反向切换需另行配置。</para>Sets a mix duration by animation names.</summary>
		public void SetMix (string fromName, string toName, float duration) {
			Animation from = skeletonData.FindAnimation(fromName);
			if (from == null) throw new ArgumentException("Animation not found: " + fromName, "fromName");
			Animation to = skeletonData.FindAnimation(toName);
			if (to == null) throw new ArgumentException("Animation not found: " + toName, "toName");
			SetMix(from, to, duration);
		}

		/// <summary><para>设置从 from 动作切换到 to 动作的混合时长（秒），最终用于 TrackEntry.MixDuration。</para>Sets a mix duration when changing from the specified animation to the other.
		/// See TrackEntry.MixDuration.</summary>
		public void SetMix (Animation from, Animation to, float duration) {
			if (from == null) throw new ArgumentNullException("from", "from cannot be null.");
			if (to == null) throw new ArgumentNullException("to", "to cannot be null.");
			AnimationPair key = new AnimationPair(from, to);
			animationToMixTime.Remove(key);
			animationToMixTime.Add(key, duration);
		}

		/// <summary><para>查询指定方向的混合时长（秒）；未配置时返回 DefaultMix。</para>
		/// The mix duration to use when changing from the specified animation to the other,
		/// or the DefaultMix if no mix duration has been set.
		/// </summary>
		public float GetMix (Animation from, Animation to) {
			if (from == null) throw new ArgumentNullException("from", "from cannot be null.");
			if (to == null) throw new ArgumentNullException("to", "to cannot be null.");
			AnimationPair key = new AnimationPair(from, to);
			float duration;
			if (animationToMixTime.TryGetValue(key, out duration)) return duration;
			return defaultMix;
		}

		/// <summary>作为字典键的有向动作对，区分来源动作和目标动作。</summary>
		public struct AnimationPair {
			/// <summary>切换前的来源动画。</summary>
			public readonly Animation a1;
			/// <summary>切换后的目标动画。</summary>
			public readonly Animation a2;

			/// <summary>创建从 a1 到 a2 的有向动作对；正向和反向混合分别配置。</summary>
			public AnimationPair (Animation a1, Animation a2) {
				this.a1 = a1;
				this.a2 = a2;
			}

			/// <summary>返回“来源动作名→目标动作名”的可读文本。</summary>
			public override string ToString () {
				return a1.name + "->" + a2.name;
			}
		}

		// 动作对比较器，避免结构体字典键比较产生装箱。 原注： Avoids boxing in the dictionary.
		/// <summary>按动作对象引用比较动作对的比较器。</summary>
		public class AnimationPairComparer : IEqualityComparer<AnimationPair> {
			/// <summary>共享比较器实例。</summary>
			public static readonly AnimationPairComparer Instance = new AnimationPairComparer();

			/// <summary>按比较器规则判断两个键是否表示同一逻辑条目。</summary>
			bool IEqualityComparer<AnimationPair>.Equals (AnimationPair x, AnimationPair y) {
				return ReferenceEquals(x.a1, y.a1) && ReferenceEquals(x.a2, y.a2);
			}

			/// <summary>计算用于字典查找的组合哈希，与相等判断使用的字段保持一致。</summary>
			int IEqualityComparer<AnimationPair>.GetHashCode (AnimationPair obj) {
				// 沿用 Tuple 的哈希合并方式，对两动画对象计算组合哈希。 原注： from Tuple.CombineHashCodes // return (((h1 << 5) + h1) ^ h2);
				int h1 = obj.a1.GetHashCode();
				return (((h1 << 5) + h1) ^ obj.a2.GetHashCode());
			}
		}
	}
}
