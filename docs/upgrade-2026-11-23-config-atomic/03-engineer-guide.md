# 工程师 / 配置原子重载实施指南（2026-11-23 Cycle 13）

## 1. `ConfigApplier`

新增 `RollTheDice/ConfigApplier.cs`：

```csharp
public static void Apply<T>(T target, T source) where T : class
```

流程：

1. 枚举 `typeof(T).GetProperties(Instance|Public)` 中 `CanRead && CanWrite` 且非索引器的属性。
2. 记录 `target` 每个属性的旧值。
3. 逐个 `property.SetValue(target, property.GetValue(source))`。
4. 任意异常：
   - 按已写入顺序恢复旧值；
   - 若是 `TargetInvocationException` 且有 inner，抛 inner；
   - 否则原样抛。

## 2. `ReloadConfigFromDisk`

```csharp
string path = Config.GetConfigPath();
if (File.Exists(path) && path.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
{
    string json = File.ReadAllText(path);
    if (!ConfigFileValidator.TryDeserialize(json, out PluginConfig? candidate, out string error) || candidate == null)
        throw new InvalidDataException(error);
    ConfigApplier.Apply(Config, candidate);
}
else
{
    PluginConfigExtensions.Reload<PluginConfig>(Config);
}
```

- JSON：校验 + 原子应用。
- TOML/其他：保持 CSS 原生 Reload。
- `Config.Dices/MapConfigs/PlayerConfigs` 的 null 兜底仍在应用后执行。

## 3. 测试

- `ApplyProbe`：A/B 正常复制。
- `ThrowingApplyProbe`：B setter 抛异常，A 回滚。
- JSON `{"enabled":false}` → `PluginConfig.Enabled == false`。

标准 65 项；真实配置 66 项。

## 4. 验证

```powershell
dotnet build RollTheDice.csproj -c Release --no-incremental
dotnet run --project tests/RollTheDice.ConfigTests -c Release
dotnet run --project tests/RollTheDice.RegistryTests -c Release
powershell -NoProfile -ExecutionPolicy Bypass -File "D:\Works\CS2\scripts\check-rtd-architecture.ps1"
```

预期：0 warning/0 error；32/32；65/65；timers=0；主类 ≤2900。
