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

	/// <summary><para>动画状态控制器：推进时间、排队播放、动作淡入淡出及多轨道叠加；详见 Spine 运行时指南的动画应用章节。</para>
	/// <para>
	/// Applies animations over time, queues animations for later playback, mixes (crossfading) between animations, and applies
	/// multiple animations on top of each other (layering).</para>
	/// <para>
	/// See <a href='http://esotericsoftware.com/spine-applying-animations/'>Applying Animations</a> in the Spine Runtimes Guide.</para>
	/// </summary>
	public class AnimationState {
		/// <summary>没有时间轴的空动画，用于淡入淡出占位。</summary>
		static readonly Animation EmptyAnimation = new Animation("<empty>", new ExposedList<Timeline>(), 0);

		/// <summary>前一个时间轴已写入此属性，因此从当前姿势向本时间轴姿势混合。</summary>
		/// 1) A previously applied timeline has set this property.<para />
		/// Result: Mix from the current pose to the timeline pose.
		internal const int Subsequent = 0;
		/// <summary>这是首次写入此属性，后续轨道项不写入它，因此从初始姿势向时间轴姿势混合。</summary>
		/// 1) This is the first timeline to set this property.<para />
		/// 2) The next track entry applied after this one does not have a timeline to set this property.<para />
		/// Result: Mix from the setup pose to the timeline pose.
		internal const int First = 1;
		/// <summary>这是首次写入，下一轨道项写入相同属性、再下一项不写入；保持该属性，避免混合过程出现姿势下陷。</summary>
		/// 1) This is the first timeline to set this property.<para />
		/// 2) The next track entry to be applied does have a timeline to set this property.<para />
		/// 3) The next track entry after that one does not have a timeline to set this property.<para />
		/// Result: Mix from the setup pose to the timeline pose, but do not mix out. This avoids "dipping" when crossfading animations
		/// that key the same property. A subsequent timeline will set this property using a mix.
		internal const int Hold = 2;
		/// <summary>连续多个轨道项写入相同属性时，记录第一个不写入该属性的后续项，用它的混合进度淡出当前属性，避免突然跳变。</summary>
		/// 1) This is the first timeline to set this property.<para />
		/// 2) The next track entry to be applied does have a timeline to set this property.<para />
		/// 3) The next track entry after that one does have a timeline to set this property.<para />
		/// 4) timelineHoldMix stores the first subsequent track entry that does not have a timeline to set this property.<para />
		/// Result: The same as HOLD except the mix percentage from the timelineHoldMix track entry is used. This handles when more than
		/// 2 track entries in a row have a timeline that sets the same property.<para />
		/// Eg, A -> B -> C -> D where A, B, and C have a timeline setting same property, but D does not. When A is applied, to avoid
		/// "dipping" A is not mixed out, however D (the first entry that doesn't set the property) mixing in is used to mix out A
		/// (which affects B and C). Without using D to mix out, A would be applied fully until mixing completes, then snap into
		/// place.
		internal const int HoldMix = 3;
		/// <summary>后续轨道项也为同一插槽设置附件时，不按淡出方向恢复初始附件，使后续变形可从当前姿势混合。</summary>
		/// 1) An attachment timeline in a subsequent track entry sets the attachment for the same slot as this attachment
		/// timeline.<para />
		/// Result: This attachment timeline will not use MixDirection.out, which would otherwise show the setup mode attachment (or
		/// none if not visible in setup mode). This allows deform timelines to be applied for the subsequent entry to mix from, rather
		/// than mixing from the setup pose.
		internal const int NotLast = 4;

		/// <summary>初始配置数据。</summary>
		protected AnimationStateData data;
		/// <summary>按轨道索引保存的当前轨道项集合。</summary>
		private readonly ExposedList<TrackEntry> tracks = new ExposedList<TrackEntry>();
		/// <summary>本次应用动画产生的事件暂存列表。</summary>
		private readonly ExposedList<Event> events = new ExposedList<Event>();
		// 与 libGDX 参考实现不同，此处用委托处理事件回调。 原注： difference to libgdx reference: delegates are used for event callbacks instead of 'final SnapshotArray<AnimationStateListener> listeners'.
		/// <summary>触发动画开始被应用。并通知已订阅的监听者。</summary>
		internal void OnStart (TrackEntry entry) { if (Start != null) Start(entry); }
		/// <summary>触发当前动画被另一动作打断，可能继续参与淡出。并通知已订阅的监听者。</summary>
		internal void OnInterrupt (TrackEntry entry) { if (Interrupt != null) Interrupt(entry); }
		/// <summary>触发此动画不再应用到骨架。并通知已订阅的监听者。</summary>
		internal void OnEnd (TrackEntry entry) { if (End != null) End(entry); }
		/// <summary>触发此轨道项将被回收，之后不可继续持有引用。并通知已订阅的监听者。</summary>
		internal void OnDispose (TrackEntry entry) { if (Dispose != null) Dispose(entry); }
		/// <summary>触发动画完成一轮播放；循环动画每轮都可能触发。并通知已订阅的监听者。</summary>
		internal void OnComplete (TrackEntry entry) { if (Complete != null) Complete(entry); }
		/// <summary>触发模型中的自定义事件关键帧。并通知已订阅的监听者。</summary>
		internal void OnEvent (TrackEntry entry, Event e) { if (Event != null) Event(entry, e); }

		/// <summary>轨道生命周期事件回调，提供触发事件的 TrackEntry。</summary>
		public delegate void TrackEntryDelegate (TrackEntry trackEntry);
		/// <summary>轨道生命周期通知，依次对应开始、打断、结束、回收和完成一轮。</summary>
		public event TrackEntryDelegate Start, Interrupt, End, Dispose, Complete;

		/// <summary>模型事件回调，提供触发的轨道项及事件参数。</summary>
		public delegate void TrackEntryEventDelegate (TrackEntry trackEntry, Event e);
		/// <summary>模型自定义关键帧事件通知。</summary>
		public event TrackEntryEventDelegate Event;
		// 与参考实现的差异说明结束。 原注： end of difference
		/// <summary>延迟派发的动画事件队列。</summary>
		private readonly EventQueue queue; // 由构造函数初始化。 原注： Initialized by constructor.
		/// <summary>本次动画混合中已经处理的属性编号集合。</summary>
		private readonly HashSet<int> propertyIDs = new HashSet<int>();
		/// <summary>轨道或混合链变化后是否需要重算时间轴关系。</summary>
		private bool animationsChanged;
		/// <summary>时间推进倍率，1 为原速。</summary>
		private float timeScale = 1;

		/// <summary>可复用轨道项的对象池。</summary>
		private readonly Pool<TrackEntry> trackEntryPool = new Pool<TrackEntry>();

		/// <summary>创建管理多轨道动作播放、队列和淡入淡出的动画状态，并初始化关联数据及缓冲区。</summary>
		public AnimationState (AnimationStateData data) {
			if (data == null) throw new ArgumentNullException("data", "data cannot be null.");
			this.data = data;
			this.queue = new EventQueue(
				this,
				delegate { this.animationsChanged = true; },
				trackEntryPool
			);
		}

		/// <summary><para>按 delta 秒推进各轨道项时间，并在到达延迟时间时将排队动画切换为当前动画。</para>
		/// Increments the track entry <see cref="TrackEntry.TrackTime"/>, setting queued animations as current if needed.</summary>
		/// <param name="delta">本次推进的时间差，单位为秒。 delta time</param>
		public void Update (float delta) {
			delta *= timeScale;
			var tracksItems = tracks.Items;
			for (int i = 0, n = tracks.Count; i < n; i++) {
				TrackEntry current = tracksItems[i];
				if (current == null) continue;

				current.animationLast = current.nextAnimationLast;
				current.trackLast = current.nextTrackLast;

				float currentDelta = delta * current.timeScale;

				if (current.delay > 0) {
					current.delay -= currentDelta;
					if (current.delay > 0) continue;
					currentDelta = -current.delay;
					current.delay = 0;
				}

				TrackEntry next = current.next;
				if (next != null) {
					// 达到下一动画的延迟时切换，并保留本次更新中剩余的时间。 原注： When the next entry's delay is passed, change to the next entry, preserving leftover time.
					float nextTime = current.trackLast - next.delay;
					if (nextTime >= 0) {
						next.delay = 0;
						next.trackTime += current.timeScale == 0 ? 0 : (nextTime / current.timeScale + delta) * next.timeScale;
						current.trackTime += currentDelta;
						SetCurrent(i, next, true);
						while (next.mixingFrom != null) {
							next.mixTime += delta;
							next = next.mixingFrom;
						}
						continue;
					}
				} else if (current.trackLast >= current.trackEnd && current.mixingFrom == null) {
					// 没有后续动画、达到轨道结束时间且不存在混合来源时，清空轨道。 原注： Clear the track when there is no next entry, the track end time is reached, and there is no mixingFrom.
					tracksItems[i] = null;
					queue.End(current);
					DisposeNext(current);
					continue;
				}
				if (current.mixingFrom != null && UpdateMixingFrom(current, delta)) {
					// 来源动画全部完成混合后，结束其混合链。 原注： End mixing from entries once all have completed.
					TrackEntry from = current.mixingFrom;
					current.mixingFrom = null;
					if (from != null) from.mixingTo = null;
					while (from != null) {
						queue.End(from);
						from = from.mixingFrom;
					}
				}

				current.trackTime += currentDelta;
			}

			queue.Drain();
		}

		/// <summary><para>全部来源动画都已完成混合时返回 true。</para>Returns true when all mixing from entries are complete.</summary>
		private bool UpdateMixingFrom (TrackEntry to, float delta) {
			TrackEntry from = to.mixingFrom;
			if (from == null) return true;

			bool finished = UpdateMixingFrom(from, delta);

			from.animationLast = from.nextAnimationLast;
			from.trackLast = from.nextTrackLast;

			// 要求 mixTime 大于 0，确保来源动画至少被应用过一次。 原注： Require mixTime > 0 to ensure the mixing from entry was applied at least once.
			if (to.mixTime > 0 && to.mixTime >= to.mixDuration) {
				// 除瞬时切换外，要求总权重为 0，确保来源动画确实已经淡出。 原注： Require totalAlpha == 0 to ensure mixing is complete, unless mixDuration == 0 (the transition is a single frame).
				if (from.totalAlpha == 0 || to.mixDuration == 0) {
					to.mixingFrom = from.mixingFrom;
					if (from.mixingFrom != null) from.mixingFrom.mixingTo = to;
					to.interruptAlpha = from.interruptAlpha;
					queue.End(from);
				}
				return finished;
			}

			from.trackTime += delta * from.timeScale;
			to.mixTime += delta;
			return false;
		}

		/// <summary><para>用当前轨道动画计算骨架姿势；除事件通知外不推进状态，可应用到多个骨架；应用过动画时返回 true。</para>
		/// Poses the skeleton using the track entry animations. There are no side effects other than invoking listeners, so the
		/// animation state can be applied to multiple skeletons to pose them identically.</summary>
		/// <returns>本次有动画被应用到骨架时返回 true。 True if any animations were applied.</returns>
		public bool Apply (Skeleton skeleton) {
			if (skeleton == null) throw new ArgumentNullException("skeleton", "skeleton cannot be null.");
			if (animationsChanged) AnimationsChanged();

			var events = this.events;
			bool applied = false;
			var tracksItems = tracks.Items;
			for (int i = 0, n = tracks.Count; i < n; i++) {
				TrackEntry current = tracksItems[i];
				if (current == null || current.delay > 0) continue;
				applied = true;

				// 轨道 0 用于基础姿势而非叠加，因此首帧之前不显示上次应用的动画。 原注： Track 0 animations aren't for layering, so do not show the previously applied animations before the first key.
				MixBlend blend = i == 0 ? MixBlend.First : current.mixBlend;

				// 先应用正在淡出的来源动画。 原注： Apply mixing from entries first.
				float mix = current.alpha;
				if (current.mixingFrom != null)
					mix *= ApplyMixingFrom(current, skeleton, blend);
				else if (current.trackTime >= current.trackEnd && current.next == null) //
					mix = 0; // 在此轨道项最后一次被应用时恢复初始姿势。 原注： Set to setup pose the last time the entry will be applied.

				// 应用当前轨道项。 原注： Apply current entry.
				float animationLast = current.animationLast, animationTime = current.AnimationTime;
				int timelineCount = current.animation.timelines.Count;
				var timelines = current.animation.timelines;
				var timelinesItems = timelines.Items;
				if ((i == 0 && mix == 1) || blend == MixBlend.Add) {
					for (int ii = 0; ii < timelineCount; ii++)
						timelinesItems[ii].Apply(skeleton, animationLast, animationTime, events, mix, blend, MixDirection.In);
				} else {
					var timelineMode = current.timelineMode.Items;

					bool firstFrame = current.timelinesRotation.Count != timelineCount << 1;
					if (firstFrame) current.timelinesRotation.Resize(timelines.Count << 1);
					var timelinesRotation = current.timelinesRotation.Items;

					for (int ii = 0; ii < timelineCount; ii++) {
						Timeline timeline = timelinesItems[ii];
						MixBlend timelineBlend = (timelineMode[ii] & AnimationState.NotLast - 1) == AnimationState.Subsequent ? blend : MixBlend.Setup;
						var rotateTimeline = timeline as RotateTimeline;
						if (rotateTimeline != null)
							ApplyRotateTimeline(rotateTimeline, skeleton, animationTime, mix, timelineBlend, timelinesRotation,
												ii << 1, firstFrame);
						else
							timeline.Apply(skeleton, animationLast, animationTime, events, mix, timelineBlend, MixDirection.In);
					}
				}
				QueueEvents(current, animationTime);
				events.Clear(false);
				current.nextAnimationLast = animationTime;
				current.nextTrackLast = current.trackTime;
			}

			queue.Drain();
			return applied;
		}

		/// <summary>先递归应用来源动画的混合链，再按当前进度淡出其时间轴。</summary>
		private float ApplyMixingFrom (TrackEntry to, Skeleton skeleton, MixBlend blend) {
			TrackEntry from = to.mixingFrom;
			if (from.mixingFrom != null) ApplyMixingFrom(from, skeleton, blend);

			float mix;
			if (to.mixDuration == 0) { // 通过单帧混合撤销来源动画造成的姿势修改。 原注： Single frame mix to undo mixingFrom changes.
				mix = 1;
				if (blend == MixBlend.First) blend = MixBlend.Setup; // 高于 0 的轨道是叠加层，不能整体重置为初始姿势。 原注： Tracks > 0 are transparent and can't reset to setup pose.
			} else {
				mix = to.mixTime / to.mixDuration;
				if (mix > 1) mix = 1;
				if (blend != MixBlend.First) blend = from.mixBlend; // 轨道 0 忽略轨道级混合模式。 原注： Track 0 ignores track mix blend.
			}

			var eventBuffer = mix < from.eventThreshold ? this.events : null;
			bool attachments = mix < from.attachmentThreshold, drawOrder = mix < from.drawOrderThreshold;
			float animationLast = from.animationLast, animationTime = from.AnimationTime;
			var timelines = from.animation.timelines;
			int timelineCount = timelines.Count;
			var timelinesItems = timelines.Items;
			float alphaHold = from.alpha * to.interruptAlpha, alphaMix = alphaHold * (1 - mix);

			if (blend == MixBlend.Add) {
				for (int i = 0; i < timelineCount; i++)
					timelinesItems[i].Apply(skeleton, animationLast, animationTime, eventBuffer, alphaMix, blend, MixDirection.Out);
			} else {
				var timelineMode = from.timelineMode.Items;
				var timelineHoldMix = from.timelineHoldMix.Items;

				bool firstFrame = from.timelinesRotation.Count != timelineCount << 1;
				if (firstFrame)	from.timelinesRotation.Resize(timelines.Count << 1); // 参考实现：调整来源时间轴的旋转缓存大小。 原注： from.timelinesRotation.setSize
				var timelinesRotation = from.timelinesRotation.Items;

				from.totalAlpha = 0;
				for (int i = 0; i < timelineCount; i++) {
					Timeline timeline = timelinesItems[i];
					MixDirection direction = MixDirection.Out;
					MixBlend timelineBlend;
					float alpha;
					switch (timelineMode[i] & AnimationState.NotLast - 1) {
						case AnimationState.Subsequent:
							timelineBlend = blend;
							if (!attachments && timeline is AttachmentTimeline) {
								if ((timelineMode[i] & AnimationState.NotLast) == AnimationState.NotLast) continue;
								timelineBlend = MixBlend.Setup;
							}
							if (!drawOrder && timeline is DrawOrderTimeline) continue;
							alpha = alphaMix;
							break;
						case AnimationState.First:
							timelineBlend = MixBlend.Setup;
							alpha = alphaMix;
							break;
						case AnimationState.Hold:
							timelineBlend = MixBlend.Setup;
							alpha = alphaHold;
							break;
						default:
							timelineBlend = MixBlend.Setup;
							TrackEntry holdMix = timelineHoldMix[i];
							alpha = alphaHold * Math.Max(0, 1 - holdMix.mixTime / holdMix.mixDuration);
							break;
					}
					from.totalAlpha += alpha;

					var rotateTimeline = timeline as RotateTimeline;
					if (rotateTimeline != null) {
						ApplyRotateTimeline(rotateTimeline, skeleton, animationTime, alpha, timelineBlend, timelinesRotation,
											i << 1, firstFrame);
					} else {
						if (timelineBlend == MixBlend.Setup) {
							if (timeline is AttachmentTimeline) {
								if (attachments || (timelineMode[i] & AnimationState.NotLast) == AnimationState.NotLast) direction = MixDirection.In;
							} else if (timeline is DrawOrderTimeline) {
								if (drawOrder) {
									direction = MixDirection.In;
								}
							}
						}

						timeline.Apply(skeleton, animationLast, animationTime, eventBuffer, alpha, timelineBlend, direction);
					}
				}
			}

			if (to.mixDuration > 0) QueueEvents(from, animationTime);
			this.events.Clear(false);
			from.nextAnimationLast = animationTime;
			from.nextTrackLast = from.trackTime;

			return mix;
		}

		/// <summary>应用旋转时间轴并保留混合方向和完整圈数，避免跨越角度边界时翻转。</summary>
		static private void ApplyRotateTimeline (RotateTimeline timeline, Skeleton skeleton, float time, float alpha, MixBlend blend,
			float[] timelinesRotation, int i, bool firstFrame) {

			if (firstFrame) timelinesRotation[i] = 0;

			if (alpha == 1) {
				timeline.Apply(skeleton, 0, time, null, 1, blend, MixDirection.In);
				return;
			}

			Bone bone = skeleton.bones.Items[timeline.boneIndex];
			if (!bone.active) return;

			float[] frames = timeline.frames;
			float r1, r2;
			if (time < frames[0]) { // 当前时间早于首个关键帧。 原注： Time is before first frame.
				switch (blend) {
					case MixBlend.Setup:
						bone.rotation = bone.data.rotation;
						return;
					default:
						return;
					case MixBlend.First:
						r1 = bone.rotation;
						r2 = bone.data.rotation;
						break;
				}
			} else {
				r1 = blend == MixBlend.Setup ? bone.data.rotation : bone.rotation;
				if (time >= frames[frames.Length - RotateTimeline.ENTRIES]) // 当前时间晚于最后一个关键帧。 原注： Time is after last frame.
					r2 = bone.data.rotation + frames[frames.Length + RotateTimeline.PREV_ROTATION];
				else {
					// 在前一个关键帧和当前关键帧之间进行插值。 原注： Interpolate between the previous frame and the current frame.
					int frame = Animation.BinarySearch(frames, time, RotateTimeline.ENTRIES);
					float prevRotation = frames[frame + RotateTimeline.PREV_ROTATION];
					float frameTime = frames[frame];
					float percent = timeline.GetCurvePercent((frame >> 1) - 1,
						1 - (time - frameTime) / (frames[frame + RotateTimeline.PREV_TIME] - frameTime));

					r2 = frames[frame + RotateTimeline.ROTATION] - prevRotation;
					r2 -= (16384 - (int)(16384.499999999996 - r2 / 360)) * 360;
					r2 = prevRotation + r2 * percent + bone.data.rotation;
					r2 -= (16384 - (int)(16384.499999999996 - r2 / 360)) * 360;
				}
			}

			// 第一帧选择两角度间的最短旋转方向，并在后续混合中保持方向。 原注： Mix between rotations using the direction of the shortest route on the first frame.
			float total, diff = r2 - r1;
			diff -= (16384 - (int)(16384.499999999996 - diff / 360)) * 360;
			if (diff == 0) {
				total = timelinesRotation[i];
			} else {
				float lastTotal, lastDiff;
				if (firstFrame) {
					lastTotal = 0;
					lastDiff = diff;
				} else {
					lastTotal = timelinesRotation[i]; // 混合旋转的角度和方向，包括已转过的完整圈数。 原注： Angle and direction of mix, including loops.
					lastDiff = timelinesRotation[i + 1]; // 两种骨骼旋转之间的角度差。 原注： Difference between bones.
				}
				bool current = diff > 0, dir = lastTotal >= 0;
				// 检测经过 0 度的位置，而不是 180 度。 原注： Detect cross at 0 (not 180).
				if (Math.Sign(lastDiff) != Math.Sign(diff) && Math.Abs(lastDiff) <= 90) {
					// 转过 360 度后再次越过零点，表示多完成了一圈。 原注： A cross after a 360 rotation is a loop.
					if (Math.Abs(lastTotal) > 180) lastTotal += 360 * Math.Sign(lastTotal);
					dir = current;
				}
				total = diff + lastTotal - lastTotal % 360; // 将完整圈数保存在 lastTotal 中。 原注： Store loops as part of lastTotal.
				if (dir != current) total += 360 * Math.Sign(lastTotal);
				timelinesRotation[i] = total;
			}
			timelinesRotation[i + 1] = diff;
			r1 += total * alpha;
			bone.rotation = r1 - (16384 - (int)(16384.499999999996 - r1 / 360)) * 360;
		}

		/// <summary>按动画区间和循环边界收集模型事件及完成事件，依次加入通知队列。</summary>
		private void QueueEvents (TrackEntry entry, float animationTime) {
			float animationStart = entry.animationStart, animationEnd = entry.animationEnd;
			float duration = animationEnd - animationStart;
			float trackLastWrapped = entry.trackLast % duration;

			// 先把完成事件之前的模型事件入队。 原注： Queue events before complete.
			var events = this.events;
			var eventsItems = events.Items;
			int i = 0, n = events.Count;
			for (; i < n; i++) {
				Event e = eventsItems[i];
				if (e.time < trackLastWrapped) break;
				if (e.time > animationEnd) continue; // 丢弃动画起止区间以外的事件。 原注： Discard events outside animation start/end.
				queue.Event(entry, e);
			}

			// 非循环动画播完或循环动画完成一轮时，将完成事件入队。 原注： Queue complete if completed a loop iteration or the animation.
			bool complete = false;
			if (entry.loop)
				complete = duration == 0 || (trackLastWrapped > entry.trackTime % duration);
			else
				complete = animationTime >= animationEnd && entry.animationLast < animationEnd;
			if (complete) queue.Complete(entry);

			// 把完成事件之后的模型事件入队。 原注： Queue events after complete.
			for (; i < n; i++) {
				Event e = eventsItems[i];
				if (e.time < animationStart) continue; // 丢弃动画起止区间以外的事件。 原注： Discard events outside animation start/end.
				queue.Event(entry, eventsItems[i]);
			}
		}

		/// <summary><para>清空所有轨道的动画，保留骨架当前姿势；若希望平滑回到初始姿势，应使用空动画淡出。</para>
		/// <para>Removes all animations from all tracks, leaving skeletons in their current pose.</para>
		/// <para>
		/// It may be desired to use <see cref="AnimationState.SetEmptyAnimations(float)"/> to mix the skeletons back to the setup pose,
		/// rather than leaving them in their current pose.</para>
		 /// </summary>
		public void ClearTracks () {
			bool oldDrainDisabled = queue.drainDisabled;
			queue.drainDisabled = true;
			for (int i = 0, n = tracks.Count; i < n; i++) {
				ClearTrack(i);
			}
			tracks.Clear();
			queue.drainDisabled = oldDrainDisabled;
			queue.Drain();
		}

		/// <summary><para>清空指定轨道的动画，保留骨架当前姿势；可先使用空动画平滑淡出。</para>
		/// <para>Removes all animations from the track, leaving skeletons in their current pose.</para>
		/// <para>
		/// It may be desired to use <see cref="AnimationState.SetEmptyAnimation(int, float)"/> to mix the skeletons back to the setup pose,
		/// rather than leaving them in their current pose.</para>
		/// </summary>
		public void ClearTrack (int trackIndex) {
			if (trackIndex >= tracks.Count) return;
			TrackEntry current = tracks.Items[trackIndex];
			if (current == null) return;

			queue.End(current);

			DisposeNext(current);

			TrackEntry entry = current;
			while (true) {
				TrackEntry from = entry.mixingFrom;
				if (from == null) break;
				queue.End(from);
				entry.mixingFrom = null;
				entry.mixingTo = null;
				entry = from;
			}

			tracks.Items[current.trackIndex] = null;

			queue.Drain();
		}

		/// <summary><para>为指定轨道编号设置当前轨道项。</para>Sets the active TrackEntry for a given track number.</summary>
		private void SetCurrent (int index, TrackEntry current, bool interrupt) {
			TrackEntry from = ExpandToIndex(index);
			tracks.Items[index] = current;

			if (from != null) {
				if (interrupt) queue.Interrupt(from);
				current.mixingFrom = from;
				from.mixingTo = current;
				current.mixTime = 0;

				// 保存被打断时的混合进度。 原注： Store the interrupted mix percentage.
				if (from.mixingFrom != null && from.mixDuration > 0)
					current.interruptAlpha *= Math.Min(1, from.mixTime / from.mixDuration);

				from.timelinesRotation.Clear(); // 重置淡出旋转缓存，处理此项此前处于淡入状态的情况。 原注： Reset rotation for mixing out, in case entry was mixed in.
			}

			queue.Start(current); // 触发动画集合变化处理。 原注： triggers AnimationsChanged
		}


		/// <summary><para>通过动作名称设置指定轨道的动画。</para>Sets an animation by name. <seealso cref="SetAnimation(int, Animation, bool)" /></summary>
		public TrackEntry SetAnimation (int trackIndex, string animationName, bool loop) {
			Animation animation = data.skeletonData.FindAnimation(animationName);
			if (animation == null) throw new ArgumentException("Animation not found: " + animationName, "animationName");
			return SetAnimation(trackIndex, animation, loop);
		}

		/// <summary><para>替换指定轨道的当前动画并丢弃排队动作；loop 控制循环，非循环超时保持末帧。返回 TrackEntry 可进一步设置；Dispose 事件之后不能保留该对象引用。</para>Sets the current animation for a track, discarding any queued animations. If the formerly current track entry was never
		/// applied to a skeleton, it is replaced (not mixed from).</summary>
		/// <param name="loop">是否循环；非循环超出时长保持末帧，轨道移除时机另由 TrackEnd 控制。 If true, the animation will repeat. If false it will not, instead its last frame is applied if played beyond its
		///          duration. In either case<see cref="TrackEntry.TrackEnd"/> determines when the track is cleared.</param>
		/// <returns>可继续配置的轨道项；收到 Dispose 事件后不得持有或使用该引用。  A track entry to allow further customization of animation playback. References to the track entry must not be kept
		///          after the <see cref="AnimationState.Dispose"/> event occurs.</returns>
		public TrackEntry SetAnimation (int trackIndex, Animation animation, bool loop) {
			if (animation == null) throw new ArgumentNullException("animation", "animation cannot be null.");
			bool interrupt = true;
			TrackEntry current = ExpandToIndex(trackIndex);
			if (current != null) {
				if (current.nextTrackLast == -1) {
					// 从未应用到骨架的动画不参与来源混合。 原注： Don't mix from an entry that was never applied.
					tracks.Items[trackIndex] = current.mixingFrom;
					queue.Interrupt(current);
					queue.End(current);
					DisposeNext(current);
					current = current.mixingFrom;
					interrupt = false; // 来源动画重新成为当前项时，不要重复触发打断事件。 原注： mixingFrom is current again, but don't interrupt it twice.
				} else {
					DisposeNext(current);
				}
			}
			TrackEntry entry = NewTrackEntry(trackIndex, animation, loop, current);
			SetCurrent(trackIndex, entry, interrupt);
			queue.Drain();
			return entry;
		}

		/// <summary><para>通过动作名称把动画加入指定轨道的播放队列。</para>Queues an animation by name.</summary>
		/// <seealso cref="AddAnimation(int, Animation, bool, float)" />
		public TrackEntry AddAnimation (int trackIndex, string animationName, bool loop, float delay) {
			Animation animation = data.skeletonData.FindAnimation(animationName);
			if (animation == null) throw new ArgumentException("Animation not found: " + animationName, "animationName");
			return AddAnimation(trackIndex, animation, loop, delay);
		}

		/// <summary><para>把动作排在当前或队尾动作之后；空轨道等同直接设置。delay 为正时是显式延迟，否则依据前一动作时长减混合时长再加 delay；循环动作使用下一轮结束时间。Dispose 后不得保留返回的轨道项。</para>Adds an animation to be played after the current or last queued animation for a track. If the track is empty, it is
		/// equivalent to calling <see cref="SetAnimation(int, Animation, bool)"/>.</summary>
		/// <param name="delay">正值显式设置延迟；非正值按前一动作时长减混合时长再加此值计算，循环动作使用下一轮结束时间。 
		/// If &gt; 0, sets <see cref="TrackEntry.Delay"/>. If &lt;= 0, the delay set is the duration of the previous track entry
		/// minus any mix duration (from the {@link AnimationStateData}) plus the specified <code>Delay</code> (ie the mix
		/// ends at (<code>Delay</code> = 0) or before (<code>Delay</code> &lt; 0) the previous track entry duration). If the
		/// previous entry is looping, its next loop completion is used instead of its duration.
		/// </param>
		/// <returns>可继续配置的轨道项；收到 Dispose 事件后不得持有或使用该引用。 A track entry to allow further customization of animation playback. References to the track entry must not be kept
		/// after the <see cref="AnimationState.Dispose"/> event occurs.</returns>
		public TrackEntry AddAnimation (int trackIndex, Animation animation, bool loop, float delay) {
			if (animation == null) throw new ArgumentNullException("animation", "animation cannot be null.");

			TrackEntry last = ExpandToIndex(trackIndex);
			if (last != null) {
				while (last.next != null)
					last = last.next;
			}

			TrackEntry entry = NewTrackEntry(trackIndex, animation, loop, last);

			if (last == null) {
				SetCurrent(trackIndex, entry, true);
				queue.Drain();
			} else {
				last.next = entry;
				if (delay <= 0) {
					float duration = last.animationEnd - last.animationStart;
					if (duration != 0) {
						if (last.loop) {
							delay += duration * (1 + (int)(last.trackTime / duration)); // 使用下一次循环结束时间。 原注： Completion of next loop.
						} else {
							delay += Math.Max(duration, last.trackTime); // 在动作时长之后开始，否则在下一次更新开始。 原注： After duration, else next update.
						}
						delay -= data.GetMix(last.animation, animation);
					} else
						delay = last.trackTime; // 在下一次更新时开始。 原注： Next update.
				}
			}

			entry.delay = delay;
			return entry;
		}

		/// <summary><para>设置没有时间轴的空动画并清空队列，用于平滑淡入淡出；淡出使前一动作逐渐回到低轨道或初始姿势。时长为 0 仍需一帧。淡入可先设空动画，再排入新动作并设置混合时长。</para>
		/// <para>Sets an empty animation for a track, discarding any queued animations, and sets the track entry's
		/// <see cref="TrackEntry.getMixDuration()"/>. An empty animation has no timelines and serves as a placeholder for mixing in or out.</para>
		/// <para>
		/// Mixing out is done by setting an empty animation with a mix duration using either <see cref="AnimationState.SetEmptyAnimation(int, float)"/>,
		/// <see cref="AnimationState.SetEmptyAnimations(float)"/>, or <see cref="AnimationState.AddEmptyAnimation(int, float, float)"/>. Mixing to an empty animation causes
		/// the previous animation to be applied less and less over the mix duration. Properties keyed in the previous animation
		/// transition to the value from lower tracks or to the setup pose value if no lower tracks key the property. A mix duration of
		/// 0 still mixes out over one frame.</para>
		/// <para>
		/// Mixing in is done by first setting an empty animation, then adding an animation using
		/// <see cref="AnimationState.AddAnimation(int, Animation, boolean, float)"/> and on the returned track entry, set the
		/// <see cref="TrackEntry.SetMixDuration(float)"/>. Mixing from an empty animation causes the new animation to be applied more and
		/// more over the mix duration. Properties keyed in the new animation transition from the value from lower tracks or from the
		/// setup pose value if no lower tracks key the property to the value keyed in the new animation.</para>
		/// </summary>
		public TrackEntry SetEmptyAnimation (int trackIndex, float mixDuration) {
			TrackEntry entry = SetAnimation(trackIndex, AnimationState.EmptyAnimation, false);
			entry.mixDuration = mixDuration;
			entry.trackEnd = mixDuration;
			return entry;
		}

		/// <summary><para>在指定轨道队尾添加空动画并设置混合时长；空轨道直接设置。正延迟显式等待，非正延迟根据前一动作时长和混合时长计算；Dispose 后不得保留返回的轨道项。</para>
		/// Adds an empty animation to be played after the current or last queued animation for a track, and sets the track entry's
		/// <see cref="TrackEntry.MixDuration"/>. If the track is empty, it is equivalent to calling
		/// <see cref="AnimationState.SetEmptyAnimation(int, float)"/>.</summary>
		/// <seealso cref="AnimationState.SetEmptyAnimation(int, float)"/>
		/// <param name="trackIndex">轨道编号，从 0 开始。 Track number.</param>
		/// <param name="mixDuration">动作混合时长，单位为秒。 Mix duration.</param>
		/// <param name="delay">正值为显式延迟；非正值相对前一动作结束时间计算并扣除混合时长，循环动作按下一次循环结束计算。 If &gt; 0, sets <see cref="TrackEntry.Delay"/>. If &lt;= 0, the delay set is the duration of the previous track entry
		/// minus any mix duration plus the specified <code>Delay</code> (ie the mix ends at (<code>Delay</code> = 0) or
		/// before (<code>Delay</code> &lt; 0) the previous track entry duration). If the previous entry is looping, its next
		/// loop completion is used instead of its duration.</param>
		/// <returns>可继续配置的轨道项；收到 Dispose 事件后不得持有或使用该引用。  A track entry to allow further customization of animation playback. References to the track entry must not be kept
		/// after the <see cref="AnimationState.Dispose"/> event occurs.
		/// </returns>
		public TrackEntry AddEmptyAnimation (int trackIndex, float mixDuration, float delay) {
			if (delay <= 0) delay -= mixDuration;
			TrackEntry entry = AddAnimation(trackIndex, AnimationState.EmptyAnimation, false, delay);
			entry.mixDuration = mixDuration;
			entry.trackEnd = mixDuration;
			return entry;
		}

		/// <summary><para>为所有轨道设置空动画，丢弃各队列，并按指定时长淡出。</para>
		/// Sets an empty animation for every track, discarding any queued animations, and mixes to it over the specified mix
		/// duration.</summary>
		public void SetEmptyAnimations (float mixDuration) {
			bool oldDrainDisabled = queue.drainDisabled;
			queue.drainDisabled = true;
			for (int i = 0, n = tracks.Count; i < n; i++) {
				TrackEntry current = tracks.Items[i];
				if (current != null) SetEmptyAnimation(current.trackIndex, mixDuration);
			}
			queue.drainDisabled = oldDrainDisabled;
			queue.Drain();
		}

		/// <summary>扩展轨道数组以容纳指定索引，新增空轨道置为 null。</summary>
		private TrackEntry ExpandToIndex (int index) {
			if (index < tracks.Count) return tracks.Items[index];
			tracks.Resize(index + 1);
			return null;
		}

		/// <summary><para>从对象池取得并初始化 TrackEntry，避免频繁分配；前一轨道项可以为 null。</para>Object-pooling version of new TrackEntry. Obtain an unused TrackEntry from the pool and clear/initialize its values.</summary>
		/// <param name="last">前一个轨道项，可以为 null。 May be null.</param>
		private TrackEntry NewTrackEntry (int trackIndex, Animation animation, bool loop, TrackEntry last) {
			TrackEntry entry = trackEntryPool.Obtain(); // 对象池复用处理。 原注： Pooling
			entry.trackIndex = trackIndex;
			entry.animation = animation;
			entry.loop = loop;
			entry.holdPrevious = false;

			entry.eventThreshold = 0;
			entry.attachmentThreshold = 0;
			entry.drawOrderThreshold = 0;

			entry.animationStart = 0;
			entry.animationEnd = animation.Duration;
			entry.animationLast = -1;
			entry.nextAnimationLast = -1;

			entry.delay = 0;
			entry.trackTime = 0;
			entry.trackLast = -1;
			entry.nextTrackLast = -1; // nextTrackLast 为 -1 表示该轨道项尚未应用到骨架。 原注： nextTrackLast == -1 signifies a TrackEntry that wasn't applied yet.
			entry.trackEnd = float.MaxValue; // 参考表达式：循环动作使用无限轨道结束时间，非循环动作使用动画时长。 原注： loop ? float.MaxValue : animation.Duration;
			entry.timeScale = 1;

			entry.alpha = 1;
			entry.interruptAlpha = 1;
			entry.mixTime = 0;
			entry.mixDuration = (last == null) ? 0 : data.GetMix(last.animation, animation);
			return entry;
		}

		/// <summary><para>释放指定轨道项之后排队的全部轨道项。</para>Dispose all track entries queued after the given TrackEntry.</summary>
		private void DisposeNext (TrackEntry entry) {
			TrackEntry next = entry.next;
			while (next != null) {
				queue.Dispose(next);
				next = next.next;
			}
			entry.next = null;
		}

		/// <summary>重算各轨道时间轴的属性覆盖和保持混合关系。</summary>
		private void AnimationsChanged () {
			animationsChanged = false;

			// 按动画实际应用顺序处理。 原注： Process in the order that animations are applied.
			propertyIDs.Clear();

			var tracksItems = tracks.Items;
			for (int i = 0, n = tracks.Count; i < n; i++) {
				TrackEntry entry = tracksItems[i];
				if (entry == null) continue;
				while (entry.mixingFrom != null) // 先移动到最后一项，再反向遍历。 原注： Move to last entry, then iterate in reverse.
					entry = entry.mixingFrom;

				do {
					if (entry.mixingTo == null || entry.mixBlend != MixBlend.Add) ComputeHold(entry);
					entry = entry.mixingTo;
				} while (entry != null);
			}

			// 按动画实际应用顺序的逆序处理。 原注： Process in the reverse order that animations are applied.
			propertyIDs.Clear();
			for (int i = tracks.Count - 1; i >= 0; i--) {
				TrackEntry entry = tracksItems[i];
				while (entry != null) {
					ComputeNotLast(entry);
					entry = entry.mixingFrom;
				}
			}
		}

		/// <summary>计算此轨道项每个时间轴应保持、混合还是交由后续项接管。</summary>
		private void ComputeHold (TrackEntry entry) {
			TrackEntry to = entry.mixingTo;
			var timelines = entry.animation.timelines.Items;
			int timelinesCount = entry.animation.timelines.Count;
			var timelineMode = entry.timelineMode.Resize(timelinesCount).Items; // 参考实现：设置时间轴混合模式数组的有效数量。 原注：timelineMode.setSize(timelinesCount);
			entry.timelineHoldMix.Clear();
			var timelineHoldMix = entry.timelineHoldMix.Resize(timelinesCount).Items; // 参考实现：设置保持混合来源数组的有效数量。 原注：timelineHoldMix.setSize(timelinesCount);
			var propertyIDs = this.propertyIDs;

			if (to != null && to.holdPrevious) {
				for (int i = 0; i < timelinesCount; i++) {
					propertyIDs.Add(timelines[i].PropertyId);
					timelineMode[i] = AnimationState.Hold;
				}
				return;
			}

			// 参考实现的外层循环标签。 原注： outer:
			for (int i = 0; i < timelinesCount; i++) {
				Timeline timeline = timelines[i];
				int id = timeline.PropertyId;
				if (!propertyIDs.Add(id))
					timelineMode[i] = AnimationState.Subsequent;
				else if (to == null || timeline is AttachmentTimeline || timeline is DrawOrderTimeline
						|| timeline is EventTimeline || !to.animation.HasTimeline(id)) {
					timelineMode[i] = AnimationState.First;
				} else {
					for (TrackEntry next = to.mixingTo; next != null; next = next.mixingTo) {
						if (next.animation.HasTimeline(id)) continue;
						if (next.mixDuration > 0) {
							timelineMode[i] = AnimationState.HoldMix;
							timelineHoldMix[i] = next;
							goto continue_outer; // 参考实现：继续外层循环。 原注： continue outer;
						}
						break;
					}
					timelineMode[i] = AnimationState.Hold;
				}
				continue_outer: {}
			}
		}

		/// <summary>标记并非最后负责切换同一插槽附件的时间轴，保留后续变形混合来源。</summary>
		private void ComputeNotLast (TrackEntry entry) {
			var timelines = entry.animation.timelines.Items;
			int timelinesCount = entry.animation.timelines.Count;
			int[] timelineMode = entry.timelineMode.Items;
			var propertyIDs = this.propertyIDs;

			for (int i = 0; i < timelinesCount; i++) {
				if (timelines[i] is AttachmentTimeline) {
					AttachmentTimeline timeline = (AttachmentTimeline)timelines[i];
					if (!propertyIDs.Add(timeline.slotIndex)) timelineMode[i] |= AnimationState.NotLast;
				}
			}
		}

		/// <summary>返回指定轨道当前播放的轨道项，没有动画则返回 null。</summary>
		/// <returns>该轨道当前播放的轨道项，没有动画时为 null。 The track entry for the animation currently playing on the track, or null if no animation is currently playing.</returns>
		public TrackEntry GetCurrent (int trackIndex) {
			if (trackIndex >= tracks.Count) return null;
			return tracks.Items[trackIndex];
		}

		/// <summary><para>丢弃尚未派发的监听通知；在事件回调切换动作后，可用于取消旧动作已经排队但不再需要的通知。</para> Discards all listener notifications that have not yet been delivered. This can be useful to call from an
		/// AnimationState event subscriber when it is known that further notifications that may have been already queued for delivery
		/// are not wanted because new animations are being set.
		/// </summary>
		public void ClearListenerNotifications () {
			queue.Clear();
		}

		/// <summary><para>全部轨道和混合的时间倍率，默认 1；单个动作的倍率应设置 TrackEntry.TimeScale。</para>
		/// <para>Multiplier for the delta time when the animation state is updated, causing time for all animations and mixes to play slower
		/// or faster. Defaults to 1.</para>
		/// <para>
		/// See TrackEntry <see cref="TrackEntry.TimeScale"/> for affecting a single animation.</para>
		/// </summary>
		public float TimeScale { get { return timeScale; } set { timeScale = value; } }

		/// <summary><para>用于查询动作间混合时长的共享配置。</para>The AnimationStateData to look up mix durations.</summary>
		public AnimationStateData Data {
			get {
				return data;
			}
			set {
				if (data == null) throw new ArgumentNullException("data", "data cannot be null.");
				this.data = value;
			}
		}

		/// <summary><para>轨道项列表，其中可包含空轨道 null。</para>A list of tracks that have animations, which may contain nulls.</summary>
		public ExposedList<TrackEntry> Tracks { get { return tracks; } }

		/// <summary>返回当前各轨道的动作名称，便于调试。</summary>
		override public string ToString () {
			var buffer = new System.Text.StringBuilder();
			for (int i = 0, n = tracks.Count; i < n; i++) {
				TrackEntry entry = tracks.Items[i];
				if (entry == null) continue;
				if (buffer.Length > 0) buffer.Append(", ");
				buffer.Append(entry.ToString());
			}
			if (buffer.Length == 0) return "<none>";
			return buffer.ToString();
		}
	}

	/// <summary><para>保存一个轨道中单个动画的播放设置及状态；对象由池管理，收到 Dispose 事件后不能继续保留或使用引用。</para>
	/// <para>
	/// Stores settings and other state for the playback of an animation on an <see cref="AnimationState"/> track.</para>
	/// <para>
	/// References to a track entry must not be kept after the <see cref="AnimationStateListener.Dispose(TrackEntry)"/> event occurs.</para>
	/// </summary>
	public class TrackEntry : Pool<TrackEntry>.IPoolable {
		/// <summary>当前关联的动画数据。</summary>
		internal Animation animation;

		/// <summary>next：排队的下一轨道项；mixingFrom：正在淡出的前一轨道项；mixingTo：正在淡入的后一轨道项。</summary>
		internal TrackEntry next, mixingFrom, mixingTo;
		// 与 libGDX 参考实现不同，单轨道事件监听使用委托。 原注： difference to libgdx reference: delegates are used for event callbacks instead of 'AnimationStateListener listener'.
		/// <summary>轨道生命周期通知，依次对应开始、打断、结束、回收和完成一轮。</summary>
		public event AnimationState.TrackEntryDelegate Start, Interrupt, End, Dispose, Complete;
		/// <summary>模型自定义关键帧事件通知。</summary>
		public event AnimationState.TrackEntryEventDelegate Event;
		/// <summary>触发动画开始被应用。并通知已订阅的监听者。</summary>
		internal void OnStart () { if (Start != null) Start(this); }
		/// <summary>触发当前动画被另一动作打断，可能继续参与淡出。并通知已订阅的监听者。</summary>
		internal void OnInterrupt () { if (Interrupt != null) Interrupt(this); }
		/// <summary>触发此动画不再应用到骨架。并通知已订阅的监听者。</summary>
		internal void OnEnd () { if (End != null) End(this); }
		/// <summary>触发此轨道项将被回收，之后不可继续持有引用。并通知已订阅的监听者。</summary>
		internal void OnDispose () { if (Dispose != null) Dispose(this); }
		/// <summary>触发动画完成一轮播放；循环动画每轮都可能触发。并通知已订阅的监听者。</summary>
		internal void OnComplete () { if (Complete != null) Complete(this); }
		/// <summary>触发模型中的自定义事件关键帧。并通知已订阅的监听者。</summary>
		internal void OnEvent (Event e) { if (Event != null) Event(this, e); }

		/// <summary>所属播放轨道的索引。</summary>
		internal int trackIndex;

		/// <summary>loop：是否循环播放；holdPrevious：混合时是否保持前一动画的完整权重。</summary>
		internal bool loop, holdPrevious;
		/// <summary>eventThreshold：淡出时继续触发事件的混合进度阈值；attachmentThreshold：淡出时继续应用附件切换的阈值；drawOrderThreshold：淡出时继续应用绘制顺序的阈值。</summary>
		internal float eventThreshold, attachmentThreshold, drawOrderThreshold;
		/// <summary>animationStart：动作播放区间起点（秒）；animationEnd：动作播放区间终点（秒）；animationLast：上次应用的动画时间（秒）；nextAnimationLast：待提交为上次时间的动画时间缓存。</summary>
		internal float animationStart, animationEnd, animationLast, nextAnimationLast;
		/// <summary>delay：开始应用前的延迟（秒）；trackTime：当前轨道项的累计播放时间（秒）；trackLast：上次应用的轨道时间；nextTrackLast：待提交的上次轨道时间；trackEnd：清除该轨道项的结束时间；timeScale：时间推进倍率，1 为原速。</summary>
		internal float delay, trackTime, trackLast, nextTrackLast, trackEnd, timeScale = 1f;
		/// <summary>alpha：应用此动作的混合权重；mixTime：动作间混合已进行的秒数；mixDuration：动作间混合总秒数；interruptAlpha：发生打断时的混合权重；totalAlpha：时间轴应用的总权重。</summary>
		internal float alpha, mixTime, mixDuration, interruptAlpha, totalAlpha;
		/// <summary>与低轨道的姿势混合方式。</summary>
		internal MixBlend mixBlend = MixBlend.Replace;
		/// <summary>各时间轴的混合处理模式。</summary>
		internal readonly ExposedList<int> timelineMode = new ExposedList<int>();
		/// <summary>各时间轴保持属性时使用的后续混合项。</summary>
		internal readonly ExposedList<TrackEntry> timelineHoldMix = new ExposedList<TrackEntry>();
		/// <summary>旋转混合的方向及累计圈数缓存。</summary>
		internal readonly ExposedList<float> timelinesRotation = new ExposedList<float>();

		// 对象归还池时通过 IPoolable.Reset 重置。 原注： IPoolable.Reset()
		/// <summary>重置对象状态，便于后续复用。</summary>
		public void Reset () {
			next = null;
			mixingFrom = null;
			mixingTo = null;
			animation = null;
			// 清空事件委托，替代参考实现清空 listener 的逻辑。 原注： replaces 'listener = null;' since delegates are used for event callbacks
			Start = null;
			Interrupt = null;
			End = null;
			Dispose = null;
			Complete = null;
			Event = null;
			timelineMode.Clear();
			timelineHoldMix.Clear();
			timelinesRotation.Clear();
		}

		/// <summary><para>此项当前播放或排队所在的轨道索引。</para>The index of the track where this entry is either current or queued.</summary>
		/// <seealso cref="AnimationState.GetCurrent(int)"/>
		public int TrackIndex { get { return trackIndex; } }

		/// <summary><para>此轨道项应用的动画数据。</para>The animation to apply for this track entry.</summary>
		public Animation Animation { get { return animation; } }

		/// <summary><para>true 循环播放；false 不循环，超出时长时保持最后一帧。</para>
		/// If true, the animation will repeat. If false it will not, instead its last frame is applied if played beyond its
		/// duration.</summary>
		public bool Loop { get { return loop; } set { loop = value; } }

		///<summary><para>延迟秒数；当前项用于延后 TrackTime 推进，排队项表示前一项开始后多久切换到此项；时间倍率会影响等待。</para>
		/// <para>
		/// Seconds to postpone playing the animation. When this track entry is the current track entry, <code>Delay</code>
		/// postpones incrementing the <see cref="TrackEntry.TrackTime"/>. When this track entry is queued, <code>Delay</code> is the time from
		/// the start of the previous animation to when this track entry will become the current track entry (ie when the previous
		/// track entry <see cref="TrackEntry.TrackTime"/> &gt;= this track entry's <code>Delay</code>).</para>
		/// <para>
		/// <see cref="TrackEntry.TimeScale"/> affects the delay.</para>
		/// </summary>
		public float Delay { get { return delay; } set { delay = value; } }

		/// <summary><para>此项成为当前项之后经过的轨道时间（秒）；可改为非零从动作中途开始，循环逻辑不受影响。</para>
		/// Current time in seconds this track entry has been the current track entry. The track time determines
		/// <see cref="TrackEntry.AnimationTime"/>. The track time can be set to start the animation at a time other than 0, without affecting
		/// looping.</summary>
		public float TrackTime { get { return trackTime; } set { trackTime = value; } }

		/// <summary><para>从轨道移除此项的轨道时间（秒），默认无限；到时且无队列、无未完成混合时恢复初始姿势并清空，可用空动画实现平滑退出。</para>
		/// <para>
		/// The track time in seconds when this animation will be removed from the track. Defaults to the highest possible float
		/// value, meaning the animation will be applied until a new animation is set or the track is cleared. If the track end time
		/// is reached, no other animations are queued for playback, and mixing from any previous animations is complete, then the
		/// properties keyed by the animation are set to the setup pose and the track is cleared.</para>
		/// <para>
		/// It may be desired to use <see cref="AnimationState.AddEmptyAnimation(int, float, float)"/>  rather than have the animation
		/// abruptly cease being applied.</para>
		/// </summary>
		public float TrackEnd { get { return trackEnd; } set { trackEnd = value; } }

		/// <summary><para>动画区间起点（秒），默认 0；修改后可同步设置 AnimationLast，防止起点之前的事件误触发。</para>
		/// <para>
		/// Seconds when this animation starts, both initially and after looping. Defaults to 0.</para>
		/// <para>
		/// When changing the <code>AnimationStart</code> time, it often makes sense to set <see cref="TrackEntry.AnimationLast"/> to the same
		/// value to prevent timeline keys before the start time from triggering.</para>
		/// </summary>
		public float AnimationStart { get { return animationStart; } set { animationStart = value; } }

		/// <summary><para>动画区间终点（秒），默认动画总时长；非循环停在此处，循环回到 AnimationStart。</para>
		/// Seconds for the last frame of this animation. Non-looping animations won't play past this time. Looping animations will
		/// loop back to <see cref="TrackEntry.AnimationStart"/> at this time. Defaults to the animation <see cref="Animation.Duration"/>.
		///</summary>
		public float AnimationEnd { get { return animationEnd; } set { animationEnd = value; } }

		/// <summary><para>上次应用的动画时间（秒）；事件区间为 (AnimationLast, AnimationTime]，默认 -1 以包含首次应用的零时刻事件。</para>
		/// The time in seconds this animation was last applied. Some timelines use this for one-time triggers. Eg, when this
		/// animation is applied, event timelines will fire all events between the <code>AnimationLast</code> time (exclusive) and
		/// <code>AnimationTime</code> (inclusive). Defaults to -1 to ensure triggers on frame 0 happen the first time this animation
		/// is applied.</summary>
		public float AnimationLast {
			get { return animationLast; }
			set {
				animationLast = value;
				nextAnimationLast = value;
			}
		}

		/// <summary><para>根据 TrackTime 计算区间内的动画时间；TrackTime 为 0 时等于 AnimationStart。</para>
		/// Uses <see cref="TrackEntry.TrackTime"/> to compute the <code>AnimationTime</code>, which is between <see cref="TrackEntry.AnimationStart"/>
		/// and <see cref="TrackEntry.AnimationEnd"/>. When the <code>TrackTime</code> is 0, the <code>AnimationTime</code> is equal to the
		/// <code>AnimationStart</code> time.
		/// </summary>
		public float AnimationTime {
			get {
				if (loop) {
					float duration = animationEnd - animationStart;
					if (duration == 0) return animationStart;
					return (trackTime % duration) + animationStart;
				}
				return Math.Min(trackTime + animationStart, animationEnd);
			}
		}

		/// <summary><para>此轨道项的时间倍率，默认 1；MixTime 不受它影响，排队动作自动计算的延迟也可能需要相应调整。</para>
		/// <para>
		/// Multiplier for the delta time when this track entry is updated, causing time for this animation to pass slower or
		/// faster. Defaults to 1.</para>
		/// <para>
		/// <see cref="TrackEntry.MixTime"/> is not affected by track entry time scale, so <see cref="TrackEntry.MixDuration"/> may need to be adjusted to
		/// match the animation speed.</para>
		/// <para>
		/// When using <see cref="AnimationState.AddAnimation(int, Animation, boolean, float)"/> with a <code>Delay</code> &lt;= 0, note the
		/// {<see cref="TrackEntry.Delay"/> is set using the mix duration from the <see cref="AnimationStateData"/>, assuming time scale to be 1. If
		/// the time scale is not 1, the delay may need to be adjusted.</para>
		/// <para>
		/// See AnimationState <see cref="AnimationState.TimeScale"/> for affecting all animations.</para>
		/// </summary>
		public float TimeScale { get { return timeScale; } set { timeScale = value; } }

		/// <summary><para>此轨道的混合权重；通常轨道 0 建立基础姿势，高轨道用 alpha 控制叠加强度。</para>
		/// <para>
		/// Values &lt; 1 mix this animation with the skeleton's current pose (usually the pose resulting from lower tracks). Defaults
		/// to 1, which overwrites the skeleton's current pose with this animation.</para>
		/// <para>
		/// Typically track 0 is used to completely pose the skeleton, then alpha is used on higher tracks. It doesn't make sense to
		/// use alpha on track 0 if the skeleton pose is from the last frame render.</para>
		/// </summary>
		public float Alpha { get { return alpha; } set { alpha = value; } }

		/// <summary><para>淡出混合进度小于该阈值时仍应用事件时间轴；默认 0，淡出时不触发模型事件。</para>
		/// When the mix percentage (<see cref="TrackEntry.MixTime"/> / <see cref="TrackEntry.MixDuration"/>) is less than the
		/// <code>EventThreshold</code>, event timelines are applied while this animation is being mixed out. Defaults to 0, so event
		/// timelines are not applied while this animation is being mixed out.
		/// </summary>
		public float EventThreshold { get { return eventThreshold; } set { eventThreshold = value; } }

		/// <summary><para>淡出混合进度小于该阈值时仍应用附件时间轴；默认 0。</para>
		/// When the mix percentage (<see cref="TrackEntry.MixTime"/> / <see cref="TrackEntry.MixDuration"/>) is less than the
		/// <code>AttachmentThreshold</code>, attachment timelines are applied while this animation is being mixed out. Defaults to
		/// 0, so attachment timelines are not applied while this animation is being mixed out.
		///</summary>
		public float AttachmentThreshold { get { return attachmentThreshold; } set { attachmentThreshold = value; } }

		/// <summary><para>淡出混合进度小于该阈值时仍应用绘制顺序时间轴；默认 0。</para>
		/// When the mix percentage (<see cref="TrackEntry.MixTime"/> / <see cref="TrackEntry.MixDuration"/>) is less than the
		/// <code>DrawOrderThreshold</code>, draw order timelines are applied while this animation is being mixed out. Defaults to 0,
		/// so draw order timelines are not applied while this animation is being mixed out.
		/// </summary>
		public float DrawOrderThreshold { get { return drawOrderThreshold; } set { drawOrderThreshold = value; } }

		/// <summary><para>当前项之后排队播放的下一项，null 表示没有；通过 Next 构成链表。</para>
		/// The animation queued to start after this animation, or null. <code>Next</code> makes up a linked list. </summary>
		public TrackEntry Next { get { return next; } }

		/// <summary><para>至少完成一轮动画后返回 true。</para>
		/// Returns true if at least one loop has been completed.</summary>
		/// <seealso cref="TrackEntry.Complete"/>
		public bool IsComplete {
			get { return trackTime >= animationEnd - animationStart; }
		}

		/// <summary><para>从前一动作混合到此动作已经经过的秒数；完成时可能略超过 MixDuration。</para>
		/// Seconds from 0 to the <see cref="TrackEntry.MixDuration"/> when mixing from the previous animation to this animation. May be
		/// slightly more than <code>MixDuration</code> when the mix is complete.</summary>
		public float MixTime { get { return mixTime; } set { mixTime = value; } }

		/// <summary><para>从前一动作切换到此动作的混合时长（秒），默认查询 AnimationStateData；新项首次 Update 前可修改，排队 delay 的自动计算不会追随之后的修改。</para>
		/// <para>
		/// Seconds for mixing from the previous animation to this animation. Defaults to the value provided by AnimationStateData
		/// <see cref="AnimationStateData.GetMix(Animation, Animation)"/> based on the animation before this animation (if any).</para>
		/// <para>
		/// The <code>MixDuration</code> can be set manually rather than use the value from
		/// <see cref="AnimationStateData.GetMix(Animation, Animation)"/>. In that case, the <code>MixDuration</code> can be set for a new
		///  track entry only before <see cref="AnimationState.Update(float)"/> is first called.</para>
		///  <para>
		///  When using <seealso cref="AnimationState.AddAnimation(int, Animation, bool, float)"/> with a <code>Delay</code> &lt;= 0, note the
		///  <see cref="TrackEntry.Delay"/> is set using the mix duration from the <see cref=" AnimationStateData"/>, not a mix duration set
		///  afterward.</para>
		/// </summary>
		public float MixDuration { get { return mixDuration; } set { mixDuration = value; } }

		/// <summary><para>与低轨道混合的方式，默认替换，Add 为加法叠加；新项首次应用之前可设置。</para>
		/// <para>
		/// Controls how properties keyed in the animation are mixed with lower tracks. Defaults to <see cref="MixBlend.Replace"/>, which
		/// replaces the values from the lower tracks with the animation values. <see cref="MixBlend.Add"/> adds the animation values to
		/// the values from the lower tracks.</para>
		/// <para>
		///  The <code>MixBlend</code> can be set for a new track entry only before <see cref="AnimationState.Apply(Skeleton)"/> is first
		///  called.</para>
		/// </summary>
		public MixBlend MixBlend { get { return mixBlend; } set { mixBlend = value; } }

		/// <summary><para>正在淡出的前一轨道项，无混合时为 null；多项来源通过 MixingFrom 构成链表。</para>
		/// The track entry for the previous animation when mixing from the previous animation to this animation, or null if no
		/// mixing is currently occuring. When mixing from multiple animations, <code>MixingFrom</code> makes up a linked list.</summary>
		public TrackEntry MixingFrom { get { return mixingFrom; } }

		/// <summary><para>正在淡入的后一轨道项，无混合时为 null；通过 MixingTo 构成混合链。</para>
		/// The track entry for the next animation when mixing from this animation to the next animation, or null if no mixing is
		/// currently occuring. When mixing to multiple animations, <code>MixingTo</code> makes up a linked list.</summary>
		public TrackEntry MixingTo { get { return mixingTo; } }

		/// <summary><para>混合过程中保持前一动作全权重，可避免高轨道同属性混合时短暂下陷；新动作没有覆盖前一动作所有属性时可能产生跳变。</para>
		/// <para>
		/// If true, when mixing from the previous animation to this animation, the previous animation is applied as normal instead
		/// of being mixed out.</para>
		/// <para>
		/// When mixing between animations that key the same property, if a lower track also keys that property then the value will
		/// briefly dip toward the lower track value during the mix. This happens because the first animation mixes from 100% to 0%
		/// while the second animation mixes from 0% to 100%. Setting <code>HoldPrevious</code> to true applies the first animation
		/// at 100% during the mix so the lower track value is overwritten. Such dipping does not occur on the lowest track which
		/// keys the property, only when a higher track also keys the property.</para>
		/// <para>
		/// Snapping will occur if <code>HoldPrevious</code> is true and this animation does not key all the same properties as the
		/// previous animation.</para>
		/// </summary>
		public bool HoldPrevious { get { return holdPrevious; } set { holdPrevious = value; } }

		/// <summary><para>重置旋转方向缓存，使下次混合重新选择最短路径；通常首次应用选择方向后会持续保持，避免两角度变化时来回翻转。</para>
		/// <para>
		/// Resets the rotation directions for mixing this entry's rotate timelines. This can be useful to avoid bones rotating the
		/// long way around when using <see cref="alpha"/> and starting animations on other tracks.</para>
		/// <para>
		/// Mixing with <see cref="MixBlend.Replace"/> involves finding a rotation between two others, which has two possible solutions:
		/// the short way or the long way around. The two rotations likely change over time, so which direction is the short or long
		/// way also changes. If the short way was always chosen, bones would flip to the other side when that direction became the
		/// long way. TrackEntry chooses the short way the first time it is applied and remembers that direction.</para>
		/// </summary>
		public void ResetRotationDirections () {
			timelinesRotation.Clear();
		}

		/// <summary>返回便于识别当前对象的名称或文本。</summary>
		override public string ToString () {
			return animation == null ? "<none>" : animation.name;
		}
	}

	/// <summary>延迟派发动画事件并回收已结束轨道项的队列。</summary>
	class EventQueue {
		/// <summary>等待派发的事件及轨道项记录列表。</summary>
		private readonly List<EventQueueEntry> eventQueueEntries = new List<EventQueueEntry>();
		/// <summary>是否暂时禁止派发事件队列。</summary>
		internal bool drainDisabled;

		/// <summary>当前播放器或事件队列所属的动画状态控制器。</summary>
		private readonly AnimationState state;
		/// <summary>可复用轨道项的对象池。</summary>
		private readonly Pool<TrackEntry> trackEntryPool;
		/// <summary>动作集合变化时通知重新计算时间轴混合关系。</summary>
		internal event Action AnimationsChanged;

		/// <summary>创建延迟派发动画事件并回收已结束轨道项的队列，并初始化关联数据及缓冲区。</summary>
		internal EventQueue (AnimationState state, Action HandleAnimationsChanged, Pool<TrackEntry> trackEntryPool) {
			this.state = state;
			this.AnimationsChanged += HandleAnimationsChanged;
			this.trackEntryPool = trackEntryPool;
		}

		/// <summary>事件类型、关联轨道项及模型事件的队列记录。</summary>
		struct EventQueueEntry {
			/// <summary>事件类型。</summary>
			public EventType type;
			/// <summary>关联的轨道项。</summary>
			public TrackEntry entry;
			/// <summary>关联的模型自定义事件。</summary>
			public Event e;

			/// <summary>创建事件类型、关联轨道项及模型事件的队列记录，并初始化关联数据及缓冲区。</summary>
			public EventQueueEntry (EventType eventType, TrackEntry trackEntry, Event e = null) {
				this.type = eventType;
				this.entry = trackEntry;
				this.e = e;
			}
		}

		/// <summary>运行时动画生命周期和模型事件类型。</summary>
		enum EventType {
			/// <summary>动画开始被应用。</summary>
			Start, 
				/// <summary>当前动画被另一动作打断，可能继续参与淡出。</summary>
				Interrupt, 
				/// <summary>此动画不再应用到骨架。</summary>
				End, 
				/// <summary>此轨道项将被回收，之后不可继续持有引用。</summary>
				Dispose, 
				/// <summary>动画完成一轮播放；循环动画每轮都可能触发。</summary>
				Complete, 
				/// <summary>模型中的自定义事件关键帧。</summary>
				Event
		}

		/// <summary>将“动画开始被应用。”通知加入队列，稍后统一派发。</summary>
		internal void Start (TrackEntry entry) {
			eventQueueEntries.Add(new EventQueueEntry(EventType.Start, entry));
			if (AnimationsChanged != null) AnimationsChanged();
		}

		/// <summary>将“当前动画被另一动作打断，可能继续参与淡出。”通知加入队列，稍后统一派发。</summary>
		internal void Interrupt (TrackEntry entry) {
			eventQueueEntries.Add(new EventQueueEntry(EventType.Interrupt, entry));
		}

		/// <summary>将“此动画不再应用到骨架。”通知加入队列，稍后统一派发。</summary>
		internal void End (TrackEntry entry) {
			eventQueueEntries.Add(new EventQueueEntry(EventType.End, entry));
			if (AnimationsChanged != null) AnimationsChanged();
		}

		/// <summary>将“此轨道项将被回收，之后不可继续持有引用。”通知加入队列，稍后统一派发。</summary>
		internal void Dispose (TrackEntry entry) {
			eventQueueEntries.Add(new EventQueueEntry(EventType.Dispose, entry));
		}

		/// <summary>将“动画完成一轮播放；循环动画每轮都可能触发。”通知加入队列，稍后统一派发。</summary>
		internal void Complete (TrackEntry entry) {
			eventQueueEntries.Add(new EventQueueEntry(EventType.Complete, entry));
		}

		/// <summary>将“模型中的自定义事件关键帧。”通知加入队列，稍后统一派发。</summary>
		internal void Event (TrackEntry entry, Event e) {
			eventQueueEntries.Add(new EventQueueEntry(EventType.Event, entry, e));
		}

		/// <summary><para>派发全部排队事件并清空队列。</para>Raises all events in the queue and drains the queue.</summary>
		internal void Drain () {
			if (drainDisabled) return;
			drainDisabled = true;

			var entries = this.eventQueueEntries;
			AnimationState state = this.state;

			// 不缓存队列数量，允许回调继续入队，例如完成回调调用 SetAnimation。 原注： Don't cache entries.Count so callbacks can queue their own events (eg, call SetAnimation in AnimationState_Complete).
			for (int i = 0; i < entries.Count; i++) {
				var queueEntry = entries[i];
				TrackEntry trackEntry = queueEntry.entry;

				switch (queueEntry.type) {
				case EventType.Start:
					trackEntry.OnStart();
					state.OnStart(trackEntry);
					break;
				case EventType.Interrupt:
					trackEntry.OnInterrupt();
					state.OnInterrupt(trackEntry);
					break;
				case EventType.End:
					trackEntry.OnEnd();
					state.OnEnd(trackEntry);
					goto case EventType.Dispose; // 继续进入后续分支，采用 C# 写法。 原注： Fall through. (C#)
				case EventType.Dispose:
					trackEntry.OnDispose();
					state.OnDispose(trackEntry);
					trackEntryPool.Free(trackEntry); // 对象池复用处理。 原注： Pooling
					break;
				case EventType.Complete:
					trackEntry.OnComplete();
					state.OnComplete(trackEntry);
					break;
				case EventType.Event:
					trackEntry.OnEvent(queueEntry.e);
					state.OnEvent(trackEntry, queueEntry.e);
					break;
				}
			}
			eventQueueEntries.Clear();

			drainDisabled = false;
		}

		/// <summary>清空尚未派发的动画事件通知。</summary>
		internal void Clear () {
			eventQueueEntries.Clear();
		}
	}

	/// <summary>复用可重置对象的泛型对象池，减少重复分配。</summary>
	public class Pool<T> where T : class, new() {
		/// <summary>对象池最多保留的空闲对象数。</summary>
		public readonly int max;
		/// <summary>对象池内可复用的空闲对象栈。</summary>
		readonly Stack<T> freeObjects;

		/// <summary>对象池中当前可直接复用的空闲对象数量。</summary>
		public int Count { get { return freeObjects.Count; } }
		/// <summary>对象池空闲对象数量的历史峰值。</summary>
		public int Peak { get; private set; }

		/// <summary>创建复用可重置对象的泛型对象池，减少重复分配，并初始化关联数据及缓冲区。</summary>
		public Pool (int initialCapacity = 16, int max = int.MaxValue) {
			freeObjects = new Stack<T>(initialCapacity);
			this.max = max;
		}

		/// <summary>从对象池取出可用对象，池为空时创建新对象。</summary>
		public T Obtain () {
			return freeObjects.Count == 0 ? new T() : freeObjects.Pop();
		}

		/// <summary>将对象归还对象池并重置状态；超过容量时不保留。</summary>
		public void Free (T obj) {
			if (obj == null) throw new ArgumentNullException("obj", "obj cannot be null");
			if (freeObjects.Count < max) {
				freeObjects.Push(obj);
				Peak = Math.Max(Peak, freeObjects.Count);
			}
			Reset(obj);
		}

// 保留的参考代码：批量释放对象到对象池。 原注：		protected void FreeAll (List<T> objects) {
// 保留的参考代码：检查对象列表非空。 原注：			if (objects == null) throw new ArgumentNullException("objects", "objects cannot be null.");
// 保留的参考代码：取得空闲对象栈。 原注：			var freeObjects = this.freeObjects;
// 保留的参考代码：取得池的最大容量。 原注：			int max = this.max;
// 保留的参考代码：遍历待回收对象。 原注：			for (int i = 0; i < objects.Count; i++) {
// 保留的参考代码：取得当前对象。 原注：				T obj = objects[i];
// 保留的参考代码：跳过空对象。 原注：				if (obj == null) continue;
// 保留的参考代码：容量未满时压入空闲栈。 原注：				if (freeObjects.Count < max) freeObjects.Push(obj);
// 保留的参考代码：重置对象状态。 原注：				Reset(obj);
//			}
// 保留的参考代码：更新空闲对象数量峰值。 原注：			Peak = Math.Max(Peak, freeObjects.Count);
//		}

		/// <summary>丢弃池中全部空闲对象引用，已被借出的对象不受影响。</summary>
		public void Clear () {
			freeObjects.Clear();
		}

		/// <summary>重置对象状态，便于后续复用。</summary>
		protected void Reset (T obj) {
			var poolable = obj as IPoolable;
			if (poolable != null) poolable.Reset();
		}

		/// <summary>支持在归还对象池时重置状态的接口。</summary>
		public interface IPoolable {
			/// <summary>重置对象状态，便于后续复用。</summary>
			void Reset ();
		}
	}

}
