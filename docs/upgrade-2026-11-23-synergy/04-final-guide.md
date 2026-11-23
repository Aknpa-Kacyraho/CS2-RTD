# 统一 Dice 联动优化指南（Cycle 10 / v1.4.13）

## 目标

优化 Combo 联动系统的**检查成本、刷新成本、公告可读性和数据完整性**，不改任何 Combo 效果与数值。

## 本轮范围

1. `HasPartner` 改为“伙伴 Dice 持有者列表”扫描，移除每次调用的 `Utilities.GetPlayers()` 分配和重复字典查找。
2. `RefreshCombos` 增加 `HasDiceSetChangedOverrides` 能力缓存；当前无覆写时跳过 171 个空虚调用。
3. `AnnounceCombo` 使用真名，不带 TAB `[Dice]` 前缀。
4. `DiceRegistry` 增加自联动、重复无序配对、空显示名校验。
5. 测试增加 Combo 数据/解析/源码守卫（标准 56 项，真实配置 57 项）。
6. 版本升到 v1.4.13，更新 CHANGELOG/README/自检。

## 明确不做

- 不新增/删除/修改任何 Combo 的配对、名称、效果、数值。
- 不改 `ResolveComboNames` 的贪心展示规则。
- 不改 TAB 联动标签的格式。
- 不改概率、冷却、伤害、持续时间、奖励、命令、配置 Key。
- 不修「超越天堂」重复显示名（玩家可见文案，需单独确认）。

## 验收门槛

- 构建 0 warning / 0 error。
- 配置测试 32/32；Registry/UI/Lifecycle/Movement/Synergy 56/56。
- 真实部署配置 57/57。
- 架构检查通过，`timers=0`，主类不超 2900。
- `DiceSynergy.cs` 不含 `Utilities.GetPlayers`。
- Combo 公告使用 `GetTruePlayerName`。
- 当前 68 个 Combo 通过自联动/重复配对/空名校验。

## 自检

- 用户：Combo 是否仍正常触发，公告名字是否更干净。
- 设计师：是否只优化成本/可读性/校验，没有改联动数值。
- 工程师：构建/测试/架构/CI、分配消除、能力缓存、真名回退。
