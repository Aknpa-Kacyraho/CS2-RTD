# 工程师 / Dice 播报实施指南（2026-11-23 Cycle 11）

## 1. `DiceUi` 新 API

```csharp
public static string HtmlEncode(string? value)
{
    return System.Net.WebUtility.HtmlEncode(value ?? string.Empty);
}
```

## 2. `AnnounceDiceRarity`

改为：

```csharp
string localized = DiceUi.DiceName(dice, Localizer);
string label = DiceUi.TierLabel(tier);
...
string effects = DiceEffects.DescribeEffects(dice.ClassName);
string effectLine = string.IsNullOrEmpty(effects)
    ? ""
    : $"...{DiceUi.HtmlEncode(effects)}...";
string description = DiceUi.DiceDescription(dice, Localizer);
string descriptionLine = string.IsNullOrEmpty(description) || description.StartsWith("Unknown", StringComparison.Ordinal)
    ? ""
    : $"...{DiceUi.HtmlEncode(description)}...";
player.PrintToCenterHtml($"...{DiceUi.HtmlEncode(localized)}...{DiceUi.HtmlEncode(label)}...", 5);
```

传说播报：

```csharp
Server.PrintToChatAll($" ... {GetTruePlayerName(player)} ...【{localized}】！");
```

## 3. 测试

新增：

- `DiceUi.HtmlEncode("<b>&\"'")` 包含 `&lt;b&gt;`/`&amp;`，不含原始 `<b>`。
- `DiceUi.DiceDescription(Awakener)` 非空、不含 `{}`、不是 key 本身。
- `RollTheDice.cs` 包含 `DiceUi.DiceDescription`、`DiceUi.HtmlEncode`、`GetTruePlayerName(player)`。

标准 59 项；真实配置 60 项。

## 4. 验证

```powershell
dotnet build RollTheDice.csproj -c Release --no-incremental
dotnet run --project tests/RollTheDice.ConfigTests -c Release
dotnet run --project tests/RollTheDice.RegistryTests -c Release
powershell -NoProfile -ExecutionPolicy Bypass -File "D:\Works\CS2\scripts\check-rtd-architecture.ps1"
```

预期：0 warning/0 error；32/32；59/59；timers=0；主类 ≤2900。
