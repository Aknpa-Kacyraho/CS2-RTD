# 特效挂点优化 + 光之剑：超新星（Supernova）设计

日期：2026-09-20
范围：RollTheDice（CS2 / CounterStrikeSharp 1.0.373 / net10.0）

## 背景与动机

1. 现有持续类特效（`Orbit` 环绕 / `Hold` 附着）以玩家原点为中心，环绕粒子会扫过准星中央，挡视线。
2. 玩家看到过一个「金色粗光柱」特效（类似激光炮）。代码排查结论：**全项目没有任何金色 `CBeam`**
   （`LaserCage`/`Drone` 红、`BlackHole`/`Singularity` 紫、`MagneticPulse`/`ThunderChain` 蓝、`WhiteHole` 白）。
   金色只可能是粒子（`ui_experience_award` 竖直金柱 / `ui_gold_halo_rays` 金色射线），默认竖直/放射，
   所以「朝向不对」。
3. 需求：用光束实体重做一条真正朝前的金色激光，作为新史诗 dice；并顺带把环绕/附着挪到背后或做成双翼。

## 决策（已与用户确认）

- 环绕/附着优化：**A3 混合**（默认背后；金色光环→背后光环；自然/护盾类→双翼）。
- 激光 dice：史诗；按 E 5s，Cd 30s；可移动但自身减速 50%；可自由转动扫射。
- 数值：DPS 100、判定半径 60、射程 1200、减速 0.5。
- 组合：配 1–2 条。
- 推进顺序：先①再②；③超位魔法另行调研（扒 VPK 找魔法阵/符文/光翼粒子）。

---

## ① 环绕 / 附着挂点优化

改动集中在 `RollTheDice.Utils\DiceEffects.cs`（设计表 + OnTick 驱动）。

### 挂点模式

新增枚举：

```csharp
public enum FxMode { Around = 0, Behind = 1, Wings = 2 }
```

`Profile` 新增字段（默认值）：

- `FxMode OrbitMode = FxMode.Behind`（环绕默认移到背后）
- `FxMode HoldMode = FxMode.Around`（附着默认保持原样＝身体中心）
- `float BehindDist = 26f`（背后距离）
- `float WingSpread = 34f`（双翼左右间距）
- `float WingBack = 22f`（双翼背后距离）
- `float WingZ = 52f`（双翼高度）

高度复用各 dice 已有的 `OrbitZ` / `HoldZ`。

### 位置计算（按玩家朝向）

每 tick 对存活真人取 `pawn.EyeAngles.Yaw`，换算弧度：

```
fw = (cos(yaw), sin(yaw))        // 前
rt = (sin(yaw), -cos(yaw))       // 右
```

- `Behind`：`origin - fw*BehindDist + (0,0,Z)`
- `Wings`：`origin - fw*WingBack ± rt*WingSpread + (0,0,WingZ)`（左右各一，用 `#L` / `#R` 键）
- `Around`：旧的整圈旋转（保留，默认不再使用）

双翼使用带后缀的键，`OnDiceAdded` / `OnDiceRemoved` 需按「SteamID + ClassName 前缀」清理（新增
`RemovePersistentByClass`）。

### 初版映射

| 模式 | dice |
|---|---|
| 背后光环 | Fate / God / Emperor / Pope（GoldHaloFlare） |
| 背后低位 | Cthulhu / FourHorsemen / Singularity（黑雾）、Ragnarok（电光）、WheelOfFate / Prophet（金环）、World / BeyondHeaven / RadarStation（能量环） |
| 双翼（orbit） | WolfKing（Nature） |
| 双翼（hold） | Shield / DeathKnight / DeathKnightComplete / RoyalBarrier / Paladin / Gargoyle（ShieldGlow / ShieldGlowHigh） |
| 背后（hold） | GunGod |

上线后按手感逐 dice 微调（只动这张表）。

---

## ② 光之剑：超新星（Supernova）

`ClassName = "Supernova"`，显示名 `光之剑：超新星`，稀有度 **epic**。

### 数值（`SupernovaConfig`）

