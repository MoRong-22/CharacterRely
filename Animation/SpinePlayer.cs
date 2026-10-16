using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Spine;

namespace CharacterRely.Animation
{
    /// <summary>动画事件类型：开始、被新动作打断、结束应用、完成一次播放、触发模型自定义事件。</summary>
    public enum SpineAnimationEventType { 
    	/// <summary>动画开始被应用。</summary>
    	Start, 
    	/// <summary>当前动画被另一动作打断，可能继续参与淡出。</summary>
    	Interrupt, 
    	/// <summary>此动画不再应用到骨架。</summary>
    	End, 
    	/// <summary>动画完成一轮播放；循环动画每轮都可能触发。</summary>
    	Complete, 
    	/// <summary>模型中的自定义事件关键帧。</summary>
    	Event }

    /// <summary>事件数据副本，不引用运行时会回收的 TrackEntry。</summary>
    /// <param name="Type">事件类型；Complete 对循环动画会在每轮结束时触发。</param>
    /// <param name="AnimationName">触发事件的动画名称。</param>
    /// <param name="Name">模型中定义的事件名；开始、结束等生命周期事件为 null。</param>
    /// <param name="Time">动画内的事件时间，单位为秒，不是现实时间。</param>
    /// <param name="IntValue">模型事件携带的整数参数。</param>
    /// <param name="FloatValue">模型事件携带的浮点参数。</param>
    /// <param name="StringValue">模型事件携带的字符串参数。</param>
    /// <param name="TrackIndex">触发事件的轨道编号；0 是基础动作，1 可用于上半身动作。</param>
    public readonly record struct SpineAnimationEvent(SpineAnimationEventType Type, string AnimationName,
        string Name, float Time, int IntValue = 0, float FloatValue = 0f, string StringValue = null, int TrackIndex = 0);

    /// <summary>独立 Spine 3.8.75 播放器。时间单位秒；Draw 在 SpriteBatch.End 后调用。</summary>
    public sealed class SpinePlayer : IDisposable
    {
        /// <summary>本播放器持有的共享模型资源。</summary>
        private readonly SpineAsset asset;
        /// <summary>当前播放器或事件队列所属的动画状态控制器。</summary>
        private readonly AnimationState state;
        /// <summary>Spine 用于轨道淡入淡出的共享空动画，通过运行时公开接口取得。</summary>
        private readonly Spine.Animation emptyAnimation;
        // 保存每根骨骼的旋转偏移，内部单位为 Spine 使用的角度；公开接口仍传弧度。
        /// <summary>各骨骼的持久旋转偏移，内部单位为度。</summary>
        private readonly float[] rotationOffsets;
        /// <summary>按插槽索引保存的持久显隐开关。</summary>
        private readonly bool[] slotVisibility;
        // 暂存事件而非动画；姿势更新完成后再通知调用方，允许回调安全切换动作。
        /// <summary>姿势计算后等待通知调用方的事件副本队列。</summary>
        private readonly Queue<SpineAnimationEvent> pendingEvents = new();
        /// <summary>当前播放器的可复用绘制几何缓存。</summary>
        private readonly SpineGeometry geometry = new();
        /// <summary>paused：是否暂停时间推进；disposed：此对象是否已释放；dispatchingEvents：是否正在派发事件，用于阻止递归派发。</summary>
        private bool paused, disposed, dispatchingEvents;
        /// <summary>供内部姿势适配器在解除绑定时判断播放器是否已释放。</summary>
        internal bool IsDisposed => disposed;
        /// <summary>时间推进倍率，1 为原速。</summary>
        private float timeScale = 1f;
        /// <summary>本实例的骨架，可访问插槽、皮肤、约束；骨架内部使用 Spine 的 Y 向上坐标。</summary>
        public Skeleton Skeleton { get; }
        /// <summary>模型中可播放的全部动画名称。</summary>
        public IReadOnlyList<string> AnimationNames => asset.AnimationNames;
        /// <summary>模型中的骨骼名称，用于旋转控制和挂点查询。</summary>
        public IReadOnlyList<string> BoneNames { get; }
        /// <summary>模型中的插槽名称，用于控制部件显隐和筛选绘制。</summary>
        public IReadOnlyList<string> SlotNames { get; }
        /// <summary>轨道 0 的当前动画名称；未播放时为空字符串。</summary>
        public string CurrentAnimationName => GetCurrentAnimationName(0);
        /// <summary>轨道 0 当前动画一轮的时长，单位为秒；未播放时为 0。</summary>
        public float Duration => GetDuration(0);
        /// <summary>轨道 0 的非循环动画是否已播放完毕。</summary>
        public bool IsComplete => IsTrackComplete(0);
        /// <summary>轨道 0 是否已选择动画且未暂停、未播完；不额外判断 TimeScale 是否为 0。</summary>
        public bool IsPlaying => GetAnimationEntry(0) != null && !paused && !IsComplete;
        /// <summary>轨道 0 当前轮进度，范围 0～1；其他轨道使用 GetProgress。</summary>
        public float Progress => GetProgress(0);

