using System;
using System.Linq;
using CharacterRely.Animation;
using CharacterRely.Content.SkillSystem;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ModLoader;

namespace CharacterRely.Content.ModCharacter;

public class TestCharacter : CharacterLib
{
    /// <summary>角色绘制与手持物品挂点共用的模型缩放。</summary>
    public const float DrawScale = 1.25f;

    /// <summary>基础移动状态，只控制轨道 0；增加 Dash 等动作时可扩展此枚举与登记。</summary>
    public enum MovementState
    {
        /// <summary>基础站立动作。</summary>
        Idle,
        /// <summary>基础移动动作。</summary>
        Move
    }
    /// <summary>上半身动作状态，只控制轨道 1，不替换基础移动状态。</summary>
    public enum ActionState
    {
        /// <summary>无覆盖动作，上半身跟随基础轨道。</summary>
        None,
        /// <summary>上半身播放一次攻击动作。</summary>
        Attack,
        /// <summary>双手蓄力、推出激光及收手，由程序姿势层控制上半身。</summary>
        Laser
    }

    // 测试时进入世界即可看见；仍可用 /spinedemo 切换显隐。
    public bool DemoEnabled { get; private set; } = true;

    public override bool IsDrawEnabled => DemoEnabled;
    public override float CharacterScale => DrawScale;

    /// <summary>示例启用且此角色已确认时才允许使用技能。</summary>
    public override bool SkillsEnabled => IsSelected && DemoEnabled;

    /// <summary>当前示例开启时接收技能槽按键。</summary>
    public override bool AcceptsSkillInput => IsSelected && DemoEnabled;

    /// <summary>激光技能实例，同时提供发波姿势和手掌发射点。</summary>
    public TestLaserSkill LaserSkill => (TestLaserSkill)GetSkill(TestLaserSkill.SkillId);

    /// <summary>为每个玩家注入独立技能；主菜单读取 Skills 时也能取得技能说明。</summary>
    protected override Skill[] CreateSkills() => new Skill[] { new TestLaserSkill(), new TestUpperBodyAttackSkill() };

    /// <summary>本玩家的移动状态机；每帧选择 Idle/Move 不会重置同一动作的进度。</summary>
    public SpineAnimationStateMachine<MovementState> MovementMachine
        => GetAnimationStateMachine<MovementState>(0, ConfigureMovement);

    /// <summary>本玩家的上半身状态机；攻击播完自动进入 None，淡出后重新跟随基础动作。</summary>
    public SpineAnimationStateMachine<ActionState> ActionMachine
        => GetAnimationStateMachine<ActionState>(1, ConfigureActions);

    /// <summary>原版物品通用动作：直接驱动上下臂，不需要为每种 useStyle 制作动画。</summary>
    public VanillaItemActionAdapter VanillaActions => GetVanillaItemActions(ConfigureVanillaActions);

    /// <summary>将物品握点放到前臂末端；朝向已包含在骨架中，无需再乘 direction。</summary>
    public override bool TryGetHeldItemDrawPosition(out Vector2 worldPosition)
    {
        worldPosition = default;
        // 当前示例只绘制本地玩家，关闭示例时也恢复原版物品位置。
        if (!IsSelected || !DemoEnabled || Main.dedServ || !Player.active || Player.dead)
            return false;
        var actions = VanillaActions;
        Animator.RefreshWorldPose();
        if (!actions.TryGetHandPosition(true, out Vector2 hand)) return false;
        worldPosition = Player.Bottom + hand * DrawScale;
        return true;
    }

    /// <summary>示例模型 hand_front/hand_back 实际是前臂，末端才是握点。</summary>
    private void ConfigureVanillaActions(VanillaItemActionAdapter actions)
    {
        actions.BindFrontArm("arm_front", "hand_front");
        actions.BindBackArm("arm_back", "hand_back");
        // 手工攻击优先；技能或 SwingHelper 也可以通过 Enabled=false 临时接管。
        actions.PoseFilter = _ => ActionMachine.CurrentState is not (ActionState.Attack or ActionState.Laser);
    }

    /// <summary>登记基础动作；每个玩家首次使用或释放后重建时执行一次。</summary>
    private void ConfigureMovement(SpineAnimationStateMachine<MovementState> machine)
    {
        machine.RegisterState(MovementState.Idle, "Idle");
        machine.RegisterState(MovementState.Move, "Move");
    }

