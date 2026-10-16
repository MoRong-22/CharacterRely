using System;
using System.Collections.Generic;
using CharacterRely.Animation;
using CharacterRely.Content.SkillSystem;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.DataStructures;
using Terraria.ModLoader;

namespace CharacterRely.Content.ModCharacter
{
    /// <summary>
    /// 角色动画基类：每种角色共享资源，每个 Player 独立播放。
    /// 首次访问 Animator 或 PlayAnimation 时创建播放器；PostUpdate 自动推进，绘制由调用方安排。
    /// </summary>
    public abstract class CharacterLib : ModPlayer
    {
        /// <summary>按角色类型保存的共享模型资源。</summary>
        private static readonly Dictionary<Type, SpineAsset> animationAssets = new();
        /// <summary>当前角色玩家独立的动画播放器。</summary>
        private SpinePlayer animator;
        /// <summary>每位玩家独立的状态机，各自控制一条轨道；释放播放器时一并清理。</summary>
        private List<ISpineAnimationStateMachine> animationStateMachines;
        /// <summary>本玩家的原版通用手臂姿势层；仅角色主动接入时创建。</summary>
        private VanillaItemActionAdapter vanillaItemActions;
        /// <summary>此角色实例的技能列表；UI 模板与每个玩家分别创建，不共享充能。</summary>
        private IReadOnlyList<Skill> skills;
        /// <summary>此角色实例的被动技能列表，与主动技能槽位无关。</summary>
        private IReadOnlyList<PassiveSkill> passiveSkills;
        /// <summary>清理回调期间禁止重新释放技能。</summary>
        private bool releasingSkills;
        private bool releasingPassiveSkills;

        public bool hidePlayer = false;
        /// <summary>此组件是否为该玩家当前唯一确认的角色。</summary>
        public bool IsSelected => CharacterRegistry.TryGet(GetType(), out CharacterDefinition definition)
            && Player != null && Player.GetModPlayer<CharacterSelectionPlayer>().SelectedCharacterId == definition.Id;

        /// <summary>该角色注册时的稳定 ID；尚未注册时返回 null。</summary>
        public string RegisteredCharacterId
            => CharacterRegistry.TryGet(GetType(), out CharacterDefinition definition) ? definition.Id : null;

        /// <summary>
        /// 注入本角色的资源。在模组 Load 时每种角色调用一次，不要在这里访问 Player。
        /// 返回 SpineAsset.Load(资源所属模组, 骨架路径, 图集路径)。
        /// 返回的资源由本类负责释放。
        /// </summary>
        protected abstract SpineAsset LoadAnimationAsset();

        /// <summary>此玩家独立的动画实例，首次访问时创建；角色基类负责更新，调用方负责绘制。</summary>
        public SpinePlayer Animator
        {
            get
            {
                if (animator != null) return animator;
                if (!animationAssets.TryGetValue(GetType(), out var asset))
                    throw new InvalidOperationException($"角色 {GetType().Name} 的动画资源尚未加载。");
                return animator = asset.CreatePlayer();
            }
        }

        /// <summary>模型中实际存在的动作名称，不假定 idle、walk 等命名。</summary>
        public IReadOnlyList<string> AnimationNames => Animator.AnimationNames;

        /// <summary>选人界面的预览动作；模型没有此动作时使用第一个导出的动画。</summary>
        public virtual string PreviewAnimationName => "Idle";

        /// <summary>创建独立的 UI 预览播放器，不更新玩家、技能或游戏内状态机。使用方负责 Dispose。</summary>
        public SpinePlayer CreatePreviewPlayer()
        {
            if (!animationAssets.TryGetValue(GetType(), out var asset))
                throw new InvalidOperationException($"角色 {GetType().Name} 的动画资源尚未加载。");
            var preview = asset.CreatePlayer();
            string animation = null;
            foreach (string name in asset.AnimationNames)
            {
                animation ??= name;
                if (name == PreviewAnimationName) { animation = name; break; }
            }
            if (animation != null) preview.Play(animation);
            return preview;
        }