        /// <summary>取得指定轨道的动画项；淡出用的空动画视为没有动作。</summary>
        private TrackEntry GetAnimationEntry(int trackIndex)
        {
            CheckAlive();
            ArgumentOutOfRangeException.ThrowIfNegative(trackIndex);
            var entry = state.GetCurrent(trackIndex);
            return entry?.Animation == emptyAnimation ? null : entry;
        }

        /// <summary>读取指定轨道的动画名称；空轨道或正在淡出时返回空字符串。</summary>
        public string GetCurrentAnimationName(int trackIndex) => GetAnimationEntry(trackIndex)?.Animation.Name ?? string.Empty;

        /// <summary>读取指定轨道当前动作一轮的时长，单位为秒；无动作时为 0。</summary>
        public float GetDuration(int trackIndex)
        {
            var entry = GetAnimationEntry(trackIndex);
            return entry == null ? 0f : entry.AnimationEnd - entry.AnimationStart;
        }

        /// <summary>指定轨道的非循环动作是否播放完毕；循环、空轨道及淡出返回 false。</summary>
        public bool IsTrackComplete(int trackIndex) => GetAnimationEntry(trackIndex) is { Loop: false, IsComplete: true };

        /// <summary>读取指定轨道的当前轮进度，范围 0～1；非循环播完为 1，无动作为 0。</summary>
        public float GetProgress(int trackIndex)
        {
            var entry = GetAnimationEntry(trackIndex);
            if (entry == null) return 0f;
            if (!entry.Loop && entry.IsComplete) return 1f;
            float duration = entry.AnimationEnd - entry.AnimationStart;
            return duration <= 0f ? 0f : MathHelper.Clamp((entry.AnimationTime - entry.AnimationStart) / duration, 0f, 1f);
        }
        /// <summary>播放速度倍率：1 为正常速度，0 不推进时间；不支持负数倒放。</summary>
        public float TimeScale
        {
            get => timeScale;
            set { ValidateTime(value, nameof(value)); timeScale = value; }
        }
        /// <summary>是否水平镜像整个骨架及手部偏移，常用于角色左右朝向切换。</summary>
        public bool FlipX
        {
            get => Skeleton.ScaleX < 0f;
            set { CheckAlive(); Skeleton.ScaleX = value ? -1f : 1f; RefreshWorldPose(); }
        }
        /// <summary>是否垂直镜像整个骨架。</summary>
        public bool FlipY
        {
            get => Skeleton.ScaleY < 0f;
            set { CheckAlive(); Skeleton.ScaleY = value ? -1f : 1f; RefreshWorldPose(); }
        }
        /// <summary>接收生命周期和模型自定义事件；在本次播放或更新操作的姿势计算完成后通知。</summary>
        public event Action<SpineAnimationEvent> AnimationEvent;

        /// <summary>动画及约束算完后的程序姿势层；只改世界矩阵，下一次重算自然恢复动画。</summary>
        internal event Action WorldPoseApplied;

