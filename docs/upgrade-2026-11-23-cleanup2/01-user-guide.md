# 用户 / 管理员二次收尾指南（2026-11-23 Cycle 15）

## 目标

把上一轮明确留下的“代码级尾巴”继续收掉：实体所有权 Dice 的断线清理、主类职责拆分。仍然不做没有实机依据的性能索引。

## 你会看到的变化

- Fate、Excalibur、FinalJudgment、MagneticPulse、Necromancer、ReverseCausality 在玩家断线/失效时会清理自己的玩家状态与实体：
  - Fate：罗盘光晕、黎明状态、移动作用域。
  - Excalibur：光剑、光带、冲击环、光晕、减速。
  - FinalJudgment：穹顶/天空法阵/光柱/冲击环/待引爆状态。
  - MagneticPulse：伤害窗口、冷却、减速。
  - Necromancer：复活无敌、死亡位置、复活记录。
  - ReverseCausality：延迟伤害、双倍伤害窗口。
- `AnnounceDiceRarity`/`BroadcastDiceSound` 移到独立 partial 文件，主类从 2730 行降到 2659 行。
- 玩法数值、播报内容、TAB、Combo、配置均不变。

## 不会变化

- Dice 概率、Combo 效果、持续时间、冷却、伤害、奖励。
- 配置 Key、命令、播报文案、音效开关。
- `!rtd status`、`!rtd list`、`!rtd tab`。
- 断线清理仍跳过“没有专用清理的实体 map”，避免误删实体。

## 验收

- 六种实体所有权 Dice 断线后不再保留对应 map/实体引用。
- `!rtd_health` 的移动锁计数正常。
- 主类行数下降，构建/测试/架构检查通过。
- 未实机验证前，不宣称“实体零残留”。
