# Excalibur · 誓约胜利之剑（传说 dice）设计

- 日期：2026-09-21
- 状态：已实现（2026-09-21；dll 858112B 已部署，服务器已加载，待实机验证）
- 关联：`Supernova`（史诗·持续激光剑）、`FinalJudgment`（传说·坠落天空）

## 1. 动机与目标

玩家诉求原话："一定要体现那种蓄力时的恢弘质感，比如渐渐的吸附周围的粒子，光剑越来越大等，最后倾泻而出的那种震撼！"

拆成三个必须达成的体验点：

1. **拼凑**：光剑不是凭空出现，而是由散落在周围的光丝被"吸"过来、一段一段嵌合成形——观众能看见"正在成形"。
2. **生长**：拼好后剑继续变粗变亮，脚下法阵向内收拢，音效渐强——蓄力进度是看得见、听得见的。
3. **倾泻**：松手瞬间一剑劈出，贯穿全场的巨型光炮把前面所有东西清干净——一次爆发、不给余地。

## 2. 定位（与现有终极技的区别）

| dice | 触发 | 形态 | 打击 |
|---|---|---|---|
| Supernova（史诗） | 按 E | 5s 细激光剑，可扫射 | 持续 100/s |
| FinalJudgment（传说） | 按 E | 20s 天空法阵群 + 浮空装置 | 定点落柱灼烧 200/s |
| **Excalibur（传说）** | **按住 E 蓄力，松手释放** | **竖直光剑拼凑生长 → 水平巨型光炮** | **一次性 500 定向贯穿** |

三者的差异点：Supernova 是"磨"、FinalJudgment 是"区域封锁"、Excalibur 是"一击斩开"。

## 3. 玩法规格

### 状态机

```
Idle ──按住 E──▶ Charging ──松手(≥min_charge)──▶ Releasing ──▶ Cooldown ──▶ Idle
                     │
                     └──松手(<min_charge)/死亡/回合结束──▶ Idle（取消，不消耗 CD）
```

### 蓄力

- 按下 E 立即起手（光晕 + 音效 + 法阵展开）；**按住期间**逐帧累积进度。
- 满蓄 `charge_seconds = 3.0`；威力系数 `scale = min_damage_scale + (1 - min_damage_scale) × clamp(elapsed / charge_seconds, 0, 1)`（默认 0.35 → 1.0）。
- 松手时 `elapsed < min_charge_seconds(0.6)`：**取消**——剑与法阵立即消散，不消耗 CD、不造成伤害（避免 E 键误触惩罚；但按下起手的演出已经暴露了自己）。
- 蓄力代价：施法者减速 50%（`SpeedBonusManager`，`Remove`/`Reset` 对称注销并回写 `VelocityModifier`）、自身金色 glow（`GlowUtil`，全场可见的预警）。
- 蓄力期间可正常开枪、移动（只是慢）；死亡即打断。

### 释放

- 方向 = **松手瞬间**的准星 forward（含俯仰角）。
- 命中判定：沿光柱线段做点到线段距离（躯干中心 = 脚底 + 40），半径 = `beam_width / 2`（"所见即所伤"约定），**贯穿**：不因命中递减、射程全程有效。
- **穿墙**：光柱与判定都不被地形截断（已与用户确认），长度恒定 `beam_length`。
- 伤害：一次性结算 `damage = 500 × scale`（每个目标只结算一次，标记集合去重）；直扣血 → 必须显式尊重 `Invulnerability.IsInvulnerable`。
- 命中附加：击退（沿光柱方向 `knockback_speed`）+ 0.5s 白屏（`Effects.Whiteout(0.5, 255)`）+ 金色爆点粒子。
- 施法者自身不受伤害。

### 冷却与清理

- CD 90s（`cooldown_seconds`），实现 `GetCooldownRemaining`（与 Supernova 同款）。
- 死亡 / 回合结束 / 插件卸载：立即清 glow、注销减速、分帧拆除所有特效实体。

