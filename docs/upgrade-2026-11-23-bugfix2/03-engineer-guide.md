# 工程师 / Bug 修复实施指南（2026-11-23 Cycle 7）

## 1. 配置重载类型校验

新增 `RollTheDice/ConfigFileValidator.cs`：

```csharp
public static bool TryDeserialize(string json, out PluginConfig? config, out string error);
```

`JsonSerializerOptions`：

- `PropertyNameCaseInsensitive = true`
- `ReadCommentHandling = JsonCommentHandling.Skip`
- `AllowTrailingCommas = true`

`ReloadConfigFromDisk`：

```csharp
string json = File.ReadAllText(path);
if (!ConfigFileValidator.TryDeserialize(json, out _, out string configError))
{
    throw new InvalidDataException(configError);
}
PluginConfigExtensions.Reload<PluginConfig>(Config);
```

只做前置校验，不替换 CSS 的 Reload 流程。类型/枚举/嵌套 Dice 配置错误会在改内存前失败。

## 2. `!givedice` 0 参数

`CommandGiveDice`：

```csharp
[CommandHelper(0, "<player> [dice]")]
```

方法内已有的 `string.IsNullOrWhiteSpace(playerName)` 列表分支因此可达。权限、匹配、随机逻辑不变。

## 3. 自动抽骰空池保护

`AutoRollTick` 在遍历玩家前：

```csharp
if (GetDrawablePool().Count == 0)
{
    return;
}
```

避免先 `RemoveDiceForPlayer` 再抽不到。

## 测试

Registry/UI/Lifecycle 测试新增：

- 配置校验接受空对象。
- 接受注释/尾逗号。
- 拒绝 `"enabled": "yes"` 类型错误。
- 忽略未知字段。
- `CommandGiveDice` 的 `MinArgs == 0`。

标准运行 34 项；设置 `RTD_CONFIG_PATH` 指向真实部署配置时额外验证 1 项（35 项）。

## 验证

```powershell
dotnet build RollTheDice.csproj -c Release --no-incremental
dotnet run --project tests/RollTheDice.ConfigTests -c Release
dotnet run --project tests/RollTheDice.RegistryTests -c Release
powershell -NoProfile -ExecutionPolicy Bypass -File "D:\Works\CS2\scripts\check-rtd-architecture.ps1"
```

预期：0 warning/0 error；32/32；34/34；timers=0；主类 ≤2900。
