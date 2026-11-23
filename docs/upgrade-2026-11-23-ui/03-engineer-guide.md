# 工程师 / UI 实施指南（2026-11-23 Cycle 4）

## 新增

`RollTheDice/DiceUi.cs`

```csharp
public static class DiceUi
{
    public static string TierLabel(string tier);
    public static char TierColor(string tier);
    public static int TierRank(string tier);
    public static string DiceName(DiceBlueprint dice, IStringLocalizer localizer);
    public static string DiceDescription(DiceBlueprint dice, IStringLocalizer localizer);
    public static string DiceListLabel(DiceBlueprint dice, IStringLocalizer localizer);
    public static string DiceLine(DiceBlueprint dice, IStringLocalizer localizer, string tier);
    public static IEnumerable<string> Columns(IEnumerable<string> items, int columns);
}
```

## 迁移点

- `RollTheDice.Help.cs`：删除本地 `TierLabel`/`TierColor`，改用 `DiceUi`。
- `RollTheDice.cs` 的 `AnnounceDiceRarity`：改用 `DiceUi.TierLabel` / `TierColor`。
- `PrintRollStatus`：按 `TierRank` 降序，标题带数量（新 key），行用 `DiceUi.DiceLine`。
- `PrintRollHelp`：保留原 key 顺序，末尾追加 `command.rtd.help.tip`。
- `CommandGiveDice` 无参数列表：`DiceUi.DiceName` + `Columns(..., 3)`，加一行数量标题（新 key）。
- `CommandHealth`：拆为 4 行，保持字段完整。

## 新增本地化 key（`lang/en.json`）

- `command.rtd.help.tip`
- `command.rtd.status.title.count`
- `command.givedice.availablecount`

所有新 key 使用现有 `{purple}` / `{green}` / `{default}` 颜色标记；CSS 的 localizer 会替换。

## 验证

```powershell
dotnet build RollTheDice.csproj -c Release --no-incremental
dotnet run --project tests/RollTheDice.ConfigTests -c Release
dotnet run --project tests/RollTheDice.RegistryTests -c Release
powershell -NoProfile -ExecutionPolicy Bypass -File "D:\Works\CS2\scripts\check-rtd-architecture.ps1"
```

补充：在 Registry 测试项目中加入 `DiceUi` 断言（稀有度映射、三列切分、名称回退）。

预期：0 warning/0 error；32/32；Registry+UI 测试全过；timers=0；主类行数不超 2900。