    /// <summary>登记上半身动作；模型需要导出只包含上半身关键帧的 Attack_Upper。</summary>
    private void ConfigureActions(SpineAnimationStateMachine<ActionState> machine)
    {
        machine.RegisterEmptyState(ActionState.None, fadeOutSeconds: 0.1f);
        // 发波由技能的双骨骼姿势层驱动，轨道 0 的移动动作继续播放。
        machine.RegisterEmptyState(ActionState.Laser, fadeOutSeconds: 0.04f);
        if (AnimationNames.Contains("Attack_Upper"))
            machine.RegisterState(ActionState.Attack, "Attack_Upper", loop: false,
                fadeInSeconds: 0.06f, nextState: ActionState.None);
        machine.ChangeState(ActionState.None);
    }

    /// <summary>
    /// 上半身攻击入口，可由武器或技能逻辑调用；不改变下半身移动状态。
    /// 模型缺少 Attack_Upper、示例停用或已在攻击时返回 false；restart 可主动重播。
    /// </summary>
    public bool TryStartUpperBodyAttack(bool restart = false)
    {
        if (!DemoEnabled || Main.dedServ || Player.dead || LaserSkill.IsActive) return false;
        var machine = ActionMachine;
        return machine.HasState(ActionState.Attack) && machine.ChangeState(ActionState.Attack, restart);
    }

    protected override SpineAsset LoadAnimationAsset()
        => SpineAsset.Load(Mod, "Assets/test.json", "Assets/images-packed/images.atlas");

    /// <summary>发波期间双手用于技能，禁止同时使用普通物品。</summary>
    public override bool CanUseItem(Item item) => !IsSelected || !DemoEnabled || !LaserSkill.IsActive;

    /// <summary>发波期间隐藏手持物品，技能结束后原版图层自动恢复。</summary>
    public override void HideDrawLayers(PlayerDrawSet drawInfo)
    {
        base.HideDrawLayers(drawInfo);
        if (IsSelected && DemoEnabled && LaserSkill.IsActive) PlayerDrawLayers.HeldItem.Hide();
    }

    public void ToggleDemo()
    {
        DemoEnabled = !DemoEnabled;
        if (!DemoEnabled)
        {
            ReleaseSkills();
            ReleaseAnimation();
        }
    }

    protected override void UpdateAnimation()
    {
        if (!IsSelected || !DemoEnabled || Main.dedServ) return;

        // 更新阶段只选择动作和朝向，实际绘制交给下方 PostDrawTiles。
        if (LaserSkill.IsActive) Player.ChangeDir(LaserSkill.Facing);
        Animator.FlipX = Player.direction < 0;
        MovementMachine.ChangeState(Math.Abs(Player.velocity.X) > 0.1f ? MovementState.Move : MovementState.Idle);
        _ = VanillaActions;
    }
}

public class SpineDemoCommand : ModCommand
{
    public override string Command => "spinedemo";
    public override CommandType Type => CommandType.Chat;
    public override string Usage => "/spinedemo [attack]";
    public override string Description => "切换 Spine 示例，或用 attack 测试独立的上半身攻击";

    public override void Action(CommandCaller caller, string input, string[] args)
    {
        var demo = caller.Player.GetModPlayer<TestCharacter>();
        if (args.Length > 0 && args[0].Equals("attack", StringComparison.OrdinalIgnoreCase))
        {
            if (!demo.DemoEnabled)
                caller.Reply("请先使用 /spinedemo 开启示例。");
            else if (!demo.AnimationNames.Contains("Attack_Upper"))
                caller.Reply("当前模型没有 Attack_Upper。请导出仅给上半身设置关键帧的攻击动画。");
            else
                caller.Reply(demo.TryStartUpperBodyAttack() ? "上半身攻击已开始，基础移动动画继续播放。" : "当前攻击尚未结束。");
            return;
        }
        demo.ToggleDemo();
        caller.Reply(demo.DemoEnabled ? "Spine 示例已开启。" : "Spine 示例已关闭。");
    }
}

public class SpineDemoDrawSystem : ModSystem
{
    public override void PostDrawTiles()
    {
        if (Main.dedServ || Main.gameMenu) return;

        foreach (Player player in Main.player)
        {
            if (player == null || !player.active || player.dead) continue;
            foreach (var component in player.ModPlayers)
            {
                if (component is CharacterLib character && character.IsSelected && character.IsDrawEnabled)
                {
                    character.DrawCharacter(Main.instance.GraphicsDevice);
                    break;
                }
            }
        }
    }
}
