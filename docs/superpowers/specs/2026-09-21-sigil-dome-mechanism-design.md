# 坠落天空 · 天穹装置化重构设计（SigilDome v4）

- 日期：2026-09-21
- 对应 dice：`FinalJudgment`（内部标识不变）
- 状态：已与用户确认（"完整对齐参考图结构；可适当减少天上法阵换取穹顶预算，但法阵不偷工减料"），待实现

## 1. 目标

按三张参考图（多层同轴圆盘的"浮空装置"线稿）把 `SigilDome` 从"三层轮盘 + 水平偏心小环"升级为**机械装置感的多层浮空法阵**。核心新增三样图上最醒目的特征：

1. **4 个倾斜侧阵门**（东/西/南/北）：独立的倾斜小圆盘（外环组 + 刻度 + 内环 + 中心核 + 背面厚度环），替换现有 4 个水平偏心小环。
2. **弧形轨道**：一圈分段环形轨道（4 段 60° 弧，段中心对准侧阵门）悬在底盘上方，每段经**弧形连接臂**接侧阵门。
3. **中心主轴 + 立体核心**：贯穿底盘→顶盘的竖直轴，底盘中心六棱台核心（图 1 底盘中心的塔状体）。

外加：底盘外缘**立式符文带**（盘的上唇，图 2「外圈符文环带」）、盘面**贯通十字轴线**（图 3「东西南北轴」）、外环组由双环加密为三环。

## 2. 图 → 实现映射

| 参考图特征 | 实现 |
|---|---|
| 上层/下层主圆盘、主阵内环/外环、外圈符文环带 | 保留 `DialShell` 三层轮盘；外环由双环改**三环 + 环间铭文带** |
| 中心核心 / 顶部椭圆阵面 / 中心圆阵 | 保留花型/核心徽记；底盘中心新增**六棱台核心** |
| 主轴连接层 | 新增**中心主轴**（4 根细竖线绕中心小圆）贯穿至顶盘 |
| 东西南北轴 | 底盘面新增 4 根中心→外缘贯通轴线 |
| 东南西北侧阵门 | 新增 4 个**倾斜侧阵门**（倾角 68°，法线朝外上方） |
| 弧形轨道 / 弧形连接轨道 | 4 段环形轨道弧 + 每段 2 根弧形连接臂 |
| 外圈符文环带（立式） | 底盘外缘**立式短符文**（`RimTicks`）+ 外唇线 + 外唇顶环 |

## 3. 几何（R = `dome_radius`，默认 480）

| 部件 | 参数 |
|---|---|
| 底盘 | 半径 R，盘面 z=0.004R；外三环 1.000/0.955/0.905 + 铭文带 0.905~0.998 |
| 中盘 | 0.70R，z=0.62R |
| 顶盘 | 0.42R，z=1.10R |
| 外缘立式符文 | r=1.002R，z 从 0.004R 到 0.059R，36 根短竖线（长短/缺口随机）；外唇线 0.998R @ z=0.012R；外唇顶环 1.018R @ z=0.055R |
| 环形轨道 | r=1.06R，z=0.30R，4 段弧（段中心对准侧阵门方位，跨 ±30°，每段 8 小段） |
| 连接臂 | 轨道段中点 → 侧阵门中心，上下各 1 条（弧弓 bow=0.03R，8 小段） |
| 侧阵门 | 中心距 1.14R，z=0.34R，半径 0.22R，倾角 68°；构成：外环（halo）/ 次环 0.86 / 刻度环 0.90→1.00（18 根）/ 内环 0.55 / 中心环 0.16 + 十字 / 辐条 0.18→0.95（6 根）/ 背面厚度环（法线偏移 -0.10rg，无 halo） |
| 中心主轴 | r=0.022R 处 4 根竖线，z 从 0.004R 到 1.10R |
| 立体核心 | 六棱台：底六边形 0.055R @ 0.004R、顶六边形 0.038R @ 0.075R、6 根竖棱、顶部 4 根小十字 |
| 贯通轴线 | 0 → 0.98R，4 个方位，z=0.006R |

## 4. 新增原语（`SigilShapes.cs`）

```csharp
// SigilShape：任意空间端点（倾斜面 / 弧形轨道 / 核心用）
public void AddLocal(float x1, float y1, float z1, float x2, float y2, float z2);
// SigilShape：自转参考系 —— 默认 (pivot=0, axis=ẑ) = 原行为（绕中心竖直轴）；
// 倾斜侧阵门传 (门中心偏移, 门法线) → 自转发生在门自身平面内，而不是绕中心公转。
public void SetSpinFrame(float px, float py, float pz, float ax, float ay, float az);

// SigilBuilder：
TiltedRing(center, offsetRadius, offsetAngle, z, radius, segments, tilt, normalOffset,
           spin, core, halo, width, haloEnabled, phase);   // 倾斜圆环（可沿法线偏移出厚度）
TiltedSpokes(center, offsetRadius, offsetAngle, z, rFrom, rTo, count, tilt, normalOffset,
             spin, core, halo, width, haloEnabled, phase);  // 倾斜面内辐条/刻度
ArcTrack(center, a0, r0, z0, a1, r1, z1, bow, segments,
         spin, core, halo, width, haloEnabled);             // 二次贝塞尔弧形轨道/连接臂
RimTicks(center, radius, count, zBase, height, seed,
         core, halo, width, haloEnabled, phase);            // 立式外缘符文（竖直短刻度）
```

