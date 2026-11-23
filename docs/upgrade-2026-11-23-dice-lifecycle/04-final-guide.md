# 统一 Dice 生命周期指南（Cycle 6 / v1.4.8–v1.4.9）

## 目标

让新老 Dice 在断线、控制器失效、prune、回合结束时使用同一套清理契约，减少旧状态、光效和移动锁残留。

## 本轮范围

1. `DiceBlueprint` 增加 controller-keyed 字典/集合的通用清理兜底，按类型缓存反射字段。
2. `RemoveBySteamId`：特殊清理 → 通用兜底。
3. `PruneInvalid`：调用 `OnHolderRemoved` → 通用兜底；有效 controller 断线的 `Remove` 后也补跑 `CleanupHolderState`。
4. `Phoenix`：新增 `OnHolderRemoved`，拆光翼/光晕/还原血量/清 9 个字典；MoveType 迁移到 `MoveLockManager`。
5. `Amber`：MoveType 迁移到 `MoveLockManager`；`Remove`/`Reset`/`OnHolderRemoved` 清理冻结。
6. Registry/UI 测试增加 9 项生命周期断言（29 项总计）。
7. 版本升到 v1.4.8，更新 CHANGELOG/README/自检。

## 明确不做

- 不改概率、权重、伤害、持续时间、奖励、命令、配置 Key。
- 不自动清 `Dictionary<ulong, T>`（可能是持久状态）。
- 不迁移 Fate/Skyline/Void 的 noclip/复活 MoveType。
- 不改 TAB/Combo/`!rtd list`。
- 不做全量 `Remove` 重构。

## 验收门槛

- 构建 0 warning / 0 error。
- 配置测试 32/32；Registry/UI/Lifecycle 测试 29/29。
- 架构检查通过，`timers=0`，主类不超 2900 行。
- 静态确认 Amber/Phoenix 不再直接写 MoveType。
- 人工审查清理顺序：特殊清理先于通用兜底。

## 自检

- 用户：断线/重连是否不再残留冻结、光翼、光晕。
- 设计师：通用兜底是否只做安全的事，特殊清理是否仍显式。
- 工程师：反射缓存、异常隔离、无 ulong 误清、构建/测试/架构/CI 通过。
