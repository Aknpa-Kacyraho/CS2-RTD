# 工程师 / 收尾清理实施指南（2026-11-23 Cycle 14）

## 1. TOML 原子重载

`ConfigFileValidator.TryDeserializeToml`：

```csharp
config = Toml.ToModel<PluginConfig>(text, options: new TomlModelOptions
{
    ConvertPropertyName = name => name
});
```

`ReloadConfigFromDisk`：

- `.json`：`TryDeserialize` + `ConfigApplier.Apply`
- `.toml`：`TryDeserializeToml` + `ConfigApplier.Apply`
- 其他：CSS Reload

## 2. Combo 改名

`DiceSynergy`：`("Izayoi", "Heaven", "月华天堂")`。

测试从“已知重复名”改为“显示名唯一”。

## 3. 播报去重

`DiceBlueprint`：

```csharp
private static int _suppressHolderNotificationDepth;
public static bool HolderNotificationsSuppressed => _suppressHolderNotificationDepth > 0;
public static void AddWithoutHolderNotification(DiceBlueprint dice, CCSPlayerController player)
{
    _suppressHolderNotificationDepth++;
    try { dice.Add(player); }
    finally { _suppressHolderNotificationDepth--; }
}
```

`NotifyPlayers` 首行检查 suppression。

`RollTheDiceForPlayer` 两处 `dice.Add` 改为 `AddWithoutHolderNotification`。

`AnnounceDiceRarity`：

- HUD 开启：聊天（稀有度）+ 中央 HUD（名称/稀有度/描述/特效）。
- HUD 关闭：聊天（稀有度）+ 聊天（描述）。

## 4. Fate MoveType

`MoveLockManager.ReleaseTo(player, key, target)`：

- 删除命名作用域；
- 若还有其它作用域，重新应用栈顶；
- 若这是最后一个，应用显式 target（Fate 用 `MOVETYPE_WALK`），而不是恢复死亡时快照。

Fate：

- 黎明冻结：`Lock(capV, "FateDawn")`
- 复活/取消/清理：`ReleaseTo(..., "FateDawn", MOVETYPE_WALK)`
- 不再直接写 `MoveType` / `m_nActualMoveType`

## 5. ulong 清理

`DiceBlueprint.GetControllerStateFields` 现在识别：

- `Dictionary<CCSPlayerController, T>`
- `HashSet<CCSPlayerController>`
- `Dictionary<ulong, T>`，T 为 primitive/enum/string/`HashSet<ulong>`
- `HashSet<ulong>`

`CleanControllerState` 对 ulong 字典删 `steamId` 键；对 ulong 集合 `Remove(steamId)`。

跳过：实体/类/复杂 tuple 值（剑、光束、光晕、延迟伤害列表等），避免通用清理丢掉唯一实体引用。

## 6. 测试

新增：

- TOML 解析/应用；
- 显示名唯一；
- Fate 源码守卫；
- ulong primitive/集合清理；
- 实体值 map 不被误清；
- Add suppression 源码守卫。

标准 73；真实配置 74。

## 验证

```powershell
dotnet build RollTheDice.csproj -c Release --no-incremental
dotnet run --project tests/RollTheDice.ConfigTests -c Release
dotnet run --project tests/RollTheDice.RegistryTests -c Release
powershell -NoProfile -ExecutionPolicy Bypass -File "D:\Works\CS2\scripts\check-rtd-architecture.ps1"
```

预期：0 warning/0 error；32/32；73/73；timers=0；主类 ≤2900。
