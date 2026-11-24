# v1.4.19 自检（cycle 16）

日期：2026-11-24

## 变更清单

| 项 | 文件 | 状态 |
|---|---|---|
| 新增 `effects.hud_duration` | `RollTheDice.Configs\EffectsConfig.cs` | 完成 |
| reveal 聊天第二行直接给描述 | `RollTheDice\RollTheDice.Announce.cs` | 完成 |
| HUD 时长改为配置 + 钳制 | `RollTheDice\RollTheDice.Announce.cs` | 完成 |
| 测试守卫 ×2 | `tests\RollTheDice.RegistryTests\Program.cs` | 完成 |
| VERSION / CHANGELOG | 仓库根目录 | 完成 |
| 文档 5 份 | `docs\upgrade-2026-11-24-dice-hint\` | 完成 |

## 构建与测试

```text
dotnet build RollTheDice.csproj -c Release --no-incremental
  → 0 warning, 0 error

RollTheDice.ConfigTests
  → 32 passed, 0 failed

RollTheDice.RegistryTests（RTD_CONFIG_PATH 指向真实配置）
  → 77 passed, 0 failed
  → 标准（不设 RTD_CONFIG_PATH）应为 76

check-rtd-architecture.ps1
  → timers: 0 (budget 2)
  → main class lines: 2659 (budget 2900)
  → Architecture check passed.
```

## 产物

```text
artifacts\RollTheDice\1.4.19\RollTheDice.dll
  → 982,016 bytes
  → 2026-11-24 13:07:39
  → 含 hud_duration / HudDuration 符号
  → 未包含 CounterStrikeSharp.API.dll
```

## 行为核对

- [x] 描述存在：聊天 = 名字行 + `└ 描述` 行
- [x] 描述缺失/Unknown：聊天第二行 = 旧 hint
- [x] `hud: true`：HUD 使用 `hud_duration`，默认 8
- [x] `hud: false`：不发中央 HUD，聊天仍有描述
- [x] `hud_duration <= 0`：回退 8
- [x] `hud_duration > 30`：钳制到 30
- [x] 未改动任何 Dice 数值、概率、命令、已有配置 Key

## 尚未验证

- [ ] 真人进服看到的两行聊天与 8 秒 HUD（BOT 不触发 RTD）
- [ ] 其他插件 `PrintToCenterHtml` 是否仍会覆盖中央面板
- [ ] 中文描述超过聊天字符上限时的截断表现

## 部署记录

- 部署前备份：`备份\deploy_20261124_130901_v1419_live\`（插件目录 + 配置，46 个文件 / 4.37MB）
- 部署后 DLL：982,016B，2026-11-24 13:07:39
- 配置：`effects.hud_duration = 8` 已写入，UTF-8 无 BOM
- 配置校验：`validate-configs.ps1` 通过（15 files）
- `rtd_health`：**未验证** —— CS2 1.42.0.1 与 Metamod/CSS 不兼容，加载即崩
- 无新 mdmp：**未验证**
- 发布：tag `v1.4.19`，Release asset `RollTheDice-1.4.19.zip`（1,458,977B）

## CS2 1.42 阻塞记录

- CS2 于 2026-11-23 14:44 更新到 **1.42.0.1**
- Metamod 2.0.0.1491（2026-11-15）、CSS 1.0.380（2026-11-17，适配 1.41.9.7）均未适配
- 崩溃：`cs2.exe` → `tier0.dll`，`0xC0000005`，偏移 `0xbc62c`
- 隔离结果：无 Metamod 稳定；只留 Metamod 核心稳定；Metamod + CSS 崩；Metamod + Bot 插件崩
- 处置：`gameinfo.gi` 切回 Online（无 metamod），恢复所有 vdf，环境可玩原版
- 待办：上游更新后切回 WithBots，补 smoke test 与真人回归
