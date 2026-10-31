# 天空法阵强化 + 光之剑特效设计

- 日期：2026-09-21
- 涉及：`FinalJudgment` 的天空法阵群（`SkySigilField`）、`Supernova`（光之剑·超新星）
- 状态：已与用户确认（"天空简陋"→ 强化；"给光之剑加上特效"），待实现

## A. 天空法阵群强化（`Utils\SkySigilField.cs`）

### A1. 诊断（为什么"简陋"）

1. **预算把每层砍空**：`cap = beam_budget × r/Σr`，`sky_beam_budget=1400` 时最大层只准 175 根，而 `SigilStyle` 大层实需 ~440 根 → `RemoveChunk` 从末尾摘掉**整个最内侧阵眼**（内环/星/辐条全没了）。
2. **细节系数过小**：`detail = clamp(r/1400, 0.25, 0.6) × sky_detail(0.75)` → 大层 0.45、小层 0.19，要素只有一半。
3. **16 层是孤立水平圆盘**：无连接、无轴、无厚度 → 抬头看不到"装置"，只看到一圈圈线。

### A2. 对策

| # | 项 | 实现 | 成本 |
|---|---|---|---|
| 1 | 补细节/预算 | 每层 `detail = clamp(r/1400, 0.32, 0.85) × sky_detail`；配置 `sky_detail 0.75→0.9`、`sky_beam_budget 1400→2800`、`sky_animated_layers 3→4`；**单层 cap 上限 420**（保护"单帧创建大量 CBeam"） | 风格刻画 ~1300 → ≤2800 |
| 2 | 每层边缘厚度（`sky_rim`） | **大层**（偶数 index）外缘一圈**朝下的立式短齿**（`RimTicks`，`height=-r*0.030`；玩家在下方抬头看 = 盘的下沿） | 8×~24 = ~190（静态） |
| 3 | 层间连接骨架（`sky_spines`） | **小层**（奇数 index）上挂 6 根**跨接两侧大层**的斜向连接线（`Add(r_prev, a, Δh_prev → r_next, a+0.22, Δh_next)`），逐层相位错开 → 螺旋漏斗塔 | 8×6 = 48（静态） |
| 4 | 中央主轴 + 轴承环（`sky_axis`） | 在最早出现的层上建**贯穿最低层→最高层**的 4 根细竖线（带光晕）；**大层**中心各加一圈小环（轴承感） | 8+8×10 = ~88（静态） |

**关键实现约束**：
- 装置件（2/3/4）**不计入也不被预算砍**：顺序必须是 `AddRange(风格) → 砍到 cap → AddRange(装置件)`（`RemoveChunk` 从末尾摘，装置件在砍之后才加）。
- 所有装置件的坐标**相对所在层的中心**（`SigilShapeGroup.Update` 传的是 layerCenter）：主轴 z = `0 → hTop-h0`、连接线 z = `h_prev-h_cur → h_next-h_cur`；`RimTicks` 传层中心 + `zBase=0`。
- 装置件全部 `spin=0`（静态，零每 tick 成本）。
- 天空每 tick 自转图元 ≈ 406（4 个大层）→ stride=2 后平均 ≈ 203。

### A3. 新增配置键（`final_judgment`）

```text
sky_rim      bool   true    # 大层外缘立式齿（厚度感）
sky_spines   bool   true    # 大层之间的斜向连接骨架
sky_axis     bool   true    # 中央主轴 + 大层轴承环
```

## B. 光之剑·超新星特效（`Dices\Supernova.cs`）

机制/伤害**零改动**，只加视觉：

