# 设计师 / 配置对齐指南（2026-11-23 Cycle 9）

## 问题

上一轮的 `ConfigFileValidator` 使用了自定义宽松选项：

- `PropertyNameCaseInsensitive = true`
- `AllowTrailingCommas = true`

但 CounterStrikeSharp 的 `ConfigManager.JsonSerializerOptions` 只有：

```csharp
WriteIndented = true,
ReadCommentHandling = JsonCommentHandling.Skip
```

也就是说 CSS **不允许尾逗号、不做大小写不敏感匹配**。预检查比 CSS 宽松，会出现“检查通过、Reload 仍失败”的错位。

另外，插件自己拼的配置路径使用 `ModuleDirectory` 的目录名；CSS 的 `GetConfigPath()` 使用程序集名。插件目录改名后两者可能指向不同文件。

## 设计决策

1. **单一事实源**：优先直接使用 `PluginConfigExtensions.JsonSerializerOptions`；在非 CSS 宿主（测试/CI）静态初始化失败时，回退到与 CSS 当前源码一致的最小选项。
2. **单一路径**：`ReloadConfigFromDisk` 与 `ReadMapDicesJson` 都改用 `Config.GetConfigPath()`，与 CSS Reload 完全一致。
3. **TOML 不误判**：只有 `.json` 才做 JSON 预检查；`.toml` 交给 CSS，不强行用 JSON 解析。
4. **纠正认知**：CSS `Reload` 是“先完整反序列化，再逐属性复制”，所以类型错误本来就不会部分更新；上一轮的预检查是更早的诊断，不是修复了部分更新。真正剩余的风险是属性 setter 在复制中途抛异常。

## 外部知识依据

- `PluginConfigExtensions.Reload<T>` 源码：完整 `JsonSerializer.Deserialize` 后才 `property.SetValue`。
- `ConfigManager.JsonSerializerOptions`：仅 `WriteIndented` + `ReadCommentHandling.Skip`。
- `PluginConfigExtensions.GetConfigPath<T>`：按程序集名拼接 `configs/plugins/<AssemblyName>/<AssemblyName>.json`。

## 不做

- 不替代 CSS Reload 的属性复制流程。
- 不改配置 Key、默认值、Dice 数值。
- 不为 TOML 写第二套解析器。
- 不做配置迁移/版本升级逻辑。

## 验收

- 预检查行为与 CSS 选项一致：注释可过、尾逗号拒绝、类型错误拒绝。
- 预检查路径与 `Config.GetConfigPath()` 一致。
- 真实部署配置通过。