        /// <summary>基于共享模型创建独立骨架与动画状态，选择初始皮肤并登记事件回调。</summary>
        internal SpinePlayer(SpineAsset asset, string skinName)
        {
            this.asset = asset;
            Skeleton = new Skeleton(asset.Data);
            if (skinName != null) { Skeleton.SetSkin(skinName); Skeleton.SetSlotsToSetupPose(); }
            state = new AnimationState(new AnimationStateData(asset.Data));
            emptyAnimation = state.SetEmptyAnimation(0, 0f).Animation;
            state.ClearTracks();
            BoneNames = Array.AsReadOnly(Skeleton.Bones.Select(b => b.Data.Name).ToArray());
            SlotNames = Array.AsReadOnly(Skeleton.Slots.Select(s => s.Data.Name).ToArray());
            rotationOffsets = new float[BoneNames.Count];
            slotVisibility = new bool[SlotNames.Count];
            Array.Fill(slotVisibility, true);
            state.Start += entry => QueueEvent(SpineAnimationEventType.Start, entry);
            state.Interrupt += entry => QueueEvent(SpineAnimationEventType.Interrupt, entry);
            state.End += entry => QueueEvent(SpineAnimationEventType.End, entry);
            state.Complete += entry => QueueEvent(SpineAnimationEventType.Complete, entry);
            state.Event += (entry, e) => pendingEvents.Enqueue(new(SpineAnimationEventType.Event,
                entry.Animation.Name, e.Data.Name, e.Time, e.Int, e.Float, e.String, entry.TrackIndex));
            Skeleton.UpdateWorldTransform();
        }

        /// <summary>复制轨道事件参数并加入待通知队列，避免持有会被回收的轨道对象。</summary>
        private void QueueEvent(SpineAnimationEventType type, TrackEntry entry)
        {
            // 空动画只负责混合，不向角色发送伪造的动作开始/完成事件。
            if (entry.Animation == emptyAnimation) return;
            pendingEvents.Enqueue(new(type, entry.Animation.Name, null, entry.AnimationTime, TrackIndex: entry.TrackIndex));
        }

        // 按触发顺序派发事件；回调期间产生的新事件继续入队，避免递归派发。
        /// <summary>依次通知等待中的事件，回调产生的新事件继续入队，避免递归派发。</summary>
        private void DispatchEvents()
        {
            if (dispatchingEvents) return;
            dispatchingEvents = true;
            try
            {
                while (!disposed && pendingEvents.Count > 0)
                {
                    var value = pendingEvents.Dequeue();
                    AnimationEvent?.Invoke(value);
                }
            }
            finally { dispatchingEvents = false; }
        }

        /// <summary>默认循环，同名同模式保持进度；restart: true 从头播放。</summary>
        /// <param name="animationName">骨架中实际存在的动画名，可从 AnimationNames 获取。</param>
        /// <param name="loop">是否循环播放；false 时播完保持最终姿势。</param>
        /// <param name="fadeInSeconds">从前一动画混合到本动画的时长，单位为秒；0 表示直接切换。</param>
        /// <param name="restart">同名同循环模式下是否强制重播；false 时保持已有进度。</param>
        /// <param name="trackIndex">轨道编号，默认 0。较高轨道覆盖其动画有关键帧的属性，不会自动限制为上半身。</param>
        /// <returns>调用完成后的当前进度；新动作通常为 0，重复选择同一动作返回已有进度。</returns>
        public float Play(string animationName, bool loop = true, float fadeInSeconds = 0f, bool restart = false, int trackIndex = 0)
        {
            CheckAlive();
            ArgumentOutOfRangeException.ThrowIfNegative(trackIndex);
            ArgumentException.ThrowIfNullOrWhiteSpace(animationName);
            ValidateTime(fadeInSeconds, nameof(fadeInSeconds));
            var animation = asset.Data.FindAnimation(animationName)
                ?? throw new ArgumentException($"动作 '{animationName}' 不存在。可用：{string.Join(", ", AnimationNames)}", nameof(animationName));
            var current = GetAnimationEntry(trackIndex);
            if (!restart && current?.Animation == animation && current.Loop == loop) return GetProgress(trackIndex);
            if (trackIndex > 0 && state.GetCurrent(trackIndex) == null && fadeInSeconds > 0f)
            {
                // 首次启用覆盖轨道也从下层姿势淡入，避免上半身突然跳到攻击姿势。
                state.SetEmptyAnimation(trackIndex, 0f);
                ApplyPose();
            }
            var entry = state.SetAnimation(trackIndex, animation, loop);
            entry.MixDuration = fadeInSeconds;
            paused = false;
            ApplyPose();
            DispatchEvents();
            return GetProgress(trackIndex);
        }

