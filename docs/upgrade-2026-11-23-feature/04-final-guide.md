# 统一功能指南（Cycle 5 / v1.4.7）

## 目标

在不改变抽骰与平衡的前提下，新增只读骰子目录 `!rtd list`，让玩家能查数量、按稀有度浏览、按关键词搜索，并看到自己是否已持有。

## 本轮范围

1. `CommandRollTheDice` 增加 `list` 子命令，位置在 `status` 之后。
2. `RollTheDice.Help.cs` 增加 `PrintDiceList` 三模式输出。
3. `DiceUi` 增加 tier 别名归一化、关键词匹配、持有标记。
4. `!rtd help` 增加 `!rtd list` 一行。
5. 新增 7 个本地化 key。
6. Registry/UI 测试增加目录相关断言。
7. 版本升到 v1.4.7，更新 CHANGELOG/README/自检。

## 明确不做

- 不新增配置 Key，不写玩家存档。
- 不改变抽骰概率、冷却、伤害、持续时间、Dice 效果。
- 不做“按名抽骰”、分页菜单、收藏、最近抽取记录。
- 不改 `!givedice`、TAB、Combo。
- 不把 171 个 Dice 一次性刷屏；必须有上限与提示。

## 验收门槛

- 构建 0 warning / 0 error。
- 配置测试 32/32；Registry/UI 测试全过。
- 架构检查通过，`timers=0`，主类不超 2900 行。
- `!rtd list`、`!rtd list 传说`、`!rtd list <关键词>` 人工走查输出符合规范。
- 未知子命令仍只打印帮助。

## 自检

- 用户：查得到、看得懂、不误触抽骰。
- 设计师：只读、有上限、与现有视觉语言一致。
- 工程师：构建/测试/架构/CI 通过，文案无硬编码，发布可复现。
