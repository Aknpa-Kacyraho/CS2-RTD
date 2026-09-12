# RollTheDice 新 dice：Combo（连击）+ PainConverter（痛觉转化）设计

日期：2026-09-12
状态：设计已获用户批准（含具体数值），进入实现

## 目标

新增两个 dice，机制上填补现有 158 个 dice 的空白：

1. **Combo 连击**——唯一以"玩家命中链"驱动的滚雪球 dice（现有 Overheat 按时间叠层、ThunderChain 按击杀连锁，均非命中连击）。
2. **PainConverter 痛觉转化**——把"受到的伤害"变成可积累/可主动释放的资源（现有 Adrenaline 是血线自动触发、DeathKnight 是血线被动减伤，均非资源槽）。

两个 dice 一律通过统一 buff 管理器注册加成（`DamageBonusManager` / `SpeedBonusManager`），不在 dice 内直接乘 `info.Damage`；加成的实际应用在 `RollTheDice.cs` 的 `OnPlayerTakeDamagePreCentral` 统一结算。

## 关键技术前提（已核实）

- `EventPlayerHurt`（`GameEvent`）可在命中/受伤后读取 `Attacker`、`Userid`、`Damage`。现有 Vampire/Cutter/PoisonBlade 等已使用。
- E 能力：`Listeners` 声明 `OnPlayerButtonsChanged`，判断 `pressed` 含 `PlayerButtons.Use`(32)；`GetCooldownRemaining` 供 UI 显示冷却。
- `DamageBonusManager.Register(player, source, percentage, cap?)` / `SpeedBonusManager.Register(player, source, percentage)`；`Unregister` 撤销。伤害加成按 `info.Damage *= 1 + total`，减伤按 `*= 1 - total`（减伤封顶 0.95）。
- 移速需在 `OnTick` 持续写 `VelocityModifier = 1f + SpeedBonusManager.GetEffective(player, 100f)` 并 `Utilities.SetStateChanged(...)`（参照 Karma）。
- 发光：`GlowUtil.CreateGlow(CBaseEntity, Color)` 返回 `(proxy, glow)` 两实体，`GlowUtil.RemoveGlow(proxy, glow)` 移除。
- 配置挂在 `RollTheDice\DicesConfig.cs`（`[JsonPropertyName]` + `new XxxConfig()`）；dice 由主插件反射注册，新增类即可。

## A. Combo 连击

**新增文件**
- `RollTheDice.Dices/Combo.cs`
- `RollTheDice.Configs/ComboConfig.cs`

**注册**
- `DicesConfig.cs` 增加 `[JsonPropertyName("combo")] public ComboConfig Combo { get; set; } = new ComboConfig();`
- `lang/en.json`（源码与部署两处）增加 `dice_Combo_name/_player/_other/_status`

**状态**（每个持有者独立）
- `Dictionary<ulong, int> _stacks`
- `Dictionary<ulong, float> _expireAt`（当前连击到期时间）
- `Dictionary<ulong, float> _lastHit`（上次计数命中时间，用于 0.1s 去抖）
- `Dictionary<CCSPlayerController, List<(CDynamicProp? proxy, CDynamicProp? glow, float expireAt)>>`（标记发光，按目标追踪）

**声明**
- `Events = ["EventPlayerHurt"]`
- `Listeners = ["OnTick"]`

**命中计数（EventPlayerHurt）**
1. `Attacker == 持有者` 且持有者在 `_players`。
2. victim 有效、存活（`LifeState == 0`）、`victim != attacker`、`victim.TeamNum != attacker.TeamNum`。
3. `event.Damage > 0`。
4. 去抖：`Server.CurrentTime - _lastHit < min_hit_interval(0.1)` → 忽略（防霰弹/连喷一次多计）。
5. `_stacks = min(_stacks + 1, max_stacks)`；`_lastHit = now`；`_expireAt = now + window_seconds`。
6. 重算收益：
   - `DamageBonusManager.Register(p, "Combo", _stacks * damage_per_stack, max_stacks * damage_per_stack)`
   - 若 `_stacks >= heal_threshold`：持有者命中回复 `heal_per_hit`（`Health = min(Health + heal, MaxHealth)`，回血不致死，无需 `CommitSuicide`）。
   - 若 `_stacks >= max_stacks`：`SpeedBonusManager.Register(p, "Combo", full_stack_speed)`；否则 `Unregister`。
   - 对 victim 挂发光 `glow_seconds`（橙红色）：已有则刷新计时，否则 `GlowUtil.CreateGlow` 并记录；到期移除。
7. `NotifyStatus` 显示 `连击 xN | +X%`（限流≤每 2s）。

**断链（OnTick）**
- `now > _expireAt` → 清零 `_stacks`、`DamageBonusManager.Unregister(p,"Combo")`、`SpeedBonusManager.Unregister(p,"Combo")`，还原 `VelocityModifier`。
- 同步所有持有者 `VelocityModifier = 1f + SpeedBonusManager.GetEffective(p,100f)`。
- 处理到期发光（移除实体）。
- HUD 限流刷新。