        /// <summary>
        /// 淡出指定轨道，使它控制的属性回到低轨道或初始姿势；不影响其他轨道的播放时间。
        /// 重复淡出同一轨道不会重新计时。不要用 ClearTrack 替代，否则可能残留最后一帧姿势。
        /// </summary>
        /// <param name="trackIndex">要停止覆盖的轨道编号。</param>
        /// <param name="fadeOutSeconds">淡出时长，单位为秒；0 也由 Spine 在一次更新中完成退出。</param>
        public void FadeOutTrack(int trackIndex, float fadeOutSeconds = 0.1f)
        {
            ValidateTime(fadeOutSeconds, nameof(fadeOutSeconds));
            if (GetAnimationEntry(trackIndex) == null) return;
            state.SetEmptyAnimation(trackIndex, fadeOutSeconds);
            ApplyPose();
            DispatchEvents();
        }

        /// <summary>暂停时间推进，保留当前动画与姿势。</summary>
        public void Pause() { CheckAlive(); paused = true; }
        /// <summary>从当前进度继续播放，不从头重播。</summary>
        public void Resume() { CheckAlive(); paused = false; }
        /// <summary>按游戏帧时间更新播放器；与秒数重载二选一调用，避免重复推进。</summary>
        public void Update(GameTime time) => Update((float)time.ElapsedGameTime.TotalSeconds);
        /// <summary>推进动画、计算骨骼姿势并派发事件；CharacterLib 已在每个游戏 tick 自动调用。</summary>
        /// <param name="deltaSeconds">本次经过的秒数，例如 60 次每秒的更新传入 1f / 60f。</param>
        public void Update(float deltaSeconds)
        {
            CheckAlive();
            ValidateTime(deltaSeconds, nameof(deltaSeconds));
            float step = paused ? 0f : deltaSeconds * timeScale;
            ValidateTime(step, nameof(deltaSeconds));
            state.Update(step);
            Skeleton.Update(step);
            ApplyPose(syncEventTimes: false);
            DispatchEvents();
        }

        /// <summary>移除旧旋转偏移，应用全部轨道，再叠加偏移并更新世界矩阵。</summary>
        /// <param name="syncEventTimes">控制操作在同一 tick 重算姿势时，同步已应用的事件时间，避免重复触发完成和模型事件。</param>
        private void ApplyPose(bool syncEventTimes = true)
        {
            // Play/淡出可能在同一 tick 多次应用骨架；同步事件游标，不推进任何轨道的时间。
            if (syncEventTimes) state.Update(0f);
            // 先去掉上一帧的偏移，再计算动画并加回偏移，防止没有旋转关键帧的骨骼逐帧累加。
            for (int i = 0; i < rotationOffsets.Length; i++) Skeleton.Bones.Items[i].Rotation -= rotationOffsets[i];
            state.Apply(Skeleton);
            for (int i = 0; i < rotationOffsets.Length; i++) Skeleton.Bones.Items[i].Rotation += rotationOffsets[i];
            RefreshWorldPose();
        }

        /// <summary>重新计算世界姿势并应用程序手臂；不推进动画时间，不改变骨骼局部动画数据。</summary>
        internal void RefreshWorldPose()
        {
            CheckAlive();
            Skeleton.UpdateWorldTransform();
            WorldPoseApplied?.Invoke();
        }

        /// <summary>跳到进度并暂停。不会补发被跳过区间的关键帧事件。</summary>
        /// <param name="progress">目标进度，超出 0～1 的值会截取到此范围；调用前需先 Play。</param>
        /// <param name="trackIndex">要跳转的轨道，默认 0；暂停仍作用于整个播放器。</param>
        public void Seek(float progress, int trackIndex = 0)
        {
            CheckAlive();
            if (!float.IsFinite(progress)) throw new ArgumentOutOfRangeException(nameof(progress));
            var current = GetAnimationEntry(trackIndex) ?? throw new InvalidOperationException("请先在此轨道调用 Play 选择动作。");
            current.TrackTime = MathHelper.Clamp(progress, 0f, 1f) * GetDuration(trackIndex);
            current.AnimationLast = current.AnimationTime;
            paused = true;
            ApplyPose(syncEventTimes: false);
            DispatchEvents();
        }

