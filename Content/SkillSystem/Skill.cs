using System;
using CharacterRely.Content.ModCharacter;
using Terraria;

namespace CharacterRely.Content.SkillSystem;

/// <summary>
/// 每位玩家独立持有的技能模块。构造时只填写 Info，不访问玩家、动画或图形资源。
/// 角色通过 CreateSkills 注入全新实例，基类自动推进充能和持续时间。
/// </summary>
public abstract class Skill
{
    /// <summary>回调期间防止重复释放；释放实例后禁止使用旧引用。</summary>
    private bool invoking, released;

    /// <summary>只读展示与配置数据，可以供角色选择 UI 读取和共享。</summary>
    public SkillInfo Info { get; }
    /// <summary>所属角色，注入角色的技能数组后赋值。</summary>
    public CharacterLib Owner { get; private set; }
    /// <summary>所属玩家；仅运行时回调中访问，不要在构造时访问。</summary>
    protected Player Player => Owner?.Player;
    /// <summary>角色内唯一的技能 ID，不使用显示名称作为索引。</summary>
    public string Id => Info.Id;
    /// <summary>技能显示名称。</summary>
    public string Name => Info.Name;
    /// <summary>技能介绍，供角色确认 UI 使用。</summary>
    public string Description => Info.Description;
    /// <summary>完整模组图标路径；未提供时由 UI 自行选择占位图。</summary>
    public string IconPath => Info.IconPath;
    /// <summary>是否正在执行持续技能。</summary>
    public bool IsActive { get; private set; }
    /// <summary>当前可用充能次数，成功释放消耗一次。</summary>
    public int Count { get; private set; }
    /// <summary>最大充能次数。</summary>
    public int MaxCount => Info.MaxCount;
    /// <summary>下一次充能恢复的剩余游戏帧数，0 表示不在充能。</summary>
    public int CooldownRemaining { get; private set; }
    /// <summary>本次持续技能已经执行的帧数；即时技能为 0。</summary>
    public int ElapsedTicks { get; private set; }
    /// <summary>持续技能的执行进度，范围 0～1；即时技能为 0。</summary>
    public float Progress => Info.DurationTicks == 0 ? 0f
        : Math.Min(1f, ElapsedTicks / (float)Info.DurationTicks);

    /// <summary>创建满充能的新技能；信息对象可以共享，技能实例不可以共享。</summary>
    protected Skill(SkillInfo info)
    {
        ArgumentNullException.ThrowIfNull(info);
        Info = info;
        Count = info.MaxCount;
    }

    /// <summary>
    /// 尝试释放。未启用角色、玩家死亡、正在执行、无充能或自定义条件不满足时返回 false。
    /// OnUse 返回 true 后才消耗充能；执行当前实例逻辑，不自动发送多人网络消息。
    /// </summary>
    public bool TryUse()
    {
        if (released || invoking || Owner == null || !Owner.CanUseSkills || IsActive || Count <= 0)
            return false;
        invoking = true;
        try
        {
            if (!CanUse() || !OnUse() || released) return false;
            Count--;
            ElapsedTicks = 0;
            IsActive = Info.DurationTicks > 0;
            if (Info.CooldownTicks == 0)
                Count = MaxCount;
            else if (CooldownRemaining == 0)
                CooldownRemaining = Info.CooldownTicks;
            return true;
        }
        finally { invoking = false; }
    }

    /// <summary>额外释放条件，例如魔力或角色状态；只检查，不消耗资源。</summary>
    protected virtual bool CanUse() => true;
    /// <summary>
    /// 执行一次技能效果，可调用角色动作入口或生成弹幕。成功返回 true，失败返回 false。
    /// 成功返回后才进入持续状态；持续期间逻辑写在 UpdateActive 中，即时技能仅执行此回调。
    /// </summary>
    protected abstract bool OnUse();
    /// <summary>持续技能每帧调用一次；可读取 ElapsedTicks，或调用 End 提前结束。</summary>
    protected virtual void UpdateActive() { }
    /// <summary>持续技能结束时调用一次；中断时 interrupted 为 true，即时技能不调用。</summary>
    protected virtual void OnEnd(bool interrupted) { }

    /// <summary>结束当前持续技能，不返还充能、不清除冷却。死亡、停用、释放实例时按中断处理。</summary>
    public void End(bool interrupted = false)
    {
        if (!IsActive) return;
        IsActive = false;
        bool previousInvoking = invoking;
        invoking = true;
        try { OnEnd(interrupted); }
        finally { invoking = previousInvoking; }
    }

    /// <summary>绑定一次，防止静态技能数组导致多个玩家共享充能与状态。</summary>
    internal void Bind(CharacterLib owner)
    {
        if (Owner != null || released)
            throw new InvalidOperationException($"技能 '{Id}' 已被绑定或释放。CreateSkills 必须返回全新实例。");
        Owner = owner;
    }

    /// <summary>基类每游戏帧推进一次；充能依次恢复，并与技能持续时间同时计时。</summary>
    internal void Update()
    {
        if (released) return;
        if (CooldownRemaining > 0 && --CooldownRemaining == 0)
        {
            Count++;
            if (Count < MaxCount) CooldownRemaining = Info.CooldownTicks;
        }
        if (!IsActive) return;
        ElapsedTicks++;
        UpdateActive();
        if (IsActive && ElapsedTicks >= Info.DurationTicks) End();
    }

    /// <summary>角色释放时中断持续技能，旧引用随后不能再使用。</summary>
    internal void Release()
    {
        released = true;
        End(interrupted: true);
    }
}
