# 坠落天空（Fallen Sky）特效重构设计

- 日期：2026-09-21
- 对应 dice：`FinalJudgment`（内部标识不变，仅改玩家可见名称）
- 状态：已与用户确认，待实现

## 1. 目标

把原「终焉审判」重做为「坠落天空」，核心是**从「精密构筑」到「暴力倾泻」的两阶段视觉叙事**：

1. 阶段一（前摇）：以施法者为原点、从地面向上生长的**苍白色立体穹顶**，内部符号带流转，临近发动时亮度脉冲。
2. 阶段二（降临）：头顶高空展开**数十个蓝白法阵**（嵌套堆叠、正反异速旋转），阵群中心垂直贯下**直径约 50 米的通天光柱**，落点炸出水平扩散的冲击环与结晶化。

**只改玩家可见名称与特效**；内部类名 / JSON 键（`final_judgment`）/ `dicefx` trigger / `tier` / `synergy` 键全部保留，零反射与配置风险。

## 2. 命名

| 位置 | 改动 |
|---|---|
| `lang\en.json` `dice_FinalJudgment_name` | 终焉审判 → 坠落天空 |
| `dice_FinalJudgment_other` / `_player` | 文案同步（发动提示 / 按 E 说明） |
| `DiceSynergy` 「末日」combo 名 | **保留**（是 combo 效果名，不是 dice 名） |
| 中央提示 / `PrintToChatAll` 硬编码中文 | 同步为「坠落天空」 |
| 内部键（类名、`final_judgment`、dicefx、tier、synergy） | **不动** |

## 3. 时间线（按 E 起算，`delay_seconds` 默认 20s）

| 时刻 | 事件 |
|---|---|
| t=0 | 锁定落点（按 E 时施法者脚下，**此后不跟随**）；打印「坠落天空」发动提示 |
| t=0–2.4s | **筑阵**：穹顶按纬度自下而上分 7 批弹出（每批间隔 ~0.35s，scale 0.05→1 ease-out），符号带自下而上点亮 |
| t≈3s | **升空**：头顶法阵群由低到高逐层弹入（每层间隔 ~0.45s，grow 0.5s ease-out） |
| t≈13–17s | **共鸣**：全部法阵加速旋转；追加亮环 / 加宽 / 尝试提亮，做亮度脉冲 |
| t≈17–20s | **聚能**：强脉冲，天空阵群向内合拢蓄力 |
| t=20s | **降临**：多层同心光柱贯落 + 冲击环扩散 + 结晶/爆炸粒子 + 闪光 + 震屏 + C4 音效 + 白屏；随后分帧拆除全部 CBeam |

穹顶与法阵群都以**落点**为几何中心。施法者按完 E 即可走出穹顶（落点固定）。

## 4. 几何与默认参数

单位换算：CS2 1u ≈ 1.9cm。

- **穹顶**：半球，半径 480u（≈10m）；纬线 8 圈 × 48 段，经线 12 条 × 8 段；苍白色；生长 7 批；末尾脉冲（加宽 + 提亮）。内部 2–3 圈符文带（`SigilBuilder.RuneBand`）不同速度方向流转。
- **天空法阵群**：16 层；高度 Z+900 → Z+3900 递增；半径**指数递减 + 奇偶交替**（基线 1200 → 320，奇数层 ×`sky_radius_alternate`(0.55) → 相邻层差 ~2 倍，远看明显"大小不一"）；每层间隔 0.45s 弹入；每层 = 外环 + 刻度环/符文带 + 星/多边形（`SigilBuilder`），正反异速旋转；最后 3s 整体向内合拢到 ~0.78 倍。
- **光柱**：直径 2600u（≈50m）；3 层同心（外 2600 暗蓝白 / 中 1600 亮蓝白 / 内 700 炽白）+ 若干螺旋亮纹；贯穿至地面。
- **冲击环**：3 圈，半径 0 → 2600u，1.5s，蓝白。
- **粒子**：`ExplosionHegrenade` / `ExplosionDistort` / `ExplosionFlashbang` + `SnowBurst`（结晶感，已在 `ParticlePaths` 且被 `PrecacheAll` 预缓存）。
- **保留**：`Shake`、`Whiteout`（越近越白）、`SoundAll("c4.explode")`。

## 5. 伤害（改为范围）

复用现有 `falloff_*` 语义，只改默认值与部署配置：

- `falloff_enabled` 默认 `true`
- `falloff_radius` 默认 `2600`（≈50m）
- `max_damage = 2000`（核心满伤）→ `min_damage = 250`（边缘）线性衰减
- 范围外无伤；**施法者自己也在范围内**（保持「无差别打击」）

## 6. 技术设计

### 新增文件