        /// <summary>
        /// 相对当前动画的局部旋转偏移，弧度；未镜像时正值在屏幕上顺时针。
        /// 0 恢复原动作。整条手臂用上臂骨骼名，子骨骼自动跟随。IK 仍会约束最终姿势。
        /// </summary>
        /// <param name="boneName">要控制的骨骼名，不是插槽名。</param>
        /// <param name="radians">相对动画姿势的局部旋转偏移，单位为弧度；重复设置不会累加。</param>
        public void SetBoneRotationOffset(string boneName, float radians)
        {
            CheckAlive();
            if (!float.IsFinite(radians)) throw new ArgumentOutOfRangeException(nameof(radians));
            var bone = Skeleton.FindBone(boneName) ?? throw new ArgumentException($"骨骼 '{boneName}' 不存在。", nameof(boneName));
            float degrees = -MathHelper.ToDegrees(MathHelper.WrapAngle(radians));
            int index = bone.Data.Index;
            bone.Rotation += degrees - rotationOffsets[index];
            rotationOffsets[index] = degrees;
            RefreshWorldPose();
        }

        /// <summary>设置插槽显隐，不受动画换图影响，不停止骨骼更新。</summary>
        /// <param name="slotName">部件所在插槽的名称，可从 SlotNames 获取。</param>
        /// <param name="visible">true 允许绘制，false 持续隐藏，直到再次设为 true。</param>
        public void SetSlotVisible(string slotName, bool visible)
        {
            CheckAlive();
            var slot = Skeleton.FindSlot(slotName) ?? throw new ArgumentException($"插槽 '{slotName}' 不存在。", nameof(slotName));
            slotVisibility[slot.Data.Index] = visible;
        }
        /// <summary>读取插槽的显隐开关；不代表它当前一定有附件，也不包含本次绘制筛选结果。</summary>
        public bool IsSlotVisible(string slotName)
        {
            CheckAlive();
            var slot = Skeleton.FindSlot(slotName) ?? throw new ArgumentException($"插槽 '{slotName}' 不存在。", nameof(slotName));
            return slotVisibility[slot.Data.Index];
        }

        /// <summary>返回 Y 向下的骨架局部矩阵，可与 Draw 的缩放、旋转、位置变换组合得到武器挂点。</summary>
        /// <param name="boneName">挂点所在的骨骼名称。</param>
        /// <param name="transform">包含骨架姿势与镜像的矩阵，不包含 Draw 的额外位置、缩放及视图变换。</param>
        /// <returns>找到骨骼返回 true；未找到返回 false，并输出单位矩阵。</returns>
        public bool TryGetBoneTransform(string boneName, out Matrix transform)
        {
            CheckAlive();
            var bone = Skeleton.FindBone(boneName);
            if (bone == null) { transform = Matrix.Identity; return false; }
            transform = new Matrix(bone.A, -bone.C, 0f, 0f, -bone.B, bone.D, 0f, 0f,
                0f, 0f, 1f, 0f, bone.WorldX, -bone.WorldY, 0f, 1f);
            return true;
        }

        /// <summary>
        /// 屏幕坐标绘制；调用前 End SpriteBatch，调用后自行恢复 Begin。
        /// slotFilter 可筛选本次绘制的部件，不改变持久显隐状态。Draw 不推进动画。
        /// </summary>
        /// <param name="graphicsDevice">游戏的图形设备；仅在客户端绘制阶段使用。</param>
        /// <param name="position">骨架原点的屏幕位置，例如 Player.Center - Main.screenPosition。</param>
        /// <param name="scale">绘制缩放倍率，必须大于 0；与资源导入缩放相乘。</param>
        /// <param name="color">整体颜色与透明度；不传时为白色，保持模型自身颜色。</param>
        /// <param name="rotation">整体旋转，单位为弧度；屏幕上正值为顺时针。</param>
        /// <param name="viewMatrix">额外视图变换；不传为单位矩阵，游戏内可传摄像机变换矩阵。</param>
        /// <param name="slotFilter">本次部件筛选；返回 true 才绘制，null 表示不额外筛选。</param>
        public void Draw(GraphicsDevice graphicsDevice, Vector2 position, float scale = 1f,
            Color? color = null, float rotation = 0f, Matrix? viewMatrix = null, Func<Slot, bool> slotFilter = null)
        {
            CheckAlive();
            ArgumentNullException.ThrowIfNull(graphicsDevice);
            if (!float.IsFinite(scale) || scale <= 0f) throw new ArgumentOutOfRangeException(nameof(scale));
            if (!float.IsFinite(rotation)) throw new ArgumentOutOfRangeException(nameof(rotation));
            // 弹幕 AI 可能在玩家更新后设置原版手臂；绘制前读取最新姿势，不推进任何轨道。
            if (WorldPoseApplied != null) RefreshWorldPose();
            var world = Matrix.CreateScale(scale, -scale, 1f) * Matrix.CreateRotationZ(rotation)
                * Matrix.CreateTranslation(position.X, position.Y, 0f);
            CollectGeometry(slotFilter);
            SpineRenderer.Draw(graphicsDevice, geometry, world * (viewMatrix ?? Matrix.Identity), color ?? Color.White);
        }

