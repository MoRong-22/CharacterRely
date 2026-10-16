using System;

namespace CharacterRely.Content.SkillSystem;

/// <summary>技能的只读说明与配置。读取信息不需要玩家、Spine 播放器或贴图加载。</summary>
public sealed class SkillInfo
{
    /// <summary>角色内唯一、区分大小写的稳定 ID，用于代码调用。</summary>
    public string Id { get; }
    /// <summary>显示名称。</summary>
    public string Name { get; }
    /// <summary>技能描述。</summary>
    public string Description { get; }
    /// <summary>可选图标路径，例如 MyMod/Assets/Skills/Slash，不含扩展名。</summary>
    public string IconPath { get; }
    /// <summary>恢复一次充能的游戏帧数，60 帧约一秒；0 表示无冷却。</summary>
    public int CooldownTicks { get; }
    /// <summary>持续帧数，0 为即时技能；持续技能到期自动调用 OnEnd。</summary>
    public int DurationTicks { get; }
    /// <summary>最大充能次数。</summary>
    public int MaxCount { get; }

    /// <summary>只定义数据，不执行技能；名称和说明由角色模组提供。</summary>
    public SkillInfo(string id, string name, string description, string iconPath = null,
        int cooldownTicks = 0, int durationTicks = 0, int maxCount = 1)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(description);
        ArgumentOutOfRangeException.ThrowIfNegative(cooldownTicks);
        ArgumentOutOfRangeException.ThrowIfNegative(durationTicks);
        ArgumentOutOfRangeException.ThrowIfLessThan(maxCount, 1);
        Id = id;
        Name = name;
        Description = description;
        IconPath = iconPath;
        CooldownTicks = cooldownTicks;
        DurationTicks = durationTicks;
        MaxCount = maxCount;
    }
}
