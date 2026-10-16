using System;
using CharacterRely.Content.ModCharacter;
using Terraria;

namespace CharacterRely.Content.SkillSystem;

/// <summary>
/// 每位玩家独立持有的被动技能。构造时只填写展示信息，不访问玩家或图形资源。
/// 被动不占主动技能槽位，没有数量、充能或冷却限制。
/// </summary>
public abstract class PassiveSkill
{
    private bool released;

    /// <summary>角色内唯一的稳定 ID，区分大小写。</summary>
    public string Id { get; }
    /// <summary>供角色界面展示的名称。</summary>
    public string Name { get; }
    /// <summary>供角色界面展示的介绍。</summary>
    public string Description { get; }
    /// <summary>可选图标路径，不含扩展名。</summary>
    public string IconPath { get; }
    /// <summary>所属角色；角色调用 CreatePassiveSkills 后绑定。</summary>
    public CharacterLib Owner { get; private set; }
    /// <summary>被动目前是否对所属玩家生效。</summary>
    public bool IsEnabled { get; private set; }
    /// <summary>所属玩家；构造函数和主菜单读取说明时不可访问。</summary>
    protected Player Player => Owner?.Player;

    protected PassiveSkill(string id, string name, string description, string iconPath = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(description);
        Id = id;
        Name = name;
        Description = description;
        IconPath = iconPath;
    }

    /// <summary>本次启用时调用一次；复活或重新启用角色时会再次调用。</summary>
    protected virtual void OnEnable() { }
    /// <summary>每次玩家 ResetEffects 后调用，适合重新施加伤害、移速等属性加成。</summary>
    protected virtual void ApplyEffects() { }
    /// <summary>每个游戏 tick 调用一次，适合检测条件或执行持续逻辑。</summary>
    protected virtual void Update() { }
    /// <summary>死亡、停用角色或释放实例时调用一次，用于清理持续状态。</summary>
    protected virtual void OnDisable() { }

    internal void Bind(CharacterLib owner)
    {
        if (Owner != null || released)
            throw new InvalidOperationException($"被动技能 '{Id}' 已被使用，CreatePassiveSkills 必须返回新实例。");
        Owner = owner;
    }

    private void Enable()
    {
        if (released || IsEnabled) return;
        IsEnabled = true;
        OnEnable();
    }

    internal void ApplyForPlayer()
    {
        Enable();
        if (!released && IsEnabled) ApplyEffects();
    }

    internal void UpdateForPlayer()
    {
        Enable();
        if (!released && IsEnabled) Update();
    }

    internal void Disable()
    {
        if (!IsEnabled) return;
        IsEnabled = false;
        OnDisable();
    }

    internal void Release()
    {
        released = true;
        Disable();
    }
}
