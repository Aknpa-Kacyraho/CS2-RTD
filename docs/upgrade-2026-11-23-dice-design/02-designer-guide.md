# 设计师 / Dice 设计指南（2026-11-23 Cycle 12）

## 问题

旧 `ResolveComboNames` 是**贪心消费**：

```csharp
foreach combo:
  if set contains A && set contains B:
     set.Remove(A); set.Remove(B); set.Add(comboName);
```

一个 Dice 只能被一个 Combo 消费。共享 Dice 很多：

- Adrenaline 同时参与 Berserker、Overheat、PainConverter 的 Combo。
- BlackHole 同时参与 GravityWell、WhiteHole。
- Drone 同时参与 RepulsionField、Satellite。

结果：TAB 标签只显示第一个命中的 Combo，其他联动被隐藏。顺序由 `_combos` 定义顺序决定，玩家无法知道全部生效联动。

## 设计决策

1. **先收集、后替换**：先找出所有“双方都在”的 Combo，记录参与 Dice 和 Combo 名；不边遍历边删。
2. **展示全部 Combo**：每个命中的 Combo 名都进入结果；同名 Combo 只展示一次。
3. **保留独立 Dice**：没有参与任何命中 Combo 的 Dice 继续作为标签。
4. **顺序稳定**：Combo 按定义顺序在前，独立 Dice 按当前 active 顺序在后；`[A]+N` 压缩仍由上层处理。
5. **不改数据**：68 个 Combo 的配对、名称、效果不变；「超越天堂」重复名仍是已知数据项。

## 外部知识依据

- 数据驱动联动系统应避免“顺序依赖的消费”；先计算完整命中集合，再做展示/结算，能避免隐藏联动。
- 共享节点的图式联动（一个 Dice 连多个 Combo）应展示所有边，而不是只走第一条边。
- UI 侧仍用条数压缩，完整集合不等于无限长度。

## 不做

- 不改 Combo 数据、效果、数值。
- 不改 TAB 格式或 `!rtd tab` 开关。
- 不修「超越天堂」重复显示名（玩家可见文案，需单独确认）。
- 不做 Combo 收藏/筛选/菜单。

## 验收

- `Adrenaline+Berserker+Overheat` 同时解析出「狂暴血脉」「狂热」。
- `Giant+RoyalBarrier+Fireball` 解析出「钢铁要塞」+ `Fireball`。
- 原有单 Combo 解析不变。
