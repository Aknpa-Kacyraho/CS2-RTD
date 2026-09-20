# 双翼特效重做：CBeam 光翼（WingFx）

日期：2026-09-20
范围：RollTheDice（CS2 / CounterStrikeSharp 1.0.373 / net10.0）

## 背景

`dicefx.json` 里 7 个 dice 的 `mode: "wings"` 使用 `particles/ui/status_levels/ui_status_level_wings.vpcf`：

- DeathKnight、DeathKnightComplete、RoyalBarrier、Shield、Gargoyle、Paladin（attach）
- WolfKing（orbit）

该文件是 **UI 状态等级复合粒子**（`m_ChildRef` → `ui_status_level_wings_ropes.vpcf`）。它的子粒子在生成点以 world-space 固化，
服务器每 tick 对发射器 `Teleport`（`Effects.MoveTo`）也带不走，实测"钉在原地、不跟人"。

素材盘点结论：

- Aknpa_packs `particles/xrole/*` 里带翅膀的角色（蕾米莉亚 / 芙兰 / 琪露诺 / 蕾蒂）全是弹幕、激光、冰花，**没有羽翼形态**。
- 游戏基数 VPK 里也没有可跟随的翅膀粒子（只有同病的 `ui_status_level_wings*`）。

## 决策

用 **CBeam 自造光翼**（`WingFx`），理由：起终点完全由服务器掌握，位置 / 朝向 / 大小 / 扇动都能精确控制，无素材依赖，
与既有 `MagicCircle` / `BeamFx` 同一套原语，必定跟随。

## 实现

### 新增 `RollTheDice.Utils\WingFx.cs`

- 每侧 N 根 CBeam（默认 4），从肩背根点向外上方扇形张开，外侧羽片更长更外展，勾出翼形。
- 只创建一次光束，之后每 tick `BeamFx.Move` 重算起终点 → 必定跟随。
- 扇动：外展角叠加 `sin(flapPhase) * flapAmount`（默认 10°）。
- 几何（复用 `FxGeom`）：根点 = `origin - fwd*back ± right*(spread*0.3)`，高度 `+wingZ`；
  羽片方向 = `normalize(up*cos + out*sin + back*0.18)`，展开角 18°→80°、长度 0.55→1.0 倍随羽片序号线性铺开。
- `Remove()` 对称销毁全部光束。

### `DiceFxTable.cs`

新增 `FxWingSlot`（`color` / `blades` / `length` / `width` / `flap`），`FxProfile` 加 `wing` 字段，
`Normalize` 里补默认值与越界兜底。

### `DiceEffects.cs`

- 新增 `Winged: Dictionary<(ulong,string), WingFx>`。
- `OnTick`：`attach.Mode == Wings` 或 `orbit.Mode == Wings` 时走 `UpdateWing`（不再走 `HoldPersistent` 的 `#L/#R` 分支）。
- `UpdateWing` 懒创建 `WingFx`，每 tick `Update(origin, yaw, now * flap * 3)`。
- 清理：`RemovePersistentByClass` / `RemovePlayerPersistent` / `ClearPersistentOnly` 都覆盖 `Winged`。
- 新增 `ResetPersistent()`；`rtdfx reload` 成功后调用，让光翼按新配置在下一 tick 重建。

## 配置（dicefx.json）

```json
"wing": { "color": [143,199,255], "blades": 4, "length": 90, "width": 3, "flap": 0.6 }
```

`mode: "wings"` 时忽略 `particle`，只用光翼渲染。

| dice | color |
|---|---|
| DeathKnight | 143,199,255 冰蓝 |
| DeathKnightComplete | 122,77,255 暗紫 |
| RoyalBarrier | 255,204,51 金 |
| Shield | 255,215,0 金 |
| Gargoyle | 154,160,166 石灰 |
| Paladin | 255,224,102 圣金 |
| WolfKing | 255,158,203 樱花粉 |

## 自检清单

- [x] `dotnet build` 通过（0 error）
- [x] `dicefx.json` JSON 合法，7 个 `wing` 配置写入
- [x] 光翼按朝向在背后上方，随玩家移动 / 转身实时重算
- [x] 移除 dice / 死亡 / 离开 / 重载 均对称清理光束
- [ ] 游戏内观感与扇动幅度按实际手感微调（`wing.*` 与 `geom.*`）
