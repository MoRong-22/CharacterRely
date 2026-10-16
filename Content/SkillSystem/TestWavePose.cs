using System;
using System.Linq;
using CharacterRely.Animation;
using Microsoft.Xna.Framework;
using Spine;

namespace CharacterRely.Content.SkillSystem;

/// <summary>示例发波动作：双手内收蓄力、快速推出、维持、收手。只覆盖两条手臂的世界姿势。</summary>
internal sealed class TestWavePose : IDisposable
{
    /// <summary>播放器及绑定的上下臂，复用现有模型而不修改模型文件。</summary>
    private readonly SpinePlayer animator;
    private readonly Bone front, frontHand, back, backHand;
    /// <summary>手部以下的部件也需要跟随 IK 更新。</summary>
    private readonly Bone[] descendants;
    /// <summary>本次固定瞄准方向、朝向、动作帧与释放标记。</summary>
    private readonly Vector2 aim;
    private readonly int facing;
    private int tick;
    private bool disposed;

    /// <summary>绑定示例骨骼，回调在每次动画算完及绘制前执行。</summary>
    public TestWavePose(SpinePlayer animator, Vector2 aim, int facing)
    {
        this.animator = animator;
        this.aim = aim;
        this.facing = facing;
        front = animator.Skeleton.FindBone("arm_front");
        frontHand = animator.Skeleton.FindBone("hand_front");
        back = animator.Skeleton.FindBone("arm_back");
        backHand = animator.Skeleton.FindBone("hand_back");
        if (front == null || back == null || frontHand?.Parent != front || backHand?.Parent != back)
            throw new InvalidOperationException("发波示例需要 arm_front/hand_front 与 arm_back/hand_back 两组手臂骨骼。");
        descendants = animator.Skeleton.Bones.Where(bone => bone != frontHand && bone != backHand
            && (IsBelow(bone, frontHand) || IsBelow(bone, backHand))).ToArray();
        animator.WorldPoseApplied += Apply;
        animator.RefreshWorldPose();
    }

    /// <summary>推进动作阶段，重算姿势不推进 Spine 动画时间。</summary>
    public void SetTick(int elapsed)
    {
        tick = elapsed;
        if (!disposed && !animator.IsDisposed) animator.RefreshWorldPose();
    }

    /// <summary>读取两只手末端中点，Y 向下；调用方应用角色 Draw 的位置和缩放。</summary>
    public Vector2 HandCenter => (Tip(frontHand) + Tip(backHand)) * 0.5f;

    /// <summary>在现有移动姿势上叠加双手动作，目标随实际肩膀移动。</summary>
    private void Apply()
    {
        if (disposed) return;
        float length = MathF.Min(ArmLength(front, frontHand), ArmLength(back, backHand));
        Vector2 shoulder = (Point(front) + Point(back)) * 0.5f;
        Vector2 down = new Vector2(-aim.Y, aim.X) * facing;
        GetPhase(tick, out float extension, out float drop, out float blend);
        Vector2 target = shoulder + length * (aim * extension + down * drop);
        Solve(front, frontHand, target - down * 0.8f, blend);
        Solve(back, backHand, target + down * 0.8f, blend);
        foreach (Bone bone in descendants) bone.UpdateWorldTransform();
    }

    /// <summary>动作阶段的参数，伸手速度在推出阶段先快后慢，末段淡出回移动动画。</summary>
    internal static void GetPhase(int elapsed, out float extension, out float drop, out float blend)
    {
        float push = MathHelper.Clamp((elapsed - TestLaserSkill.ChargeTicks) / (float)TestLaserSkill.PushTicks, 0f, 1f);
        push = 1f - MathF.Pow(1f - push, 3f);
        float recover = MathHelper.Clamp((elapsed - TestLaserSkill.FireTick - TestLaserSkill.BeamTicks)
            / (float)TestLaserSkill.RecoverTicks, 0f, 1f);
        extension = MathHelper.Lerp(MathHelper.Lerp(0.2f, 0.9f, push), 0.3f, recover);
        drop = MathHelper.Lerp(MathHelper.Lerp(0.35f, 0f, push), 0.5f, recover);
        blend = MathHelper.SmoothStep(0f, 1f, MathHelper.Clamp(elapsed / 6f, 0f, 1f))
            * (1f - MathHelper.SmoothStep(0f, 1f, recover));
    }

    /// <summary>用模型自身臂长求双骨骼 IK，不改变局部 Rotation，不影响腿部轨道。</summary>
    private static void Solve(Bone upper, Bone lower, Vector2 target, float blend)
        => IkConstraint.Apply(upper, lower, target.X, -target.Y, 1, false, 0f, blend);
    /// <summary>肩膀与肘部距离加前臂长度。</summary>
    private static float ArmLength(Bone upper, Bone lower)
        => Vector2.Distance(Point(upper), Point(lower)) + lower.Data.Length * lower.WorldScaleX;
    /// <summary>骨骼起点转为 Y 向下坐标。</summary>
    private static Vector2 Point(Bone bone) => new(bone.WorldX, -bone.WorldY);
    /// <summary>骨骼末端转为 Y 向下坐标。</summary>
    private static Vector2 Tip(Bone bone)
        => new(bone.WorldX + bone.A * bone.Data.Length, -(bone.WorldY + bone.C * bone.Data.Length));
    /// <summary>检查需要跟随手部的子部件。</summary>
    private static bool IsBelow(Bone bone, Bone parent)
    {
        for (bone = bone.Parent; bone != null; bone = bone.Parent)
            if (bone == parent) return true;
        return false;
    }
    /// <summary>解除程序动作，恢复原动画手臂姿势。</summary>
    public void Dispose()
    {
        if (disposed) return;
        disposed = true;
        animator.WorldPoseApplied -= Apply;
        if (!animator.IsDisposed) animator.RefreshWorldPose();
    }
}