## 4. 视觉分镜

### 蓄力 0 → 3.0s

**t=0 起手**
- 施法者金色 glow；`charge_sound`（`sanae/laser_draw`）从施法者身上发声。
- 脚下 `SigilStyles.Build(Sun)` 金色法阵展开（半径 `sigil_radius_start = 420`），描边随进度加粗。
- 头顶剑基座处一次金色闪光（`GoldHaloFlare`）。

**0 → 2.2s 拼凑（核心演出）**
- 剑身主金层切成 `segment_count = 12` 段短光丝（`segment_width ≈ 2.5`）。
- 每段初始散落在玩家周围球壳（半径 250~450、高度 -50~+250，随机方位），按随机延迟 0~1.6s 起飞。
- 飞行路径 = 二次贝塞尔（中点加随机垂向偏移，避免直线机械感），`0.45~0.7s`，ease-out；段朝向始终沿剑轴。
- 目标槽位沿剑轴（玩家头顶，世界 up 方向，不跟视角）按**当前剑长**均匀分布：剑长随进度从 `sword_charge_length = 100` 伸展到 `sword_length = 650`，段追着自己的槽位飞（槽位随剑变长而动）→ 视觉上既"拼凑"又"越来越长"。
- 段长 = 槽位间距 × 1.3（重叠避免缝隙）。
- **落位瞬间**：段宽脉冲一下（2.5 → 5 → 2.5）+ 一次 `GoldHaloFlare` 小闪（每段一次）；进入阈值距离即视为落位。
- 拼凑期间玩家移动 → 所有段每 tick 跟随（重算当前剑长与槽位）。

**2.2 → 3.0s 生长**
- 白芯（1 根整条，宽 0 → 3）与外辉光（1 根整条，宽 0 → 34）从拼好的剑里点亮。
- 光丝汇聚流：细金丝从外围环带（`streak_radius = 450`）被持续"吸"向剑体、循环重生；数量随进度从 ~8 增到 `streak_count = 32`，密度/速度加快（这是"吸附周围粒子"的持续层）。
- 法阵半径 `420 → 90` 收拢（`SigilShapeGroup` 重摆），`SetWidthScale` 脉冲加粗。
- 音效 `charge_loop_sound`（`laser_active`）循环，音量 0.25 → 0.7 渐强。
- 满蓄瞬间：剑体最亮 + `GoldHaloRaysRadiate` 爆发 + `ready_sound`（就绪音）。

### 释放（松手）

- **0.12s 挥落**：剑轴方向从世界 up 用 slerp（ease-in）转到准星 forward，12 段 + 白芯 + 辉光逐帧重摆——"劈"的感觉。提前释放（未满蓄）时，未落位的段在挥落开始时**强制吸附到位**，保证剑完整再劈。
- **光柱倾泻**：4 层水平 CBeam（外辉光 ≈1.4× / 主金 / 亮金 / 白芯），宽度 `beam_width_start = 40` → `beam_width = 200` 在 `beam_expand_seconds = 0.15` 内 ease-out；长度 `beam_length = 2600`；**穿地形不截断**。
- 全服音效（`burst_sound` = `marisa/explode_laser_fire`）+ `Effects.Shake`（幅度/频率/时长/半径可配）+ 地面冲击环（`ShockRingFx`）。
- 命中者：白屏 + 击退 + 金色爆点。
- 光柱存活 `beam_life_seconds = 0.8`，随后宽度收缩淡出。

### 拆除（崩溃防护铁律）

- 法阵（~100 束）、汇聚流（32）、剑（14 段）全部走 **`SigilTeardown` 分帧队列**（`ExcaliburSword` 实现 `IBeamGroup`）。
- **释放帧只做**：创建光柱 4 根 + 冲击环 + 把旧特效交给拆除队列——绝不在同一帧批量删除（历史上同帧删 ~1170 根 CBeam 触发过 tier0 写 NULL 崩溃）。

## 5. 技术实现

### 文件清单

