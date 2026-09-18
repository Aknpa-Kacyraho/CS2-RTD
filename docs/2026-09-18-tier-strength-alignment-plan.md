# 史诗 / 传说 强度对齐预案（2026-09-18）

> 状态：**已实施**（2026-09-18）。落地清单见文末「实施记录」。文档中未列出的 dice 未改动。
> 依据：源码逐条阅读 + 部署 `RollTheDice.json` 实际数值。标注「已复核」= 本次直接读过实现；「审计待复核」= 来自探索代理，尚未逐行确认。

## 0. 对齐口径

- **传说**：必须能**大幅影响对局**——不只强，还要能改变回合走向。触发不能过度依赖运气/苛刻条件，且**不应有净负面**（净负面=持有者自己亏，配不上传说）。
- **史诗**：至少给持有者**巨大优势**（单人质变），但不能反过来碾压传说的存在感。
- 判据维度：影响范围（个人/团队/全服）、可靠性（触发难度·持续·冷却）、可反制性、以及**是否需要先修 bug 才能兑现强度**。
- 核心目标：消除**档位倒挂**（史诗明显强于传说 / combo 弱于史诗）。

## 1. 结论速览

### A. 传说档

| Dice | 判定 | 一句话 |
|---|---|---|
| Cthulhu | **偏弱/自残** | 自身 1HP+定身+无免伤，收益全押 100s；HP 上限不还原（泄漏） |
| Fate | **偏弱/随机** | 全服随机发命运，自己可能拿害己的；`dice_luck` 是死代码 |
| FourHorsemen | **边缘偏弱** | 敌我同享随机骑士；famine 过强、death 可能砸队友 |
| God | 达标 | 666/666+×2 伤+20% 减伤，90s 试炼；容错偏低但够格 |
| Ragnarok | **净负面，需重做** | 30s 无敌 → 自爆 + 随机献祭一名队友 |
| SwordSaint | **偏弱/极情境** | 持刀+正对才挡子弹，不能还击，收益面太窄 |
| WheelOfFate | 达标偏强 | 66% 可反复复活；复活发的是**击杀者的枪**（bug） |
| WolfKing | 边缘达标 | 300HP+双 40%，扎实但无质变；HP 上限移除不还原 |
| World | **偏弱/看脸** | 本体零收益，额外 3 骰纯随机且依赖补骰窗口 |
| Awakener | 达标偏强 | 击杀无限成长；**不判敌我可刷**（bug） |

combo 档：BeyondHeaven 偏强达标、DeathKnightComplete 坦度达标但**禁骰近乎不触发**、FireDragon/IceDragon/Phoenix 偏强达标、**RadarStation 偏弱**（自减速 40%+自我暴露）。

### B. 史诗档

| 分类 | Dice |
|---|---|
| **过强（接近/超过传说，应削或升档）** | Taotie、Titanfall、Nirvana、Forsaken、（边缘）Pope、Drone、Emperor |
| **达标（巨大优势成立）** | DeathKnight、DivineResurrection、Dragonborn、GunGod、InfiniteProliferation、Prophet、Void、Yagorou |
| **偏弱（够不上巨大优势）** | Mosquito、ImposterSyndrome、Tactician、Reincarnation、NukeLeak、Izayoi、FourtyTwo、RoyalBarrier、Singularity、（边缘）Kinship |

## 2. 档位倒挂清单（最该先解决的）

- **史诗 > 传说**：`Forsaken`（子弹×4）、`Taotie`（命中夺全部骰）、`Titanfall`（锁定每 tick 满血）、`Nirvana`（任意受击瞬移满血）、`Pope`（全队上限×2 并回满）、`Drone`（被动 auto DPS）　>　`Cthulhu`/`Fate`/`Ragnarok`/`SwordSaint`/`World`
- **combo < 史诗**：`RadarStation` 弱于大多数史诗。
- 结论：**单向加强传说不够**，必须同时**削过强史诗**，否则档位感永远立不住。

## 3. 分项建议

> 所有数值均为**方向性建议**，需实测微调；标「升档」表示可考虑改稀有度而非削数值。

### 3.1 传说 → 加强/重做

