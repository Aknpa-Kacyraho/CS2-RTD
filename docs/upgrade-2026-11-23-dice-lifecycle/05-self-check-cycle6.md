# Cycle 6 自检报告（v1.4.8）

## 用户视角

- 断线/重连后，旧回合的 Phoenix 光翼/光晕、Amber 冻结更不容易残留。
- `!givedice` 替换或死亡移除 Amber 时，冻结立即解除，不再等计时结束。
- Phoenix 的涅槃时长、爆炸、444HP/护甲/AK 奖励不变；Amber 的冻结概率/时长不变。
- 尚未实机验证；需要重点看断线重连、Phoenix 10 秒结束、Amber 冻结解除、回合结束/换图。

## 设计师视角

- 新老 Dice 现在共享一条清理链路：`OnHolderRemoved`（特殊清理）→ `CleanControllerState`（controller 键兜底）。
- 只自动清 `CCSPlayerController` 键；`Dictionary<ulong, T>` 不动，避免误删可能跨会话保留的状态。
- `PruneInvalid` 与断线同权，不再只删 `_players`。
- Amber/Phoenix 迁移到 `MoveLockManager`，解决“直接写 MoveType 覆盖其他状态”的老问题；冻结/漂浮表现保持。
- 仍保留 Fate/Skyline/Void 的 noclip/复活 MoveType，因为机制不同，需单独设计快照/恢复策略。
- 没有为 171 个 Dice 批量重写 `Remove`，避免大范围行为变化。

## 工程师视角

- 新增 `DiceBlueprint.GetControllerStateFields`（按类型缓存反射字段）、`CleanControllerState`、`ShouldRemoveController`。
- `RemoveBySteamId`：特殊清理异常隔离，之后仍执行通用兜底。
- `PruneInvalid`：收集失效控制器 → 读 SteamID → `OnHolderRemoved`（隔离）→ 通用兜底。
- `Phoenix`：新增 `OnHolderRemoved`、`RemoveState`；光晕在有效性判断前拆除；MoveType 迁移到 `MoveLockManager`。
- `Amber`：`Remove`/`Reset`/`OnHolderRemoved` 清理冻结；`OnTick` 用 Lock/Unlock。
- 构建：0 warning / 0 error。
- 配置测试：32/32。
- Registry/UI/Lifecycle 测试：28/28。
- 架构检查：`timers=0`，主类 2696/2900，通过。
- 未部署、未实机；CI 将在提交后运行。

## 结论

v1.4.8 是新老 Dice 生命周期对齐版本，构建、配置测试、生命周期断言、架构检查均通过。仍未达到“无 Bug”，剩余明确风险：

1. `Fate`、`Skyline`、`Void` 仍直接写 MoveType（noclip/复活机制）。
2. `Dictionary<ulong, T>` 状态不做自动清理，仍依赖各 Dice 的 `Remove`/`OnHolderRemoved`。
3. «超越天堂» Combo 显示名重复。
4. 配置 Reload 类型错误与地图 fallback。
5. 高频 Dice 扫描与伤害 wrapper 性能。
6. 主类其他职责仍可拆。
7. 所有改动尚未实机验证。

下一轮建议：实机回归 + Fate/Skyline/Void 的 MoveType 快照方案，或配置 Reload 安全性。