| 文件 | 职责 |
|---|---|
| `RollTheDice.Utils\SigilDome.cs` | 半球穹顶：球面 CBeam 网格 + 生长动画 + 脉冲；实现 `IBeamGroup` |
| `RollTheDice.Utils\SkySigilField.cs` | 天空法阵群：指数递减 + 奇偶大小交替，每层带 `SpawnTime` 的弹入动画 + 合拢；实现 `IBeamGroup` |
| `RollTheDice.Utils\ShockRingFx.cs` | 扩散冲击环：持 3 个 `MagicCircle`，按进度 SetRadius |

### 修改

| 文件 | 改动 |
|---|---|
| `Utils\SigilShapes.cs` | 新增 `IBeamGroup` 接口（`IsEmpty` / `RemoveChunk` / `Remove` / `BeamCount`） |
| `Utils\ArcaneSigil.cs` | `SigilTeardown` 泛化：条目持 `List<IBeamGroup>`，保留 `Add(MagicSigil?, MagicTower?, Vector)` 兼容（`SkyVerdict` 已删，现无调用者） |
| `Utils\BeamFx.cs` | 新增 `SetWidth` / `SetColor`（运行时改宽/改色，带 `SetStateChanged`） |
| `Utils\Effects.cs` | 新增 `SkyPillar(...)` 多层同心光柱 |
| `Dices\FinalJudgment.cs` | 重写为时间线驱动，替换 `_ground`/`_tower` |
| `Configs\FinalJudgmentConfig.cs` | 删除旧 `tower_*` / `ground_*` / `rune_*` / `sigil_rings/shrink/contract` 键，新增 `dome_*` / `sky_*` / `pillar_radius` / `shock_*` |
| `lang\en.json` | 名称与文案 |

### 生长 / 弹入动画机制

延迟创建 + 每层自带 `SpawnTime`：到达时刻才创建该批 CBeam，创建时 `scale=0.05`，之后 `Update` 内按 `ease-out = 1-(1-t)³` 在 `growSeconds` 内升到 1。仅用 `BeamFx.Move`（同步已由 `SetStateChanged(..., "CBeam", "m_vecEndPos")` 解决），不依赖新引擎能力。

## 7. 新增配置键

```text
dome_enabled        bool    true
dome_radius         float   480
dome_latitude_rings int     8
dome_segments       int     48
dome_meridians      int     12
dome_grow_seconds   float   2.4
dome_spin           float   0.35
dome_pulse_seconds  float   3

sky_enabled         bool    true
sky_count           int     16
sky_height_start    float   900
sky_height_end      float   3900
sky_radius_start    float   1200
sky_radius_end      float   320
sky_radius_alternate float  0.55   # 奇数层 = 相邻偶数层 × 该值（明显大小不一）
sky_start_delay     float   3
sky_layer_delay     float   0.45
sky_grow_seconds    float   0.5
sky_spin            float   0.5
sky_contract        float   0.22

pillar_radius       float   1300   # 半径（直径 2600）
shock_radius        float   2600
shock_seconds       float   1.5
```

保留（继续使用）：`delay_seconds` / `cooldown_seconds` / `pillar_height` / `pillar_life` / `whiteout_seconds` / `shake_*` / `explosion_sound*` / `falloff_*` / `max_damage` / `min_damage` / `sigil_style` / `sigil_density` / `rune_ticks` / `tick_ring_count` / `star_points` / `star_skip` / `polygon_sides` / `double_line` / `sigil_seed`。

## 8. 性能与崩溃防护

- Beam 预算：穹顶 ~480 + 符号带 ~90 + 天空法阵 ~950 + 光柱 ~12 + 冲击环 ~90 ≈ **~1700**。
- 创建分批（穹顶 7 批 / 天空 16 批）→ 峰值同帧创建 < 400。
- 拆除沿用 `SigilTeardown` 分帧（每 tick ~300 根）。**绝不同帧批量删上千 beam**（实测 526~1170 阈值会触发 `tier0` 写 NULL 原生崩溃）。
- 新增粒子均有 `lifeSeconds` 自动回收；避免与引爆实体创建挤在同一帧。

## 9. 验证

1. `dotnet build RollTheDice.csproj -c Release --no-incremental`（锁 1.0.373）无错误。
2. 部署：备份 dll → 覆盖 → 同步 `RollTheDice.json` 配置段。
3. 实机：`!givedice * FinalJudgment`（或 `@rollthedice/admin`），观察穹顶生长 → 天空阵群 → 光柱 → 冲击环 → 伤害。
4. 伤害范围：两名玩家一近一远，验证近处满伤、远处衰减、范围外无伤。
5. 无 dice 异常堆栈（`$css\logs\rtd_debug.txt` / `log-cssharp*.txt`）；无新增 `.mdmp`。