| 文件 | 动作 | 内容 |
|---|---|---|
| `RollTheDice.Dices\Excalibur.cs` | 新增 | dice 逻辑 + 状态机（按住/松手、CD、伤害结算、glow/减速、调试指令） |
| `RollTheDice.Configs\ExcaliburConfig.cs` | 新增 | 配置类（§6） |
| `RollTheDice.Utils\ExcaliburSword.cs` | 新增 | 拼凑剑 + 汇聚流 + 挥落 + 光柱的封装（`IBeamGroup`） |
| `RollTheDice\DicesConfig.cs` | 改 | `public ExcaliburConfig Excalibur { get; set; }` |
| `RollTheDice.Configs\RarityConfig.cs` | 改 | `{ "Excalibur", "legendary" }` |
| `RollTheDice\SoundConfig.cs` | 改 | `legendary_sounds` 加 `Excalibur → sanae/laser_draw` |
| `RollTheDice\RollTheDice.cs` | 改 | `OnServerPrecacheResources` 预缓存蓄力/释放音效 |
| `lang\en.json`（源码 + 部署两处） | 改 | 抽到/提示/广播文案 |
| `$css\configs\plugins\RollTheDice\RollTheDice.json`（部署） | 改 | 新增 `Excalibur` 配置节 + 广播音（需 UTF-8 无 BOM） |

### 复用原语

- `BeamFx`（Create/CreateStyled/Move/SetWidth/SetColor/Kill）
- `SigilStyles.Build(Sun)` + `SigilShapeGroup`（法阵收拢）
- `SigilTeardown`（分帧拆除，已泛化为任意 beam 组）
- `ShockRingFx`（地面冲击环）
- `Effects.Whiteout / SoundAll / Shake / Play`
- `GlowUtil`（施法者预警 glow）
- `SpeedBonusManager`（减速，对称注销）
- `Invulnerability.IsInvulnerable`（直扣血必须尊重）
- 点到线段距离：新增公共工具（放 `Utils\Vectors.cs`），Excalibur 使用；**不改 Supernova**（避免回归，其私有实现保留）

### 性能预算

| 阶段 | 实体数 | 每 tick 更新 |
|---|---|---|
| 蓄力满档 | 段 12 + 芯/辉光 2 + 汇聚流 32 + 法阵 ~100 | ≈ 96 次 Move/SetStateChanged（法阵走 stride 2） |
| 释放 0.12s | 挥落 14 根 | ≈ 14 × 8 帧 |
| 释放 0.8s | 光柱 4 根 + 冲击环 | 仅宽度/颜色变化时标脏（阈值节流） |

对照：天空法阵群实测每 tick ≈182 次，本 dice 峰值 ≈96，安全。

### 已知风险与对策

| 风险 | 对策 |
|---|---|
| 同帧批量删 beam 崩进程 | 全部走 `SigilTeardown` 分帧队列（§4 拆除） |
| `OnPlayerButtonsChanged` 的 `released` 与 `pressed` 双参可用性 | Supernova 已在用同签名（含 `released` 参数），可用；若异常则退化为"点按两次"（方法 B），逻辑隔离在状态机内 |
| 按住 E 与游戏 +use（拆包/开门）冲突 | 已知局限（§8），不额外处理，蓄力起手有演出+glow，误触成本已很低 |
| 长按 E 期间玩家切武器/死亡 | 每 tick 校验 `LifeState`，死亡走打断清理 |
| 预缓存音效需换图/重启才生效 | 部署后换图验证（§10） |

## 6. 配置键（ExcaliburConfig，snake_case）

