# 实施计划：法阵刻画重构（arcane sigil detail）

日期：2026-09-20
规格：`docs/superpowers/specs/2026-09-20-arcane-sigil-detail-design.md`
说明：`writing-plans` 技能未安装，改用本仓库既有的 `docs/2026-*-plan.md` 约定承载实施计划。

## Goal

把三件套的地面法阵 / 法阵塔从"圆环 + 辐条"升级为多层次刻画法阵（五芒星、符文带、刻度环、
内嵌多边形、多层同心环、顶点辐条、中心纹章），并保持每 tick 网络成本不升。

## 阶段

### Phase 1 — 原语层 `RollTheDice.Utils\SigilShapes.cs` ✅ complete
- `SigilShape`（统一"极坐标线段组 + 自转 + 静态/动画 + 双色描边"）。
- `SigilBuilder`：`Arc` / `Star` / `Polygon` / `Spokes` / `TickRing` / `RuneBand`。
- `SigilParams` / `SigilPalette` / `SigilGeometry.Hash01`。

### Phase 2 — 组合层 `RollTheDice.Utils\ArcaneSigil.cs` ✅ complete
- `ArcaneGroundSigil`（规格 §5 的 14 项；静态件按 center/scale 阈值触发）。
- `ArcaneTower`（8 层 = 细底环 + 轮换刻画）。

### Phase 3 — 接线 `MagicSigil.cs` / `MagicTower.cs` ✅ complete
- 保留 legacy 构造；新增 arcane 构造重载；`Update`/`Remove` 委派。

### Phase 4 — ~~`BeamFx.LiveBeams` 计数器~~ ⛔ cancelled
- 改为 `SigilShape.BeamCount` 精确计数（含晕层），避免实体有效性判定的歧义。

### Phase 5 — 配置 ✅ complete
- `SkyVerdictConfig` / `FinalJudgmentConfig` 各加 9 字段（默认 `sigil_style = "arcane"`）。

### Phase 6 — dice 接线 ✅ complete
- `SkyVerdict.cs` / `FinalJudgment.cs`：按 `sigil_style` 分支；配色 SkyVerdict 铂金 / FinalJudgment 金→赤。

### Phase 7 — 调试指令 ✅ complete
- `rtdsigil [radius] [density] [seconds]`（`@rollthedice/admin`）：脚下生成测试阵 + 塔，
  回复实际 CBeam 数，到期自动移除（0.05s 重复 timer 驱动旋转）。

### Phase 8 — 构建 / 验证
- ✅ `dotnet build RollTheDice.csproj -c Release --no-incremental` → **0 错误**（144 个既有 CS8632 警告）。
- ✅ 部署：备份 `RollTheDice.dll.bak_20260920_225134` → 覆盖 `$css\plugins\RollTheDice\RollTheDice.dll`。
- ⏳ 实机验证（需换图/重载 + `rtdsigil` 目视）——**未做，不能声称视觉达标**。
- 实测 beam 数（density 1.0，解析值）：地面 **613**、塔 **556**、单次 SkyVerdict **1169**（旧 526）。
  以 `rtdsigil` 打印为准。

## 决策记录

| # | 决策 | 理由 |
|---|---|---|
| 1 | 统一"极坐标线段"表示所有图元 | 一个类覆盖圆/星/多边/刻度/辐条，减少重复 |
| 2 | 静态件用 center/scale 阈值触发更新 | 圆环旋转对称，转它看不见；省每 tick 成本 |
| 3 | 双色描边只给关键线条（6 项） | 避免 beam 数翻倍，保留"刻画发光"观感 |
| 4 | `sigil_style` 提供 legacy 回退 | 出问题可一键回退，不改代码 |
| 5 | 晕色改用高饱和暖金/赤（非暗色） | 无加法混合时，"粗暗底"会显成脏棕色；饱和度高的底更像发光 |

## Errors Encountered

| Error | Attempt | Resolution |
|---|---|---|
| `CS0100: 参数名"halo"重复`（SigilShape ctor） | 1 | 把 `bool halo` 改名 `haloEnabled` |
| `BeamCount` 在双色描边时少算一半 | 1 | 改为 `BeamCount += (halo != null) ? 2 : 1` |
| `ArcaneTower` 每帧重摆底环（`_lastScale` 无条件更新） | 1 | 阈值判定提到循环外，只在真正重摆时更新 `_lastScale` |

## Risk

- 单帧 ~1169 实体可能瞬时卡顿 → 待实测，必要时降 `sigil_density`。
- 晕层颜色/宽度只能实机评估。

## 崩溃调查（2026-09-20 23:04:39，FinalJudgment 引爆）

崩溃转储 `game\bin\win64\cs2_2026_0920_230439_0_accessviolation.mdmp`：

- `0xC0000005`（**写**，p0=1）**写到地址 0**，故障点 `tier0.dll+0x128D99`。
- dump 内含 ` !givedice * FinalJudgment` → 确认是**强制给骰子后引爆**的场景。
- 对照：改动前今天的两次崩溃是 `0x80000003`（KERNELBASE，引擎 `Error()`/断言），**签名不同**；且在
  那些 dump 里搜不到 FinalJudgment 痕迹。→ 这次是**新签名**。
- 关键推理：法阵**渲染了整整 20s 没崩**才在引爆瞬间崩 → 不是"实体数超上限就崩"（否则创建时即崩）；
  引爆帧相对旧版（526 根）唯一的新增量 = **一帧内多删 ~644 根 CBeam**。

**处置（画面零改动）**：新增 `SigilTeardown` 分帧拆除队列 + `RemoveChunk/IsEmpty`。引爆时不再同帧
`Remove()` 上千实体，而是交给队列每 tick 拆 ~300 根（~5 tick ≈ 75ms），把"拆除"与"爆炸实体创建"分到不同帧。
`Reset()` 用 `Flush()` 兜底立即清空。

**✅ 实测确认（2026-09-20 23:1x）**：加上分帧拆除后复测引爆，**不再崩溃**。
→ 根因确认为"**同一帧内批量 `Remove()` 上千 CBeam + 生成爆炸实体**"，与纯实体数量无关
（旧版 526 根同帧删没事，1170 根就触发）。规律：**CBeam 的批量销毁不要和爆炸/大量实体创建挤在同一帧**。

## Next Step

实机：`css_reload RollTheDice` → `!givedice * FinalJudgment` 复测引爆。
仍崩则跑 `rtdsigil 520 2 60` 判定分支；想先稳玩可把 `sigil_style` 改成 `"legacy"`。