| Dice | 现状问题 | 建议方向 |
|---|---|---|
| **Cthulhu** | 自身 1HP+定身+无免伤=活靶；HP/Health 上限 `Remove` 不还原（已复核） | 持有者改为「祭坛」：给 50%+ 减伤或短时无敌、血量正常化；`KillTime` 100s→70~80s；**必修上限还原**。让它靠站场施压，而非自杀 |
| **Fate** | 全服随机、自己可能拿负面；`dice_luck` 死代码（审计待复核） | 持有者**必得正向命运**；修复 `dice_luck` |
| **Ragnarok** | 30s 无敌后自爆 + 随机献祭一名队友（已复核）= 净负面 | 重做：限制无敌 → 改为对**敌方**的终焉处决/全场高伤；若坚持献祭设定，也**不要拉队友** |
| **WolfKing** | 数值达标但无质变；HP 上限移除不还原（审计待复核） | 修还原；加质变（如附近 Wolf 数量强化/召唤），拉开与史诗的差距 |
| **Awakener** | 成长无 cap；击杀不判敌我（已复核 `CheckKill` 无 team 判定） | **加敌我判定**；

### 3.2 传说 → 达标（仅修 bug）

- **God**：达标。可选微调：删除试炼，击杀回少量血（当前完全不回血，被消耗即等死）。
- **WheelOfFate**：修「复活继承击杀者武器」→ 应保留自己武器。
- **(combo) DeathKnightComplete**：修「禁骰只在持刀击杀触发」（实战几乎不触发）；改为任意武器击杀触发。
- **(combo) FireDragon**：燃烧 DPS 无 cap，建议加封顶；确认直扣血是否穿透无敌。

### 3.3 史诗 → 削弱 / 升档

| Dice | 现状 | 建议 |
|---|---|---|
| **Titanfall** | 锁定期**每 tick `Health=MaxHealth`**（已复核）+ 500/500 + 解禁 +50% 伤/速 | 移除每 tick 满血（只插值，不自动回复）
| **Drone** | 被动 800 码自动 20-35/1.2s，无操作成本（审计待复核） | 微调伤害/射速，或改开火才触发 |
| **Emperor** | 全队每人每回合各一次满血复活+2s 无敌（审计待复核） | 每回合全队共享 3~4 次

### 3.4 史诗 → 加强/重做

| Dice | 现状问题 | 建议方向 |
|---|---|---|
| **ImposterSyndrome** | 纯信息 + 每 30s 一颗诱饵 | 诱饵更频繁/假身份（杀诱饵者受罚），或击杀后暴露全敌数秒 |
| **Tactician** | 15s 一次仅 1s 透视 | 时长 1s→3~5s；击杀后暴露全敌并对目标增伤 |
| **Reincarnation** | 延到下回合，才给 1-2 个随机骰 | 本回合立即给 1 额外骰，死亡再叠加 |
| **NukeLeak** | 60s 全服同归于尽（含自己） |可控制引爆/持有者免疫；否则属「搅局」非优势 |
| **Izayoi** | `host_timescale` **全局**随机，坑自己也坑队友（审计待复核） | 只影响敌方或固定给自己加速；改为个人层面时间效果 |
| **FourtyTwo** | 42s→4s 无敌，uptime 太低 | 间隔 42→20~25s，或无敌 6s |
| **RoyalBarrier** | 444 血换 30% 龟速 + 不能跳 + HP/甲移除不还原 | 移速 0.3→0.5~0.6；允许跳；修还原 |
| **Singularity** | 冷却 all-holders 共享、2000 单位难瞄、拉自己（审计待复核） | 冷却是 per-player；只拉敌人；判定范围放宽 |
| **Kinship** | 1s 无敌太短、只死亡瞬间 | 延长 2~3s，或改为可叠加护盾 |

## 4. 必修 bug（不修则调整无效）