```
enabled               = true
charge_seconds        = 3.0      # 满蓄时长
min_charge_seconds    = 0.6      # 低于此松手 = 取消
min_damage_scale      = 0.35     # 最低威力系数
damage                = 500      # 满蓄一次性伤害
cooldown_seconds      = 90
slow_percent          = 0.5      # 蓄力减速
knockback_speed       = 600      # 命中击退速度
blind_seconds         = 0.5      # 命中白屏
beam_length           = 2600     # 光柱长度（穿墙，恒定）
beam_width            = 200      # 光柱视觉直径 = 伤害直径（所见即所伤）
beam_width_start      = 40       # 释放瞬间起始宽度
beam_expand_seconds   = 0.15     # 宽度扩张用时
beam_life_seconds     = 0.8      # 光柱存活
sword_length          = 650      # 满蓄剑长
sword_charge_length   = 100      # 起手剑长（随进度伸展到 sword_length）
segment_count         = 12       # 拼凑段数
segment_width         = 2.5      # 段宽
streak_count          = 32       # 汇聚光丝数
streak_radius         = 450      # 汇聚半径
sigil_radius_start    = 420      # 法阵起始半径
sigil_radius_end      = 90       # 法阵收拢半径
shake_amplitude       = 22
shake_frequency       = 40
shake_duration        = 1.2
shake_radius          = 3000
charge_sound          = "sounds/touhou/xrole/sanae/laser_draw.vsnd"
charge_loop_sound     = "sounds/touhou/xrole/sanae/laser_active.vsnd"
ready_sound           = "sounds/touhou/powerup.vsnd"
burst_sound           = "sounds/touhou/xrole/marisa/explode_laser_fire.vsnd"
```

（默认值均可实机微调；音效选择未经试听，按文件名主题推断，实机可换。）

## 7. 调试指令

- `rtdex [seconds]`（需 `@rollthedice/admin`）：无需持有 dice，直接在准星方向演示一次完整流程。
  - `rtdex` / `rtdex 3` = 完整蓄力演示（3s 后自动释放）；
  - `rtdex 0` = 跳过蓄力，立即释放（调光柱/伤害/性能）。
- 回显：实际蓄力时长、释放方向、命中人数。

## 8. 平衡与已知局限

- 强度锚点：Ragnarok 250 一击 / FinalJudgment 200×5s / Supernova 100×5s。Excalibur 满蓄 500 一击 = 秒满血满甲，**代价**是 3s 蓄力（减速 + 全场可见 + 死亡可打断）+ 定向预判（可走位躲直线）+ 90s CD。
- 穿墙已确认：掩体无法规避，只能横向离开弹道——这是设计意图（对城宝具），但会显著提高击杀率，实机后可考虑降到 450 或提高 CD。
- 按住 E 可能触发游戏 +use 交互（拆包/开门）——已知局限，接受。
- 只服务真人（bot/HLTV 排除），不换武器、不碰 WeaponPaints。

## 9. 不做 / 后续

- ~~被墙截断~~（已改为穿墙）。
- 旁观者近距白屏：首版不做。
- 剑格（护手光环）等装饰：首版不做，防堆料。
- DiceSynergy 组合：首版不加，后续再定（候选：与圣光系 dice 的联动）。

## 10. 验证计划

1. 编译：`dotnet build RollTheDice.csproj -c Release --no-incremental`（0 warning 目标）。
2. 部署：备份 dll → 替换 → `css_reload RollTheDice`；配置/音效需**换图或重启**才能预缓存生效。
3. 调试：`rtdex` 系列逐项核对——
   - 拼凑：12 段逐段飞来落位、有落位闪、无缝隙；
   - 生长：白芯/辉光点亮、法阵收拢、汇聚流密度递增、音效渐强；
   - 释放：0.12s 挥落、光柱 0.15s 扩张、穿墙、震屏、全服音效；
   - 命中：500 伤害（含无敌目标不掉血）、击退、白屏、爆点；
   - 打断：蓄力中死亡/回合结束无残留（无 glow、无减速、无实体）；
   - CD：90s 内二次按 E 无效。
4. 性能：满档蓄力期间 `stats` 帧率与实体数观察；确认无 `log-cssharp` 异常。
5. 实机体验调值：`segment_count`/`streak_count`/`sigil_radius_*`/`shake_*` 现场微调。