| 键 | 默认 | 说明 |
|---|---|---|
| `enabled` | true | |
| `duration_seconds` | 5 | 持续开火时长 |
| `cooldown_seconds` | 30 | 冷却 |
| `range` | 1200 | 射程 |
| `damage_per_second` | 100 | 连续伤害 |
| `hit_radius` | 60 | 敌人到光柱线段距离阈值 |
| `slow_percent` | 0.5 | 开火期间自身减速（0.5=减 50%） |

### 行为

- 监听 `OnPlayerButtonsChanged`（`PlayerButtons.Use`＝E）与 `OnTick`。
- 按 E（持有者、存活、非冷却中、未在开火）：开火 `duration` 秒，进入 `cooldown`。
- 开火期间：
  - 起点＝玩家胸口（`AbsOrigin.Z + 50`），方向＝`EyeAngles` 前向，终点＝起点＋方向×`range`。
  - **穿墙穿人**：不做任何 trace，纯数学判定。
  - 对每个**敌方**存活玩家：算其到线段距离，≤ `hit_radius` 即命中。
  - 命中伤害按 tick 累加：`dps * Server.TickInterval`，用每目标 float 累加器存小数，
    取整后扣 `Health`（连续、平滑，不跳伤）；`Health<=0` → `CommitSuicide`。
  - **尊重无敌**：扣血前显式 `Invulnerability.IsInvulnerable(victim)`。
  - 自身减速走统一系统：`SpeedBonusManager.Register(player,"Supernova",-slow_percent)`，
    每 tick 写 `VelocityModifier = 1f + SpeedBonusManager.GetEffective(player,100f)`；结束时 `Unregister` 并还原。
- 结束（超时/死亡/移除 dice/换图）：移除光束、注销减速、还原速度、清累加器。

### 视觉

- 双层 `CBeam`：外层金色 `FromArgb(255,255,215,0)` 宽 4，内层白色宽 1.5；每 tick 更新起点与 `EndPos`。
- 枪口：`GoldHaloFlare`（金色光柱感），节流 ~0.1s。
- 命中：`Sparks` / `ImpactMetal`，节流。

### 组合（实时 `HasPartner`）

| 组合 | 名称 | 效果 |
|---|---|---|
| Supernova + DivinePunishment | 圣裁天罚 | 激光 DPS ×1.5（100→150） |
| Supernova + LaserCage | 光轮圣裁 | 判定半径 +40（60→100），更易扫中 |

在 `DiceSynergy._combos` 注册，并在 `Supernova.OnTick` 里实时判定。

### 落点文件

- 新增 `RollTheDice.Dices\Supernova.cs`
- 新增 `RollTheDice.Configs\SupernovaConfig.cs`
- `RollTheDice\DicesConfig.cs` 加 `[JsonPropertyName("supernova")] public SupernovaConfig Supernova { get; set; }`
- `RollTheDice.Configs\RarityConfig.cs` 加 `{ "Supernova", "epic" }`
- `RollTheDice.Utils\DiceEffects.cs` 加 `A("Supernova", FxTier.Epic, ...)`
- `RollTheDice.Utils\DiceSynergy.cs` 加 2 条组合
- `lang\en.json` 加 `dice_Supernova_name/_other/_player`（**源码 + 部署两处**）
- 文档：`README.md` / `docs\dice-rarity-list.md` / `TEST_COMMANDS.txt` / `docs\dice-cn-tier.txt`（可选同步）

### 边界与风险

- 多持有者：所有状态字典按 `SteamID` 键，天然隔离。
- 目标为 BOT：可作为受击目标（dice 只发给真人）。
- 无敌目标：不扣血，但光束视觉照常。
- 不伤队友、不自伤。
- 与其它移速来源共存：走 `SpeedBonusManager` 求和，注销对称。
- 直接扣血绕过伤害钩子 → 必须显式查 `Invulnerability`（AGENTS 硬规则）。

## 自检清单

- [ ] `dotnet build` 通过（CSS 1.0.373 / net10.0）
- [ ] `InitializeModules` 反射匹配：类名 `Supernova` == `DicesConfig.Supernova` 属性名；配置有 `Enabled`
- [ ] `_players` / 光束 / 减速 / 累加器在 `Remove` / `Reset` / `Destroy` 对称清理
- [ ] 双翼键在 dice 移除时按类前缀清理，不残留
- [ ] 背后/双翼位置按朝向计算，永远在背后
- [ ] `en.json` 源码与部署两处一致
