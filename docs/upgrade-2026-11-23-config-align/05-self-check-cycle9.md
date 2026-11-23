# Cycle 9 自检报告（v1.4.12）

## 用户视角

- 配置预检查与 CSS 实际重载使用同一文件、同一套 JSON 规则。
- `//` 注释仍然可用；尾逗号会被预检查拒绝（CSS 也不允许）。
- 预检查失败时旧配置保持生效，聊天/控制台提示错误。
- `.toml` 配置不会被 JSON 预检查误判，仍由 CSS 处理。
- 插件目录改名不会让预检查读错文件。
- 配置 Key、Dice 数值、命令均未改。
- 尚未实机验证；需要重点回归 `!rtd reload`、注释/尾逗号/类型错误、地图 overlay。

## 设计师视角

- 回归“单一事实源”：JSON 选项优先复用 CSS，路径复用 `Config.GetConfigPath()`。
- 非 CSS 宿主有同值回退，测试/CI 不会因 `ConfigManager` 静态初始化失败而误报。
- 纠正上一轮认知：CSS Reload 先完整反序列化再复制属性，类型错误本来就不会部分更新；预检查是更早的诊断。
- 剩余风险收窄为“属性 setter 在复制中途抛异常”。
- 没有为 TOML 写第二套解析器，没有改玩法数值。

## 工程师视角

- `ConfigFileValidator`：优先 `PluginConfigExtensions.JsonSerializerOptions`，异常时回退 `WriteIndented + ReadCommentHandling.Skip`（与 CSS 源码一致）。
- `ReloadConfigFromDisk`：`Config.GetConfigPath()` + `.json` 后缀判断 + 预检查 + CSS Reload。
- `ReadMapDicesJson`：同一路径；非 `.json` 返回 null，走旧模型 merge。
- 删除手拼的 `PluginConfigPath()`。
- 构建：0 warning / 0 error。
- 配置测试：32/32。
- Registry/UI/Lifecycle/Movement：标准 46/46；真实部署配置 47/47。
- 架构检查：`timers=0`，主类 2709/2900，通过。
- 未部署、未实机；CI 将在提交后运行。

## 外部知识核对

- `PluginConfigExtensions.Reload<T>`：完整 `JsonSerializer.Deserialize` 成功后才逐属性 `SetValue`。
- `ConfigManager.JsonSerializerOptions`：`WriteIndented=true`、`ReadCommentHandling=Skip`，没有大小写不敏感/尾逗号。
- `PluginConfigExtensions.GetConfigPath<T>`：按程序集名拼接 `configs/plugins/<AssemblyName>/<AssemblyName>.json`。

## 结论

v1.4.12 是配置预检查对齐版本，构建、配置测试、46/47 项断言、架构检查均通过。仍未达到“无 Bug”，剩余明确风险：

1. CSS Reload 的属性复制阶段若 setter 抛异常，仍可能部分更新。
2. `Fate` 仍直接写 MoveType（dawn/darktide）。
3. `Dictionary<ulong, T>` 状态不做自动清理。
4. «超越天堂» Combo 显示名重复。
5. 高频 Dice 扫描与伤害 wrapper 性能。
6. 主类仍有职责可拆。
7. 所有改动尚未实机验证。
