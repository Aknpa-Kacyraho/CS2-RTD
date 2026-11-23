# 工程师 / 架构升级指南（2026-11-23 Cycle 2）

## 范围

新增 `RollTheDice/PlayerNameService.cs`，一次性迁移名称/TAB 逻辑；不同时改 Dice 行为、配置或 Timer 语义。

## API

```csharp
Configure(getDices, getDiceByName, getPlayerConfig, localizer, addTimer, logError)
RebuildKnownPrefixes(dices, localizer, comboNames)
ClearKnownPrefixes()
Refresh(player, scheduleReapply = true)
RestoreAll()
StopRefreshTimers()
StopRefreshTimer(ulong steamId)
Forget(CCSPlayerController? player)
ClearNames()
GetTrueName(player)
GetCachedOrCurrentName(player)
HasCachedName(player)
RefreshTimerCount
```

## 迁移点

- `InitializeModules`：Configure + RebuildKnownPrefixes
- `DestroyModules`：ClearKnownPrefixes
- `OnPlayerDisconnect`：Forget + StopRefreshTimer
- `OnMapEnd`/`Unload`：StopRefreshTimers + ClearNames
- `RemoveDicesForPlayers`：RestoreAll
- `rtdinfo`：GetTrueName/HasCachedName
- `OnPlayerHurtReveal`：GetCachedOrCurrentName
- 所有 `RefreshPlayerDiceName` 调用：`Refresh`
- `rtd_health`：RefreshTimerCount

## 验证

```powershell
dotnet build RollTheDice.csproj -c Release --no-incremental
dotnet run --project tests/RollTheDice.ConfigTests -c Release
powershell -NoProfile -ExecutionPolicy Bypass -File "D:\Works\CS2\scripts\check-rtd-architecture.ps1"
```

检查：

- 主类 2851 行必须下降。
- 无 `_originalPlayerNames`/`_nameRefreshTimers`/`_knownDicePrefixes` 残留。
- 无重复名称计算实现。
- 事件注册/注销不受影响。
