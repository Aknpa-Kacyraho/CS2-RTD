# 工程师 / Dice Registry 实施指南（2026-11-23 Cycle 3）

## 新增

`RollTheDice/DiceRegistry.cs`

## API

```csharp
DiceRegistry.Build(
    IEnumerable<DiceBlueprint> dices,
    DicesConfig config,
    IStringLocalizer localizer,
    IEnumerable<(string A, string B, string ComboName)> combos)

registry.LogSummary();
registry.ErrorCount;
registry.WarningCount;
```

## 校验项

- `ClassName` 与 `GetType().Name` 不一致
- 重复 `ClassName`
- `DicesConfig` 无对应属性
- 属性无 `Enabled` 或未启用（warning，不算 error）
- `RarityConfig.DiceTier` 缺失（warning，默认 common）
- `dice_<Name>_name` / `_player` 缺失
- Combo A/B 不存在
- Combo 显示名重复
- `dicefx.json` profile 不存在（info，不报错）

## 接入

- `InitializeModules` 结束后构建。
- `DestroyModules` 置空。
- `!rtd_health` 显示 `registry=...`。
- 不改变 Dice 生命周期、配置或命令。

## 验证

```powershell
dotnet build RollTheDice.csproj -c Release --no-incremental
dotnet run --project tests/RollTheDice.ConfigTests -c Release
dotnet run --project tests/RollTheDice.RegistryTests -c Release
powershell -NoProfile -ExecutionPolicy Bypass -File "D:\Works\CS2\scripts\check-rtd-architecture.ps1"
```

预期：0 warning/0 error，配置 32/32，Registry 8/8，timers=0，主类不增长过多。
