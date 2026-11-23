# 工程师 / Dice 设计实施指南（2026-11-23 Cycle 12）

## 1. `ResolveComboNames`

返回类型从 `HashSet<string>` 改为 `List<string>`：

```csharp
public static List<string> ResolveComboNames(HashSet<string> activeDice)
{
    HashSet<string> participating = new(StringComparer.OrdinalIgnoreCase);
    HashSet<string> comboSeen = new(StringComparer.Ordinal);
    List<string> comboNames = new();

    foreach (var (a, b, name) in _combos)
    {
        if (!activeDice.Contains(a) || !activeDice.Contains(b)) continue;
        participating.Add(a);
        participating.Add(b);
        if (comboSeen.Add(name)) comboNames.Add(name);
    }

    foreach (string dice in activeDice)
    {
        if (!participating.Contains(dice)) comboNames.Add(dice);
    }
    return comboNames;
}
```

要点：

- 不再边遍历边 `Remove`，避免共享 Dice 被第一个 Combo 消费。
- `comboSeen` 去重显示名（「超越天堂」仍只显示一次）。
- `participating` 只影响“是否保留独立 Dice”，不影响 Combo 名集合。
- 返回 `List` 保留稳定顺序；上层 `PlayerNameService` 的 `.Select().ToList()` 无需改。

## 2. 测试

新增：

- 共享 Dice：`Adrenaline+Berserker+Overheat` → 同时含「狂暴血脉」「狂热」，不含三个原 Dice。
- 独立 Dice：`Giant+RoyalBarrier+Fireball` → 含「钢铁要塞」与 `Fireball`，Combo 在前。
- 原 `Giant+RoyalBarrier` → 「钢铁要塞」。

标准 61 项；真实配置 62 项。

## 3. 验证

```powershell
dotnet build RollTheDice.csproj -c Release --no-incremental
dotnet run --project tests/RollTheDice.ConfigTests -c Release
dotnet run --project tests/RollTheDice.RegistryTests -c Release
powershell -NoProfile -ExecutionPolicy Bypass -File "D:\Works\CS2\scripts\check-rtd-architecture.ps1"
```

预期：0 warning/0 error；32/32；61/61；timers=0；主类 ≤2900。
