# 工程师 / 配置对齐实施指南（2026-11-23 Cycle 9）

## 1. `ConfigFileValidator`

```csharp
private static JsonSerializerOptions? _options;
private static JsonSerializerOptions Options
{
    get
    {
        if (_options != null) return _options;
        try { _options = PluginConfigExtensions.JsonSerializerOptions; }
        catch
        {
            _options = new JsonSerializerOptions
            {
                WriteIndented = true,
                ReadCommentHandling = JsonCommentHandling.Skip
            };
        }
        return _options;
    }
}
```

原因：

- 生产环境直接复用 CSS 选项，避免漂移。
- 测试/CI 没有 CSS 宿主，`ConfigManager` 静态构造会失败，回退到与 CSS 源码一致的最小选项。
- 回退只影响预检查，不影响 CSS Reload。

## 2. 路径统一

`ReloadConfigFromDisk`：

```csharp
string path = Config.GetConfigPath();
if (File.Exists(path) && path.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
{
    string json = File.ReadAllText(path);
    if (!ConfigFileValidator.TryDeserialize(json, out _, out string configError))
    {
        throw new InvalidDataException(configError);
    }
}
PluginConfigExtensions.Reload<PluginConfig>(Config);
```

`ReadMapDicesJson` 同样使用 `Config.GetConfigPath()`，并在非 `.json` 时返回 null。

删除旧的 `PluginConfigPath()`，避免目录名与程序集名漂移。

## 3. 测试

新增/更新：

- 注释 JSON 通过。
- 尾逗号 JSON **拒绝**（与 CSS 一致）。
- 类型错误拒绝。
- 未知字段忽略。
- 真实部署配置通过（`RTD_CONFIG_PATH`）。

标准 46 项；带真实配置 47 项。

## 4. 验证

```powershell
dotnet build RollTheDice.csproj -c Release --no-incremental
dotnet run --project tests/RollTheDice.ConfigTests -c Release
dotnet run --project tests/RollTheDice.RegistryTests -c Release
powershell -NoProfile -ExecutionPolicy Bypass -File "D:\Works\CS2\scripts\check-rtd-architecture.ps1"
```

预期：0 warning/0 error；32/32；46/46；timers=0；主类 ≤2900。
