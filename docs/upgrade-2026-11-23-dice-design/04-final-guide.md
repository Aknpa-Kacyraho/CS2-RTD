# 统一 Dice 设计优化指南（Cycle 12 / v1.4.15）

## 目标

修掉 TAB Combo 标签的“贪心消费”设计问题：共享 Dice 的多个联动不再被第一个命中的 Combo 吞掉，同时保留独立 Dice 与现有压缩格式。

## 本轮范围

1. `DiceSynergy.ResolveComboNames` 改为“先收集全部命中 Combo，再保留未参与 Dice”。
2. 返回 `List<string>`，Combo 按定义顺序在前，独立 Dice 在后。
3. 同名 Combo 只展示一次（「超越天堂」仍为已知重复项）。
4. 测试新增共享 Dice、独立 Dice 两个场景（标准 61，真实配置 62）。
5. 版本升到 v1.4.15，更新 CHANGELOG/README/自检。

## 明确不做

- 不改 68 个 Combo 的配对、名称、效果、数值、触发。
- 不改 TAB 格式、`!rtd tab` 开关、名字压缩规则。
- 不修「超越天堂」重复显示名。
- 不改概率、冷却、伤害、持续时间、奖励、命令、配置 Key。

## 验收门槛

- 构建 0 warning / 0 error。
- 配置测试 32/32；Registry/UI/Lifecycle/Movement/Synergy/Broadcast/Design 61/61。
- 真实部署配置 62/62。
- 架构检查通过，`timers=0`，主类不超 2900。
- `Adrenaline+Berserker+Overheat` 同时展示两个 Combo。
- 独立 Dice 不被误删。

## 自检

- 用户：TAB 标签是否更完整，是否仍然可读。
- 设计师：是否只修展示集合，没有改 Combo 数据/数值。
- 工程师：构建/测试/架构/CI、返回类型调用点、顺序稳定性。
