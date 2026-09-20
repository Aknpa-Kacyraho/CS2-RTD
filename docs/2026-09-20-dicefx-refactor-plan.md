# DiceFX 配置化重构（P1）实现计划

- 依据 spec：`docs\superpowers\specs\2026-09-20-dicefx-config-refactor-design.md`
- 工程：单工程 `RollTheDice.csproj`（`RollTheDice.Utils\` 只是文件夹，全编进 `RollTheDice.dll`）
- 构建：`dotnet build RollTheDice.csproj -c Release`（锁 CSS 1.0.373 / net10.0）
- 原则：**先做等价迁移，再删旧表**；每步可独立构建验证。

---

## 步骤

### 1. 新增 `RollTheDice.Utils\DiceFxTable.cs`（纯数据层）
- 反序列化模型：
  - `FxSlot` `{ string Particle; FxMode Mode; float Z; float Radius; float Speed; float Interval; }`
  - `FxGeom` `{ float Behind; float Spread; float Back; float WingZ; }`
  - `FxProfile` `{ FxSlot Trigger{burst,remove}; FxSlot Attach; FxSlot Orbit; FxSlot Trail; FxGeom Geom; FxEvents Events; }`
  - `FxEvents` `{ Fire, Kill, KillSelf, KillHeadshot, Death, Hurt, Hit, HitHeadshot, RoundStart, RoundEnd }`
- 默认值按 spec §5 表；`JsonPropertyName` 一一对应（snake_case 或原样 camelCase，**集中在一处**）。
- `static class DiceFxTable`：`Reload(string path)`、`TryGet(string className, out FxProfile)`、`Count`、`All`。
- `JsonSerializerOptions`：`PropertyNameCaseInsensitive=true`、`ReadCommentHandling=Skip`、`AllowTrailingCommas=true`、`Converters.Add(new JsonStringEnumConverter())`。
- 校验（fail-soft）：版本、未知 dice、未知 mode、数值非法 → 警告日志（`Logger`）+ 默认值。
- **验证**：`dotnet build` 通过；一段临时脚本喂一份样例 JSON 能解析出正确字段。

### 2. 离线迁移工具（优先）— 生成 `dicefx.json`
- 目的：反射调用旧 `DiceEffects.Build()`（私有、纯数据、不碰引擎），物化 `Burst`/Rare 的 `OnKill`，序列化 173 条。
- 做法：临时 console 工程引用已构建的 `RollTheDice.dll` + `CounterStrikeSharp.API.dll`（同目录 `AssemblyResolve` 兜底），反射 `GetMethod("Build", NonPublic|Static)` → 遍历 → 写 JSON（`WriteIndented`）。
- 输出：`configs\plugins\RollTheDice\dicefx.json`（UTF-8 无 BOM）。
- **兜底**：工具跑不通 → 临时加 `rtdfx dump`（admin）在游戏内导出一次。
- **验证**：键集 == Build() 键集（173）；逐条比对物化结果。

### 3. 改写 `RollTheDice.Utils\DiceEffects.cs`
- 删除：`Build()`、`Filler()`、`Profile`、`Theme`、`FxTier` 及其引用。
- 引擎方法签名不变（`OnDiceAdded/Removed/OnTick/OnWeaponFire/OnPlayerKill/OnPlayerDeath/OnPlayerDamaged/OnRoundStart/OnRoundEnd/DescribeEffects/OnPlayerLeft/ClearAll`），内部改为 `DiceFxTable.TryGet`。
- `OnTick` 里 attach/orbit 几何字段改从 `FxProfile.Geom`/`FxSlot` 读取；`trail.z` 用 `FxSlot.Z`（旧常量 4 → 现可配）。
- `DescribeEffects` 保持旧类别集合（**不含** `Remove`）。
- **验证**：编译通过；离线跑「新旧 DescribeEffects 类别集合」逐条相等。

### 4. 挂载重载 + 新命令（`RollTheDice\RollTheDice.cs`）
- `ReloadConfigFromDisk()`（:305）与 `CommandAdmin` 的 `reload` 分支（:274）各加 `DiceFxTable.Reload(configDir)`。
- 路径：`Path.Combine(Path.GetDirectoryName(Path.GetDirectoryName(ModuleDirectory))!, "configs","plugins", Path.GetFileName(ModuleDirectory), "dicefx.json")`。
- 新增 `[ConsoleCommand("rtdfx")]` `!rtdfx reload`（权限 `@rollthedice/admin`）。
- **验证**：`!rtdfx reload` 改一处粒子后热生效；删文件后日志报错、dice 本体正常。

### 5. `RollTheDice.Utils\Effects.cs` 收尾
- `PlayScaled/AttachScaled/SetScale/ApplyGlowScale` 标 `[Obsolete]`，新表不再引用（项目 `<NoWarn>` 已含 CS0618）。
- **验证**：编译 0 新增警告（除已知 NoWarn）。

### 6. 全量校验（离线）
1. `dicefx.json` 键集 == 173。
2. 每条非空粒子路径 ⊂ `_workshop_analysis\aknpa_fx_catalog.json`（P0 目录）或 VPK 文件清单。
3. 新旧 `DescribeEffects` 类别集合逐条一致。
4. `dotnet build -c Release --no-incremental` → 0 warning / 0 error。

### 7. 部署与冒烟
- 备份 `plugins\RollTheDice\RollTheDice.dll` → `*.bak_yyyyMMdd_HHmmss`。
- 覆盖 dll；放置 `configs\plugins\RollTheDice\dicefx.json`。
- `css_reload RollTheDice`（或重启）→ 看日志 `Loaded 173 fx profiles`。
- 冒烟：`rtdeffect <某路径> self 3` 能播；`!rtdfx reload` 生效。
- 回滚：还原 dll 备份即可（`dicefx.json` 留着无害）。

---

## 验证矩阵

| 项 | 方法 | 通过标准 |
|---|---|---|
| 编译 | `dotnet build -c Release --no-incremental` | 0 warning / 0 error |
| 键集 | 脚本比对 JSON keys vs Build() keys | 完全相等（173） |
| 路径有效 | 脚本比对 P0 目录 | 非空路径 100% 命中 |
| 行为等价 | 新旧 DescribeEffects 集合 | 逐条相等 |
| 热重载 | `!rtdfx reload` | 改粒子即时生效 |
| 缺失容错 | 重命名 dicefx.json | 日志报错、dice 正常、无崩溃 |

## 风险

- **离线反射**：`RollTheDice.dll` 加载需 CSS API 程序集；用 `AssemblyResolve` 指向插件输出目录。失败即走 `rtdfx dump` 兜底。
- **字段映射错位**：所有 `JsonPropertyName` 集中在 `DiceFxTable.cs` 一处，便于核对。
- **大表 diff**：生成后 `dicefx.json` 约 1500+ 行，属预期；靠脚本校验而非肉眼。
