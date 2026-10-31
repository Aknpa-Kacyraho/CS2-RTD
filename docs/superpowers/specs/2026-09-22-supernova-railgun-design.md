# 光之剑：超新星 —— 电磁炮构造体（3D 化）设计

- 日期：2026-09-22
- 涉及：`Supernova`（光之剑·超新星，史诗）、新增 `Utils\RailgunRig.cs`
- 状态：方向已与用户确认（"特效是 2D，我要 3D" → "参考爱丽丝的超新星" → "不要用模型，可用 CBeam 或别的方法"）；配色＝**白甲 + 青蓝光**
- 硬约束：**零素材依赖**——不引入模型 / 工坊 addon；只用 CBeam + 基础游戏粒子 / 音效 → 插件可直传 GitHub

## 1. 背景与诊断

现状（`Dices\Supernova.cs`）：刃身 = 3 条**同轴 CBeam**（淡金辉光 2.4× + 主金 20 + 白芯 9），起点一圈 8 段光环 + 4 短刺，末端 4 根十字。CBeam 是 sprite 面片（永远面向镜头的一张"片"），三层只是**同一平面上的宽窄叠加** → 观感是"一片平面光"（2D）。

参考对象（用户指定）：**《蔚蓝档案》天童爱丽丝的固有武器「光之剑：超新星」**——宇宙战舰主炮级的**巨型电磁轨道炮**：白色装甲 + 青色导轨/能量通道 + 大炮口 + 橙色能量电池 + 开阖机关。所以"3D 化"＝**用 CBeam 拼出一门轨道炮构造体**，而不是把激光加粗。

## 2. 设计

### A. 炮体构造体（`Utils\RailgunRig.cs`）

局部坐标 `(x=右, y=前, z=上)`，炮尾在 `y=0`；每 tick 按玩家准星帧 `(f, r, u)` 变换到世界：

| 部件 | 构成 | 束数 |
|---|---|---|
| 机匣 | 4 长棱 + 2 箍环（6 段）+ 4 斜撑 | 20 |
| 炮管 | 4 长棱 + 3 箍环（6 段） | 22 |
| 双导轨 + 能量通道 | 上/下导轨 2 + 中间亮通道 1 | 3 |
| 炮口 | 外环 8 + 内膛暗环 8 + 后方线圈环 8（脉冲） | 24 |
| 顶部导轨 / 瞄具 | 1 长梁 + 2 小环（4 段） | 9 |
| 握把 / 枪托 / 电池 / 缆线 | 2 + 2 + 2 + 2 | 8 |
| **合计** | | **≈86** |

配色（爱丽丝）：装甲＝白/浅蓝白；导轨/通道/线圈＝青；内膛＝深蓝灰；电池＝橙；缆线＝灰。

位置：`胸口 + 右×gun_side(42) − 上×gun_drop(18) + 前×gun_forward(30)`，炮口朝准星（平行）→ **第一人称可见、不挡准星**；光束从炮口射出到伤害线末端。

### B. 充能（`charge_seconds`，默认 0.5s）

- 炮体**逐段亮起组装**（部件按 Reveal 顺序创建 + 落位宽度脉冲；≈3 束/tick，避免同帧创建上百实体）
- 导轨**张开**（±6 → ±11，sin 曲线，发射瞬间合拢）＝ 开阖机关
- 能量通道的亮段从后往前扫；线圈环脉冲加速
- 到点：炮口爆发（`EnergyCircle` 粒子）+ 震屏 + 全服音效（`CSGO_EMP.Impact`）→ 进入发射

### C. 光束（发射 5s）

- 3 层（`CreateStyled`：光晕 + 首尾淡出）：白芯 9 / 青主 20（halo 1.2）/ 淡蓝辉光 2.4×
- **流光环**：`ring_count`(3) 个圆环沿光束从炮口向末端循环推进（Excalibur 奔流光环同款）→ 立体感
- **脉冲**：每 `pulse_interval`(0.35s) 一道亮段从炮口向外推进 + 炮口电光（电磁炮连射感）
- 命中反馈：`hit_spark` → 基础游戏电火花（节流 0.15s）
- 结束：0.2s 收细淡出 → 转 `SigilTeardown` 分帧拆除（120/tick）

### D. 不变的部分（机制/伤害零改动）

5s、100HP/s（`dps×dt` 累积小数）、`hit_radius` 70、扫射、CD 30s、减速 50%、组合（`DivinePunishment` ×1.5、`LaserCage` +40）、尊重 `Invulnerability`；伤害线仍是"胸口 → 胸口 + range×准星"（炮体与光束纯视觉）。新增的 0.5s 充能发生在**开火前**（`charge_seconds` 可设 0 关闭）。

## 3. 素材与性能

- 粒子全部取自 `ParticlePaths`（已随 `PrecacheAll` 预缓存）＋ 基础游戏 soundevent → **无需新增预缓存、无需换图**
- 炮体 ≈86 + 光束 3 + 环 30 + 脉冲 2 ≈ **121 束**；只在开火中的 1~2 名玩家上、每 tick 变换；低于穹顶（≈1366 束 / 每 tick ≈324）
- 拆除走 `SigilTeardown`，不在结束帧批量删

## 4. 配置（`supernova`）

保留：`enabled / duration_seconds / cooldown_seconds / range / damage_per_second / hit_radius / slow_percent / beam_width_outer / beam_width_inner / glow_width_scale / hit_spark / blast_sound / blast_sound_volume`（语义微调：outer=主层、inner=白芯；`blast_sound` 默认改基础游戏 `CSGO_EMP.Impact`）。
删除：`hand_ring` / `tip_cross`（旧平面剑格/剑尖）。
新增：`charge_seconds / gun_enabled / gun_scale / gun_side / gun_drop / gun_forward / ring_count / ring_radius / ring_speed / pulse_interval / hit_particle / muzzle_particle`。

## 5. 验证

1. `dotnet build RollTheDice.csproj -c Release --no-incremental` 无错误
2. 备份 dll → 部署 → `css_reload RollTheDice`
3. 实机 `!givedice * Supernova` 按 E：充能组装 → 炮口爆发 → 白蓝光束扫射（炮体/光束跟随准星；第三人称立体、第一人称不挡准星）
4. `log-cssharp*.txt` 无异常、无新增 `.mdmp`
