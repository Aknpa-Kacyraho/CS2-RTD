# 统一二次收尾指南（Cycle 15 / v1.4.18）

## 目标

继续完成上一轮列出的代码级尾巴：

1. 六个实体所有权 Dice 的断线清理；
2. 主类播报方法拆分；
3. 测试覆盖；
4. 明确性能索引/实机验证仍不做。

## 本轮范围

1. Fate `OnHolderRemoved` → `CleanupFate`。
2. Excalibur `OnHolderRemoved` → `Cancel` + `FinishBeam` + shock/cooldown。
3. FinalJudgment `OnHolderRemoved` → `ClearPending`（含 shock）+ cooldown。
4. MagneticPulse `OnHolderRemoved` → `CleanupPlayer` + slow unregister。
5. Necromancer `OnHolderRemoved` → 三个状态 map。
6. ReverseCausality `OnHolderRemoved` → 延迟伤害/双倍伤害清理。
7. `RollTheDice.Announce.cs` 拆分 `AnnounceDiceRarity`/`BroadcastDiceSound`。
8. 测试新增实体清理 override；标准 74，真实 75。
9. 版本升到 v1.4.18，更新 CHANGELOG/README/自检。

## 明确不做

- 性能索引：需要实机 profiling。
- 其他实体 map 的通用清理：必须 Dice 专用。
- 实机验证：需要服务器。
- 不改 Dice 数值、Combo、配置 Key、命令、播报文案。

## 验收门槛

- 构建 0 warning / 0 error。
- 配置测试 32/32；Registry/UI/Lifecycle/Movement/Synergy/Broadcast/Design/Config/Cleanup 74/74。
- 真实部署配置 75/75。
- 架构检查通过，`timers=0`，主类 2659/2900。
- 六个实体 Dice 均覆写 `OnHolderRemoved`。
- 主类不再包含 `AnnounceDiceRarity`/`BroadcastDiceSound`。

## 自检

- 用户：断线后实体/状态是否清，播报是否正常。
- 设计师：是否只补专用清理与拆分，没有改行为。
- 工程师：构建/测试/架构/CI、分帧拆除仍生效、partial 拆分无重复。
