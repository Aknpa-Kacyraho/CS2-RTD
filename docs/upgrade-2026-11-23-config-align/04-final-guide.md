# 统一配置对齐指南（Cycle 9 / v1.4.12）

## 目标

修正“预检查规则/路径与 CSS Reload 不一致”的 Bug：预检查必须检查 CSS 真正会读的那个文件，并使用 CSS 真正使用的 JSON 规则。

## 本轮范围

1. `ConfigFileValidator` 优先复用 `PluginConfigExtensions.JsonSerializerOptions`；非 CSS 宿主回退到同值选项。
2. `ReloadConfigFromDisk` 与 `ReadMapDicesJson` 使用 `Config.GetConfigPath()`。
3. 只有 `.json` 做 JSON 预检查；`.toml` 交给 CSS。
4. 删除手拼的 `PluginConfigPath()`。
5. 测试增加尾逗号拒绝；标准 46 项，真实配置 47 项。
6. 版本升到 v1.4.12，更新 CHANGELOG/README/自检。

## 纠正上一轮认知

CSS `PluginConfigExtensions.Reload` 源码是：

1. 完整 `JsonSerializer.Deserialize<T>`；
2. 成功后逐属性 `SetValue`。

所以类型错误**不会**部分更新；上一轮的预检查是更早的诊断。真正剩余的窗口是属性 setter 在复制中途抛异常。本轮不替换 CSS Reload，只把预检查与 CSS 对齐。

## 明确不做

- 不替代 CSS 属性复制流程，不做自定义原子提交。
- 不为 TOML 写第二套解析器。
- 不改配置 Key、默认值、Dice 数值、命令。
- 不改移动作用域、生命周期清理、UI。

## 验收门槛

- 构建 0 warning / 0 error。
- 配置测试 32/32；Registry/UI/Lifecycle/Movement 46/46。
- 真实部署配置 47/47 通过。
- 架构检查通过，`timers=0`，主类不超 2900。
- 注释可用、尾逗号拒绝、类型错误拒绝。

## 自检

- 用户：注释仍可用，错误更早提示，旧配置保持。
- 设计师：规则/路径单一事实源，TOML 不误判。
- 工程师：CSS 选项/路径复用、回退可测、构建/测试/架构/CI 通过。
