# 统一 Dice 播报优化指南（Cycle 11 / v1.4.14）

## 目标

让 Dice 抽到播报的**描述来源、转义、玩家名**一致，修掉 HUD 显示英文/Unknown/过期描述，以及动态 HTML 破坏版面的问题。

## 本轮范围

1. `DiceUi.HtmlEncode`。
2. `AnnounceDiceRarity` 的 HUD：
   - 名称用 `DiceUi.DiceName`；
   - 描述用 `DiceUi.DiceDescription`（本地化 `_player`）；
   - 描述/特效/名称/稀有度全部 HTML 编码；
   - `Unknown` 开头的描述不显示。
3. 传说/组合全服播报用 `GetTruePlayerName`。
4. 测试新增 HTML 编码、描述本地化、源码守卫（标准 59，真实配置 60）。
5. 版本升到 v1.4.14，更新 CHANGELOG/README/自检。

## 明确不做

- 不合并 Dice 自身 `NotifyPlayers` 与稀有度 reveal 的播报层级。
- 不改播报文案、音效、全服播报开关、HUD 开关。
- 不改概率、效果、持续时间、命令、配置 Key。
- 不做播报冷却/去重。

## 验收门槛

- 构建 0 warning / 0 error。
- 配置测试 32/32；Registry/UI/Lifecycle/Movement/Synergy/Broadcast 59/59。
- 真实部署配置 60/60。
- 架构检查通过，`timers=0`，主类不超 2900。
- HUD 描述与 `!rtd status` 一致。
- 动态文本经过 HTML 编码。

## 自检

- 用户：HUD 描述是否中文且正确，传说播报是否真名。
- 设计师：是否只改播报来源/转义，没有改玩法或播报层级。
- 工程师：构建/测试/架构/CI、编码正确、回退安全。
