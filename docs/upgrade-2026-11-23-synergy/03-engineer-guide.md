# 工程师 / Dice 联动优化实施指南（2026-11-23 Cycle 10）

## 1. `HasPartner` 改造

`RollTheDice` 新增：

```csharp
public DiceBlueprint? GetDiceByName(string diceClassName);
public string GetTruePlayerName(CCSPlayerController player);
```

`DiceSynergy.HasPartner`：

```csharp
DiceBlueprint? partner = instance.GetDiceByName(partnerClassName);
if (partner == null) return false;
ulong steamId = player.SteamID;
int team = player.TeamNum;
foreach (CCSPlayerController holder in partner._players)
{
    if (holder == null || !holder.IsValid || holder.IsHLTV) continue;
    if (holder.SteamID == steamId || holder.TeamNum == team) return true;
}
return false;
```

效果：

- 不再 `Utilities.GetPlayers()` 分配全服列表。
- 不再对每个玩家重复 Dice 字典查找。
- 语义保持：自己持有或同队持有伙伴 Dice 即成立。

## 2. `RefreshCombos` 能力缓存

`DiceBlueprint`：

```csharp
public static bool HasDiceSetChangedOverrides { get; }
```

扫描一次程序集，判断是否有子类覆写 `OnDiceSetChanged(CCSPlayerController)`。

`RefreshCombos`：

```csharp
if (DiceBlueprint.HasDiceSetChangedOverrides)
{
    HashSet<ulong> affectedIds = ...;
    foreach (DiceBlueprint dice in _dices) { ... dice.OnDiceSetChanged(p); }
}
```

当前没有覆写 → 跳过 171 个空虚调用；未来有覆写 → 自动恢复。

## 3. Combo 公告真名

`DiceSynergy.AnnounceCombo`：

```csharp
string playerName = player == null
    ? "?"
    : RollTheDice.Instance?.GetTruePlayerName(player) ?? ((CBasePlayerController)player).PlayerName;
```

`GetTruePlayerName` 走 `PlayerNameService.GetTrueName`，不带 TAB 装饰。

## 4. Registry 数据校验

`DiceRegistry.CheckCombos` 新增：

- `A == B` → warning：自联动。
- 无序配对重复 → warning：重复 Combo。
- 空显示名 → warning。
- 原有：未知 Dice、重复显示名。

只读，不修改 Combo。

## 5. 测试

新增：

- Combo 表 68 条。
- 无自联动。
- 无重复无序配对。
- 已知重复显示名「超越天堂」x2。
- Registry 无新增 Combo 警告。
- `ResolveComboNames(Giant+RoyalBarrier)` 得到「钢铁要塞」。
- `HasDiceSetChangedOverrides == false`。
- `DiceSynergy.cs` 不含 `Utilities.GetPlayers`、包含 `GetTruePlayerName`。

标准 56 项；带真实配置 57 项。

## 验证

```powershell
dotnet build RollTheDice.csproj -c Release --no-incremental
dotnet run --project tests/RollTheDice.ConfigTests -c Release
dotnet run --project tests/RollTheDice.RegistryTests -c Release
powershell -NoProfile -ExecutionPolicy Bypass -File "D:\Works\CS2\scripts\check-rtd-architecture.ps1"
```

预期：0 warning/0 error；32/32；56/56；timers=0；主类 ≤2900。