        /// <summary>按当前骨架姿势和可选插槽筛选收集可绘制几何，不推进播放时间。</summary>
        internal SpineGeometry CollectGeometry(Func<Slot, bool> filter = null)
        {
            CheckAlive();
            geometry.Collect(Skeleton, slotVisibility, filter);
            return geometry;
        }
        /// <summary>要求时间值有限且非负，不合法时抛出参数异常。</summary>
        private static void ValidateTime(float value, string name)
        {
            if (!float.IsFinite(value) || value < 0f) throw new ArgumentOutOfRangeException(name);
        }
        /// <summary>若对象已经释放则抛出 ObjectDisposedException，阻止继续操作。</summary>
        private void CheckAlive() => ObjectDisposedException.ThrowIf(disposed, this);
        /// <summary>释放本实例的事件和几何缓存，并解除资源登记；不释放其他播放器共用的模型。</summary>
        public void Dispose()
        {
            if (disposed) return;
            disposed = true;
            AnimationEvent = null;
            WorldPoseApplied = null;
            state.ClearTracks();
            pendingEvents.Clear();
            geometry.Clear();
            asset.Release(this);
        }
    }
}

namespace CharacterRely.Animation
{
    /// <summary>角色基类统一推进不同枚举类型的动画状态机所用的内部接口。</summary>
    internal interface ISpineAnimationStateMachine
    {
        /// <summary>此状态机独占控制的动画轨道。</summary>
        int TrackIndex { get; }
        /// <summary>推进状态计时并检查播放完成后的转换。</summary>
        void Update(float deltaSeconds);
    }

    /// <summary>
    /// 一个枚举状态机控制一条 Spine 轨道；移动与攻击分别创建实例，共享一个播放器。
    /// 状态名称及转换条件由角色定义，本类负责保持进度和播放完毕后的转换。
    /// </summary>
    /// <typeparam name="TState">角色自行定义的状态枚举，例如移动状态或动作状态。</typeparam>
    public sealed class SpineAnimationStateMachine<TState> : ISpineAnimationStateMachine where TState : struct, Enum
    {
        /// <summary>共享的玩家动画实例。</summary>
        private readonly SpinePlayer animator;
        /// <summary>状态对应的动画、过渡和完成后目标；null 动画表示退出此轨道的覆盖。</summary>
        private readonly Dictionary<TState, StateDefinition> states = new();

        /// <summary>一个状态的播放配置；非循环动作可指定播放完毕后的下一状态。</summary>
        private readonly record struct StateDefinition(string AnimationName, bool Loop, float FadeSeconds, TState? NextState);

        /// <summary>此状态机独占控制的轨道编号。不要同时直接用 Play 操作这条轨道。</summary>
        public int TrackIndex { get; }
        /// <summary>当前逻辑状态；首次 ChangeState 前为 null。</summary>
        public TState? CurrentState { get; private set; }
        /// <summary>进入当前状态后的游戏时间，单位为秒；与其他状态机独立计时。</summary>
        public float StateTime { get; private set; }
        /// <summary>此状态机的动画播放进度，范围 0～1；空状态为 0。</summary>
        public float Progress => animator.GetProgress(TrackIndex);
        /// <summary>此轨道的非循环动画是否已经播放完毕。</summary>
        public bool IsComplete => animator.IsTrackComplete(TrackIndex);

        /// <summary>将一个独立状态机绑定到播放器的指定轨道。</summary>
        /// <param name="animator">每位玩家独立的 Spine 播放器。</param>
        /// <param name="trackIndex">轨道编号，例如移动用 0、上半身动作用 1。</param>
        public SpineAnimationStateMachine(SpinePlayer animator, int trackIndex)
        {
            ArgumentNullException.ThrowIfNull(animator);
            ArgumentOutOfRangeException.ThrowIfNegative(trackIndex);
            this.animator = animator;
            TrackIndex = trackIndex;
        }

