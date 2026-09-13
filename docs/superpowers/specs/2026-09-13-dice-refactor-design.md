# RollTheDice 全面重构设计（2026-09-13）

> 状态：草案，待用户审阅后进入分批实现。

## 1. 目标与验收标准

**目标**：在不新增骰子、不删除任何骰子、不更改骰子名字的前提下，把 166 个 dice 全面重构成
"贴合 CS2 核心玩法、每个都有操作/取舍、没有两个骰子做同一件事"的状态。

**CS2 尺子**：CS2 的核心不是击杀，是赢下这一回合——**炸弹、经济、购买、投掷物**才是玩家真正在做的决策。
一个 dice 合格的判定：

1. 要么改变"这一回合怎么打 / 怎么买 / 怎么下包拆包"；
2. 要么有明确的操作、时机、风险或团队互动；
3. **不接受**只加一个数值的骰子。

**验收**：每个家族内任意两个骰子，玩家能一句话说清"它俩不一样在哪"。

## 2. 硬约束

- **不新增 dice 文件**；只改现有 166 个的行为。
- **不下架、不合并不改名**；重复靠"差异化"解决。
- **逐个审批**：每次只提交一个 dice 的完整改造方案（效果/触发/代价 + 受影响的 combo + lang/config 变更），
  用户明确批准后才改代码；未批准不动。一个 dice 改完并验证后再提交下一个。
- 改效果时，**凡引用到该 dice 的 combo 必须同步**：
  - `RollTheDice.Utils\DiceSynergy._combos` 对应条目；
  - 该 dice 内的 `DiceSynergy.HasPartner` 分支。
  缺一不触发。
- 改完必须同步 `lang\en.json`（源码 + 部署两处）与 config。
- 遵循 AGENTS 既有约定：伤害/减伤/移速 buff 走 `DamageBonusManager`/`DamageReductionManager`/`SpeedBonusManager`，
  主插件 `OnPlayerTakeDamagePreCentral` 统一结算；`Remove`/`Reset` 对称注销；不做 `Add` 时算一次存实例字段。
- `ResetOnReload` 现在 `Respawn()`（换弹=瞬移出生点满血）是 bug，必须去掉。

## 3. 稀有度系统（复用现有 `Weight`）

`WeightedRandomDraw` 已在读 `dice.Weight`（现全为 1）。改为 4 档，**集中在 `dices.rarity` 配置**（不逐个改 Config 类）：

```jsonc
"rarity": {
  "tier_weights": { "common": 100, "rare": 40, "epic": 12, "legendary": 3 },
  "dice_tier": { "Shield": "common", "Fate": "legendary" }
}
```

- `DiceBlueprint.Weight` 查 `dice_tier`→`tier_weights`；查不到按 common。
- 初始只给一批明显骰定档，其余随逐个重做时定档。
- `IsSpecial`（DragonSoul / WolfKing）保持独立池，不参与权重。

## 4. 重复家族差异化原则（11 组，逐一换角色）

每组内的骰子必须落到**不同的触发条件 / 目标 / 代价**上：

