using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Spine;
using Terraria;
using Terraria.ID;

namespace CharacterRely.Animation
{
    /// <summary>原版物品动作分类，供角色判断当前动作；不需要同名 Spine 动画。</summary>
    public enum VanillaItemAction
    {
        /// <summary>无普通物品动作；弹幕仍可通过复合手臂提供姿势。</summary>
        None,
        /// <summary>挥砍、挖掘、挥杆。</summary>
        Swing,
        /// <summary>短剑、刺剑戳刺。</summary>
        Thrust,
        /// <summary>枪、弓、法杖等瞄准使用。</summary>
        Shoot,
        /// <summary>进食、喝药。</summary>
        Use,
        /// <summary>持有、举起、提灯及割草。</summary>
        Hold,
        /// <summary>持有或弹奏吉他。</summary>
        Guitar
    }

    /// <summary>通用手臂姿势；Reach 为肩膀到手的屏幕方向，长度 1 表示完整臂长。</summary>
    /// <param name="Active">是否接管手臂；false 时保留角色动画。</param>
    /// <param name="Reach">Y 向下的方向和伸展比例，已含玩家朝向，不要再乘 direction。</param>
    public readonly record struct VanillaArmPose(bool Active, Vector2 Reach);

    /// <summary>原版动作快照，提供分类、实际攻速进度及前后手姿势。</summary>
    /// <param name="Action">动作分类，不强制选择手工动画。</param>
    /// <param name="ItemType">当前物品类型。</param>
    /// <param name="UseStyle">使用姿势编号。</param>
    /// <param name="HoldStyle">持有姿势编号。</param>
    /// <param name="AnimationRemaining">使用动画剩余帧数。</param>
    /// <param name="AnimationDuration">使用动画总帧数，包含攻速调整。</param>
    /// <param name="BodyFrameIndex">原版身体姿势帧编号。</param>
    /// <param name="FrontArm">前手姿势。</param>
    /// <param name="BackArm">后手姿势。</param>
    public readonly record struct VanillaItemActionFrame(VanillaItemAction Action, int ItemType,
        int UseStyle, int HoldStyle, int AnimationRemaining, int AnimationDuration, int BodyFrameIndex,
        VanillaArmPose FrontArm, VanillaArmPose BackArm)
    {
        /// <summary>是否实际使用物品；仅按鼠标但使用失败不会生效。</summary>
        public bool IsUsingItem => ItemType > 0 && AnimationRemaining > 0;
        /// <summary>本轮使用进度 0～1；无有效总帧数时为 0。</summary>
        public float Progress => AnimationDuration <= 0 ? 0f
            : MathHelper.Clamp(1f - AnimationRemaining / (float)AnimationDuration, 0f, 1f);

        /// <summary>读取最终姿势，不修改物品使用、伤害、弹幕或碰撞逻辑。</summary>
        public static VanillaItemActionFrame Capture(Player player)
        {
            ArgumentNullException.ThrowIfNull(player);
            Item item = player.HeldItem;
            bool available = player.active && !player.dead && !player.frozen && !player.pulley;
            bool usingItem = available && !item.IsAir && player.itemAnimation > 0;
            bool holding = available && !item.IsAir && item.holdStyle != ItemHoldStyleID.None
                && player.CanVisuallyHoldItem(item)
                && !(player.wet && item.noWet && !ItemID.Sets.WaterTorches[item.type]);
            VanillaItemAction action = GetAction(usingItem, holding, item.useStyle, item.holdStyle);
            int frameIndex = player.bodyFrame.Height > 0 ? player.bodyFrame.Y / player.bodyFrame.Height : 0;
            bool framePose = action != VanillaItemAction.None;
            return new(action, item.type, item.useStyle, item.holdStyle,
                usingItem ? player.itemAnimation : 0, player.itemAnimationMax, frameIndex,
                ReadArm(player, true, available, framePose, frameIndex),
                ReadArm(player, false, available, framePose, frameIndex));
        }

        /// <summary>将使用与持有姿势归类；隐藏使用和未知编号不强行套用普通动作。</summary>
        public static VanillaItemAction GetAction(bool usingItem, bool holding, int useStyle, int holdStyle)
        {
            if (usingItem)
                return useStyle switch
                {
                    ItemUseStyleID.Swing or ItemUseStyleID.GolfPlay => VanillaItemAction.Swing,
                    ItemUseStyleID.Thrust or ItemUseStyleID.Rapier => VanillaItemAction.Thrust,
                    ItemUseStyleID.Shoot => VanillaItemAction.Shoot,
                    ItemUseStyleID.EatFood or ItemUseStyleID.DrinkLong or ItemUseStyleID.DrinkOld
                        or ItemUseStyleID.DrinkLiquid => VanillaItemAction.Use,
                    ItemUseStyleID.HoldUp or ItemUseStyleID.RaiseLamp or ItemUseStyleID.MowTheLawn => VanillaItemAction.Hold,
                    ItemUseStyleID.Guitar => VanillaItemAction.Guitar,
                    _ => VanillaItemAction.None
                };
            if (!holding) return VanillaItemAction.None;
            return holdStyle switch
            {
                ItemHoldStyleID.HoldGuitar => VanillaItemAction.Guitar,
                ItemHoldStyleID.HoldFront or ItemHoldStyleID.HoldUp or ItemHoldStyleID.HoldHeavy
                    or ItemHoldStyleID.HoldGolfClub or ItemHoldStyleID.HoldLamp or ItemHoldStyleID.HoldRadio => VanillaItemAction.Hold,
                _ => VanillaItemAction.None
            };
        }

        /// <summary>优先读取连续旋转手臂；旧式动作读取原版手部位置表，按标准臂长换算。</summary>
        private static VanillaArmPose ReadArm(Player player, bool front, bool available, bool framePose, int frameIndex)
        {
            if (!available) return default;
            Player.CompositeArmData arm = front ? player.compositeFrontArm : player.compositeBackArm;
            Vector2 offset;
            float referenceLength;
            if (arm.enabled)
            {
                // Get*HandPosition 从向下轴计算旋转，已经包含玩家左右朝向。
                Vector2 hand = front ? player.GetFrontHandPosition(arm.stretch, arm.rotation)
                    : player.GetBackHandPosition(arm.stretch, arm.rotation);
                Vector2 shoulder = player.MountedCenter + new Vector2((front ? -4f : 6f) * player.direction, -2f);
                offset = hand - shoulder;
                referenceLength = front ? MathF.Sqrt(109f) : 12f;
            }
            else
            {
                if (!framePose) return default;
                Vector2[] offsets = front ? Main.OffsetsPlayerOnhand : Main.OffsetsPlayerOffhand;
                if (offsets == null || (uint)frameIndex >= (uint)offsets.Length) return default;
                // 同原版 HandPosition 的身体贴图坐标；标准肩膀避免模型体积改变姿势比例。
                offset = offsets[frameIndex] * 2f
                    - new Vector2(player.bodyFrame.Width * 0.5f + (front ? -4f : 6f), player.bodyFrame.Height * 0.5f - 2f);
                offset.X *= player.direction;
                offset.Y *= player.gravDir;
                referenceLength = front ? MathF.Sqrt(164f) : MathF.Sqrt(200f);
            }
            return new(true, offset.RotatedBy(player.fullRotation) / referenceLength);
        }
    }

