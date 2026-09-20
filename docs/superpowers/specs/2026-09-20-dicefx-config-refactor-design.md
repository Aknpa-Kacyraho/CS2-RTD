# DiceFX 配置化重构（P1）设计

- 日期：2026-09-20
- 范围：RollTheDice 的**事件驱动常驻特效系统**（`RollTheDice.Utils\DiceEffects.cs`）结构重构
- 性质：设计文档（spec）。实现另出计划（writing-plans）。
- 上游背景：`docs\research\2026-09-20-cs2-particle-system.md`（粒子机制）、`_workshop_analysis\aknpa_fx_catalog.md`（素材目录，P0）

---

## 1. 目标

把"每个 dice 用哪些粒子/特效、怎么挂"从 **C# 硬编码表** 迁到 **`dicefx.json`**；C# 只保留**引擎**职责。改特效/调参**不再需要重编译**。

同时消除三个已知问题：
1. `DiceEffects.cs` 1127 行，数据表 + 事件分发 + 挂点几何 + 生命周期混在一起。
2. 隐式魔法：`Burst = burst ?? theme`、`Rare` 档 `OnKill ??= theme`（`Filler`）——"真相"分散在代码里。
3. 误导抽象：`Effects.PlayScaled / AttachScaled / SetScale / ApplyGlowScale` 依赖 `RadiusScale`，调研已证其**非通用倍率**。

## 2. 非目标（YAGNI）

- 不做"可组合效果步骤解释器"（一个事件挂多段、各自延迟/时长）——现有行为不需要。
- 不做 per-dice 的**事件**生命期配置（`BurstLife/EventLife/DeathLife/FireLife` 仍是代码常量）。持续类（attach/orbit/trail）的 z/几何本来就 per-dice，照旧可配。
- **不换素材**（那是 P2）、**不加新特效原语**（那是 P3）。
- 不改 `EffectsConfig`（`enabled/trails/hud`）语义。
- 不碰"按 E 的终极 dice"自己 `OnTick` 里 spawn 的 CBeam 逻辑。

## 3. 行为等价（parity）是第一约束

迁移必须与原行为**逐项一致**：
- `Build()` 中 `A()` 会做 `Burst = burst ?? theme`（对**所有**档位）。
- `Filler()` 仅对 **`Rare`** 档做 `OnKill ??= theme`。
迁移时把这些**物化**为 JSON 里的显式值；之后删除 `Theme` / `Filler` / `FxTier`。
- `FxMode` 枚举**保留**（引擎 `OnTick` 仍用）。

## 4. 配置位置与加载

- 文件：`configs\plugins\RollTheDice\dicefx.json`（UTF-8 无 BOM）。
  路径由 `<css>/plugins/RollTheDice`（`ModuleDirectory`）上溯两级再拼 `configs/plugins/RollTheDice/dicefx.json` 得到。
- 加载/重载挂点（两处，覆盖全部路径）：
  1. `RollTheDice.ReloadConfigFromDisk()`（`RollTheDice.cs:305`）——覆盖 `Load`、换图重载。
  2. `CommandAdmin` 的 `reload` 分支（`RollTheDice.cs:274`）——覆盖 `!rollthedice reload`。
- 另外提供一个只重载特效表的命令 `!rtdfx reload`（权限 `@rollthedice/admin`），便于单独迭代。
- 解析失败 → 保留上一份成功的**内存**表（首次失败则特效关闭）；日志报错，**不影响 dice 本体**。

## 5. `dicefx.json` schema（完整 + 分组）

```jsonc
{
  "version": 1,
  "dice": {
    "God": {
      "trigger": { "burst": "particles/...vpcf", "remove": "" },
      "attach":  { "particle": "particles/...vpcf", "mode": "wings", "z": 40 },
      "orbit":   { "particle": "particles/...vpcf", "mode": "behind",
                   "radius": 70, "speed": 2, "z": 40 },
      "trail":   { "particle": "particles/...vpcf", "interval": 2, "z": 4 },
      "geom":    { "behind": 26, "spread": 34, "back": 22, "wingZ": 52 },
      "events":  {
        "fire": "", "kill": "", "killSelf": "", "killHeadshot": "",
        "death": "", "hurt": "", "hit": "", "hitHeadshot": "",
        "roundStart": "", "roundEnd": ""
      }
    }
    // … 共 173 条，key = dice 的 ClassName（反射匹配 DicesConfig 属性名）
  }
}
```

**字段语义与默认值**（省略即默认；空串 = 该事件/槽无特效）

