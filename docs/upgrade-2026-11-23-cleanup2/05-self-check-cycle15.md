# Cycle 15 自检报告（v1.4.18）

## 用户视角

- 六个实体所有权 Dice 断线/失效后会清理自己的玩家状态与实体：
  - Fate：罗盘光晕、黎明状态、移动作用域；
  - Excalibur：光剑、光带、冲击环、光晕、减速；
  - FinalJudgment：穹顶、天空法阵、光柱、冲击环、待引爆；
  - MagneticPulse：伤害窗口、冷却、减速；
  - Necromancer：复活无敌、死亡位置、复活记录；
  - ReverseCausality：延迟伤害、双倍伤害窗口。
- 抽到 Dice 的播报、TAB、Combo、配置均未改。
- 尚未实机验证；需要重点看断线/复活/换图后这些 Dice 的实体与状态。

## 设计师视角

- 上一轮通用清理故意跳过实体 map；本轮为明确实体型 Dice 补专用 `OnHolderRemoved`，没有做危险的“通用删实体”。
- Excalibur 光剑继续走 `_fade` 分帧拆除，`NeedsBackgroundTick` 保证无持有者时也能排空。
- FinalJudgment 的 `ClearPending` 增加 `_shock` 清理；不改变死亡时保留 pending 的既有策略。
- 主类拆分只移动播报方法，行为不变；主类 2730→2659。
- 性能索引仍未做：需要实机 profiling，盲目索引会失效。
- 其余实体 map 仍需 Dice 专用清理。

## 工程师视角

- 新增/修改：
  - `Fate.OnHolderRemoved` → `CleanupFate`。
  - `Excalibur.OnHolderRemoved` → `Cancel` + `FinishBeam` + shock/cooldown。
  - `FinalJudgment.OnHolderRemoved` → `ClearPending` + cooldown；`ClearPending` 加 `_shock`。
  - `MagneticPulse.OnHolderRemoved` → `CleanupPlayer` + slow unregister。
  - `Necromancer.OnHolderRemoved` → 三个状态 map。
  - `ReverseCausality.OnHolderRemoved` → 延迟/双倍伤害 + damage bonus。
- 新增 `RollTheDice.Announce.cs`（AnnounceDiceRarity/BroadcastDiceSound）。
- 构建：0 warning / 0 error。
- 配置测试：32/32。
- Registry/UI/Lifecycle/Movement/Synergy/Broadcast/Design/Config/Cleanup：标准 74/74；真实配置 75/75。
- 架构检查：`timers=0`，主类 2659/2900，通过。
- 未部署、未实机；CI 将在提交后运行。

## 剩余 backlog

| 项 | 状态 |
|---|---|
| TOML 原子重载 | v1.4.17 已做 |
| 重复 Combo 名 | v1.4.17 已做 |
| 重复播报 | v1.4.17 已做 |
| Fate MoveType | v1.4.17 已做 |
| ulong 安全值清理 | v1.4.17 已做 |
| 实体所有权 Dice 清理 | 本轮已做六个 |
| 主类拆分 | 本轮已做（2659） |
| 性能索引 | 未做，需 profiling |
| 其他实体 map | 未做，需 Dice 专用 |
| 实机验证 | 未做，需服务器 |

## 结论

v1.4.18 是二次收尾版本，已把上一轮明确列出的“实体 Dice 清理 + 主类拆分”做完；剩余只有需要实机 profiling 的性能索引、需要逐 Dice 设计的其他实体 map，以及无法替代的实机回归。仍未达到“无 Bug”的证明状态。