| Dice | 问题 | 证据 |
|---|---|---|
| Cthulhu | `MaxHealth/Health=1` 在 `Remove` 不还原 | 已复核 |
| Awakener | 击杀/助攻不判敌我，可刷 | 已复核 |
| World | 补骰时机；窗口内抽到会被清 pending | 审计待复核 |
| Fate | `dice_luck` 在池外，永远抽不到 | 审计待复核 |
| WheelOfFate | 复活继承击杀者武器 | 审计待复核 |
| DeathKnightComplete | 禁骰只在持刀击杀触发 | 审计待复核 |
| RadarStation | `Reset` 不还原移速；持有者死亡时敌人 glow 残留 | 审计待复核 |
| Singularity | 冷却字段单实例共享 | 审计待复核 |
| Izayoi | 全局 timescale，`Remove` 不还原 | 审计待复核 |
| FourHorsemen | `Remove` 不注销 war 加成 | 审计待复核 |
| InfiniteProliferation / Dragonborn / Phoenix | 复活/变身时武器取自击杀者；HP 上限移除残留 | 审计待复核 |

## 5. 建议执行顺序

1. **批次 1｜修 bug**：低风险、直接兑现既有强度（§4）。
2. **批次 2｜削过强史诗**。
3. **批次 3｜重做净负/自残传说**。
4. **批次 4｜加强偏弱史诗**。
5. **批次 5｜复核**：全部改完后按判据重排一次稀有度（含是否升档）。

---

## 7. 实施记录（2026-09-18）

**修 bug（§4）**

- `Cthulhu`：移除 1HP+定身自残；改为持有者 60% 减伤（`damage_reduction`），`kill_time` 100→80；不再修改/残留 HP 上限。
- `Awakener`：击杀/助攻加敌我判定（只计敌人），杜绝刷队友。
- `World`：抽到即刻补发额外骰（`ForceExtraDiceForPlayer`），不再依赖 3s 补骰窗口；主插件补骰循环改为每次重算上限。
- `Fate`：`_fatePool` 补入 `dice_luck`（修复死代码）；持有者必得正向命运（天秤/罗盘/黎明）。
- `WheelOfFate`：复活保留的是**自己**的武器（原为击杀者武器）。
- `InfiniteProliferation`：同上，保留自己的武器。
- `DeathKnightComplete`：禁骰改为任意武器击杀触发（原仅持刀）。
- `RadarStation`：`Reset` 还原移速；持有者死亡时清理敌人 glows。
- `Singularity`：冷却改为按玩家独立；仅吸附敌人（不拉队友/自己）。
- `FourHorsemen`：`Remove` 时注销战争加成。
- `Dragonborn` / `Phoenix` / `RoyalBarrier`：`Remove`/`Reset` 还原 HP 上限（原残留）。

**传说**

- `God`：删除 90s 试炼；整回合 666/666、伤害×2、减伤 20%；击杀回 50HP（Goddess ×2）+1.5s 无敌。
- `Ragnarok`：删除自爆与随机献祭队友；60s 后对所有敌人造成 250 审判伤害。
- `WolfKing`：修复 HP 上限残留；新增击杀回 50HP。
- `Izayoi`：弃用全局 `host_timescale` 随机加减速，改为个人周期时间加速（移速×1.5~2.2，5s/10s）。
- `Cthulhu` / `Fate` / `Awakener` 见上。
- **未动**：`SwordSaint`、`World`(加强已含)、`FourHorsemen`(仅修 bug)。

**史诗**

- 削：`Titanfall`（移除锁定期每 tick 满血）、`Drone`（14~24 / 1.6s）、`Emperor`（每回合全队共享 4 次复活）。
- 加强：`Tactician`（透视 4s）、`FourtyTwo`（22s/6s）、`Kinship`（3s）、`RoyalBarrier`（移速 0.55 + 可跳 + 修还原）、`Reincarnation`（立即 +1 骰）、`NukeLeak`（持有者免疫）、`ImposterSyndrome`（诱饵 15s + 击杀暴露全敌 3s）、`Singularity`（见 bug）、`Izayoi`（见上）。`FireDragon` 灼烧 DPS **不设上限**（打中才叠，门槛即平衡；用户明确要求不削）。
- **用户拍板未动**：`Taotie`、`Nirvana`、`Forsaken`、`Pope`、`Mosquito`、`RadarStation`（§3.3/§3.4 中已从计划删除）。

**跨档 / 全局**

- 主插件减伤总封顶 `0.95 → 0.99`，使 `Awakener`/`C4Expert` 等 0.99 源真正生效。
- lang（源码+部署）对应文案已同步。