        /// <summary>检查状态是否已注册，可在缺少某个模型动画时禁用对应入口。</summary>
        public bool HasState(TState state) => states.ContainsKey(state);

        /// <summary>登记状态与模型动画的对应关系；只需在创建状态机时登记一次。</summary>
        /// <param name="state">角色自己的逻辑状态。</param>
        /// <param name="animationName">模型中实际存在的动画名。上半身动作应只给上半身设置关键帧。</param>
        /// <param name="loop">是否循环；移动通常为 true，攻击通常为 false。</param>
        /// <param name="fadeInSeconds">进入此状态的动画过渡时长，单位为秒。</param>
        /// <param name="nextState">非循环播放完成后自动进入的状态，例如 Attack 完成后回到 None。</param>
        public void RegisterState(TState state, string animationName, bool loop = true,
            float fadeInSeconds = 0.1f, TState? nextState = null)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(animationName);
            ValidateTime(fadeInSeconds, nameof(fadeInSeconds));
            if (!animator.AnimationNames.Contains(animationName))
                throw new ArgumentException($"模型中不存在动画 '{animationName}'。", nameof(animationName));
            if (loop && nextState.HasValue)
                throw new ArgumentException("循环状态不会播放完毕，不能指定完成后的下一状态。", nameof(nextState));
            states.Add(state, new(animationName, loop, fadeInSeconds, nextState));
        }

        /// <summary>登记无动作状态；进入后淡出本轨道，让基础轨道重新控制相关部件。</summary>
        /// <param name="state">无动作的逻辑状态，例如 None。</param>
        /// <param name="fadeOutSeconds">退出覆盖动作的时长，单位为秒。</param>
        public void RegisterEmptyState(TState state, float fadeOutSeconds = 0.1f)
        {
            ValidateTime(fadeOutSeconds, nameof(fadeOutSeconds));
            states.Add(state, new(null, false, fadeOutSeconds, null));
        }

        /// <summary>切换状态；每帧重复选择同一状态保持进度和计时，不会重新播放。</summary>
        /// <param name="state">已经注册的目标状态。</param>
        /// <param name="restart">为 true 时允许重新进入当前状态，例如主动重播攻击。</param>
        /// <returns>发生转换或重播时为 true；已经处于该状态且不重播时为 false。</returns>
        public bool ChangeState(TState state, bool restart = false)
        {
            if (!states.TryGetValue(state, out var definition))
                throw new InvalidOperationException($"轨道 {TrackIndex} 尚未注册状态 '{state}'。");
            if (definition.NextState.HasValue && !states.ContainsKey(definition.NextState.Value))
                throw new InvalidOperationException($"请先注册完成后的目标状态 '{definition.NextState.Value}'。");
            if (!restart && CurrentState.HasValue && EqualityComparer<TState>.Default.Equals(CurrentState.Value, state)) return false;

            CurrentState = state;
            StateTime = 0f;
            if (definition.AnimationName == null)
                animator.FadeOutTrack(TrackIndex, definition.FadeSeconds);
            else
                animator.Play(definition.AnimationName, definition.Loop, definition.FadeSeconds, restart: true, trackIndex: TrackIndex);
            return true;
        }

        /// <summary>
        /// 在播放器 Update 后调用，推进此状态的计时并处理完成后的转换。
        /// 通过 CharacterLib.GetAnimationStateMachine 创建的状态机由基类自动调用，不要重复推进。
        /// </summary>
        public void Update(float deltaSeconds)
        {
            ValidateTime(deltaSeconds, nameof(deltaSeconds));
            if (!CurrentState.HasValue) return;
            StateTime += deltaSeconds;
            var definition = states[CurrentState.Value];
            if (definition.NextState.HasValue && IsComplete)
                ChangeState(definition.NextState.Value);
        }

        /// <summary>要求时间值有限且非负，避免无效的混合时长和状态计时。</summary>
        private static void ValidateTime(float value, string name)
        {
            if (!float.IsFinite(value) || value < 0f) throw new ArgumentOutOfRangeException(name);
        }
    }
}