| 家族 | 差异化轴 |
|---|---|
| 武器特化（DeagleKing/SniperElite/PistolMaster/GrenadeKing） | 按武器类型分：沙鹰连杀返弹 / 狙击距离与开镜 / 手枪经济 / 投掷物返雷 |
| 移速 | `IncreaseSpeed` 独占"越跑越快·中弹清零"；其余速度只作为各自机制的一部分 |
| 回血 | 触发分：脱战(Regeneration) / 跳跃(JumpHeal) / 命中(GunHealer) / 吸血(Vampire) / 破上限(Gaia) / 团队(Knight/Priest) |
| 减伤 | 条件分：吸收额度(Shield) / 震波(HighGravity) / 正面格挡(RoyalBarrier) / 爆头窗口(IronHead) / 蹲伏(Crouch) / 换弹(ReloadGap) / 低血(Adrenaline) / 刀(Frostmourne/DeathKnight) / 省钱(Miser) |
| 复活 | 按触发与目标分：自身1次(Respawn) / 自身多段(InfiniteProliferation) / 概率循环(WheelOfFate) / 团队击杀(DivineResurrection) / 团队耗血(Necromancer) / 团队被动首次(Emperor) / 团队花钱(Empress) / 反噬(Redemption) / 变身(Phoenix) / 重置(Countdown) |
| 击杀成长 | Awakener 质变 / Berserker 残血 / Glutton 永久+额外骰 / Overheat 时间层数·击杀降温 / Evolution 时间随机 / Combo 连击窗口 / SpeedOnKill 速度爆发 / Parasite 标记击杀 / Karma 传播 |
| 透视/标记 | BoneMaggot 标记增伤 / Tactician 周期暴露 / RadarStation 全敌发光·减速 / ImposterSyndrome 感知雷达 / SmokeVision 穿烟 |
| 位移/引力 | ChaosStorm 周期换位 / Twilight 开局换位 / MagneticPulse 受击脉冲 / RepulsionField 投掷物弹回 / BlackHole·GravityWell·WhiteHole·Singularity 各自定位 / Swap 单体换位 |
| 时间 | Heaven 手动加速 / Izayoi 随机乱流 / BeyondHeaven 暂停 / Rewind 全员回溯 / Afterimage 单体回溯 / ReverseCausality 延迟伤害 / FourtyTwo 周期无敌隐 |
| 体型 | Giant 巨大有代价 / Mosquito 极小叮咬 / PlayAsChicken 鸡神 / RoyalBarrier 重装 |
| 经济 | Bank 队友注资 / Capitalist 存款生息 / LoanShark 贷款 / Miser 省钱换减伤 / Lottery 全图事件 / Empress 收入机制 / Pickpocket 偷 / Bounty 赏金 / Payback 死亡掠夺 |

## 5. 六轴编制与逐骰处置

### ① 炸弹轴（补强核心目标）
| dice | 处置 |
|---|---|
| C4Expert | 保留 |
| HotPotato | 保留 |
| ResetOnReload | 改造：去 Respawn，改下包/拆包进度加速 |
| Synced | 改造：拆包时附近队友共享拆包进度 |
| ReturnToSender | 改造：击中下包/拆包中的敌人→打断+击退 |

### ② 经济轴
| dice | 处置 |
|---|---|
| Bank / LoanShark / Bounty / Pickpocket | 保留 |
| Capitalist | 改造：存款生息 / 可提现 / 死亡清零 |
| Miser | 保留（省钱换减伤，归减伤轴） |
| Lottery | 保留（全图事件） |
| Empress | 改造：收入机制与队友经济共享，区别于 Capitalist |
| PistolMaster | 移入：手枪/ECO 回合经济补偿 |
| Payback | 改造：死亡时掠夺击杀者（区别于 Bounty 赏金） |

### ③ 购买/武器轴
| dice | 处置 |
|---|---|
| WeaponRoulette / Disarm | 保留 |
| DeagleKing | 改造：沙鹰爆头击杀返弹并叠连杀 |
| SniperElite | 改造：距离越远伤害越高、开镜静止强化 |
| InfiniteAmmo | 改造：换弹保留剩余弹药→转化为下一发强化弹（购买/资源向） |

### ④ 投掷物轴
| dice | 处置 |
|---|---|
| SmokeVision / ToxicSmoke / Fireball / FireLord | 保留 |
| LongerFlashes | 改造：双向——被闪者短暂愤怒加速 |
| GrenadeKing | 改造：投掷物击杀返雷、范围随连杀扩 |
| Satellite | 改造：滞空投掷轨迹/范围（战术道具向） |

### ⑤ 团队/复活轴
| dice | 处置 |
|---|---|
| Bugle / Priest / Knight / Sacrifice / SacrificeSelf / Kinship | 保留 |
| Pope | 改造：圣印——队友伤害按比例分担给教皇 |
| Respawn / InfiniteProliferation / WheelOfFate / DivineResurrection / Necromancer / Emperor / Empress / Redemption / Countdown / Phoenix | 全部保留，按"复活家族差异化轴"各自换角色 |

