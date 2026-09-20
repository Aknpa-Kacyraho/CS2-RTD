# DiceFX 换素材（P2）方案

- 依据：`aknpa_fx_catalog.md`（P0 目录）、`dicefx.json`（P1 产物）
- 前提：所有新粒子均为 **非屏幕空间 + 非复合 + 有 renderer**（从反编译属性筛出）
- 未进游戏测试：按名称/语义挑选，个别不合意可直接改 `dicefx.json`

---

## P2a — 修真正的屏幕空间错用（19 处）

反编译证实：69 条在用路径里**只有 3 条**是 `m_bScreenSpaceEffect=true`（会被画到 HUD 上，不是世界特效）：

| 原路径 | 用在哪 | 替换为 |
|---|---|---|
| `survival_fx/danger_zone_loop_black` | Cthulhu/FourHorsemen/BlackHole/GravityWell 的 burst·kill | `particles/xrole/suika/blackhalo.vpcf` |
| `survival_fx/danger_zone_loop` | NukeLeak burst | `particles/xrole/suika/blackball.vpcf` |
| `survival_fx/danger_trail_spores` | BoneMaggot/Parasite/Plague/PoisonBlade 的 burst·trail·kill·hit | 见 POISON 调色板 |

---

## P2b — 主题升级（全部 173 个 dice）

> 实现采用**路径翻译表**（`_workshop_analysis\tools\p2_reskin.ps1`）：保留功能性粒子（弹壳/枪口/爆炸/脚步/雷达/金钱），
> 只把 **46 条主题性 stock 粒子**翻译成 Aknpa 世界粒子 → 共改动 **657 处赋值**（涉及所有用到主题粒子的 dice，含稀有/普通）。
> 下表按主题汇总目标粒子；翻译表以脚本为准（含目标「非屏幕空间 + 非复合」校验）。

同一主题的 dice 共用一套调色板（与旧表"同档共用 stock 粒子"的思路一致，风格统一、易维护）。槽位为空则保持原值。

### FIRE — FireDragon, Phoenix, Dragonborn, GunGod, Titanfall
| 槽 | 粒子 |
|---|---|
| burst | `particles/xrole/suika/fireball_explode_trail.vpcf` |
| trail | `particles/xrole/suika/fireball_trail.vpcf` |
| kill | `particles/touhou/laser/laser_fire.vpcf` |
| killSelf | `particles/xrole/suika/fireball_trail.vpcf` |
| hurt | `particles/xrole/suika/fireball_trail.vpcf` |
| hit | `particles/xrole/reimu/spell_1_fire.vpcf` |
| death | `particles/xrole/suika/fireball_explode_trail.vpcf` |
| roundStart | `particles/xrole/suika/fireball_trail.vpcf` |
| roundEnd | `particles/touhou/laser/laser_fire.vpcf` |

### ICE — IceDragon
| 槽 | 粒子 |
|---|---|
| burst | `particles/xrole/cirno/perfect_freeze_explode.vpcf` |
| trail | `particles/xrole/cirno/perfect_summer_ice_trail.vpcf` |
| kill | `particles/xrole/cirno/ice_explode_ring.vpcf` |
| hurt | `particles/xrole/cirno/freeze_smoke.vpcf` |
| hit | `particles/xrole/cirno/perfect_freeze_icicle.vpcf` |
| death | `particles/xrole/cirno/perfect_freeze_explode.vpcf` |
| roundStart | `particles/xrole/cirno/perfect_summer_ice_snow.vpcf` |
| roundEnd | `particles/xrole/letty/frost_explode_ring.vpcf` |

### LIGHTNING — Ragnarok, Drone
| 槽 | 粒子 |
|---|---|
| burst | `particles/xrole/reimilia/spear_lighting.vpcf` |
| kill | `particles/ruiyiwelkin/models/remuru/lightning_p1.vpcf` |
| hurt | `particles/xrole/reimilia/ganggenier_lighting_trail.vpcf` |
| hit | `particles/xrole/reimilia/ganggenier_lighting_trail.vpcf` |
| death | `particles/xrole/reimilia/spear_lighting.vpcf` |
| roundStart | `particles/ruiyiwelkin/models/remuru/lightning_p2.vpcf` |
| roundEnd | `particles/xrole/reimilia/spear_lighting.vpcf` |

### HOLY — God, Fate, WheelOfFate, SkyVerdict, DivineDescent, FinalJudgment, Awakener, Emperor, Pope, Prophet, Nirvana, DivineResurrection, RoyalBarrier
| 槽 | 粒子 |
|---|---|
| burst | `particles/xrole/reimu/trigger_talisman_light.vpcf` |
| orbit | `particles/xrole/sanae/talisman_ring.vpcf` |
| kill | `particles/xrole/reimu/trigger_talisman_explode_up.vpcf` |
| hit | `particles/xrole/reimu/trigger_talisman_ring.vpcf` |
| hurt | `particles/xrole/reimu/spell_2.vpcf` |
| death | `particles/xrole/reimu/spell_1_explode_const.vpcf` |
| roundStart | `particles/touhou/spell/spellcall.vpcf` |
| roundEnd | `particles/xrole/reimu/talisman_explode2_tail.vpcf` |

