# 工程师 / 二次收尾实施指南（2026-11-23 Cycle 15）

## 1. 实体所有权 Dice 的 `OnHolderRemoved`

| Dice | 调用 |
|---|---|
| Fate | `CleanupFate(steamId)` |
| Excalibur | `Cancel(sid)` + `FinishBeam(sid)` + `_cooldownEnd.Remove` + `_shocks.Remove` |
| FinalJudgment | `ClearPending(sid)` + `_cooldownEnd.Remove`；`ClearPending` 增加 `_shock.Remove` |
| MagneticPulse | `CleanupPlayer(sid)` + `SpeedBonusManager.UnregisterBySteamId` |
| Necromancer | 移除 `_invincibilityEndTime/_deathPositions/_revivedThisRound` |
| ReverseCausality | 移除 `_delayedDamage/_doubleDamageUntil` + `DamageBonusManager.UnregisterBySteamId` |

注意：

- Excalibur 的光剑拆除继续走 `_fade` 分帧队列；`NeedsBackgroundTick => _fade.Any` 保证无持有者时仍能排空。
- FinalJudgment 的 `ClearPending` 直接 Remove 穹顶/天空/光柱/冲击环；这是现有死亡/离场路径，不在本轮改变拆除策略。
- Fate 的 dawn 作用域用 `ReleaseTo(..., WALK)`，不恢复死亡快照的 0。

## 2. 主类拆分

新增 `RollTheDice/RollTheDice.Announce.cs`：

- `AnnounceDiceRarity`
- `BroadcastDiceSound`

主类删除这两个方法；partial 共享字段，无需改调用点。主类 2730 → 2659。

## 3. 测试

新增：

- `entity dice cleanup overrides`：Fate/Excalibur/FinalJudgment/MagneticPulse/Necromancer/ReverseCausality 都覆写 `OnHolderRemoved`。
- 播报源码守卫改读 `RollTheDice.Announce.cs`；Add suppression 仍读主类。

标准 74；真实配置 75。

## 4. 验证

```powershell
dotnet build RollTheDice.csproj -c Release --no-incremental
dotnet run --project tests/RollTheDice.ConfigTests -c Release
dotnet run --project tests/RollTheDice.RegistryTests -c Release
powershell -NoProfile -ExecutionPolicy Bypass -File "D:\Works\CS2\scripts\check-rtd-architecture.ps1"
```

预期：0 warning/0 error；32/32；74/74；timers=0；主类 2659/2900。