### ⑥ 战斗数值轴（大改造，去纯数值）
| dice | 改造方向 |
|---|---|
| DamageMultiplier | 静止蓄力叠伤害 / 移动清零（或过载：连发升·停火衰减） |
| IncreaseSpeed | 越跑越快，中弹清零 |
| Regeneration | 脱战 3s 才回血，受击中断 |
| GunHealer | 命中敌人回血、打空扣血 |
| Shield | 吸收额度，打碎后击杀补给 |
| HighGravity | 落地震波眩晕+伤害，自身免摔伤 |
| Giant | 巨大化→受击面大、爆头×2、移速慢（有代价） |
| GunGod | 条件性武装（持械/换弹节奏）替代纯免伤 |
| IronHead | 爆头后 2s 免爆头，但身体伤害+30% |
| Thorns | 反弹需充能、次数有限，击杀补能 |
| Mosquito | 小体型+叮咬减速/吸血 |
| Hermit | 开火/受击现形，背刺加成 |
| Forsaken | 命中累积→第 N 发必爆头（或仅首发） |
| JumpHeal | 连跳叠跳跃高度，落地回血 |
| Vampire | 掉血时吸血翻倍，可主动爆发 |
| Gaia | 站地回血破上限，离地/受击转护盾 |
| RoyalBarrier | 正面格挡减半、背后×2 |
| Fibonacci | 伤害取最近斐波那契数（双向，可感知） |
| Deaf | 加声音可视化/短 CD 透视补偿 |
| NoRecoil | 静止 1s 进精准态（无后坐+伤害+10%），移动退出 |
| God | 神之试炼：强但有倒计时/N 杀续命，否则坠落 |
| Countdown | 重做：单体"倒计时重置"（自身回出生点满状态），与 Rewind 的全员回溯区分 |
| ReverseCausality | 保留微调（延迟伤害+期间增伤，机制已可感知） |
| FourtyTwo | 保留，降权为传说（每 42s 无敌+隐身的周期事件） |

保留不动（机制已达标，最多调数值）：
`Crouch`、`ReloadGap`、`LastStand`、`Combo`、`Overheat`、`Awakener`、`Berserker`、`Anatomist`、
`Guillotine`、`DeathKnight`、`Frostmourne`、`DeathKnightComplete`、`Adrenaline`、`NoRecoil`(改造) 之外的既有主动技骰。

### ⑦ 位移/引力轴
| dice | 处置 |
|---|---|
| BlackHole / GravityWell / WhiteHole / Singularity / Swap | 保留 |
| ChaosStorm / Twilight | 保留名字，区分周期换位 vs 开局换位 |
| MagneticPulse / RepulsionField | 保留名字，区分受击脉冲 vs 投掷物弹回 |

### ⑧ 时间轴
`Rewind / Heaven / Izayoi / BeyondHeaven / Afterimage` 保留；
`Countdown / ReverseCausality / FourtyTwo` 按第 4 节差异化重做，避免与 Rewind 重叠。

### ⑨ 其余（保留）
所有已有主动技/条件/团队互动的骰子均保留：
Afterimage, Amber, Anatomist, Awakener, Bank, Berserker, BeyondHeaven, BlackHole, BoneMaggot, Bounty,
Bugle, C4Expert, ChaosStorm, Combo, Corona, Cthulhu, Cupid, Curse, Cutter, DeadHand, DeathKnight,
DeathKnightComplete, DecoyDummy, Disarm, DivinePunishment, DivineResurrection, Dragonborn, DragonSoul,
Drone, DuskDawn, Echo, Eclipse, Evasion, Evolution, Fate, FireDragon, FireLord, FogOfWar, Fool,
FourHorsemen, FrontlineBeast, Gargoyle, Glutton, Goddess, GravityWell, GuardianAngel, HangedMan,
Heaven, HotPotato, IceBeam, IceDragon, InfoHole, Izayoi, Jammer, Jester, Karma, Kinship, Knight,
LaserCage, LoanShark, Lucky, MagneticPulse, Martyrdom, Mimic, Nightglow, Nirvana, NoExplosives,
NukeLeak, Overheat, PainConverter, Paladin, Parasite, Phoenix, Pickpocket, Plague, PlayAsChicken,
PoisonBlade, Prayer, Prophet, RadarJammer, Ragnarok, Rally, Reincarnation, RepulsionField, ReturnToSender(改),
ReverseCausality, Rewind, RouletteGambler, Sacrifice, SacrificeSelf, ShadowWarrior, Singularity, Skyline,
SlyFox, SmokeBomb, SmokeVision, SoulEater, Swap, SwordSaint, Tactician, Taotie, Teneril, ThunderChain,
Titanfall, ToxicSmoke, Traitor, Trickster, Twilight, Universe, Void, WASDChaos, WeaponRoulette, WhiteHole,
Wolf, WolfKing, World, Yagorou.

## 6. Combo 同步规则

