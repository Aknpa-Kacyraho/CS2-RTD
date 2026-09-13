# 2026-09-13 新 dice「集结 Rally」+ 痛觉转化 PainConverter 重做

## 背景

- PainConverter 现以「绝对伤害 1:1 蓄痛、上限 250」计。玩家满血仅 100，护甲还会吃掉一部分 `DmgHealth`，因此实际永远填不满 250，UI 显示「40%」而爆发收益微弱，体感无用。
- 需要一个新的团队协作型 dice。选定「集结 Rally」：持有者附近有队友时给光环共享的伤害/减伤加成。

## 目标

1. PainConverter 改为按「掉血比例」蓄痛，显示与体感一致。
2. 新增 Rally：邻近队友越多，持有者与其邻近队友越强，鼓励抱团。
3. 全部走既有 `StackingBonusManager` / `DamageBonusManager` / `DamageReductionManager`，主插件 `OnPlayerTakeDamagePreCentral` 统一结算，不在 dice 内自行乘 `info.Damage`。

## 1. PainConverter 重做（改法 A）

### 蓄痛
`EventPlayerHurt` 中 `gain` 由 `damage` 改为按受害者当前最大生命的比例：

```
gain = damage / victimPawn.MaxHealth * 100f
```
- `victimPawn` 无效或 `MaxHealth <= 0` 时回退 `gain = damage`。
- 含义：掉 50% 最大生命 = 50 痛（无论其 MaxHealth 被其它 dice 改成多少）。
- Adrenaline combo 的 `<40% HP 时 gain ×2` 保留。

### 配置数值（PainConverterConfig）

| 字段 | 旧 | 新 |
|---|---|---|
| `max_pain` | 250 | 100 |
| `min_pain_to_activate` | 80 | 40 |
| `damage_per_pain` | 0.01 | 0.02 |
| `speed_per_pain` | 0.001 | 0.002 |
| `damage_pain_cap` | 200 | 100 |
| `decay_per_second` | 4 | 3 |
| `cooldown` / `burst_duration` / `burst_hp_per_second` | 20 / 5 / 15 | 不变 |

满痛（100）→ 爆发 +200% 伤害 / +20% 移速；最低 40 痛 → +80% / +8%。

### 连带：DeathKnight combo 重标定
`SyncDeathKnightReduction` 原为 `Math.Min(pain / 25f * 0.05f, 0.5f)`。pain 上限降到 100 后满痛仅 20%。改为 `Math.Min(pain / 10f * 0.05f, 0.5f)`（每 10 痛 = +5% 减伤，满痛 50%），`AnnounceCombo` 文案同步为「每 10 点痛觉转化为 5% 减伤」。

### lang
`dice_PainConverter_player` 同步新数值（源码 `lang\en.json` 与部署 `plugins\RollTheDice\lang\en.json` 两份）。

## 2. 新 dice「集结 Rally」

### 文件
- `RollTheDice.Dices\Rally.cs`（ClassName = `Rally`）
- `RollTheDice.Configs\RallyConfig.cs`（含 `Enabled`）
- `RollTheDice\DicesConfig.cs` 增加 `[JsonPropertyName("rally")] public RallyConfig Rally`
- `lang\en.json` 增加 `dice_Rally_name/_other/_player`
- `AGENTS.md` 登记

### 配置
| 字段 | 默认 |
|---|---|
| `enabled` | true |
| `radius` | 500 |
| `damage_per_ally` | 0.05 |
| `reduction_per_ally` | 0.02 |
| `max_stacks` | 5 |
| `refresh_interval` | 0.25 |

### 算法（OnTick，按 refresh_interval 节流）
对每个持有者 H（有效、存活）：
1. 统计「距 H ≤ `radius`、存活、同队、**非 bot / 非 HLTV** 的其它玩家」数量 `n`。
2. `layers = min(n, max_stacks)`；`layers == 0` 时 H 无光环。
3. `layers > 0` 时施加给：H 自己 + 上述这些邻近队友。
   - 伤害：`damage_per_ally * layers`
   - 减伤：`reduction_per_ally * layers`

### 多持有者
每个持有者使用独立 source `Rally_{SteamID}`，跨 source 由主插件求和。单持有者最高 +25% 伤害 / +10% 减伤。

### 落地与清理
- 用 `DamageBonusManager.RegisterBySteamId` / `DamageReductionManager.RegisterBySteamId` 写，`UnregisterBySteamId` 撤（受 buff 者只有 steamId 即可）。
- 维护 `(受影响玩家 steamId, 持有者 steamId) → 上次伤害/减伤值`；reconcile 时只对变化的条目写，失效条目注销。
- `Remove(holder)`：把 `Rally_{holder}` 从所有受影响玩家注销。`Reset()`：全部注销并清空簿记。对称、无泄漏。
- 断线玩家的残留条目在后续 reconcile / Reset 清理。

### 边界
- 不计 bot：bot SteamID 多为 0，会共用同一 map key 导致串 buff；且掷骰只给真人。
- 受 buff 者同样限定真实玩家。

## 非目标
- 不新增 combo（后续需要再加）。
- 不改主插件伤害结算、不动其它 dice。

## 验证
- `dotnet build` 通过。
- 部署 dll + lang 到 `$css\plugins\RollTheDice\`（先备份 dll）。
- 运行期：`css_reload RollTheDice`；Rally 与 PainConverter 在 `dices` 配置中可见、可开关。
