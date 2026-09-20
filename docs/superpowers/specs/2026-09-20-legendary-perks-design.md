# 传说/史诗表现强化：音效 + 弹道 + 附赠武器换模

日期：2026-09-20
范围：RollTheDice（CS2 / CounterStrikeSharp 1.0.373 / net10.0）
关联：双翼重做见 `2026-09-20-dice-wings-beamfx-design.md`

## 目标

给传说 / combo 档 dice 增加"排面"表现：抽到时全服专属音效、开火时的主题枪口粒子、抽到时附赠换模武器。

## ① 传说音效（抽到全服广播）

- 时机：**抽到该 dice 的瞬间**，放给**所有在线真人**。
- 粒度：19 条专属（13 传说 + 6 combo）。
- 配置：`RollTheDice.json` → `sounds.legendary_sounds`（dice 类名 → 音效路径，运行期 `sounds/....vsnd`）。
- 实现：
  - `SoundConfig.LegendarySounds` 默认表（源码 + 部署配置两处）。
  - `OnServerPrecacheResources` 遍历表内音效 `manifest.AddResource`。
  - `AnnounceDiceRarity` 末尾按类名查表 → `BroadcastDiceSound`：逐在线真人、从各自身上 `EmitSound`（无距离衰减 = 人人听清）。
- 默认映射（按主题猜，可直接改 JSON）：

| dice | 音效 |
|---|---|
| Awakener | `sounds/touhou/powerup.vsnd` |
| Cthulhu | `sounds/zr/mother_scream.vsnd` |
| Fate | `sounds/touhou/cardget.vsnd` |
| FourHorsemen | `sounds/zombies/specialspawn.vsnd` |
| God | `sounds/touhou/public/spell_call.vsnd` |
| Ragnarok | `sounds/touhou/xrole/remilia/spear_throw.vsnd` |
| WheelOfFate | `sounds/touhou/timeout.vsnd` |
| SwordSaint | `sounds/touhou/xrole/youmu/slash_glow.vsnd` |
| SkyVerdict | `sounds/touhou/public/warning.vsnd` |
| DivineDescent | `sounds/touhou/public/boon.vsnd` |
| FinalJudgment | `sounds/touhou/bullet/explode4.vsnd` |
| WolfKing | `sounds/touhou/public/wolf.vsnd` |
| World | `sounds/touhou/xrole/sakuya/the_world.vsnd` |
| DeathKnightComplete | `sounds/touhou/bullet/frost.vsnd` |
| FireDragon | `sounds/touhou/bullet/fire2.vsnd` |
| IceDragon | `sounds/touhou/bullet/ice_explode_big.vsnd` |
| Phoenix | `sounds/touhou/extend.vsnd` |
| BeyondHeaven | `sounds/touhou/public/lit_power.vsnd` |
| RadarStation | `sounds/touhou/xrole/koishi/brain_wave.vsnd` |

