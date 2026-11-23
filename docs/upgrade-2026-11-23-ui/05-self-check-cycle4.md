# Cycle 4 自检报告（v1.4.6）

## 用户视角

- `!rtd help` 多一行提示，原命令顺序与含义不变。
- `!rtd status` 现在先显示数量，再按稀有度排序；每行仍是“名称 [稀有度] — 效果”。
- `!givedice` 无目标时从 171 行英文类名改为三列“中文名（类名）” + 数量标题。
- `!rtd_health` 拆成四行，管理员更容易看。
- 抽骰结果、冷却、自动抽骰、TAB 开关、概率、伤害、持续时间均未改。
- 尚未实机验证；需要看 1080p 下三列列表与状态行是否折行。

## 设计师视角

- 稀有度颜色完全沿用 v1.4.5：普通 1、稀有 4、史诗 3、传说 9、组合 2。
- `DiceUi` 只负责命令面板，不进入 Dice 运行路径；没有把 UI 状态写进配置。
- 没有新增命令，没有改 `!rtd` 子命令解析，没有重构 335 处 Dice 内提示。
- 已知取舍：`!givedice` 列表仍有约 57 行，三列只是降低高度；真正搜索/筛选留给功能轮。
- `!rtd status` 的排序是展示顺序，不影响抽骰或持有状态。

## 工程师视角

- 新增 `RollTheDice/DiceUi.cs`；`RollTheDice.Help.cs` 删除重复的 TierLabel/TierColor。
- 新增 3 个本地化 key：`command.givedice.availablecount`、`command.rtd.help.tip`、`command.rtd.status.title.count`。
- 构建：0 warning / 0 error。
- 配置测试：32/32。
- Registry + UI 测试：16/16（新增 8 项 UI 断言）。
- 架构检查：`timers=0`，主类 2691/2900，通过。
- 未部署、未实机；CI 将在提交后运行。

## 结论

v1.4.6 是命令界面一致性版本，构建、配置测试、Registry/UI 测试、架构检查均通过。剩余风险仍是既有问题：

1. «超越天堂» Combo 显示名重复。
2. Phoenix 断线实体/状态清理。
3. `PruneInvalid` 不触发 `OnHolderRemoved`。
4. Amber/Fate/Phoenix/Skyline 直接写 MoveType。
5. 配置 Reload 类型错误与地图 fallback。
6. 高频 Dice 扫描与伤害 wrapper 性能。
7. 主类其他职责仍可拆。

因此仍未达到“无 Bug”；下一轮按用户要求做「功能设计」，优先只读、可回退的功能。
