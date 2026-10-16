using CharacterRely.Content.ModCharacter;

namespace CharacterRely.Content.SkillSystem;

/// <summary>技能注入示例：复用已有上半身动作入口，具体伤害由角色模组实现。</summary>
public sealed class TestUpperBodyAttackSkill : Skill
{
    /// <summary>角色内唯一的技能 ID，供按钮、按键或技能逻辑调用。</summary>
    public const string SkillId = "upper_body_attack";

    /// <summary>可共用只读信息，各技能实例独立维护充能。</summary>
    private static readonly SkillInfo skillInfo = new(
        SkillId, "上半身攻击", "播放一次上半身攻击动作，下半身继续移动。当前示例不造成伤害。",
        cooldownTicks: 60);

    /// <summary>构造时不访问玩家或动画，可供角色选择 UI 获取信息。</summary>
    public TestUpperBodyAttackSkill() : base(skillInfo) { }

    /// <summary>动作不存在或未能进入攻击状态时返回 false，不扣充能。</summary>
    protected override bool OnUse()
        => Owner is TestCharacter character && character.TryStartUpperBodyAttack();
}