- 被改造的 dice，其上/下游 combo 必须重写为与新机制一致的效果（示例：`Shield+Evasion` 钢铁壁垒、
  `GrenadeKing+Martyrdom` 爆炸艺术家、`Satellite+Drone` 天网等）。
- 重写三处：`_combos` 条目、主动方 dice 的 `HasPartner` 分支、`lang` 文案。

## 7. 系统层（第 0 批，先落地）

1. 稀有度 4 档 + `Weight` config 化（`DicesConfig` / 各 `*Config`）。
2. 组合实时判定：修 `_comboActive` 在 `Add` 时算一次的死角——组合统一改在效果读取点实时 `HasPartner`。
3. 158 处 `Console.WriteLine(_localizer["dice.class.initialize"]...)` → `LogDebug`。
4. 单元素 List 样板（`CollectionsMarshal.SetCount`）→ 集合表达式（可选，风险低再动）。

## 8. 实施方式：逐个 dice 审批

不再整批推进。流程固定为：

1. 我提交**一个** dice 的改造方案（含 config 字段、`_combos` 影响、lang 文案、风险）。
2. 用户**批准 / 修改 / 否决**；否决则讨论替代方案。
3. 批准后我改代码 + config + combo + lang，`dotnet build` 通过后报告。
4. 再提交下一个。

顺序（按**上手频率**重排，用户可随时指定）：
**枪战 > 经济 > 投掷物 > 信息/走位 > 炸弹/复活**。低频机制（拆包等）改造成高频，或做成"高频+偶尔爆发"。
系统层已完成。

## 9. 验证与回归

- 编译：`dotnet build "...\RollTheDice.csproj" -c Release` 必须成功，不新增编译警告。
- 每个 dice 抽查：掷骰能触发、`Remove`/`Reset` 后 buff 无残留（对照 AGENTS 的泄漏坑）、combo 实时生效、
  lang 文案与实际效果一致。
- 部署前备份 dll；`css_reload RollTheDice` 生效。

## 10. 进度

| # | 内容 | 状态 |
|---|---|---|
| 0 | 系统层：稀有度 config 化 / 组合实时管道 `OnDiceSetChanged` / 158 处日志改 LogDebug | 已完成（编译 0 err） |
| 1 | `ResetOnReload`（快手）→ 击杀/换弹瞬间补满弹匣（枪战高频） | 已完成（编译 0 err） |
| 2 | 枪战/生存第一批：`DamageMultiplier`(站定蓄力) / `IncreaseSpeed`(越跑越快·受伤清零) / `Regeneration`(脱战回血) / `GunHealer`(命中回血) / `Shield`(吸收护盾) / `HighGravity`(落地震波+免摔伤) | 已完成（编译 0 err） |
| 3 | 武器/道具第二批：`DeagleKing`(沙鹰×3+爆头击杀回50可破上限) / `SniperElite`(开镜蓄力最多+200%) / `PistolMaster`(手枪+100%+击杀$300) / `GrenadeKing`(手雷×2.5+击杀返雷) / `LongerFlashes`(闪光双向) | 已完成（编译 0 err） |
| 4 | 生存/战斗第三批 13 个：`Vampire`(吸血·半血翻倍) / `Gaia`(站地+最大生命) / `JumpHeal`(落地回血) / `NoRecoil`(静止0.1s精准态) / `God`(40s试炼) / `GunGod`(击杀叠减伤) / `Mosquito`(叮咬减速) / `Hermit`(开火现形) / `Deaf`(受击透视) / `Satellite`(滞空加速) / `Synced`(换弹连累敌人) / `Countdown`(按E回溯) / `Payback`(死亡掠夺) | 已完成（编译 0 err） |
| 5 | 经济/复活第四批：`Bank`(支援最穷队友) / `Capitalist`(存款生息) / `Respawn`+`InfiniteProliferation`+`DivineResurrection`+`Necromancer`+`Emperor`(复活后无敌) + 新增共享 `Invulnerability`（主插件统一伤害钩子拦截） | 已完成（编译 0 err） |

**第三批跳过未改**：`IronHead`、`Thorns`、`Forsaken`、`Giant`、`Fibonacci`、`InfiniteAmmo`、`RoyalBarrier`（跳过=不改）。
**第四批未改（保留原状）**：`Miser`（本已实时读花费）、`LoanShark`、`Lottery`、`Pickpocket`、`Bounty`、`WheelOfFate`（跳过）。

> 部署与部署版 lang 统一在重构收尾时进行（用户要求）。