### DARK / VOID — Cthulhu, FourHorsemen, Void, Singularity, DeathKnight, Forsaken, Taotie, BlackHole, GravityWell, NukeLeak
| 槽 | 粒子 |
|---|---|
| burst | `particles/xrole/suika/blackhalo.vpcf` |
| orbit | `particles/xrole/suika/blackhalo.vpcf` |
| kill | `particles/xrole/suika/blackball.vpcf` |
| killSelf | `particles/xrole/suika/blackhalo.vpcf` |
| hurt | `particles/xrole/suika/blackball.vpcf` |
| hit | `particles/xrole/suika/blackhalo_deb.vpcf` |
| death | `particles/xrole/shinki/eyedoom_laser_explode.vpcf` |
| roundStart | `particles/xrole/suika/blackhalo.vpcf` |
| roundEnd | `particles/xrole/suika/blackball.vpcf` |

### GHOST — Reincarnation
| 槽 | 粒子 |
|---|---|
| burst | `particles/xrole/youmu/ghost_ring.vpcf` |
| trail | `particles/xrole/youmu/ghost_child.vpcf` |
| kill | `particles/xrole/koishi/ghost_dush.vpcf` |
| hit | `particles/xrole/youmu/ghost_child.vpcf` |
| hurt | `particles/xrole/koishi/ghost_smoke.vpcf` |
| death | `particles/xrole/youmu/ghost_fire.vpcf` |

### BLOOD — Mosquito
| 槽 | 粒子 |
|---|---|
| burst | `particles/xrole/reimilia/blood_buff.vpcf` |
| trail | `particles/xrole/reimilia/blood_trail.vpcf` |
| kill | `particles/xrole/reimilia/blood_trail.vpcf` |
| hit | `particles/xrole/reimilia/blood_trail.vpcf` |
| hurt | `particles/xrole/reimilia/blood_buff.vpcf` |

### BLADE — SwordSaint, Izayoi
| 槽 | 粒子 |
|---|---|
| burst | `particles/xrole/youmu/blade.vpcf` |
| kill | `particles/xrole/youmu/blade_wave.vpcf` |
| hit | `particles/xrole/youmu/chop.vpcf` |
| hurt | `particles/touhou/slash/slash.vpcf` |
| roundStart | `particles/xrole/youmu/slash_glow_rise.vpcf` |

### STAR — Supernova
| 槽 | 粒子 |
|---|---|
| burst | `particles/xrole/marisa/comet_trail_halo.vpcf` |
| kill | `particles/xrole/flandre/star_break_explode.vpcf` |
| hit | `particles/xrole/marisa/comet_hit_trail.vpcf` |
| hurt | `particles/touhou/star/potions_explode.vpcf` |
| roundStart | `particles/touhou/star/const.vpcf` |

### NATURE — WolfKing
| 槽 | 粒子 |
|---|---|
| burst | `particles/killeffect/lily/cherry_explode.vpcf` |
| trail | `particles/xrole/youmu/trail_sakura.vpcf` |
| kill | `particles/killeffect/lily/cherry_flower.vpcf` |
| hit | `particles/xrole/yamame/cobweb.vpcf` |
| hurt | `particles/xrole/yamame/cobweb.vpcf` |
| orbit | `particles/xrole/aya/wind_halo2.vpcf` |

### POISON — BoneMaggot, Parasite, Plague, PoisonBlade（Aknpa 无合适毒系）
| 槽 | 粒子 |
|---|---|
| burst | `particles/ambient_fx/impact_generic_smoke_large.vpcf`（世界空间 smoke） |
| trail | `particles/burning_fx/chaotic_embers_basic.vpcf` |
| kill | `particles/burning_fx/chaotic_embers_basic.vpcf` |
| hit | `particles/ambient_fx/impact_generic_smoke_large.vpcf` |

---

## 不动的部分
- 其余 common/rare 及中性 dice（FourtyTwo、ImposterSyndrome、Kinship、Tactician…）保持原 stock 粒子。
- 复合体（25 条）暂不动（`ui_status_level_wings` 证明可用）。
- orb/attach 几何、时长等一律不变，只换路径。

## 执行方式
- 只改 `configs\plugins\RollTheDice\dicefx.json`，**不重编译**；改完 `css_reload RollTheDice` 或 `!rtdfx reload` 生效。
- 同步更新 `_workshop_analysis\dicefx.generated.json`（作为工作副本）。
