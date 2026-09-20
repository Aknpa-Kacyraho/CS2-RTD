# DiceFX P3 — CBeam 原语 + 声明式光束槽

- 日期：2026-09-20
- 上游：`2026-09-20-dicefx-config-refactor-design.md`（P1）、`2026-09-20-dicefx-reskin-plan.md`（P2）

## 目标
把散落在 11 个 dice 里的 CBeam 重复代码抽成公共原语，并让光束效果**可配置**（改 JSON、不重编译），与 P1 的数据驱动方向一致。

## 3.1 新原语 `RollTheDice.Utils\BeamFx.cs`
- 低层：`Create(color,width)` / `Move(beam,start,end)` / `IsAlive` / `Kill` / `KillAll`
- 柱：`Pillar(ground,height,color,width,life)`（委托 `Effects.BeamColumn`）
- 环：`Ring(...)`（返回 `MagicCircle` 句柄）
- `MagicCircle` 内部改为调用 `BeamFx`；对外 API 不变。

## 3.2 去重重构（部分）
- 已完成：`MagicCircle`、`Supernova`（CreateBeam/MoveBeam/IsBeamValid/RemoveBeam → BeamFx）。
- **未做（有意）**：`BlackHole / MagneticPulse / ThunderChain / Drone / LaserCage / Singularity / WhiteHole` —— 这些文件是反编译产物（`(float?)null` 之类），改动易引入不可见回归，且当前无法进游戏验证。留待后续按 dice 逐个迁移。

## 3.3 声明式光束槽（`dicefx.json` → `beams`）
每个 dice 可加 `beams`，与粒子槽并行；事件：`trigger / remove / kill / killSelf / death / hurt / hit / roundStart / roundEnd`。

规格字段：
| 字段 | 含义 |
|---|---|
| `shape` | `ring` \| `pillar` |
| `color` | `[r,g,b]` |
| `width` / `life` | 线宽 / 存活秒 |
| `spin` / `grow` | 环：每秒转速（圈）/ 生命末半径增量（负=收缩） |
| `radius` / `outer` / `segments` / `spokes` / `z` | 环几何 |
| `height` | 柱高 |

引擎：`DiceEffects.PlayBeams` 在对应事件生成；环句柄由 `OnTick` 驱动旋转/缩放并在生命末回收；柱走一次性 `Effects.BeamColumn`（自带计时回收）。`ClearAll` 一并清理。

已配置 11 个 dice：Ragnarok / Cthulhu / God / World / FireDragon / IceDragon / FourHorsemen / NukeLeak / SkyVerdict / DivineDescent / FinalJudgment。

## 验证
- `dotnet build -c Release --no-incremental` → 0 错误，警告 141（无新增）。
- P2→P3 逐字段 diff：**粒子字段变化 0**（只新增 `beams`）。
- `DiceFxTable` 回读：173 profiles / 4671 粒子字段 **0 不匹配**；`beams` 解析正确（Ragnarok/God 抽查）。
- 部署：dll 与 `dicefx.json`（p3）hash 与构建产物一致；备份 `*.bak_20260920_171905`。

## 生效
`!rtdfx reload`（或 `css_reload RollTheDice` / 重启）。
