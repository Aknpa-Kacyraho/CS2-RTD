# 超位魔法三件套（SkyVerdict / DivineDescent / FinalJudgment）设计

日期：2026-09-20
范围：RollTheDice（CS2 / CounterStrikeSharp 1.0.373 / net10.0）
前置：①② 已上线（特效挂点 + 光之剑·超新星）

## 需求（用户确认）

- 三个 dice，全部**按 E 触发**、稀有度 **传说**、表现**最强震撼**。
- 特效要放大；天穹裁决必须**全图可见**。
- 天穹裁决：按 E 锁定**施法者准星处**，**不穿墙**；CD 60s。
- 神圣降临：触发时翅膀**要大**，可以动了之后再变小；8s 内自身无敌 + 伤害+100% + 移速+30% + 每 0.5s 回 30HP（**可突破上限**）；结束光翼收起、buff 对称注销；CD 90s。
- 终焉审判：按 E 在施法者**头顶 1200** 生成**分层巨型法阵（魔法阶层）** + 收缩环逐圈倒数 + 天空印记；4s 后引爆：**全图所有人**（含队友）500 伤害（按距离衰减），越靠近中心越白 + 最强震屏 + 扭曲；CD 90s；法阵敌人可见可躲。

## 技术前提（已用探针编译验证，1.0.373 均可用）

- `CounterStrikeSharp.API.Modules.Utils.Trace.TraceEndShape(start, end, ignoreEntity, options)` → `TraceResult`（`EndPos`/`DidHit()`）＝不穿墙锁定。
- `CGameSceneNode.Scale`（`m_flScale`）＝粒子放大（**实验性**，游戏内验证；不行改分层堆叠兜底）。
- `CCSPlayerPawnBase.FlashDuration` / `FlashMaxAlpha`＝每玩家白屏强度/时长（越近越白）。
- `StackingBonusManager`（dmg/spd）＋ `Invulnerability`（无敌）＋ 直接改 `Health/MaxHealth`＝破上限回血。

## 共享改动

- `ParticlePaths.cs` 新增（VPK 实路径）：
  `GoldHaloRaysRot`、`GoldHaloRaysRoll`、`GoldHaloRaysRadiate`、`GoldAwardBurstCircle`、`GoldAwardRays`、
  `MvpWinnerSigil`、`MvpWinnerStars`、`AnnotationGroundCircles`、`StatusBurstCircles`、`GoldHaloSparkles`、
  `StatusLevelWings`(已加)、`StatusLevelWingsFlares`。
- `Effects.cs` 新增：`PlayScaled` / `SetScale`（`CGameSceneNode.Scale`）、`Whiteout`（`FlashDuration/FlashMaxAlpha`）。

## A. 天穹裁决 `SkyVerdict`（传说，CD 60s）

- E → `TraceEndShape(eye, eye+forward*Range)`，取 `EndPos` 为准星命中点（撞墙即止）。
- 视觉：命中点地面法阵 `AnnotationGroundCircles` + 天空印记 `MvpWinnerSigil` + 3 圈金环 `GoldHaloRaysRot`（Z+300/600/900）放大布置，保证全图可见。
- `DelaySeconds`（1.5s）后：`GoldAwardRays` 光柱 + `env_explosion` + 超大半径 `env_shake` + 命中点白屏。
- 半径 `Radius`（250）内**敌人** `Damage`（250，衰减）+ 上抛；尊重无敌。
- 配置：Range 1200 / Delay 1.5 / Damage 250 / Radius 250 / Falloff 500 / Cooldown 60 / Shake*。

## B. 神圣降临 `DivineDescent`（传说，CD 90s）

- E → 真光翼 `StatusLevelWings` 挂自身、放大（`WingScaleBig` 1.8）；`WingShrinkAt`(2s) 后缩到 `WingScaleSmall`(1.0)；脚下法阵 + `GoldHaloRaysRoll`。
- `Duration`(8s)：`Invulnerability.Grant` + `DamageBonusManager`(+100%) + `SpeedBonusManager`(+30%) + 每 `HealInterval`(0.5s) 回 `HealPerTick`(30)（临时把 MaxHealth 抬高 `OverhealBonus`(200) 实现破上限）。
- 结束：光翼移除、buff 对称注销、`Invulnerability.Clear`、MaxHealth 还原并夹血。

## C. 终焉审判 `FinalJudgment`（传说，CD 90s）

- E → 以施法者 (X,Y) 为中心，Z 分层 `Height`(1200) 生成 `LayerCount`(4) 层法阵（+300/600/900/1200，旋转金环）+ 天空印记。
- 倒数 `DelaySeconds`(4s)：地面收缩环 `StatusBurstCircles` 逐圈缩小；施法者屏幕倒计时。
- 引爆：**全图所有玩家（除施法者，含队友）** 按到中心水平距离衰减的 `MaxDamage`(500)→`MinDamage`(50)（`FalloffRadius` 2500）；越近白屏越强越久；最强震屏 + `ExplosionDistort` 扭曲；尊重无敌。

## 组合（各 1 条，实时 HasPartner）

| 组合 | 名称 | 效果 |
|---|---|---|
| SkyVerdict + DivinePunishment | 天罚共振 | 光柱半径 +50% |
| DivineDescent + God | 神临 | 无敌时长 +4s |
| FinalJudgment + Ragnarok | 末日 | 伤害范围/衰减半径 +30% |

## 落点文件

新增：`SkyVerdict.cs` / `DivineDescent.cs` / `FinalJudgment.cs` + 各自 `*Config.cs`；
改动：`ParticlePaths.cs`、`Effects.cs`、`DicesConfig.cs`、`RarityConfig.cs`（传说 ×3）、`DiceEffects.cs`、
`DiceSynergy.cs`、`lang\en.json`（源码 + 部署）、文档清单。

## 边界与风险

- 放大为实验项：`SceneNode.Scale` 不生效时改用多层堆叠，不虚报。
- 全图 500 含队友：按用户要求；尊重无敌。
- 对称清理：翅膀/法阵实体、buff、MaxHealth、白屏在 `Remove`/`Reset`/`Destroy` 全部还原。
- 直接扣血绕过伤害钩子 → 显式查 `Invulnerability`。
- 新粒子需 `Effects.PrecacheAll` 覆盖（加入 ParticlePaths 即自动）。