| # | 项 | 实现 | 成本 |
|---|---|---|---|
| 1 | **三层剑身** | 现"金 + 白芯"外加一层**淡金辉光**（宽 = `beam_width_outer × glow_width_scale`(2.4)） | 3 根 CBeam |
| 2 | **剑格**（`hand_ring`） | 起点处**垂直于光束**的一圈环（8 段）+ 4 根径向短刺（R 26→60）：右向/上向基 `right=fwd×ẑ`、`up=right×fwd` | 12 根 |
| 3 | **剑尖**（`tip_cross`） | 末端 4 根垂直短刺（十字锋，长 34） | 4 根 |
| 4 | **开火瞬间** | `Effects.Play(start, GoldHaloRaysRadiate, 1.2f)` + `Effects.SoundAll(blast_sound, vol)`（默认 `sounds/touhou/xrole/youmu/slash_glow.vsnd`，**已在 legendary_sounds → 已预缓存，无需新增**） | 一次性 |
| 5 | **命中反馈**（`hit_spark`） | `ApplyDamage` 记录最近命中点 → 节流 0.15s 播 `GoldHaloFlare`（命中点） | 一次性 |

剑格/剑尖每 tick 按当前 `EyeAngles` 重算（剑格 12 + 剑尖 4 + 剑身 3 = **19 根/施法者**，可忽略；只对开火中的 1 名玩家）。

### B1. 新增配置键（`supernova`）

```text
glow_width_scale  float  2.4     # 外层辉光宽度倍率（×beam_width_outer）
hand_ring         bool   true    # 剑格（起点垂直双环 + 短刺）
tip_cross         bool   true    # 剑尖十字
hit_spark         bool   true    # 命中金色爆点
blast_sound       string sounds/touhou/xrole/youmu/slash_glow.vsnd
blast_sound_volume float 0.8
```

## C. 性能账

| 项目 | 现状 | 改后 |
|---|---|---|
| 天空 CBeam | ~1300 | ~3100（风格 ~2800 + 装置件 ~330） |
| 天空每 tick 自转（原始/平均） | ~330 / ~165 | ~406 / ~203 |
| 穹顶 CBeam | ~1366 | 不变 |
| 穹顶每 tick（原始/平均） | ~170 / ~85 | 不变 |
| 两 dice 每 tick 平均合计 | ~250 | ~288 |
| 单帧创建上限 | ~400 | ≤420（新增 cap 保护） |

## D. 改动文件

| 文件 | 改动 |
|---|---|
| `RollTheDice.Utils\SkySigilField.cs` | Options 3 键；`Build` 细节区间/单层 cap 保护/装置件（`BuildDecor`）；z 以层中心为原点 |
| `RollTheDice.Configs\FinalJudgmentConfig.cs` | 3 个新键 + Options 映射（`FinalJudgment.cs`） |
| `RollTheDice.Utils\`（复用） | `RimTicks` / `Spokes3D` / `SigilBuilder.StarAngles` / `SigilShape.Add`（跨层线段） |
| `RollTheDice.Dices\Supernova.cs` | `BeamSet`（3 层）+ `BladeFrame`（剑格/剑尖）+ 开火粒子/音效 + 命中反馈 |
| `RollTheDice.Configs\SupernovaConfig.cs` | 6 个新键 |
| `$css\configs\plugins\RollTheDice\RollTheDice.json` | 两段新键 + 天空让路值调整 |
| `RollTheDice\RollTheDice.cs` | `rtdsky` 回显（若需） |

## E. 验证

1. `dotnet build RollTheDice.csproj -c Release --no-incremental` 无错误。
2. 备份 dll → 部署 → 配置同步（UTF-8 无 BOM）。
3. `rtdsky 30`：观察逐层弹出 + 主轴/骨架/边缘齿；回显实际束数（应 ≤ 预算 + 装置件）。
4. `rtddome 20` 无回归（穹顶不受影响）。
5. 实机 Supernova：`!givedice * Supernova` 按 E → 三层剑身 + 剑格/剑尖跟随准星 + 开火爆发/音效 + 命中爆点。
6. 无 `log-cssharp*.txt` 异常、无新增 `.mdmp`；`rtdsky` 拆除走 `SigilTeardown` 分帧。