        /// <summary>模组加载时为当前角色类型登记共享资源；每个玩家使用独立的播放器。</summary>
        public sealed override void Load()
        {
            animationAssets.Add(GetType(), LoadAnimationAsset()
                ?? throw new InvalidOperationException($"角色 {GetType().Name} 未提供动画资源。"));
        }

        // tModLoader 会克隆玩家用于同步等用途；克隆不能共享原玩家的可变播放器。
        /// <summary>克隆玩家组件时清除播放器引用，避免两个玩家共享可变动画状态。</summary>
        public override ModPlayer Clone(Player newEntity)
        {
            var clone = (CharacterLib)base.Clone(newEntity);
            clone.animator = null;
            clone.animationStateMachines = null;
            clone.vanillaItemActions = null;
            clone.skills = null;
            clone.passiveSkills = null;
            clone.releasingSkills = false;
            clone.releasingPassiveSkills = false;
            return clone;
        }

        /// <summary>
        /// 注入技能数组，顺序也是以后 UI 的展示顺序。每次返回 new 出来的技能实例。
        /// 主菜单读取说明时也会调用此方法，只构造技能，不访问 Player 或 Animator。
        /// </summary>
        protected virtual Skill[] CreateSkills() => Array.Empty<Skill>();

        /// <summary>
        /// 注入任意数量的被动技能；每次创建都返回 new 出来的实例。
        /// 主菜单读取说明时也会调用，因此构造函数不要访问 Player 或 Animator。
        /// </summary>
        protected virtual PassiveSkill[] CreatePassiveSkills() => Array.Empty<PassiveSkill>();

        /// <summary>允许角色进一步限制技能；基类仍会强制要求该角色已确认。</summary>
        public virtual bool SkillsEnabled => IsSelected;

        /// <summary>是否启用被动技能；默认跟随当前角色的 SkillsEnabled，可由角色单独重写。</summary>
        public virtual bool PassiveSkillsEnabled => SkillsEnabled;

        /// <summary>
        /// 是否接收技能槽按键，默认只由已确认角色接收。
        /// </summary>
        public virtual bool AcceptsSkillInput => IsSelected;

        /// <summary>技能执行的统一检查；UI 模板没有玩家时始终为 false。</summary>
        internal bool CanUseSkills => !releasingSkills && IsSelected && SkillsEnabled
            && Player != null && Player.active && !Player.dead;

        private bool CanRunPassiveSkills => !releasingPassiveSkills && IsSelected && PassiveSkillsEnabled
            && Player != null && Player.active && !Player.dead && !Main.gameMenu;

        /// <summary>
        /// 首次访问时创建。主菜单用 ModContent.GetInstance&lt;角色类型&gt;().Skills 读取 Info；
        /// 运行时用 player.GetModPlayer&lt;角色类型&gt;().Skills，二者状态独立。
        /// </summary>
        public IReadOnlyList<Skill> Skills
        {
            get
            {
                if (skills != null) return skills;
                Skill[] created = (Skill[])(CreateSkills()
                    ?? throw new InvalidOperationException("CreateSkills 不能返回 null，请返回空数组。")).Clone();
                var ids = new HashSet<string>(StringComparer.Ordinal);
                foreach (Skill skill in created)
                {
                    if (skill == null) throw new InvalidOperationException("角色技能数组不能包含 null。");
                    if (!ids.Add(skill.Id)) throw new InvalidOperationException($"角色技能 ID 重复：{skill.Id}");
                    if (skill.Owner != null) throw new InvalidOperationException($"技能 '{skill.Id}' 已被使用，请重新 new。");
                }
                foreach (Skill skill in created) skill.Bind(this);
                return skills = Array.AsReadOnly(created);
            }
        }

