# 工程师 / 生命周期实施指南（2026-11-23 Cycle 6）

## 1. DiceBlueprint 通用清理

新增：

```csharp
private static readonly ConcurrentDictionary<Type, FieldInfo[]> ControllerStateFields;
private static FieldInfo[] GetControllerStateFields(Type type);
private void CleanControllerState(ulong steamId);
private static bool ShouldRemoveController(CCSPlayerController controller, ulong steamId);
```

规则：

- 只扫描实例字段。
- 只处理 `Dictionary<CCSPlayerController, T>` 与 `HashSet<CCSPlayerController>`。
- 匹配条件：`controller == null || !controller.IsValid || controller.SteamID == steamId`。
- `Dictionary` 用非泛型 `IDictionary` 遍历并删除；`HashSet` 用 `ISet<CCSPlayerController>`。
- 字段扫描结果按类型缓存，避免每次断线反射全部字段。

调用点：

```csharp
public virtual void RemoveBySteamId(ulong steamId, DiceRemoveReason reason)
{
    RemoveBySteamId(steamId);
    try { OnHolderRemoved(steamId, reason); } catch { }
    CleanControllerState(steamId);
}
```

`PruneInvalid`：

- 先收集失效控制器。
- 从 `_players` 移除。
- 读取 SteamID（失败则 0）。
- 调用 `OnHolderRemoved(steamId, Disconnect)`（失败隔离）。
- 调用 `CleanControllerState(steamId)`。

## 2. Phoenix

- `DeactivatePhoenix`：先拆光翼、光晕，再判断控制器有效性；光晕清理不再依赖有效 Pawn。
- 新增 `RemoveState`（统一删 9 个字典）。
- 新增 `OnHolderRemoved`：合并所有字典键，按 SteamID/失效匹配，执行 `DeactivatePhoenix` + `RestoreMaxHealth` + `RemoveState`，最后 `MoveLockManager.Clear(steamId)`。
- MoveType 迁移：
  - 触发涅槃：`MoveLockManager.Lock(player, "Phoenix")`。
  - 漂浮 tick：`MoveLockManager.Lock(player, "Phoenix")`。
  - 结束：`MoveLockManager.Unlock(player, "Phoenix")`。
  - 不再直接写 `MoveType` / `m_nActualMoveType`。

## 3. Amber

- `Remove`：有效玩家则移除冻结记录并 `MoveLockManager.Unlock(player, "Amber")`，再 `base.Remove`。
- `OnHolderRemoved`：移除 ulong 冻结记录 + `MoveLockManager.Clear(steamId)`。
- `Reset`：逐个冻结玩家 `Unlock`，找不到玩家则 `Clear(steamId)`。
- `OnTick`：冻结用 `Lock`，解冻用 `Unlock`，不再直接写 MoveType。

## 验证

```powershell
dotnet build RollTheDice.csproj -c Release --no-incremental
dotnet run --project tests/RollTheDice.ConfigTests -c Release
dotnet run --project tests/RollTheDice.RegistryTests -c Release
powershell -NoProfile -ExecutionPolicy Bypass -File "D:\Works\CS2\scripts\check-rtd-architecture.ps1"
```

预期：0 warning / 0 error；32/32；28/28；timers=0；主类 ≤2900。

## 风险与回退

- 通用清理只覆盖 controller 键；ulong 键仍由各 Dice 自己处理。
- 反射字段缓存以 `GetType()` 为键；当前 171 个 Dice 都直接继承 `DiceBlueprint`。
- 回退点：`DiceBlueprint.RemoveBySteamId/PruneInvalid`、`Phoenix.cs`、`Amber.cs` 三处可独立 revert。