**清理**
- `Remove`：按 SteamID 清状态、`Unregister`、还原移速、移除该玩家所有发光。
- `Reset` / `Destroy`：遍历 `ToList()` 全清并移除所有发光。

**配置 `ComboConfig`**（JSON 键名见括号）
- `enabled`(enabled)=true
- `window_seconds`(window_seconds)=3f
- `damage_per_stack`(damage_per_stack)=0.10f
- `max_stacks`(max_stacks)=10
- `heal_threshold`(heal_threshold)=5
- `heal_per_hit`(heal_per_hit)=5
- `full_stack_speed`(full_stack_speed)=0.15f
- `glow_seconds`(glow_seconds)=5f
- `min_hit_interval`(min_hit_interval)=0.1f

## B. PainConverter 痛觉转化

**新增文件**
- `RollTheDice.Dices/PainConverter.cs`
- `RollTheDice.Configs/PainConverterConfig.cs`

**注册**
- `DicesConfig.cs` 增加 `[JsonPropertyName("pain_converter")] public PainConverterConfig PainConverter { get; set; } = new PainConverterConfig();`
- `lang/en.json`（源码与部署两处）增加 `dice_PainConverter_name/_player/_other/_status`

**状态**
- `Dictionary<ulong, float> _pain`
- `Dictionary<CCSPlayerController, float> _burstEnd`（爆发结束时间，0=未爆发）
- `Dictionary<CCSPlayerController, float> _cooldowns`

**声明**
- `Events = ["EventPlayerHurt"]`
- `Listeners = ["OnPlayerButtonsChanged", "OnTick"]`

**蓄能（EventPlayerHurt）**
1. `Userid == 持有者` 且持有者在 `_players`。
2. attacker 有效、非空、`!= victim`、`attacker.TeamNum != victim.TeamNum`（无 attacker 的摔伤/世界伤害自然排除）。
3. `event.Damage > 0` → `_pain = min(_pain + Damage, max_pain)`。

**主动（OnPlayerButtonsChanged，Use=32）**
- 需 `pressed` 含 Use、持有者存活、`GetCooldownRemaining == 0`、`_pain >= min_pain_to_activate`。
- 触发：
  - `float amount = min(_pain, damage_pain_cap)`（pain 上限截断）
  - `_pain = 0`
  - `_cooldowns[p] = now + cooldown`
  - `_burstEnd[p] = now + burst_duration`
  - `DamageBonusManager.Register(p, "PainConverter", amount * damage_per_pain)`
  - `SpeedBonusManager.Register(p, "PainConverter", amount * speed_per_pain)`
  - `NotifyStatus` 播报爆发

**持续（OnTick）**
- 衰减：`_pain = max(0, _pain - decay_per_second * dt)`（未爆发也衰减）。
- 爆发中：`Health = min(Health + burst_hp_per_second * dt, MaxHealth)`（累计小数）；同步 `VelocityModifier`。
- 到期 `now > _burstEnd`：`DamageBonusManager.Unregister`、`SpeedBonusManager.Unregister`、还原移速、清 `_burstEnd`。
- HUD：`NotifyStatus` 显示 `痛觉 NN%`（限流≤每 2s，仅 pain>0 或爆发中）。

**清理**
- `Remove`：清该玩家 `_pain/_burstEnd/_cooldowns`、`Unregister` 两个加成、还原移速。
- `Reset` / `Destroy`：遍历 `ToList()` 全清并还原。

**配置 `PainConverterConfig`**
- `enabled`(enabled)=true
- `max_pain`(max_pain)=250f
- `decay_per_second`(decay_per_second)=4f
- `min_pain_to_activate`(min_pain_to_activate)=80f
- `cooldown`(cooldown)=20f
- `burst_duration`(burst_duration)=5f
- `damage_per_pain`(damage_per_pain)=0.01f
- `damage_pain_cap`(damage_pain_cap)=200f
- `speed_per_pain`(speed_per_pain)=0.001f
- `burst_hp_per_second`(burst_hp_per_second)=15f

## C. 平衡说明

- Combo 满层 +100% 伤害但断链即清零，属高风险滚雪球。
- PainConverter 在 pain=200 时爆发为 **+200% 伤害 / +20% 移速 / 15HP·s⁻¹ 回血**，5s 窗口、20s CD，属"强势窗口"。数值全部可配，实测偏强优先调 `damage_per_pain`。

## D. 验证与交付

1. `dotnet build RollTheDice.csproj -c Release` 0 错（锁 `1.0.373`/net10）。
2. lang 键完整：`dice_Combo_*`、`dice_PainConverter_*`（源码 + 部署 `plugins\RollTheDice\lang\en.json`）。
3. 部署：备份 `plugins\RollTheDice\RollTheDice.dll` → 覆盖 dll（dll 名须=目录名）→ `css_reload RollTheDice`（或重启）。
4. 服务器配置 `configs\plugins\RollTheDice\RollTheDice.json` 会自动补 `combo` / `pain_converter` 段（默认值）。
5. 本机 git 不能直推 github.com:443；如需推送走 GitHub API。