        /// <summary>被动列表不受五个主动技能槽限制；模板可读取名称、描述和图标，不会启动效果。</summary>
        public IReadOnlyList<PassiveSkill> PassiveSkills
        {
            get
            {
                if (passiveSkills != null) return passiveSkills;
                PassiveSkill[] created = (PassiveSkill[])(CreatePassiveSkills()
                    ?? throw new InvalidOperationException("CreatePassiveSkills 不能返回 null，请返回空数组。")).Clone();
                var ids = new HashSet<string>(StringComparer.Ordinal);
                foreach (PassiveSkill passive in created)
                {
                    if (passive == null) throw new InvalidOperationException("被动技能数组不能包含 null。");
                    if (!ids.Add(passive.Id)) throw new InvalidOperationException($"被动技能 ID 重复：{passive.Id}");
                    if (passive.Owner != null) throw new InvalidOperationException($"被动技能 '{passive.Id}' 已被使用，请重新 new。");
                }
                foreach (PassiveSkill passive in created) passive.Bind(this);
                return passiveSkills = Array.AsReadOnly(created);
            }
        }

        /// <summary>按 ID 查询被动技能；不存在时返回 null。</summary>
        public PassiveSkill GetPassiveSkill(string id)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(id);
            foreach (PassiveSkill passive in PassiveSkills)
                if (passive.Id == id) return passive;
            return null;
        }

