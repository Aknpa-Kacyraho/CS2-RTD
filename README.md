# RollTheDice — CS2 骰子插件（Kacyra 增强版）

[![License](https://img.shields.io/badge/License-GPLv3-blue)](LICENSE)

**基于 [Kandru/cs2-roll-the-dice](https://github.com/Kandru/cs2-roll-the-dice) 的深度修改版**，扩展至 169 个骰子、65 对组合技（Combo Synergy）、四档稀有度抽取、多骰子叠加、统一无敌/buff 系统与作弊指令守卫等大量新机制。

> **原项目**：[Kandru/cs2-roll-the-dice](https://github.com/Kandru/cs2-roll-the-dice) by [@Kandru](https://github.com/Kandru) & [@derkalle4](https://github.com/derkalle4)  
> 本项目遵循 GPLv3 协议，保留原作者全部版权。

---

## 目录

- [特性概览](#特性概览)
- [骰子列表](#骰子列表)
- [安装指南](#安装指南)
- [命令说明](#命令说明)
- [JSON 配置详解](#json-配置详解)
- [平衡性调整指南](#平衡性调整指南)
- [编译指南](#编译指南)
- [常见问题](#常见问题)
- [许可证](#许可证)

---

## 特性概览

相比原版 35 个骰子，本版大幅扩展：

| 特性 | 说明 |
|------|------|
| **169 个骰子** | 涵盖战斗、生存、团队、经济、全局等各类效果 |
| **6 个合成 / 进化骰** | 冰巨龙、火巨龙、死亡骑士完全体、超越天堂、菲尼克斯、雷达站（稀有度 combo 档，仍可极低概率抽到） |
| **65 对组合技 (Combo Synergy)** | 两名玩家持有特定骰子对时双方效果增强，按持有者同队实时判定 |
| **四档稀有度抽取** | 普通 / 稀有 / 史诗 / 传说 + combo 档，权重与逐骰定档均可配置；抽到传说/combo 全服播报 |
| **多骰子叠加** | 玩家可同时持有多个不同类型的骰子 |
| **统一 buff 系统** | 伤害 / 减伤 / 移速经 `StackingBonusManager` 统一结算，支持叠层与限时 |
| **统一无敌** | `Invulnerability` 由主插件伤害钩子统一拦截，复活类骰子共用 |
| **作弊指令守卫 (CheatGuard)** | `sv_cheats 1` 环境下，仅 ≥2 真人时拦截非管理员的动作类作弊指令 |
| **全中文界面** | 所有提示、骰子名、描述均已汉化 |
| **高度可配置** | 每个骰子的参数均通过 JSON 配置，支持地图级覆盖 |

### 设计理念

- 骰子名称偏好创意短名（如"涅槃""疾风步""命悬一线"），描述白话易懂
- 数值优先使用随机区间（如 `min: 40, max: 120`）而非固定值
- 全局效果持续整回合，临时 buff 到期只移除自身加成
- 组合技优先考虑跨玩家触发（每人只有 1 个骰子时也能体验）

---

## 骰子列表

### 🔥 战斗增强（伤害 / 攻击）

| 类名 | 骰子名 | 效果简述 |
|------|--------|---------|
| `Berserker` | 狂战士 | 血量越低伤害越高，最高 +300% |
| `DamageMultiplier` | 毁灭之力 | 伤害 ×1.4~2.2（随机） |
| `Cutter` | 刺客信条 | 持刀一击必杀，+30% 速度 |
| `DeagleKing` | 沙漠之鹰 | 沙鹰伤害 ×3 |
| `SniperElite` | 狙击精英 | 狙击枪伤害 ×3 |
| `PistolMaster` | 手枪大师 | 手枪伤害 +100%（左轮除外） |
| `GrenadeKing` | 手雷王 | 手雷伤害大幅提升 |
| `Fireball` | 火球术 | 燃烧弹爆炸范围 + 火焰伤害 |
| `FireLord` | 炎魔 | 免疫火焰，所有投掷物变燃烧瓶 |
| `ThunderChain` | 雷劫 | 击杀引发雷霆连锁弹射 |
| `PoisonBlade` | 淬毒之刃 | 攻击附带中毒持续伤害 |
| `IceBeam` | 寒冰射线 | 攻击概率冻结敌人 |
| `Amber` | 琥珀 | 受击概率冻结攻击者 |
| `Disarm` | 缴械 | 攻击能打掉敌人武器 |
| `Martyrdom` | 殉道者 | 死亡时原地爆炸 |
| `DeadHand` | 死手 | 开枪自伤 15HP，命中回复 15HP |
| `RouletteGambler` | 轮盘赌徒 | 开枪 2% 概率死亡，否则叠加速度/伤害 |
| `Overheat` | 红温 | 每 5s 叠加速度/伤害，击杀降层 |
| `SwordSaint` | 剑仙 | 持刀时免疫子弹伤害 |
| `GunGod` | 枪神 | 免疫投掷物和刀伤，减伤 66% |
| `Awakener` | 觉醒者 | 击杀升级：伤害 ×2 + 速度 ×1.5 |
| `Forsaken` | Forsaken | 子弹必爆头 |
| `ReverseCausality` | 因果倒置 | 受伤延迟 5s，期间伤害翻倍 |
| `Vampire` | 吸血鬼 | 造成伤害回复自身 HP |
| `ReturnToSender` | 遣返 | 射击概率将敌人送回出生点 |
| `Combo` | 连击 | 连续命中叠层：每层 +10% 伤害（上限 10 层），≥5 层每次命中回 5HP，满层 +15% 移速并标记敌人 5s |
| `PainConverter` | 痛觉转化 | 受敌伤按掉血比例蓄痛（上限 100），按 E 释放 5s 爆发：伤害 / 移速 / 回血 |
| `Anatomist` | 解剖学家 | 爆头伤害 ×1.6，非爆头伤害 ×0.8 |
| `IronHead` | 铁头功 | 受到爆头伤害减少 99% |
| `Guillotine` | 斩首 | 对 HP < 35% 的敌人造成致死伤害 |
| `LastStand` | 最后一发 | 弹匣只剩最后一发时，该发伤害 ×3 |

### 🛡️ 生命与护甲

| 类名 | 骰子名 | 效果简述 |
|------|--------|---------|
| `Giant` | 巨人 | 体型极大化，MaxHP ×4 |
| `RoyalBarrier` | 皇家壁垒 | 超高血甲，但移速极慢 |
| `Shield` | 坚盾 | 额外护甲 + 50% 减伤 |
| `Regeneration` | 生命之泉 | 每秒回复 HP |
| `Gaia` | 大地母亲 | 每秒 +1HP，最高 500HP |
| `GunHealer` | 绝命枪师 | 每次开枪回复 5HP |
| `JumpHeal` | 跳跳糖 | 跳跃时回复 5-8HP |
| `Mosquito` | 蚊子 | 体型极小（20%），22HP |
| `GuardianAngel` | 守护天使 | 抵挡下一次致命伤害 |
| `Evasion` | 闪避 | 概率闪避攻击 |
| `Nirvana` | 彼岸 | 受击概率传送回出生点 + 满血 |
| `DuskDawn` | 暮光 | 命悬一线时爆发回血 |
| `FrontlineBeast` | 前线巨兽 | 击杀回出生点 + 满血 + 速度 ×2 |
| `Sacrifice` | 献祭 | 队友死 4 人时触发群体复活 |
| `SacrificeSelf` | 牺牲 | 按 E 牺牲自己，全队 +20% 伤害 +50% 速度 |
| `WheelOfFate` | 命运之轮 | 死亡 66% 概率复活 |
| `Respawn` | 涅槃 | 死亡后复活 |
| `InfiniteProliferation` | 无限增殖 | 复活 4 次，每次 HP 减半 |
| `Emperor` | 皇帝 | 队友首次死亡后复活 |
| `Phoenix` | 菲尼克斯 | 受致命伤触发涅槃，10s 无敌后爆炸回血（combo 档） |
| `Crouch` | 蹲伏 | 蹲下时减伤 30% 并每秒回复 5HP |
| `Kinship` | 羁绊 | 队友阵亡时自己 1s 无敌；自己阵亡时存活队友 1s 无敌 |

### ⚡ 速度与移动

| 类名 | 骰子名 | 效果简述 |
|------|--------|---------|
| `IncreaseSpeed` | 疾风步 | 速度提升（随机区间） |
| `SpeedOnKill` | 猎杀快感 | 击杀获得速度爆发 + 投掷物 |
| `HighGravity` | 千钧 | 超高重力 + 60% 减伤 |
| `Satellite` | 卫星 | 极低重力 + 空中精准射击 |
| `Skyline` | 天际 | 按 E 飞行 3 秒 |
| `Void` | 虚无 | 按 E 遁入虚无：无敌 + noclip + 隐身 |
| `Gargoyle` | 石像 | 按 E 石化 3s，免疫子弹 |
| `Titanfall` | 泰坦陨落 | 锁定 60s，HP/甲涨至 500 后觉醒 |
| `Adrenaline` | 肾上腺素 | HP < 40% 时速度爆发 |

### 🎯 武器与装备

| 类名 | 骰子名 | 效果简述 |
|------|--------|---------|
| `InfiniteAmmo` | 无限火力 | 无限弹药，无需换弹 |
| `NoRecoil` | 铁腕 | 无后坐力，伤害 +10% |
| `WeaponRoulette` | 武器轮盘 | 每开一枪随机换武器，伤害 ×1.3 |
| `ResetOnReload` | 快手 | 换弹触发重生 |
| `LongerFlashes` | 闪光大师 | 闪光弹时间延长 + 减速 |
| `SmokeVision` | 烟雾克星 | 看穿烟雾 |
| `ToxicSmoke` | 致命毒雾 | 烟雾弹有毒 |
| `SmokeBomb` | 迷雾逃生 | HP < 25% 自动烟雾弹 + 隐身 |
| `SlyFox` | 狡猾狐狸 | 投掷物爆炸时间随机 |
| `NoExplosives` | 哑火 | 随机 2 名敌人禁用爆炸物 |
| `DecoyDummy` | 假人诱饵 | 获得真诱饵弹，定时补给 |
| `ReloadGap` | 换弹窗口 | 换弹过程中减伤 80%，换完 3s 内伤害 +40% |

### 🌍 全局效果（影响全图）

| 类名 | 骰子名 | 效果简述 |
|------|--------|---------|
| `FogOfWar` | 迷雾行者 | 战争迷雾，视野受限 |
| `Jammer` | 干扰器 | 全场 HUD 屏蔽，持有者保留准心 |
| `Nightglow` | 夜光 | 地图极暗，但所有玩家发光 |
| `FourHorsemen` | 四骑士 | 全图分配战争/瘟疫/饥荒/死亡 |
| `ChaosStorm` | 混沌风暴 | 每 45s 存活玩家位置随机交换 |
| `WASDChaos` | 方向错乱 | 全员 WASD 随机旋转 |
| `NukeLeak` | 核弹泄露 | 60s 后全员死亡 |
| `Ragnarok` | 终焉 | 30s 无敌，60s 后回合结束 |
| `Cthulhu` | 克苏恩 | 1HP 不能移动，100s 后全灭 |
| `Twilight` | 黄昏之时 | 全部玩家出生点随机互换 |
| `Synced` | 心有灵犀 | 全员换弹同步 |
| `Lottery` | 彩票 | 全员随机获得 0~5000 元 |
| `DeadHand` | 死手 | 全玩家效果：开枪自伤 + 命中回血 |

### 💰 经济系统

| 类名 | 骰子名 | 效果简述 |
|------|--------|---------|
| `Bank` | 银行 | 每 15s 随机 2 队友获得随机金额 |
| `Capitalist` | 资本家 | 每死一名玩家获得 500 |
| `Pickpocket` | 扒窃 | 攻击偷取敌人金钱 |
| `Bounty` | 赏金猎人 | 击杀获赏金 + 指定目标额外骰子 |
| `LoanShark` | 高利贷 | 获得 50000 贷款，未击杀则破产 |
| `Miser` | 吝啬鬼 | 花费满 785 后减伤 |
| `Empress` | 女皇 | 金钱收入 +100%，每 2000 复活队友 |

### 👥 团队 / 辅助

| 类名 | 骰子名 | 效果简述 |
|------|--------|---------|
| `Pope` | 教皇 | 所有队友 +100% HP |
| `Priest` | 牧师 | 攻击队友治疗他们 |
| `Knight` | 骑士 | 队友每 10s 吸取 50HP，持有者 300HP |
| `Goddess` | 女神 | 随机 2 队友额外获得骰子 |
| `Bugle` | 冲锋号 | 开局 60s 全队 +100% 速度 +20% 伤害 |
| `Paladin` | 圣骑士 | 200 额外护甲，受击叠加速度/HP |
| `Necromancer` | 死灵法师 | 消耗 50HP 复活阵亡队友 |
| `DivineResurrection` | 神迹 | 击杀概率复活队友 |
| `Prophet` | 先知 | 每 5s +1 层预知（上限 10），每层抵挡一次敌人伤害 |
| `ImposterSyndrome` | 第六感 | 被雷达发现时通知 + 诱饵弹高亮敌人 |

### 🌀 特殊机制

| 类名 | 骰子名 | 效果简述 |
|------|--------|---------|
| `World` | 世界 | 额外获得 +3 个骰子 |
| `Fool` | 愚者 | 暗加 1 骰，攻击 50% 无效，受击 50% 无敌 2s |
| `DragonSoul` | 巨龙之魂 | 250HP，双龙魂合成冰/火巨龙 |
| `Glutton` | 暴食者 | 每杀一人下回合多一骰子 |
| `Mimic` | 模仿大师 | 击杀获得其骰子 |
| `Trickster` | 诡术师 | 假骰子，首次击杀揭露真相 |
| `Reincarnation` | 轮回 | 每死一次下回合多一骰子 |
| `Evolution` | 进化 | 每 25s 随机进化，最多 5 次 |
| `Corona` | 日冕 | 45s 后烧死 → 复活为太阳神 |
| `Countdown` | 倒计时 | 时间到回溯出生点满状态 |
| `Izayoi` | 十六夜 | 时间之力躁动 |
| `Heaven` | 天堂 | 按 E 打开天堂之门，时间加速 |
| `Afterimage` | 残影 | 留下空间残影，按 E 时空回溯 |
| `Dragonborn` | 龙裔 | 杀 2 人后按 E 化龙：300HP + 300 护甲 |
| `Parasite` | 寄生 | 攻击目标死亡时获得增益 |
| `Plague` | 瘟疫 | 感染瘟疫，传染敌人 |
| `Karma` | 因果报应 | 移速 ×1.5 + 回血，击杀传播 |
| `SoulEater` | 噬魂者 | 击杀吸取灵魂回复 + 获得投掷物 |
| `Taotie` | 饕餮 | 攻击吃掉对方骰子 + 100HP |
| `Lucky` | 幸运星 | 定时获随机金币 + Buff |
| `FortyTwo` | 42 | 每 42s 获得无敌 + 隐身 |
| `Eclipse` | 月蚀 | 新月/满月循环切换 |
| `Cupid` | 丘比特 | 死亡随机带走一名玩家 |
| `Traitor` | 叛徒 | 杀队友随机杀一敌人 |
| `Payback` | 以牙还牙 | 杀你的人 -50HP + 金钱清零 |
| `Redemption` | 赎罪 | +40% 伤害，死亡复活被击杀者 |
| `DivinePunishment` | 天罚 | 每杀 2 人随机处决敌人 |
| `C4Expert` | C4 专家 | CT 秒拆，T 安包 3s 爆炸 |
| `HotPotato` | 烫手山芋 | C4 附近玩家持续受伤 |
| `BoneMaggot` | 附骨之疽 | 击中敌人发光标记 5s |
| `Tactician` | 军师 | 每 15s 暴露敌人位置 1s |
| `InfoHole` | 信息黑洞 | 敌方雷达屏蔽 |
| `RadarJammer` | 雷达干扰 | 击杀后黑敌人雷达 20s |
| `RadarStation` | 雷达站 | 所有敌人发光，移速 ×0.6（combo 档） |
| `ShadowWarrior` | 影子武士 | 按 E 召唤分身 |
| `Hermit` | 隐者 | 脚步无声，几乎完全隐身 |
| `LaserCage` | 激光牢笼 | 旋转激光牢笼环绕 |
| `Drone` | 无人机 | 无人机环绕自动攻击 |
| `BlackHole` | 小型黑洞 | 吸附周围武器和投掷物 |
| `WhiteHole` | 白洞 | 死亡生成白洞推离敌人 |
| `GravityWell` | 黑洞 | 死亡生成引力井吸附全图 |
| `Singularity` | 奇点 | 按 E 释放全图引力 |
| `RepulsionField` | 斥力场 | 周围投掷物自动弹回 |
| `PlayAsChicken` | 鸡神 | 变成大鸡，300HP + 40% 速度 |
| `HangedMan` | 倒吊者 | 每秒流失 1HP，击杀反转 |
| `Jester` | 小丑 | 移动掉血 + 速度，击杀反转 |
| `Molt` | 蜕皮 | 死亡复活，体型缩小 |
| `DeathKnight` | 死亡骑士 | 每失 1HP 获得 1% 减伤，持刀回血 |
| `Frostmourne` | 霜之哀伤 | 持刀 60% 减伤 + 回血 |
| `God` | 上帝 | 90s 试炼：3× 移速 / 3× 伤害、+500 护甲、60% 减伤；击杀回 150HP 并短暂无敌 |
| `Knight` | 骑士 | 300HP，队友定期吸取 50HP |
| `Prayer` | 祈愿 | 每 20s 祈祷，成功 3 次全灭敌方 |
| `Wolf` | 狼 | 每只狼给所有狼 +30HP +30 甲 +10% 伤害 +10% 速度 |
| `WolfKing` | 狼王 | 300HP +40% 伤害 +40% 速度，队友必定得狼 |
| `Fibonacci` | 斐波那契 | 受到斐波那契数列伤害时免疫 + 回复 |
| `Deaf` | 失聪 | 失聪 + 按 E 透视敌人 |
| `Rally` | 集结 | 400~500 码内每名存活队友为自己与附近队友提供光环伤害 / 减伤（上限 5 层） |
| `Echo` | 回音 | 命中敌人 0.8s 后对同一目标追加一次回音伤害 |
| `Curse` | 怨念 | 自身阵亡后标记击杀者受伤增加，存活队友获得短时伤害加成 |
| `Yagorou` | 亚戈鲁 | 击杀后短时无敌；每回合一次致命伤免疫 |
| `Teneril` | 忒尼尔 | 队友阵亡时随机诅咒一名敌人（减速 + 削减当前血量） |

### 🧬 合成 / 进化骰（稀有度 combo 档）

> 平时主要通过特定骰子组合合成触发；同时也以 combo 档（权重 2）参与随机抽取，极难直接抽到。`DragonSoul`（巨龙之魂）与 `WolfKing`（狼王）为独立特殊池。

| 类名 | 骰子名 | 合成条件 |
|------|--------|---------|
| `IceDragon` | 冰巨龙 | 双龙魂触发：222HP + 333 甲，攻击附冰冻 |
| `FireDragon` | 火巨龙 | 双龙魂触发：333HP + 222 甲，攻击附灼烧 |
| `DeathKnightComplete` | 死亡骑士完全体 | 死亡骑士 + 霜之哀伤：333HP/333 甲，99% 减伤 |
| `BeyondHeaven` | 超越天堂 | 世界/十六夜 + 天堂：按 E 暂停时间 9s，其他玩家无法移动/开火 |
| `Phoenix` | 菲尼克斯 | 彼岸 + 守护天使：受致命伤触发涅槃 |
| `RadarStation` | 雷达站 | 所有敌人发光，自身移速 ×0.6 |

---

## 安装指南

### 前提条件

- CS2 服务器已安装 [CounterStrikeSharp](https://github.com/roflmuffin/CounterStrikeSharp)（net10.0 运行时）
- 服务器安装 [Metamod:Source](https://www.sourcemm.net/)

### 安装步骤

1. 从 [Releases](../../releases) 下载最新版本
2. 将 `RollTheDice` 文件夹复制到服务器的 `/addons/counterstrikesharp/plugins/` 目录
3. 语言文件已在 `lang/` 中（中文内容），无需额外配置
4. 重启服务器，或在控制台执行 `css_plugins reload RollTheDice`

### 更新

直接覆盖所有插件文件，插件会自动热重载。

---

## 命令说明

### 玩家命令

| 命令 | 说明 |
|------|------|
| `!rtd` / `!dice` / `!rollthedice` | 掷骰子 |
| `!dice auto` | 开启/关闭自动掷骰（每回合自动掷） |

**绑定按键掷骰**（在控制台输入）：
```
bind o rtd
```

### 管理员命令（需要 `@rollthedice/admin` 权限）

| 命令 | 说明 |
|------|------|
| `!givedice` | 给所有玩家随机掷骰 |
| `!givedice PlayerName` | 给指定玩家随机掷骰 |
| `!givedice * DiceName` | 给所有玩家发指定骰子 |
| `!givedice PlayerName DiceName` | 给指定玩家发指定骰子 |

> 骰子名大小写不敏感，对应 `src/dices/` 目录中的类名（如 `Glow`、`IncreaseSpeed`）。

### 服务器控制台命令

| 命令 | 说明 |
|------|------|
| `rollthedice reload` | 重载配置 |
| `rollthedice enable` | 启用插件 |
| `rollthedice disable` | 禁用插件 |
| `rollthedice createmapconfig` | 为当前地图创建独立配置 |
| `rollthedice deletemapconfig` | 删除当前地图独立配置 |

---

## JSON 配置详解

配置文件位置：`/addons/counterstrikesharp/configs/plugins/RollTheDice/RollTheDice.json`

插件首次加载时自动生成完整配置文件。以下是完整配置项说明：

### 全局设置

```json
{
  "enabled": true,                // 全局开关，false 则插件不工作
  "debug": false,                 // 调试模式，排查问题用
  "allow_rtd_during_warmup": false, // 是否允许热身时间掷骰
  "trigger": {
    "event": "RoundStart",        // 自动掷骰触发时机: "RoundStart" 或 "RoundFreezeEnd"
    "force_all_players": false,   // true = 强制所有玩家自动掷骰
    "allow_player_auto_rtd": true, // 允许玩家使用 !dice auto 自动掷骰
    "roll_the_dice_every_x_seconds": 0 // 每隔 X 秒自动掷骰，0 = 禁用
  },
  "cooldown_rounds": 0,           // 掷骰冷却回合数（与 cooldown_seconds 二选一）
  "cooldown_seconds": 0,          // 掷骰冷却秒数（与 cooldown_rounds 二选一）
  "price_to_dice": 0,             // 掷骰花费（$），0 = 免费
  "allow_dice_after_respawn": false, // 复活后是否允许再次掷骰
  "notify_other_players_about_dices_rolled": true,  // 是否广播掷骰结果
  "notify_player_via_chatmsg": true,   // 通过聊天消息通知
  "notify_player_via_centermsg": true, // 通过屏幕中央消息通知
  "max_dice_count": 2,            // 每玩家最多同时持有骰子数（默认 2）
```

### 骰子配置

每个骰子都在 `dices` 下有一个独立配置块：

```json
{
  "dices": {
    "berserker": {
      "enabled": true,            // 是否启用此骰子
      "max_damage_multiplier": 4.0, // 最高伤害倍率（1.0 = 无加成，4.0 = +300%）
      "min_health_threshold": 1   // 触发最低血量（1HP 时达到最大倍率）
    },
    "increase_speed": {
      "enabled": true,
      "min_speed": 1.5,           // 最小速度倍率
      "max_speed": 2.0            // 最大速度倍率（实际值在区间内随机）
    }
  }
}
```

#### 配置模式说明

大多数骰子遵循以下命名约定：

| 后缀 | 含义 | 示例 |
|------|------|------|
| `_min` / `_max` | 随机区间，掷骰时在区间内随机取值 | `"min_speed": 1.5, "max_speed": 2.0` |
| 单独数值 | 固定值 | `"gravity_scale": 0.4` |
| 无参数 | 纯开关型骰子 | `"enabled": true` |

### 地图独立配置

```json
{
  "maps": {
    "de_dust2": {
      "dices": {
        "low_gravity": { "enabled": false }  // 在 dust2 禁用低重力
      }
    }
  }
}
```

在 `maps` 下添加地图名（如 `de_dust2`、`de_inferno`），覆盖全局骰子配置。也可以用 `rollthedice createmapconfig` 命令自动生成模板。

### 音效配置

```json
{
  "sounds": {
    "dice_sound": "sounds/ui/coin_pickup_01.vsnd",  // 掷骰音效（空字符串 = 禁用）
    "play_on_command_only": false  // true = 仅手动 !rtd 播放音效
  },
  "precache": {
    "soundevent_file": "soundevents/soundevents_rollthedice.vsndevts"
  }
}
```

### 稀有度配置

```jsonc
{
  "dices": {
    "rarity": {
      "tier_weights": { "common": 70, "rare": 20, "epic": 6, "legendary": 2, "combo": 2 },
      "broadcast_legendary": true,
      "dice_tier": {
        "Fate": "legendary",
        "God": "legendary",
        "Shield": "rare",
        "GunGod": "rare",
        "Anatomist": "common"
        // ... 未列出的默认 common
      }
    }
  }
}
```

- `tier_weights`：各档位抽取权重（会按当前池中实际出现的档位归一化，所以档位概率即配置值）。
- `dice_tier`：逐骰定档；完整清单见仓库 `docs/dice-cn-tier.txt` 与 `docs/dice-rarity-list.md`。
- `broadcast_legendary`：抽到传说 / combo 时是否全服播报（默认 `true`）。

### 作弊指令守卫 (CheatGuard)

dice 的「无扩散」「时间」类效果需要服务器 `sv_cheats 1`，为防止加入的玩家借此开挂，插件内置守卫：

```jsonc
{
  "cheat_guard": {
    "enabled": true,
    "bypass_permission": "@css/root",
    "blocked_commands": [
      "noclip", "god", "buddha", "notarget", "hurtme",
      "give", "impulse",
      "setpos", "setang", "setpos_exact", "setang_exact", "getpos",
      "ent_create", "ent_remove", "ent_remove_all", "ent_fire", "ent_dump", "ent_info", "ent_text", "ent_teleport", "ent_bbox",
      "thirdperson"
    ]
  }
}
```

- 只拦「玩家动作类」作弊，**不拦** `sv_*` / `mp_*` / `bot_*` / `map` / `changelevel`（避免误伤引擎与开局流程）；也不拦普通玩家的 `drop`（丢枪指令）。
- **只在 ≥2 个真人（非 bot/HLTV）时启用**：单机 / 本地人机完全不受影响。
- `bypass_permission` 指定的管理员始终放行。
- 客户端本地 cvar（透视 `r_drawothermodels`、线框 `mat_wireframe`）服务器收不到，无法拦截 —— 引擎层限制。

---

## 平衡性调整指南

### 快速上手：调整骰子强度

所有骰子的参数都在 `RollTheDice.json` 中。以下是常见调整场景：

#### 1. 禁用某个太强的骰子

```json
"dices": {
  "god": {
    "enabled": false   // 把 true 改成 false
  }
}
```

#### 2. 调整数值区间

```json
// 举例：削弱疾风步的速度
"increase_speed": {
  "enabled": true,
  "min_speed": 1.2,    // 原来 1.5 → 1.2
  "max_speed": 1.5     // 原来 2.0 → 1.5
}
```

#### 3. 调整概率型骰子

```json
// 举例：降低彼岸触发概率
"nirvana": {
  "enabled": true,
  "min_chance": 0.15,  // 原来 0.30 → 0.15 (15%)
  "max_chance": 0.30   // 原来 0.50 → 0.30 (30%)
}
```

#### 4. 限制多骰子上限

```json
"max_dice_count": 1   // 每玩家最多 1 个骰子（关闭多骰子系统）
```

#### 5. 设置冷却

```json
"cooldown_rounds": 2   // 每 2 回合才能掷一次
// 或
"cooldown_seconds": 120 // 每 120 秒才能掷一次
```

### 进阶：理解骰子分发机制

本版使用**四档稀有度两段式抽取**：

1. **定档**：每个骰子在 `dices.rarity.dice_tier` 中有一个档位（`common` / `rare` / `epic` / `legendary` / `combo`），未列出的默认 `common`。
2. **选档**：在「当前可抽取池中实际出现的档位」之间按 `dices.rarity.tier_weights` 归一化抽取 —— 因此档位概率就等于配置值，不随各档骰子数量偏移。默认权重：普通 70 / 稀有 20 / 史诗 6 / 传说 2 / combo 2。
3. **选骰**：在该档位内均匀随机选一个骰子。
4. **特殊池**：`DragonSoul`、`WolfKing` 为 `IsSpecial`，走独立概率池，不参与上述档位抽取。

抽到 `legendary` 或 `combo` 时默认全服播报（`broadcast_legendary`），每个骰子都会按档位颜色给玩家发一条「🎲 你抽到了 [档位] 名字」。

### 调整特殊骰子概率

```csharp
// 在源码中调整（需重新编译）：
// DragonSoul.cs: SecondRoundProbability = 0.1  → 10% 队友获额外龙魂
// WolfKing.cs: SecondRoundProbability = 0.9    → 90% 队友获狼
```

### 地图级别的平衡

不同地图适合不同的骰子组合。例如：

```json
{
  "maps": {
    "de_dust2": {
      "dices": {
        "satellite": { "enabled": false },  // 长距离地图禁用浮空
        "giant": { "enabled": false }        // 窄通道地图禁用巨大化
      }
    },
    "de_nuke": {
      "dices": {
        "low_gravity": { "enabled": false }  // 多层地图禁用低重力
      }
    }
  }
}
```

### 常见调平建议

| 问题 | 解决方案 |
|------|---------|
| 某些骰子出现太频繁 | 在 `dices.xxx.enabled = false` 禁用 |
| 游戏节奏太快 | 增加 `cooldown_rounds` 或设置 `price_to_dice` |
| 多骰子太强 | 降低 `max_dice_count` 到 1 |
| 热身时间不想让玩家掷骰 | `allow_rtd_during_warmup: false` |
| 只想手动掷骰 | `trigger.force_all_players: false` 且 `trigger.allow_player_auto_rtd: false` |
| 管理员测试用 | `!givedice PlayerName DiceName` 强制发骰 |
| 某地图骰子太 OP | 用 `maps` 地图配置单独禁用 |

---

## 编译指南

### 环境要求

- .NET 10.0 SDK
- CounterStrikeSharp 官方 NuGet 包

### 编译步骤

```bash
# 克隆仓库
git clone https://github.com/你的用户名/cs2-roll-the-dice.git
cd cs2-roll-the-dice

# 还原依赖
dotnet restore

# Debug 编译
dotnet build

# Release 发布
dotnet publish
```

编译产物在 `src/bin/Debug/net10.0/` 或 `src/bin/Release/net10.0/publish/`。

---

## 常见问题

### Q: 为什么有些骰子从没见过？

A: 骰子按稀有度两段式抽取：先按 `tier_weights`（普通 70 / 稀有 20 / 史诗 6 / 传说 2 / combo 2）选档，再在该档内均匀抽一个。传说档总概率仅 2%，单个传说骰概率更低；combo 档（冰巨龙、火巨龙、死亡骑士完全体、超越天堂、菲尼克斯、雷达站）平时主要靠合成触发。

### Q: 组合技为什么很少触发？

A: 骰子池有 169 个，两人同时抽到特定一对骰子的概率极低。这是数学事实，不是 bug。想体验 combo 可以用 `!givedice` 命令手动测试。

### Q: 如何测试某个骰子？

A: 管理员使用 `!givedice * DiceName` 给所有人发指定骰子，或 `!givedice YourName DiceName` 给自己发。

### Q: 配置文件改了不生效？

A: 执行 `rollthedice reload` 重载配置，或 `css_plugins reload RollTheDice` 重载插件。

### Q: 如何让骰子只在特定地图启用？

A: 用 `maps` 配置。在 `maps` 下添加地图名，覆盖 `dices` 中的 `enabled` 状态。也可用 `rollthedice createmapconfig` 命令自动生成。

### Q: 插件不工作？

A: 检查：
1. `configs/plugins/RollTheDice/RollTheDice.json` 中 `"enabled": true`
2. CSS 控制台日志中是否有报错
3. `css_plugins list` 确认插件已加载

---

## 许可证

本项目基于 [Kandru/cs2-roll-the-dice](https://github.com/Kandru/cs2-roll-the-dice) 修改，遵循 **GNU General Public License v3.0 (GPLv3)**。

- 原项目版权 © [Kandru](https://github.com/Kandru) & [derkalle4](https://github.com/derkalle4)
- 修改部分版权 © Kacyra

完整许可证文本见 [LICENSE](LICENSE)。

> GPLv3 要求：如果你分发此项目的修改版，必须同样以 GPLv3 开源，并保留原始版权声明。

---

## 致谢

- [Kandru](https://github.com/Kandru) — 原版 RollTheDice 作者
- [derkalle4](https://github.com/derkalle4) — 原版开发
- [CounterStrikeSharp](https://github.com/roflmuffin/CounterStrikeSharp) — CS2 插件框架
- [CS2-Warcraft-Plugin](https://github.com/abnerfs/cs2-warcraft-plugin) — 部分实体 API 参考
- [jRandomSkills](https://github.com/IMFROMCYBERTRUCK/jRandomSkills) — 部分技能实现参考