    /// <summary>
    /// 原版姿势到 Spine 双骨骼手臂的适配层。不占用轨道，不要求制作各物品动画。
    /// 只修改本帧世界矩阵，腿部动画、播放进度和局部旋转偏移仍由播放器负责。
    /// </summary>
    public sealed class VanillaItemActionAdapter : IDisposable
    {
        /// <summary>本角色独立的播放器。</summary>
        private readonly SpinePlayer animator;
        /// <summary>基类自动接入的玩家；独立使用时为 null，由 Update 提供快照。</summary>
        private readonly Player player;
        /// <summary>前后手绑定；未绑定的手臂保留原动画。</summary>
        private ArmBinding front, back;
        /// <summary>开关及释放标记。</summary>
        private bool enabled = true, disposed;
        /// <summary>Draw 的整体旋转弧度。</summary>
        private float drawRotation;
        /// <summary>绑定时缓存双骨骼及后代，避免每帧分配。</summary>
        private sealed record ArmBinding(Bone Upper, Bone Lower, int BendDirection, Bone[] Descendants);
        /// <summary>本帧原版动作，供角色自己的状态逻辑读取。</summary>
        public VanillaItemActionFrame Frame { get; private set; }
        /// <summary>最近一次重算是否驱动了至少一条手臂。</summary>
        public bool IsControlling { get; private set; }
        /// <summary>可选接管条件；返回 false 时保留角色姿势，可排除特定物品。</summary>
        public Func<VanillaItemActionFrame, bool> PoseFilter { get; set; }
        /// <summary>默认 0；若 Draw 传入 rotation，这里应传相同弧度，抵消绘制旋转。</summary>
        public float DrawRotation
        {
            get => drawRotation;
            set
            {
                if (!float.IsFinite(value)) throw new ArgumentOutOfRangeException(nameof(value));
                drawRotation = value;
            }
        }
        /// <summary>技能或 SwingHelper 接管骨骼时设为 false，立即恢复动画姿势。</summary>
        public bool Enabled
        {
            get => enabled;
            set
            {
                ObjectDisposedException.ThrowIf(disposed, this);
                if (enabled == value) return;
                enabled = value;
                animator.RefreshWorldPose();
            }
        }

