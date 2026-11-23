# Cycle 13 自检报告（v1.4.16）

## 用户视角

- JSON 配置重载现在是“全有或全无”：setter 中途失败会回滚，旧配置继续生效。
- 类型错误仍在应用前失败，不会改动配置。
- setter 错误会显示真实原因，而不是只看到 `TargetInvocationException`。
- TOML 配置仍走 CSS 原生 Reload。
- 配置 Key、Dice 数值、命令、播报、TAB、Combo 均未改。
- 尚未实机验证；需要重点回归 `!rtd reload`、坏类型、坏 setter、TOML、地图 overlay。

## 设计师视角

- CSS `Reload` 源码是“先完整反序列化，再逐属性复制”，没有回滚；类型错误本来就不会部分更新，真正的风险是复制中途 setter 抛异常。
- `ConfigApplier` 保持 Config 对象身份，先快照旧值，再应用候选，失败按已写入顺序回滚。
- 只对 JSON 启用原子应用；TOML 保持 CSS 路径，避免第二套解析器。
- 异常解包后抛出 inner exception，错误信息更可读。
- 没有改配置 Key、默认值、Dice 数值或命令。

## 工程师视角

- 新增 `RollTheDice/ConfigApplier.cs`：`Apply<T>(target, source)`。
- `ReloadConfigFromDisk`：JSON 走 `ConfigFileValidator` + `ConfigApplier`；其他走 `PluginConfigExtensions.Reload`。
- 回滚实现：记录可写属性旧值，失败恢复已应用项；`TargetInvocationException` 解包。
- 测试：
  - 正常复制；
  - 第二个 setter 抛异常，第一个属性回滚；
  - JSON 解析；
  - JSON 应用到 `PluginConfig`。
- 构建：0 warning / 0 error。
- 配置测试：32/32。
- Registry/UI/Lifecycle/Movement/Synergy/Broadcast/Design/Config：标准 65/65；真实配置 66/66。
- 架构检查：`timers=0`，主类 2721/2900，通过。
- 未部署、未实机；CI 将在提交后运行。

## 外部知识核对

- CSS `PluginConfigExtensions.Reload<T>`：完整 `JsonSerializer.Deserialize` 后逐属性 `SetValue`，无回滚。
- 事务式配置应用：快照 → 应用 → 失败恢复；保持对象身份比替换引用更安全。
- 反射 `PropertyInfo.SetValue` 会把 setter 异常包成 `TargetInvocationException`，需要解包。

## 结论

v1.4.16 是配置原子重载版本，构建、配置测试、65/66 项断言、架构检查均通过。仍未达到“无 Bug”，剩余明确风险：

1. TOML 重载仍无回滚（走 CSS 原生路径）。
2. 「超越天堂」重复显示名仍只展示一次。
3. `NotifyPlayers` 与稀有度 reveal 仍各发一次。
4. `Fate` 仍直接写 MoveType。
5. `Dictionary<ulong, T>` 状态不做自动清理。
6. 高频 Dice 扫描与伤害 wrapper 性能。
7. 主类 2721/2900，仍有职责可拆。
8. 所有改动尚未实机验证。
