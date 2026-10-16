using System;
using System.Collections.Generic;
using Terraria;
using Terraria.ModLoader;

namespace CharacterRely.Content.ModCharacter;

/// <summary>角色模组传给选人界面的资料。Id 在存档和联机同步中保持稳定。</summary>
public sealed class CharacterDefinition
{
    public string Id { get; }
    public string Name { get; }
    public string Description { get; }
    public string IconPath { get; }
    public Type CharacterType { get; }
    private readonly Func<CharacterLib> getTemplate;

    internal CharacterDefinition(string id, string name, string description, string iconPath,
        Type characterType, Func<CharacterLib> getTemplate)
    {
        Id = id;
        Name = name;
        Description = description;
        IconPath = iconPath;
        CharacterType = characterType;
        this.getTemplate = getTemplate;
    }

    /// <summary>只用于读取预览资源和技能说明；游戏中的角色实例始终从 Player 获取。</summary>
    public CharacterLib Template => getTemplate();

    /// <summary>取得某位玩家独立的角色实例。未选中时也可获取，但不能执行动作或技能。</summary>
    public CharacterLib GetForPlayer(Player player)
    {
        ArgumentNullException.ThrowIfNull(player);
        foreach (ModPlayer component in player.ModPlayers)
            if (component.GetType() == CharacterType) return (CharacterLib)component;
        return null;
    }
}

/// <summary>其他模组在 Load 或 PostSetupContent 中调用 Register 注入可选角色。</summary>
public static class CharacterRegistry
{
    private static readonly Dictionary<string, CharacterDefinition> byId = new(StringComparer.Ordinal);
    private static readonly Dictionary<Type, CharacterDefinition> byType = new();
    private static readonly List<CharacterDefinition> entries = new();
    private static readonly IReadOnlyList<CharacterDefinition> readOnlyEntries = entries.AsReadOnly();

    /// <summary>
    /// 注册一个角色。存档 ID 为“模组名/角色 ID”，不要随版本更改 localId。
    /// 例如：CharacterRegistry.Register&lt;MyCharacter&gt;(this, "hero", "勇者", "介绍");
    /// T 必须是该模组自动加载的 CharacterLib 子类。
    /// </summary>
    public static CharacterDefinition Register<T>(Mod owner, string localId, string name,
        string description, string iconPath = null) where T : CharacterLib
    {
        ArgumentNullException.ThrowIfNull(owner);
        ArgumentException.ThrowIfNullOrWhiteSpace(localId);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(description);
        if (localId.Contains('/')) throw new ArgumentException("角色 ID 不能包含 /。", nameof(localId));
        string id = owner.Name + "/" + localId;
        if (byId.ContainsKey(id)) throw new InvalidOperationException($"角色 ID 已注册：{id}");
        if (byType.ContainsKey(typeof(T))) throw new InvalidOperationException($"角色类型已注册：{typeof(T).FullName}");

        var definition = new CharacterDefinition(id, name, description, iconPath, typeof(T),
            () => ModContent.GetInstance<T>());
        byId.Add(id, definition);
        byType.Add(typeof(T), definition);
        entries.Add(definition);
        return definition;
    }

    /// <summary>按注册顺序列出的可选角色。</summary>
    public static IReadOnlyList<CharacterDefinition> Entries => readOnlyEntries;

    public static bool TryGet(string id, out CharacterDefinition definition)
        => byId.TryGetValue(id ?? string.Empty, out definition);

    internal static bool TryGet(Type type, out CharacterDefinition definition)
        => byType.TryGetValue(type, out definition);

    internal static void Clear()
    {
        byId.Clear();
        byType.Clear();
        entries.Clear();
    }
}