        /// <summary>独立接入入口：绑定一次，再通过 Update 传入动作快照。</summary>
        public VanillaItemActionAdapter(SpinePlayer animator) : this(animator, null) { }
        /// <summary>供基类自动接入：动画及绘制重算时读取该玩家最新姿势。</summary>
        internal VanillaItemActionAdapter(SpinePlayer animator, Player player)
        {
            ArgumentNullException.ThrowIfNull(animator);
            this.animator = animator;
            this.player = player;
            animator.WorldPoseApplied += Apply;
        }
        /// <summary>绑定前手的上臂、直接子骨骼前臂；肘部弯曲方向为 1 或 -1。</summary>
        public void BindFrontArm(string upperArm, string forearm, int bendDirection = 1)
        {
            front = CreateBinding(upperArm, forearm, bendDirection, back);
            animator.RefreshWorldPose();
        }
        /// <summary>绑定后手；前臂末端为握点，不是与上臂连接的原点。</summary>
        public void BindBackArm(string upperArm, string forearm, int bendDirection = 1)
        {
            back = CreateBinding(upperArm, forearm, bendDirection, front);
            animator.RefreshWorldPose();
        }
        /// <summary>独立适配器传入快照并立即应用，不推进动画时间。</summary>
        public void Update(VanillaItemActionFrame frame)
        {
            ObjectDisposedException.ThrowIf(disposed, this);
            if (player != null) throw new InvalidOperationException("基类创建的适配器自动读取玩家，不需要手动 Update。");
            Frame = frame;
            animator.RefreshWorldPose();
        }
        /// <summary>读取手臂末端，返回 Y 向下的骨架局部坐标；不含 Draw 的位置、缩放和旋转。</summary>
        public bool TryGetHandPosition(bool frontHand, out Vector2 position)
        {
            ObjectDisposedException.ThrowIf(disposed, this);
            ArmBinding binding = frontHand ? front : back;
            if (binding == null) { position = default; return false; }
            Bone bone = binding.Lower;
            position = new(bone.WorldX + bone.A * bone.Data.Length, -(bone.WorldY + bone.C * bone.Data.Length));
            return true;
        }
        /// <summary>校验骨骼层级并缓存后代；双骨骼 IK 使用普通变换继承的手臂。</summary>
        private ArmBinding CreateBinding(string upperName, string lowerName, int bendDirection, ArmBinding other)
        {
            ObjectDisposedException.ThrowIf(disposed, this);
            ArgumentException.ThrowIfNullOrWhiteSpace(upperName);
            ArgumentException.ThrowIfNullOrWhiteSpace(lowerName);
            if (bendDirection != 1 && bendDirection != -1) throw new ArgumentOutOfRangeException(nameof(bendDirection));
            Bone upper = animator.Skeleton.FindBone(upperName) ?? throw new ArgumentException($"骨骼 '{upperName}' 不存在。");
            Bone lower = animator.Skeleton.FindBone(lowerName) ?? throw new ArgumentException($"骨骼 '{lowerName}' 不存在。");
            if (upper.Parent == null || lower.Parent != upper || lower.Data.Length <= 0f)
                throw new ArgumentException("上臂必须有父骨骼，前臂必须是上臂的直接子骨骼，且前臂长度大于 0。");
            if (upper.Data.TransformMode != TransformMode.Normal || lower.Data.TransformMode != TransformMode.Normal)
                throw new ArgumentException("通用手臂需要使用 Spine 普通变换继承模式。");
            if (other != null && (IsUnder(upper, other.Upper) || IsUnder(other.Upper, upper)))
                throw new ArgumentException("前后手必须绑定独立手臂，不能使用重叠的骨骼层级。");
            var descendants = new List<Bone>();
            foreach (Bone bone in animator.Skeleton.Bones)
                if (bone != upper && bone != lower && IsUnder(bone, upper)) descendants.Add(bone);
            return new(upper, lower, bendDirection, descendants.ToArray());
        }
        /// <summary>检查骨骼自身或父链是否属于指定骨骼。</summary>
        private static bool IsUnder(Bone bone, Bone ancestor)
        {
            for (; bone != null; bone = bone.Parent) if (bone == ancestor) return true;
            return false;
        }
        /// <summary>在动画和约束之后应用手臂，不写入 Rotation 等局部动画属性。</summary>
        private void Apply()
        {
            if (player != null) Frame = VanillaItemActionFrame.Capture(player);
            IsControlling = false;
            if (!enabled || disposed || (PoseFilter != null && !PoseFilter(Frame))) return;
            IsControlling = Solve(front, Frame.FrontArm) | Solve(back, Frame.BackArm);
        }
        /// <summary>用角色自身臂长生成目标，调用 Spine 双骨骼 IK，再更新跟随部件。</summary>
        private bool Solve(ArmBinding binding, VanillaArmPose pose)
        {
            if (binding == null || !pose.Active || !binding.Upper.Active || !binding.Lower.Active) return false;
            Vector2 reach = pose.Reach;
            if (!float.IsFinite(reach.X) || !float.IsFinite(reach.Y)) return false;
            reach = reach.RotatedBy(-drawRotation);
            Bone upper = binding.Upper, lower = binding.Lower, parent = upper.Parent;
            float determinant = parent.A * parent.D - parent.B * parent.C;
            float armLength = Vector2.Distance(new(upper.WorldX, upper.WorldY), new(lower.WorldX, lower.WorldY))
                + lower.Data.Length * lower.WorldScaleX;
            if (MathF.Abs(determinant) < 0.00001f || !float.IsFinite(armLength) || armLength <= 0.00001f) return false;
            // Reach 已含朝向，世界矩阵已含镜像；在同一世界空间求解，不再二次翻转。
            IkConstraint.Apply(upper, lower, upper.WorldX + reach.X * armLength,
                upper.WorldY - reach.Y * armLength, binding.BendDirection, false, 0f, 1f);
            foreach (Bone bone in binding.Descendants) bone.UpdateWorldTransform();
            return true;
        }
        /// <summary>解除姿势层并恢复动画；基类释放播放器时自动调用。</summary>
        public void Dispose()
        {
            if (disposed) return;
            disposed = true;
            animator.WorldPoseApplied -= Apply;
            IsControlling = false;
            if (!animator.IsDisposed) animator.RefreshWorldPose();
        }
    }
}
