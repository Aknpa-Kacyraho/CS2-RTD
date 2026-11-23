# 设计师 / 二次收尾指南（2026-11-23 Cycle 15）

## 本轮目标

上一轮把通用清理扩展到 controller/ulong 安全值，但**实体所有权 map** 仍故意跳过。本轮为几个明确的实体型 Dice 增加专用 `OnHolderRemoved`，并把播报方法移出主类。

## 逐个设计

### Fate

`CleanupFate(steamId)` 已能：

- 清罗盘光晕（`ClearCompassGlows`，实体真正 Remove）；
- 清 `_assignments/_orbitDeathTime/_diceLuckNextRoll/...`；
- 恢复 dawn 最大生命值；
- `ReleaseTo(..., "FateDawn", WALK)`。

新增 `OnHolderRemoved` 直接调用它，覆盖断线路径。

### Excalibur

`Cancel(sid)` 负责：

- 光剑 `RetireAll` 入分帧拆除队列；
- 清蓄力/充能/命中/释放/队伍/光晕/减速。

`FinishBeam(sid)` 负责光带与命中集合。新增 `OnHolderRemoved` 同时调用 `Cancel` + `FinishBeam`，并移除该 sid 的 `_shocks` 与冷却。

### FinalJudgment

`ClearPending(sid)` 清穹顶/天空/光柱/待引爆/燃烧戳；新增移除 `_shock[sid]`。`OnHolderRemoved` 调用 `ClearPending` + 冷却清理。

### MagneticPulse

`CleanupPlayer(sid)` 清伤害窗口/冷却/待脉冲；新增 `SpeedBonusManager.UnregisterBySteamId`。

### Necromancer

直接移除 `_invincibilityEndTime/_deathPositions/_revivedThisRound`。

### ReverseCausality

直接移除 `_delayedDamage/_doubleDamageUntil`，并 `DamageBonusManager.UnregisterBySteamId`。

## 主类拆分

新增 `RollTheDice.Announce.cs`，只放：

- `AnnounceDiceRarity`
- `BroadcastDiceSound`

原因：两者是播报职责，和主类抽骰/生命周期无强耦合；partial 共享字段，行为不变。

## 不做

- 性能索引：没有 profiling 数据，盲目索引会失效并增加维护成本。
- 其余实体 map 的通用清理：必须 Dice 专用，避免丢实体。
- 实机验证：需要服务器。

## 验收

- 六个 Dice 都有 `OnHolderRemoved`。
- 主类行数下降。
- 74/75 项断言通过。
