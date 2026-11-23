# 工程师 / 全面进化实施指南（2026-11-23 Cycle 8）

## 1. `MoveScopeStack`（纯逻辑）

新增 `RollTheDice.Utils/MoveScopeStack.cs`：

```csharp
public int Count;
public int? CurrentTarget;
public bool Contains(string key);
public int Push(string key, int target);
public bool Remove(string key, out int? currentTarget);
public void Clear();
```

规则：

- 新 key 追加到栈顶；已存在 key 只更新目标，不改变优先级。
- `Remove` 返回新的栈顶目标；栈空返回 null，调用方恢复原始 MoveType。
- 纯 `string/int`，不依赖 CCS，便于单测。

## 2. `MoveLockManager` 泛化

`LockState`：

```csharp
MoveScopeStack Stack;
int OriginalMoveType;
int OriginalActualMoveType;
```

API：

```csharp
Lock(player, key);                    // 兼容旧调用，目标 MOVETYPE_NONE(0)
Lock(player, key, MoveType_t target); // Skyline/Void 用 MOVETYPE_NOCLIP(7)
Unlock(player, key);
IsLocked(player);
Clear(steamId);
ClearAll();
ActiveLockCount;
LockedPlayerCount;
```

写入：

```csharp
((CBaseEntity)pawn).MoveType = target;
((CBaseEntity)pawn).ActualMoveType = target;
Utilities.SetStateChanged(pawn, "CBaseEntity", "m_MoveType", 0);
```

恢复：把 `OriginalMoveType` / `OriginalActualMoveType` 一起写回，不再固定 Walk。

## 3. Skyline / Void 迁移

- Skyline：激活/每 tick 用 `Lock(..., MOVETYPE_NOCLIP)`；`EndFlight` 用 `Unlock`；`Remove` 额外 `Unlock`，覆盖死亡时 Pawn 失效的情况。
- Void：`ActivateVoid`/每 tick 用 `Lock(..., MOVETYPE_NOCLIP)`；`DeactivateVoid` 用 `Unlock`；`Remove` 额外 `Unlock`。
- Phoenix：`Unlock` 移到 `LifeState` 判断之外，死亡/无效时也释放命名作用域。
- 不再写 `Schema.SetSchemaValue("m_nActualMoveType")`；由 `ActualMoveType` ref 属性同步。

## 4. 诊断

`!rtd_health` 第 3 行：

```text
fx=... | timers[...] | move_locks={keys}keys/{players}p
```

## 5. 测试

新增：

- `MoveScopeStack` 空栈、push、更新保持优先级、pop、未知 key、clear（6 项）。
- `MoveLockManager` 启动时 0 锁。
- 源码守卫：Amber/Phoenix/Skyline/Void 不含 `.MoveType =`（4 项）。

标准总数 45。

## 验证

```powershell
dotnet build RollTheDice.csproj -c Release --no-incremental
dotnet run --project tests/RollTheDice.ConfigTests -c Release
dotnet run --project tests/RollTheDice.RegistryTests -c Release
powershell -NoProfile -ExecutionPolicy Bypass -File "D:\Works\CS2\scripts\check-rtd-architecture.ps1"
```

预期：0 warning/0 error；32/32；45/45；timers=0；主类 ≤2900。
