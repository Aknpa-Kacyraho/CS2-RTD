# 设计师 / 架构升级指南（2026-11-23）

## 设计目标

建立明确的“状态所有权”，而不是把 171 个 Dice 合并成统一基类。

## 生命周期边界

| 生命周期 | 所有者 | 必须清理 |
|---|---|---|
| Plugin | RollTheDice | Timer、Event、Listener、全局 Manager |
| Round | DiceBlueprint / 主类 | ScopedTimer、Player-scoped Buff、Combo 状态 |
| Player | DiceBlueprint / Manager | 冷却、血量快照、MoveLock、无敌、名称缓存 |
| Dice | 具体 Dice | 实体、粒子、Beam、私有字典 |
| Temporary Effect | Effects / BeamFx | 短生命周期实体 |

## 核心风险

1. **`OnHolderRemoved` 没有全量覆盖**
   - 默认只清 `_players`；
   - 14 个生命值 Dice 中仍有多个未覆写；
   - `PruneInvalid` 不触发该钩子。
   - 设计上应让“按 SteamID 清理”成为 Dice 的可选钩子，不要要求所有 Dice 改键类型。

2. **MoveType 多来源**
   - `MoveLockManager` 已记录/恢复原始 MoveType；
   - Amber/Fate/Phoenix/Skyline 仍绕过它；
   - Skyline 的 MoveType=7 是特殊飞行状态，不能简单套用锁定到 0。

3. **Timer 所有权**
   - 已统一到 `ScopedTimer` / `CancelScopedTimer`；
   - 裸 `new Timer` 已清零；
   - 仍需防止手动 Kill 后残留、回合外执行、卸载后回调。

4. **配置系统**
   - 原始 JSON overlay 已解决“显式默认值”问题；
   - fallback 仍保留旧默认值判断；
   - CSS Reload 的类型错误回滚仍不完整。

## 设计原则

- 共享**状态原语**（HealthSnapshot、CooldownTracker、TimerScope），不共享 Dice 效果逻辑。
- 共享**校验谓词**（Player/Pawn），但不隐藏 Bot/HLTV/死亡语义。
- 共享**实体原语**（BeamFx、Effects、Entities、ChildGuard），不强制所有 Dice 改视觉实现。
- 主类拆分按职责，不按行数；每次只移一个职责。

## 不建议

- 不为 55 个 `EventPlayerDeath` 建统一基类。
- 不把 14 个生命值 Dice 合成一个 `HealthDiceBase`。
- 不把 53 个伤害钩子合并成一个大方法。
- 不为了减少行数批量重写 171 个类。