| 组 | 字段 | 默认 | 对应旧字段 |
|---|---|---|---|
| trigger | `burst` | `""` | `Burst`（物化后） |
| trigger | `remove` | `""` | `Remove` |
| attach | `particle` | `""` | `Hold` |
| attach | `mode` | `around` | `HoldMode` |
| attach | `z` | `40` | `HoldZ` |
| orbit | `particle` | `""` | `Orbit` |
| orbit | `mode` | `behind` | `OrbitMode` |
| orbit | `radius` | `70` | `OrbitR` |
| orbit | `speed` | `2` | `OrbitSpeed` |
| orbit | `z` | `40` | `OrbitZ` |
| trail | `particle` | `""` | `Trail` |
| trail | `interval` | `2` | `TrailInterval` |
| trail | `z` | `4` | （旧为常量 `TrailZOffset`，提为可配） |
| geom | `behind` | `26` | `BehindDist`（attach 的 behind 与 orbit 的 behind 共用） |
| geom | `spread` | `34` | `WingSpread`（attach/orbit 的 wings 共用） |
| geom | `back` | `22` | `WingBack` |
| geom | `wingZ` | `52` | `WingZ` |
| events | `fire` | `""` | `OnFire` |
| events | `kill` | `""` | `OnKill`（Rare 档物化） |
| events | `killSelf` | `""` | `OnKillSelf` |
| events | `killHeadshot` | `""` | `OnKillHeadshot` |
| events | `death` | `""` | `OnDeath` |
| events | `hurt` | `""` | `OnHurt` |
| events | `hit` | `""` | `OnHit` |
| events | `hitHeadshot` | `""` | `OnHitHeadshot` |
| events | `roundStart` | `""` | `RoundStart` |
| events | `roundEnd` | `""` | `RoundEnd` |

- `mode` 取值：`around` | `behind` | `wings`（枚举 `FxMode`，`JsonStringEnumConverter`）。
- 粒子路径写逻辑路径，带不带 `.vpcf_c` 均可（沿用 `Effects.Normalize`）。

## 6. 代码结构

| 文件 | 职责 |
|---|---|
| `RollTheDice.Utils\DiceFxTable.cs`（新） | JSON 模型（`FxProfile`/`FxSlot`/`FxGeom`）+ 反序列化 + 校验 + `TryGet(className)` + `Reload(path)` |
| `RollTheDice.Utils\DiceEffects.cs`（瘦身） | 引擎：事件分发（`OnDiceAdded/Removed/OnTick/OnWeaponFire/OnPlayerKill/Death/Damaged/OnRoundStart/End/DescribeEffects/OnPlayerLeft`）、挂点几何、Holds/Orbits 生命周期；只读 `DiceFxTable` |
| `RollTheDice.Utils\Effects.cs` | 原语保留；`PlayScaled/AttachScaled/SetScale/ApplyGlowScale` 标 `[Obsolete]` |

- 删除：`Build()`、`Filler()`、`Profile`（私有类）、`Theme`、`FxTier` 及其全部引用。
- `DescribeEffects` 逻辑不变，只是读 `FxProfile` 的槽位非空判断（注意：旧实现**不含** `Remove`，HUD 摘要里也不显示它——保持该行为以保平价）。

## 7. 校验规则（fail-soft）

| 情况 | 处理 |
|---|---|
| `dicefx.json` 缺失 | 错误日志；特效关闭（dice 本体不受影响） |
| JSON 解析/反序列化失败 | 保留上一份成功的内存表；日志报错 |
| dice 键不在现有 ClassName 集 | 警告，忽略该条 |
| 未知字段 | 警告，忽略 |
| `mode` 非法 / 数值越界 | 警告，用默认 |
| 粒子路径不在 VPK | 警告（比对 P0 素材目录/VPK 文件清单），仍播放 |

## 8. 迁移（保证等价）

**首选：离线反射导出**（不依赖游戏）
1. 用现成的 `RollTheDice.Utils.dll`（含旧 `Build()`）写一个最小 console 工具，反射调用私有 `DiceEffects.Build()`。
2. 遍历 173 条 `Profile`，**物化** `Burst`/Rare 的 `OnKill`，序列化为 `dicefx.json`（同名同值）。
3. 校验（§9）通过后，删除 `Build()`/`Filler`/`Theme`/`FxTier`。

**兜底**：离线工具不可行时，临时加 `rtdfx dump`（admin）在游戏内导出一次（一次性动作，非玩法测试）。

## 9. 验证

1. 键集 == 173 个 dice ClassName，无缺无多。
2. 逐条重算 `DescribeEffects` 的类别集合，与旧实现输出**完全一致**（离线可跑）。
3. 表中所有非空粒子路径 ⊂ `_workshop_analysis\aknpa_fx_catalog.json` / VPK 文件清单。
4. 编译 0 warning / 0 error（锁 CSS 1.0.373，net10.0）。
5. `rtdeffect` 抽样能播放；`!rtdfx reload` 能热重载。

## 10. 风险 / 未知

- **字段命名分叉**：JSON 用一个名字、代码用一个名字，容易对不上 → 用一个 `JsonPropertyName` 映射表集中管理，并在 spec 实现时以 §5 表为准。
- **离线反射工具**：`RollTheDice.Utils.dll` 依赖 `CounterStrikeSharp.API.dll`，加载时需正确的程序集解析；若失败即走兜底。
- **`FxMode` 反序列化大小写**：用 `JsonStringEnumConverter`（读取大小写不敏感）。
- **`geom` 抽组**：属于"保持字段但重组"，序列化/反序列化需一一对应，验证覆盖。

## 11. 交付与部署

- 变更：新增 `DiceFxTable.cs` + `dicefx.json`；改写 `DiceEffects.cs`；`RollTheDice.cs` 两处挂载 `FxTable.Reload`；新增 `rtdfx reload` 命令。
- 部署：备份 `RollTheDice.dll` → 构建 → 覆盖 dll → 放置 `dicefx.json` → `css_reload RollTheDice`（或重启）。
- 回滚：还原 dll 备份；`dicefx.json` 可保留（新代码才读）。
