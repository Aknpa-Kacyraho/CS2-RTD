# 工程师 / 功能实施指南（2026-11-23 Cycle 5）

## 新增命令分支

`CommandRollTheDice` 在 `status` 之后、`tab` 之前加入：

```csharp
if (arg.Equals("list", StringComparison.OrdinalIgnoreCase))
{
    PrintDiceList(player, command, (command.GetArg(2) ?? string.Empty).Trim());
    return;
}
```

`CommandHelper(0, "")` 不变；`!rtd list` 不会落入 unknown/roll 分支。

## 新增方法

`RollTheDice.Help.cs`：

```csharp
private void PrintDiceList(CCSPlayerController player, CommandInfo command, string filter);
```

## `DiceUi` 扩展

```csharp
public static string? NormalizeTier(string filter);
public static bool MatchesFilter(DiceBlueprint dice, IStringLocalizer localizer, string filter);
public static string DiceNameMarked(DiceBlueprint dice, IStringLocalizer localizer, bool held);
```

- `NormalizeTier`：`传说/legendary/leg` → `legendary`，其余同理；未知返回 null。
- `MatchesFilter`：中文名或 `ClassName` 包含关键词，`OrdinalIgnoreCase`。
- `DiceNameMarked`：持有则追加绿色 `✓`。

## 输出规则

- 空 filter：总数 + 各 tier 数量 + 用法。
- tier filter：该 tier 全部名称，4 列，最多 24 个；超出提示剩余数量。
- 关键词：`DiceUi.DiceLine`，最多 8 条；超出提示。
- 无匹配：`command.rtd.list.none`。
- 所有文本走 `lang/en.json` 新 key；颜色标签需通过 `ReplaceColorTags` 验证。

## 新增 key

- `command.rtd.list.title`
- `command.rtd.list.tiers`
- `command.rtd.list.usage`
- `command.rtd.list.tier`
- `command.rtd.list.truncated`
- `command.rtd.list.none`
- `command.rtd.help.list`

## 验证

```powershell
dotnet build RollTheDice.csproj -c Release --no-incremental
dotnet run --project tests/RollTheDice.ConfigTests -c Release
dotnet run --project tests/RollTheDice.RegistryTests -c Release
powershell -NoProfile -ExecutionPolicy Bypass -File "D:\Works\CS2\scripts\check-rtd-architecture.ps1"
```

Registry 测试新增：tier 别名归一化、关键词匹配、持有标记、列表截断、颜色标签替换。