- **素材未试听**：映射按文件名主题猜，均在 Aknpa_packs `sounds\` 内；不满意直接改路径，不重编译。

## ② 传说/史诗弹道美化（枪口粒子，数据驱动）

- 机制复用既有 `events.fire`（`DiceEffects.OnWeaponFire` → `Effects.PlayAtCrosshair`，节流 0.12s），**不改代码**。
- 分档：
  - 13 传说 → 主题粒子。
  - 26 史诗 + 6 combo → `particles/unified_weapon_fx/uweapon_muzflsh_gen_spark.vpcf`（基础游戏）。
- 传说主题映射（`dicefx.json`）：

| dice | 粒子 |
|---|---|
| Awakener | `particles/xrole/sanae/talisman_ring.vpcf` |
| Cthulhu | `particles/xrole/suika/blackball.vpcf` |
| Fate | `particles/touhou/point/gold.vpcf` |
| FourHorsemen | `particles/xrole/suika/blackhalo.vpcf` |
| God | `particles/touhou/star/const.vpcf` |
| Ragnarok | `particles/xrole/reimilia/spear_lighting.vpcf` |
| WheelOfFate | `particles/touhou/point/gold.vpcf` |
| SwordSaint | `particles/xrole/youmu/slash_glow.vpcf` |
| SkyVerdict | `particles/xrole/reimu/talisman_explode2_tail.vpcf` |
| DivineDescent | `particles/xrole/reimu/yinyang_glow.vpcf` |
| FinalJudgment | `particles/xrole/shinki/eyedoom_laser_glow.vpcf` |
| WolfKing | `particles/xrole/aya/wind_debris.vpcf` |
| World | `particles/xrole/clown_piece/moon.vpcf` |

## ③ 传说/combo 附赠换模武器

- 时机：抽到 dice 的瞬间发放（**发放瞬间** `ChangeSubclass` 才作用于 viewmodel，见 AGENTS §3）。
- 规则：`weapon_reward.weapons` 里有映射的用主题武器；没有的从 `random_knives` / `random_grenades` **随机一件**。
- 配置：`RollTheDice.json` → `weapon_reward`。
- 主题映射：

| dice | vdata 条目 |
|---|---|
| SwordSaint | `weapon_knife_youmu_katana` |
| Ragnarok | `weapon_knife_gungair` |
| WolfKing | `weapon_knife_kagerou_claw` |
| God | `weapon_knife_miko_yubi` |
| DivineDescent | `weapon_knife_reimu_rod` |
| SkyVerdict | `weapon_sanae_signnade` |
| FinalJudgment | `weapon_magic_potion` |

- 实现：`WeaponSubclass.Give(player, entry)`（新 `RollTheDice.Utils\WeaponSubclass.cs`）：
  - 条目 → 基础武器（`BaseOf` 表，来自 vdata `_base`）：刀发 `weapon_knife`、雷按 `_base` 发对应基础雷、枪发对应枪。
  - 刀：玩家本来就有，直接对现有刀 `ChangeSubclass`，不多给一把；无刀则先发。
  - `GiveNamedItem` 后 `Server.NextWorldUpdate` 里 `AcceptInput("ChangeSubclass", weapon, weapon, entry, 0)`。
- 挂钩：`RollTheDiceForPlayer` 两条路径在 `AnnounceDiceRarity` 后调 `ApplyLegendaryWeapon`。

## 已知风险

- **刀与 WeaponPaints 抢 subclass**：WP 也会对刀 `ChangeSubclass`（切刀皮），谁后应用谁生效。dice 在抽到时应用，通常晚于开回合的 WP；仍需实机确认。
- **音效映射未试听**：主题匹配靠文件名，可能有个别不贴。
- **新增声音预缓存只在载图触发** → 让新音效生效需**换图或重启**（`css_reload` 不够）。粒子走既有按需加载。
- **重复抽到同一传说**会重复发放武器（刻意？）：枪/雷无去重，刀有"已有就不多给"守卫。
- **雷改皮目标**：`GiveNamedItem` 返回值是 `nint`（非实体），无法精确拿新句柄，按 `DesignerName` 找第一把同类；玩家已持同类雷时可能改到旧那把。

## 自检结论（独立审查 + 素材核对）

- 已核对工坊包 `3804875873` 文件树：**19 条音效、13 条传说弹道粒子、32 个武器模型/ vdata 条目全部存在**（`sounds` 1822 个、`phase2` 285 个、`scripts/weapons.vdata_c`）。
- 独立审查发现并已修复：音效未按 tier 判定（M2）、`flap:0` 无法关闭（m1）、武器发放缺 bot/HLTV 防护（m3）、`ChangeSubclass` 静默吞异常（m6）、attach+orbit 同时 wings 重复更新（m7）、空 `WingFx` 入字典（m9）、`#L/#R` 死代码（m2）。
- 未采纳：`beams.roundEnd` 被 `ClearAll` 清理（M1）—— 主题为既有行为且**当前无任何 dice 配 `beams.roundEnd`**，无影响。

## 自检清单

- [x] `dotnet build` 0 error
- [x] `dicefx.json` / `RollTheDice.json` JSON 合法、UTF-8 无 BOM（173 profiles / 19 音效 / 7 武器映射）
- [x] 工坊包内含全部音效 / 粒子 / 武器模型
- [ ] 游戏内：抽到传说听音效、开火见枪口粒子、手上出现换模刀/雷
- [ ] 换图或重启后验证新音效

