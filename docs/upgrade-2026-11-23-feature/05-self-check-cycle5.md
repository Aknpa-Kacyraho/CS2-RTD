# Cycle 5 自检报告（v1.4.7）

## 用户视角

- `!rtd list` 只读，不会抽骰、不会改冷却或持有状态。
- 无参数：总数 + 传说/组合/史诗/稀有/普通分布 + 用法。
- 稀有度：中文/英文别名均可；最多 24 个名字，4 列；已持有带绿色 `✓`。
- 关键词：中文名或英文类名都能搜；最多 8 条详细行；超出会提示剩余数量。
- 未知子命令仍只打印帮助；`!rtd` 无参数仍然抽骰。
- 尚未实机验证；需要看 1080p 聊天行宽、中文名截断、`✓` 是否清晰。

## 设计师视角

- 功能是“目录/检索”，不是新的抽骰入口；没有改变抽骰池或权重。
- 复用 `DiceUi` 的稀有度颜色、名称/描述和骰子行，视觉与 `!rtd status` 一致。
- 有明确上限与提示，不把 171 个 Dice 一次性刷屏。
- 不新增配置 Key、不写存档、不做菜单/分页/收藏，避免过度设计。
- 稀有度别名中英文兼容，降低玩家记忆成本。
- 已知取舍：`!rtd list 普通` 只能显示前 24 个，剩余需用关键词搜索；这是有意的刷屏保护。

## 工程师视角

- 新增 `PrintDiceList` 三分支；`CommandRollTheDice` 增加 `list` 分派，位置在 `status` 之后。
- `DiceUi` 新增 `NormalizeTier`、`MatchesFilter`、`DiceNameMarked`。
- 新增 7 个本地化 key；颜色标签经 `ReplaceColorTags` 测试，`{lightyellow}`/`{lightpurple}` 可用。
- 构建：0 warning / 0 error。
- 配置测试：32/32。
- Registry/UI 测试：20/20（新增 4 项目录相关断言）。
- 架构检查：`timers=0`，主类 2696/2900，通过。
- 未部署、未实机；CI 将在提交后运行。

## 结论

v1.4.7 是只读目录功能版本，构建、配置测试、Registry/UI 测试通过后即可发布。仍未达到“无 Bug”，既有风险不变：

1. «超越天堂» Combo 显示名重复。
2. Phoenix 断线实体/状态清理。
3. `PruneInvalid` 不触发 `OnHolderRemoved`。
4. Amber/Fate/Phoenix/Skyline 直接写 MoveType。
5. 配置 Reload 类型错误与地图 fallback。
6. 高频 Dice 扫描与伤害 wrapper 性能。
7. 主类其他职责仍可拆。

下一轮建议回到稳定性：Phoenix + `PruneInvalid` + MoveType。
