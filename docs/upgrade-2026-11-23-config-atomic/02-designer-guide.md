# 设计师 / 配置原子重载指南（2026-11-23 Cycle 13）

## 问题

CounterStrikeSharp `PluginConfigExtensions.Reload<T>` 源码：

1. 完整 `JsonSerializer.Deserialize<T>`；
2. 成功后对每个可写属性 `property.SetValue(config, newValue)`。

类型错误在第 1 步就失败，所以**不会**部分更新。但第 2 步没有回滚：如果某个 setter 在复制中途抛异常，前面的属性已经改了，后面的没改，内存配置会处于半套状态。

## 设计决策

1. **保留对象身份**：不替换 `Config` 引用，继续修改 CSS 已持有的对象，避免 CSS 内部引用漂移。
2. **先快照、再应用**：`ConfigApplier.Apply` 先记录所有可写属性的旧值，再逐个写入。
3. **失败回滚**：任何 setter 抛异常，按已写入顺序恢复旧值，再把原始异常抛给 `ReloadConfigFromDisk` 的 catch。
4. **只对 JSON 启用**：JSON 走 `ConfigFileValidator` + `ConfigApplier`；TOML 仍走 CSS Reload，避免维护第二套 TOML 解析器。
5. **异常解包**：`PropertyInfo.SetValue` 会包一层 `TargetInvocationException`；回滚后抛出 inner exception，错误信息更可读。

## 外部知识依据

- CSS `PluginConfigExtensions.Reload` 源码：完整反序列化后逐属性复制，无回滚。
- 事务式配置应用：先快照，再应用，失败恢复；保持对象身份比替换引用更安全。
- 输出错误时保留原始异常信息，避免只看到“TargetInvocationException”。

## 不做

- 不替代 TOML Reload。
- 不改配置 Key、默认值、Dice 数值。
- 不做配置版本迁移。
- 不改 `!rtd reload` 命令语义。

## 验收

- `ConfigApplier` 正常复制。
- 第二个属性 setter 抛异常时，第一个属性回滚。
- JSON 候选值能应用到 `PluginConfig`。
- 旧配置在失败后继续生效。