**★ 自转参考系（关键）**：`SigilShape.Update` 现为 `p = (r·cos(a+rot)·scale, r·sin(a+rot)·scale, z)`，
这等价于"局部点绕 ẑ 转 rot"。把它泛化为：局部点 `p=(x·scale, y·scale, z)` → `p -= pivot` →
`RotateAroundAxis(p, axis, rot)` → `p += pivot`（`axis=ẑ, pivot=0` 时与原实现逐位等价，现有图元行为不变）。
倾斜侧阵门的图元用 `SetSpinFrame(门中心, 门法线)` → 自转发生在倾斜面内。

倾斜面坐标系：`u=(cos a0, sin a0, 0)` 径向、`v=(-sin a0, cos a0, 0)` 切向、`w=u·cos(tilt)+ẑ·sin(tilt)`；
环上点 `P(t)=O + v·r·cos t + w·r·sin t`，`O = center + u·offsetRadius + ẑ·z + n·normalOffset`，
法线 `n = -u·sin(tilt) + ẑ·cos(tilt)`。`tilt=0°` 水平环、`90°` 竖直环（法线朝外）。

## 5. 动画顺序（沿用 SpawnTime + ease-out 生长）

| 比例 | 部件 |
|---|---|
| 0.00 | 底盘 + 外缘立式符文/外唇 |
| 0.10 | 环形轨道 + 贯通轴线 |
| 0.16 | 4 个侧阵门 + 连接臂 |
| 0.28 | 中心主轴 + 立体核心 |
| 0.42 | 中盘 |
| 0.62 | 顶盘 |
| 0.80 | 空间锚定斜线（现有） |
| 0.95 | 能量柱（现有） |

静态/自转划分：**只有侧阵门的刻度环与辐条自转**（走门平面参考系）；环、厚度环、轨道、主轴、外缘符文、轴线全部静态（零每 tick 成本）。

## 6. 新增配置键（`final_judgment` 段）

```text
dome_gates      bool    true    # 4 个倾斜侧阵门
dome_gate_tilt  float   68      # 侧阵门倾角（度）：0=水平盘，90=竖直（法线朝外）
dome_gate_size  float   0.22    # 侧阵门半径系数（× dome_radius）
dome_tracks     bool    true    # 环形轨道 + 连接臂
dome_axis       bool    true    # 中心主轴 + 立体核心
dome_rim        bool    true    # 外缘立式符文带 + 外唇
```

**`dome_base_ring`（旧）删除**：其控制的 4 个水平偏心小环已被倾斜侧阵门取代；C# 属性与部署 JSON 中的该键一并移除（`System.Text.Json` 忽略未知键，旧配置不会报错），由 `dome_rim` 承接"底盘外缘"语义。

## 7. 性能账

| 项目 | 现状 | 改后 |
|---|---|---|
| 穹顶 CBeam | 859 | ~1450 |
| 穹顶自转图元 | 227 | ~325 |
| 天空 CBeam | 1489 | ~1300（`sky_beam_budget` 1600→1400、`sky_animated_layers` 4→3；`sky_count`/`sky_detail`/每层构成不动） |
| 合计每 tick 平均（stride=2） | ~324 | ~310 |

## 8. 改动文件

| 文件 | 改动 |
|---|---|
| `RollTheDice.Utils\SigilShapes.cs` | `SigilShape.AddLocal`；`SigilBuilder` 新增 4 原语 |
| `RollTheDice.Utils\SigilDome.cs` | `SigilDomeOptions` 新键；装配新部件；`DialShell` 外三环 |
| `RollTheDice.Configs\FinalJudgmentConfig.cs` | 6 个新键；删除 `dome_base_ring` |
| `RollTheDice\RollTheDice.cs` | `rtddome` 选项映射 + 回显 |
| `$css\configs\plugins\RollTheDice\RollTheDice.json` | 6 个新键 + 删 `dome_base_ring` + 天空让路值 |

## 9. 验证

1. `dotnet build RollTheDice.csproj -c Release --no-incremental` 无错误。
2. 备份 dll → 部署 → 重启/换图（CBeam 无预缓存需求，但配置键要重载）。
3. 实机 `rtddome 20`：观察 底盘/外缘符文 → 轨道 → 侧阵门 → 主轴核心 → 中盘 → 顶盘 → 锚线 → 能量柱。
4. 实机 `!givedice * FinalJudgment` 走完整 20s 引爆，确认无异常堆栈、无 `.mdmp`。
5. `rtdsky` 回显确认天空降预算后仍完整（每层风格/环带无缺失）。