        /// <summary>按稳定 ID 获取技能；不存在时返回 null，查询不会执行技能。</summary>
        public Skill GetSkill(string id)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(id);
            foreach (Skill skill in Skills)
                if (skill.Id == id) return skill;
            return null;
        }

        /// <summary>按技能 ID 尝试释放；不存在或当前不能释放时返回 false。</summary>
        public bool TryUseSkill(string id) => CanUseSkills && (GetSkill(id)?.TryUse() ?? false);

        /// <summary>按数组下标尝试释放；无效下标返回 false，适合以后 UI 或按键绑定。</summary>
        public bool TryUseSkill(int index) => CanUseSkills && (uint)index < (uint)Skills.Count && Skills[index].TryUse();

        /// <summary>只更新已经创建的技能；读取 UI 信息不会自动激活或播放技能。</summary>
        private void UpdateSkills()
        {
            if (skills == null) return;
            foreach (Skill skill in skills)
            {
                if (CanUseSkills) skill.Update();
                else skill.End(interrupted: true);
            }
        }

        /// <summary>每次属性重置后重新施加被动效果；UI 模板和主菜单预览不会执行。</summary>
        public sealed override void ResetEffects()
        {
            if (CanRunPassiveSkills)
                foreach (PassiveSkill passive in PassiveSkills) passive.ApplyForPlayer();
            else if (passiveSkills != null)
                foreach (PassiveSkill passive in passiveSkills) passive.Disable();
        }

        private void UpdatePassiveSkills()
        {
            if (CanRunPassiveSkills)
                foreach (PassiveSkill passive in PassiveSkills) passive.UpdateForPlayer();
            else if (passiveSkills != null)
                foreach (PassiveSkill passive in passiveSkills) passive.Disable();
        }

        /// <summary>死亡时及时中断持续技能，保留充能计时；复活并启用角色后继续计时。</summary>
        public override void UpdateDead()
        {
            if (skills != null)
                foreach (Skill skill in skills) skill.End(interrupted: true);
            if (passiveSkills != null)
                foreach (PassiveSkill passive in passiveSkills) passive.Disable();
        }

        /// <summary>切换角色或卸载时释放技能。旧引用失效，下次访问 Skills 创建全新满充能实例。</summary>
        public void ReleaseSkills()
        {
            if (skills == null || releasingSkills) return;
            releasingSkills = true;
            try
            {
                foreach (Skill skill in skills) skill.Release();
                skills = null;
            }
            finally { releasingSkills = false; }
        }

        /// <summary>切换角色或卸载时停用并丢弃被动实例；下次访问会创建新实例。</summary>
        public void ReleasePassiveSkills()
        {
            if (passiveSkills == null || releasingPassiveSkills) return;
            releasingPassiveSkills = true;
            try
            {
                foreach (PassiveSkill passive in passiveSkills) passive.Release();
                passiveSkills = null;
            }
            finally { releasingPassiveSkills = false; }
        }

        /// <summary>
        /// 获取本玩家指定轨道的状态机；首次创建时执行 configure，之后保持独立状态与计时。
        /// 可分别用移动枚举和动作枚举获取轨道 0、1；同一轨道只能由一种状态机控制。
        /// 状态机的完成转换由基类自动更新，角色只需调用 ChangeState。
        /// </summary>
        /// <typeparam name="TState">角色定义的状态枚举。</typeparam>
        /// <param name="trackIndex">独占控制的轨道编号，通常基础动作是 0，上半身动作是 1。</param>
        /// <param name="configure">首次创建时登记状态与对应动画；播放器释放后重建时会再次执行。</param>
        protected SpineAnimationStateMachine<TState> GetAnimationStateMachine<TState>(int trackIndex,
            Action<SpineAnimationStateMachine<TState>> configure = null) where TState : struct, Enum
        {
            ArgumentOutOfRangeException.ThrowIfNegative(trackIndex);
            animationStateMachines ??= new();
            foreach (var machine in animationStateMachines)
            {
                if (machine.TrackIndex != trackIndex) continue;
                return machine as SpineAnimationStateMachine<TState>
                    ?? throw new InvalidOperationException($"轨道 {trackIndex} 已被另一种状态枚举占用。");
            }
            var created = new SpineAnimationStateMachine<TState>(Animator, trackIndex);
            configure?.Invoke(created);
            animationStateMachines.Add(created);
            return created;
        }

        /// <summary>
        /// 获取本玩家独立的原版通用动作适配器，首次 configure 时绑定前后手的上下臂骨骼。
        /// 基类负责读取原版姿势，播放器在动画算完及绘制前应用；不需要逐帧手动 Update。
        /// </summary>
        /// <param name="configure">首次创建时绑定骨骼或设置接管条件；释放重建后重新执行。</param>
        protected VanillaItemActionAdapter GetVanillaItemActions(Action<VanillaItemActionAdapter> configure = null)
        {
            if (vanillaItemActions != null) return vanillaItemActions;
            var created = new VanillaItemActionAdapter(Animator, Player);
            try { configure?.Invoke(created); }
            catch { created.Dispose(); throw; }
            return vanillaItemActions = created;
        }

        /// <summary>
        /// 提供原版手持物品的握点，使用世界坐标；返回 false 时保留原版绘制位置。
        /// 应与角色 Draw 的位置、缩放和旋转一致，尚未应用屏幕缩放矩阵。
        /// 只平移物品绘制，不改变物品使用逻辑。
        /// </summary>
        public virtual bool TryGetHeldItemDrawPosition(out Vector2 worldPosition)
        {
            worldPosition = default;
            return false;
        }

        /// <summary>选择动作。重复调用同名、同循环模式的动作保持进度；restart: true 从头开始。</summary>
        /// <param name="name">模型中实际存在的动画名称。</param>
        /// <param name="loop">是否循环播放，默认为 true。</param>
        /// <param name="fadeInSeconds">从前一动作过渡到本动作的时长，单位为秒。</param>
        /// <param name="restart">是否强制从头重播同一个动作。</param>
        /// <param name="trackIndex">轨道编号，默认 0；较高轨道只覆盖其动画有关键帧的属性。</param>
        /// <returns>当前轮的播放进度，范围 0～1；非循环播放结束后为 1。</returns>
        public float PlayAnimation(string name, bool loop = true, float fadeInSeconds = 0f, bool restart = false, int trackIndex = 0)
            => Animator.Play(name, loop, fadeInSeconds, restart, trackIndex);

        /// <summary>淡出指定轨道，使上半身等覆盖部件重新跟随基础动作。</summary>
        public void FadeOutAnimationTrack(int trackIndex, float fadeOutSeconds = 0.1f)
            => Animator.FadeOutTrack(trackIndex, fadeOutSeconds);

        /// <summary>
        /// 手部骨骼的局部旋转偏移，单位为弧度。叠加于当前动画，0 恢复动画姿势。
        /// boneName 是骨骼名，不是插槽名；整条手臂应传上臂骨骼名。
        /// 设置会保留，且重复设置不会累加；左右翻转由 Animator.FlipX 处理。
        /// </summary>
        /// <param name="boneName">手部或上臂的骨骼名，可从 Animator.BoneNames 获取。</param>
        /// <param name="radians">相对动画姿势的弧度偏移；未镜像时正值为屏幕顺时针。</param>
        public void SetHandRotation(string boneName, float radians)
            => Animator.SetBoneRotationOffset(boneName, radians);

        /// <summary>每个游戏 tick 先执行角色动画选择，再将已创建的播放器推进 1/60 秒。</summary>
        public sealed override void PostUpdate()
        {
            if (!IsSelected) return;
            UpdateSkills();
            UpdatePassiveSkills();
            UpdateAnimation();
            // 子类可以在 UpdateAnimation 中首次选择动作；只有真正创建播放器后才推进时间。
            animator?.Update(1f / 60f);
            // 播放器推进后分别检查各状态机；攻击结束不会重置移动轨道的时间。
            for (int i = 0; animationStateMachines != null && i < animationStateMachines.Count; i++)
                animationStateMachines[i].Update(1f / 60f);
        }

        public override void HideDrawLayers(PlayerDrawSet drawInfo)
        {
            if (!IsSelected || !IsDrawEnabled) return;
            // if (hidePlayer)
            // {
                Player.invis = true;
                for (int i = 0; i < Player.hideVisibleAccessory.Length; i++)
                    Player.hideVisibleAccessory[i] = true;
                PlayerDrawLayers.Torso.Hide();
                PlayerDrawLayers.Leggings.Hide();
                PlayerDrawLayers.HandOnAcc.Hide();
                PlayerDrawLayers.OffhandAcc.Hide();
                PlayerDrawLayers.Shoes.Hide();
                PlayerDrawLayers.Wings.Hide();
                PlayerDrawLayers.Shield.Hide();
            // }
        }
        /// <summary>
        /// 每个游戏 tick、推进动画前调用。在这里选择动作、设置朝向和手部角度。
        /// 基类负责 Update；这里不要再次推进播放器。
        /// </summary>
        protected virtual void UpdateAnimation() { }

        /// <summary>允许临时关闭模型绘制；停用时原版玩家图层也会恢复。</summary>
        public virtual bool IsDrawEnabled => true;

        /// <summary>模型绘制缩放，默认 1；握点计算应使用相同缩放。</summary>
        public virtual float CharacterScale => 1f;

        /// <summary>绘制该玩家当前选中的 Spine 模型；需要特殊绘制的角色可重写。</summary>
        public virtual void DrawCharacter(GraphicsDevice graphicsDevice)
        {
            if (!IsSelected || !IsDrawEnabled || Main.dedServ || Player.dead) return;
            Animator.FlipX = Player.direction < 0;
            Animator.Draw(graphicsDevice, Player.Bottom - Main.screenPosition, scale: CharacterScale,
                viewMatrix: Main.GameViewMatrix.TransformationMatrix);
        }

        /// <summary>切换到其他角色时清理本实例的技能、被动和动画状态。</summary>
        internal void OnDeselected()
        {
            ReleaseSkills();
            ReleasePassiveSkills();
            ReleaseAnimation();
        }

        /// <summary>停用角色或丢弃预览时释放此实例；下次访问 Animator 会重新创建。</summary>
        public void ReleaseAnimation()
        {
            vanillaItemActions?.Dispose();
            vanillaItemActions = null;
            animator?.Dispose();
            animator = null;
            animationStateMachines = null;
        }

        /// <summary>模组卸载时释放本实例和当前角色类型的共享资源及其播放器。</summary>
        public sealed override void Unload()
        {
            ReleaseSkills();
            ReleasePassiveSkills();
            ReleaseAnimation();
            if (animationAssets.Remove(GetType(), out var asset)) asset.Dispose();
        }
    }
}
