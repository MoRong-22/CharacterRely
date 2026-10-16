using CharacterRely.Content.ModCharacter;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;

namespace CharacterRely.Content.SkillSystem;

/// <summary>激光示例技能：按下时锁定鼠标方向，双手推出后生成一束持续激光。</summary>
public sealed class TestLaserSkill : Skill
{
    /// <summary>技能 ID。</summary>
    public const string SkillId = "laser_wave";
    /// <summary>蓄力帧数。</summary>
    public const int ChargeTicks = 18;
    /// <summary>双手向前推出的帧数。</summary>
    public const int PushTicks = 6;
    /// <summary>激光出现的动作帧。</summary>
    public const int FireTick = ChargeTicks + PushTicks;
    /// <summary>激光持续帧数。</summary>
    public const int BeamTicks = 36;
    /// <summary>收手帧数。</summary>
    public const int RecoverTicks = 12;
    /// <summary>示例基础伤害。</summary>
    public const int BaseDamage = 40;
    /// <summary>只读展示数据；充能恢复时间为三秒。</summary>
    private static readonly SkillInfo skillInfo = new(SkillId, "掌心激光",
        "向鼠标方向双手蓄力后发射激光，可继续移动。光束遇实心物块停止，充能恢复时间为 3 秒。",
        cooldownTicks: 180, durationTicks: FireTick + BeamTicks + RecoverTicks);
    /// <summary>本次动作实例和生成的光束；身份值防止误删复用弹幕槽。</summary>
    private TestWavePose pose;
    private Projectile beam;
    private int beamIdentity;
    /// <summary>释放时固定的世界方向，已包含左右朝向。</summary>
    public Vector2 Aim { get; private set; } = Vector2.UnitX;
    /// <summary>发波期间固定的玩家朝向。</summary>
    public int Facing { get; private set; } = 1;

    /// <summary>构造时只提供技能说明，UI 查询不会创建动画和弹幕。</summary>
    public TestLaserSkill() : base(skillInfo) { }

    /// <summary>由本地玩家发起；先等待普通物品或上半身攻击结束。</summary>
    protected override bool CanUse() => !Main.dedServ && Player.whoAmI == Main.myPlayer
        && !Player.CCed && !Player.pulley && Player.itemAnimation == 0 && Player.heldProj < 0
        && Owner is TestCharacter character
        && character.ActionMachine.CurrentState is null or TestCharacter.ActionState.None;

    /// <summary>锁定瞄准、进入上半身发波状态；此时尚未生成弹幕。</summary>
    protected override bool OnUse()
    {
        var character = (TestCharacter)Owner;
        Aim = (Main.MouseWorld - Player.MountedCenter).SafeNormalize(new Vector2(Player.direction, 0f));
        Facing = Aim.X < -0.001f ? -1 : Aim.X > 0.001f ? 1 : Player.direction;
        Player.ChangeDir(Facing);
        character.Animator.FlipX = Facing < 0;
        _ = character.VanillaActions; // 先注册原版姿势层，发波层在其后应用。
        character.ActionMachine.ChangeState(TestCharacter.ActionState.Laser);
        pose = new TestWavePose(character.Animator, Aim, Facing);
        beam = null;
        return true;
    }

    /// <summary>动作计时到推出末端时只生成一次光束，后续跟随双手。</summary>
    protected override void UpdateActive()
    {
        if (Player.CCed || Player.pulley) { End(interrupted: true); return; }
        pose.SetTick(ElapsedTicks);
        if (ElapsedTicks != FireTick) return;
        TryGetOrigin(out Vector2 origin);
        Vector2 offset = origin - Player.Center;
        int index = Projectile.NewProjectile(Player.GetSource_Misc("CharacterSkill:" + Id), origin, Aim,
            ModContent.ProjectileType<TestLaserProjectile>(), BaseDamage, 1f, Player.whoAmI, offset.X, offset.Y);
        if ((uint)index >= (uint)Main.maxProjectiles) { End(interrupted: true); return; }
        beam = Main.projectile[index];
        beamIdentity = beam.identity;
    }

    /// <summary>取当前双掌中点的世界坐标，与角色绘制使用相同缩放。</summary>
    public bool TryGetOrigin(out Vector2 worldPosition)
    {
        worldPosition = Player?.MountedCenter ?? Vector2.Zero;
        if (pose == null) return false;
        Owner.Animator.RefreshWorldPose();
        worldPosition = Player.Bottom + pose.HandCenter * TestCharacter.DrawScale;
        return true;
    }

    /// <summary>退出动作并结束本次光束，死亡或关闭示例也会经过这里。</summary>
    protected override void OnEnd(bool interrupted)
    {
        if (beam != null && beam.active && beam.identity == beamIdentity
            && beam.owner == Player.whoAmI && beam.ModProjectile is TestLaserProjectile) beam.Kill();
        beam = null;
        pose?.Dispose();
        pose = null;
        if (Owner is TestCharacter character && character.ActionMachine.CurrentState == TestCharacter.ActionState.Laser)
            character.ActionMachine.ChangeState(TestCharacter.ActionState.None);
    }
}
