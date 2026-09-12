# RollTheDice 新 dice 第二批：命中部位 / 蹲伏 / 弹药（6 个）设计

日期：2026-09-12
状态：设计已获用户批准（含数值与实现取舍），进入实现

## 目标

新增 6 个 dice，全部基于**此前从未使用的引擎信号**：

- 命中部位 `CTakeDamageInfo.GetHitGroup()` → `HitGroup_t`（`HITGROUP_HEAD=1`…）：解剖学家 / 铁头功 / 斩首
- 按键 `PlayerButtons.Duck`：蹲伏
- 弹药 `CBasePlayerWeapon.Clip1` / `CCSWeaponBase.InReload`：最后一发 / 换弹窗口

## buff 接入分类（重要）

| dice | 类型 | 落地方式 |
|------|------|----------|
| Anatomist 解剖学家 | per-hit 命中条件 | **本地** `info.Damage` 乘算（AGENTS 记录的"条件类"例外；不注册 manager → 不会重复放大） |
| IronHead 铁头功 | per-hit 命中条件 | 本地 `info.Damage` 乘算（99% 超过 `DamageReductionManager` 的 0.95 上限，必须本地） |
| Guillotine 斩首 | per-hit 条件 | 本地把 `info.Damage` 置致死值（走引擎伤害路径，可正常计击杀；`Server.NextFrame` 兜底 `CommitSuicide`） |
| Crouch 蹲伏 | 常驻减伤 | `DamageReductionManager`（manager）；回血直改 `Health` 封顶 `MaxHealth` |
| LastStand 最后一发 | 短窗伤害 | `DamageBonusManager`（manager），OnTick 到期 `Unregister` |
| ReloadGap 换弹窗口 | 状态减伤 + 短窗伤害 | `DamageReductionManager` + `DamageBonusManager`（manager） |

## 新增文件

- Dices：`Anatomist.cs`、`IronHead.cs`、`Guillotine.cs`、`Crouch.cs`、`LastStand.cs`、`ReloadGap.cs`
- Configs：`AnatomistConfig.cs`、`IronHeadConfig.cs`、`GuillotineConfig.cs`、`CrouchConfig.cs`、`LastStandConfig.cs`、`ReloadGapConfig.cs`
- `DicesConfig.cs` 增加 6 个同名属性（`dice 类名 == DicesConfig 属性名`）
- `lang/en.json`（源码+部署）各加 `dice_<名>_name/_player/_other/_status`（Crouch/LastStand/ReloadGap 的 `_status` 可省，但要保持完整就都加）

## 1. Anatomist 解剖学家

- `Listeners = ["OnPlayerTakeDamagePre"]`
- 持有者为 attacker、victim 为存活敌人、`info.Damage > 0`：
  - `info.GetHitGroup() == HITGROUP_HEAD` → `info.Damage *= 1 + headshot_bonus`
  - 否则 → `info.Damage *= 1 - body_penalty`
- 配置：`enabled`、`headshot_bonus=0.6`、`body_penalty=0.2`
- 说明：手雷/刀等非头部命中算"非爆头"，同样 −20%。

## 2. IronHead 铁头功

- `Listeners = ["OnPlayerTakeDamagePre"]`
- victim == 持有者、`info.Damage > 0`、`info.GetHitGroup() == HITGROUP_HEAD` → `info.Damage *= 1 - headshot_reduction`
- 配置：`enabled`、`headshot_reduction=0.99`（99% 减伤，留 1%）

## 3. Guillotine 斩首

- `Listeners = ["OnPlayerTakeDamagePre"]`
- attacker 为持有者、victim 为存活敌人、`info.Damage > 0`，且 victim `Health / MaxHealth < execute_hp_threshold`：
  - `info.Damage = 1000000f`（引擎致死、计击杀、bot 同样受引擎伤害死亡）
  - `Server.NextFrame` 兜底：若 victim 仍存活则 `Health = 0` + `SetStateChanged` + `CommitSuicide(false,true)`（try/catch）
- 配置：`enabled`、`execute_hp_threshold=0.35`

## 4. Crouch 蹲伏

- `Listeners = ["OnPlayerButtonsChanged", "OnTick"]`
- `OnPlayerButtonsChanged`：`pressed` 含 `PlayerButtons.Duck` → `DamageReductionManager.Register(player,"Crouch",damage_reduction)` 并标记蹲伏；`released` 含 Duck → `Unregister` 并取消标记
- `OnTick`：蹲伏且存活 → 每秒回 `heal_per_second`（累计小数），封顶 `MaxHealth`
- `Remove`/`Reset`：`Unregister` + 清状态
- 配置：`enabled`、`damage_reduction=0.30`、`heal_per_second=5`

## 5. LastStand 最后一发

- `Events = ["EventWeaponFire"]`；`Listeners = ["OnTick"]`
- 开火时若 `weapon` 有效且 `VData.MaxClip1 > 1`（排除刀/无弹匣）且 `weapon.Clip1 <= clip_threshold`：
  - `DamageBonusManager.Register(player,"LastStand", damage_multiplier - 1)`；`_expire[player] = now + window_seconds`
- `OnTick`：到期 `Unregister` 并清 `_expire`
- `Remove`/`Reset`：`Unregister`
- 配置：`enabled`、`damage_multiplier=3.0`、`clip_threshold=0`、`window_seconds=0.2`
- 说明：判定为"打空那一发（事件时 `Clip1==0`）"；若实测引擎给出的是 1，把 `clip_threshold` 调 1 即可。

## 6. ReloadGap 换弹窗口

- `Events = ["EventWeaponReload"]`；`Listeners = ["OnTick"]`
- 换弹开始：`DamageReductionManager.Register(player,"ReloadGap",reload_damage_reduction)`；记录武器 handle + 开始时间
- `OnTick`：
  - 若当前武器 handle 变化 → 换弹被打断：`Unregister` 减伤，取消（不给伤害加成）
  - 否则若 `!((CCSWeaponBase)weapon).InReload` 且已过 0.15s（或超时 10s）→ 换弹完成：`Unregister` 减伤；`DamageBonusManager.Register(player,"ReloadGap", post_reload_damage_bonus)` 并记录 `now + post_reload_duration`
  - 伤害加成到期 → `Unregister`
- `Remove`/`Reset`：`Unregister` 两个 buff + 清状态
- 配置：`enabled`、`reload_damage_reduction=0.80`、`post_reload_damage_bonus=0.40`、`post_reload_duration=3.0`

## 验证与交付

1. `dotnet build RollTheDice.csproj -c Release` 0 错。
2. 6 个配置类均含 `Enabled`；`DicesConfig` 属性名与 dice 类名一致。
3. `lang/en.json` 源码 + 部署两处键完整。
4. 备份 → 覆盖 `plugins\RollTheDice\RollTheDice.dll` → 服务器 `css_reload RollTheDice`。
5. 服务器 `RollTheDice.json` 自动补 6 段默认值（或手动补）。
6. 同步 `AGENTS.md`（dice 数、词条、平衡、接入分类）。
