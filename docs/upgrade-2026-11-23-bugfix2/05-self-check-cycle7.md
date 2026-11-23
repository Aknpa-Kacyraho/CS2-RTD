# Cycle 7 自检报告（v1.4.10）

## 用户视角

- 配置里把 `"enabled"` 写成字符串、把数字写成文本等类型错误，重载会在改内存前失败，旧配置继续生效，并提示错误。
- `!givedice` 0 参数现在能列出可用 Dice（中文名 + 类名、三列、数量）；`!givedice <玩家> [dice]` 不变。
- 自动抽骰在没有可抽 Dice 时不再先删玩家现有 Dice。
- 抽骰概率、冷却、伤害、持续时间、奖励、命令语义均未改。
- 尚未实机验证；需要重点回归配置重载、`!givedice`、AutoRoll。

## 设计师视角

- 三处都是“先破坏、后失败”，修复遵循“先验证/预检，再执行破坏性操作”。
- 配置只做前置类型校验，不替代 CSS Reload，不引入第二套配置真相源。
- `!givedice` 只放开 0 参数，权限/匹配/随机逻辑未动。
- AutoRoll 只是空池短路，不改变替换策略、间隔或计数。
- 没有顺手改 Fate/Skyline/Void MoveType、ulong 状态清理或 Combo 重名。

## 工程师视角

- 新增 `RollTheDice/ConfigFileValidator.cs`：宽松 JSON 选项（注释、尾逗号、大小写不敏感、忽略未知字段），反序列化临时 `PluginConfig`。
- `ReloadConfigFromDisk` 在 CSS Reload 前调用校验；失败抛 `InvalidDataException`，由既有 catch 提示并保留旧配置。
- `CommandGiveDice` 的 `CommandHelper` 由 `MinArgs=1` 改为 0，列表分支可达。
- `AutoRollTick` 在遍历玩家前检查 `GetDrawablePool().Count == 0`。
- 构建：0 warning / 0 error。
- 配置测试：32/32。
- Registry/UI/Lifecycle：标准 34/34；设置 `RTD_CONFIG_PATH` 指向真实部署配置时 35/35，真实配置解析通过。
- 架构检查：`timers=0`，主类 2709/2900，通过。
- 未部署、未实机；CI 将在提交后运行。

## 结论

v1.4.10 是配置与命令 Bug 修复版本，构建、配置测试、Registry/UI/Lifecycle 断言、真实配置解析、架构检查均通过。仍未达到“无 Bug”，剩余明确风险：

1. `Fate`、`Skyline`、`Void` 仍直接写 MoveType（noclip/复活机制）。
2. `Dictionary<ulong, T>` 状态不做自动清理，仍依赖各 Dice 自己处理。
3. «超越天堂» Combo 显示名重复。
4. 配置重载只做前置类型校验；CSS Reload 自身若在应用阶段失败，仍可能留下部分更新。
5. 高频 Dice 扫描与伤害 wrapper 性能。
6. 主类 2709/2900，仍有职责可拆。
7. 所有改动尚未实机验证。
