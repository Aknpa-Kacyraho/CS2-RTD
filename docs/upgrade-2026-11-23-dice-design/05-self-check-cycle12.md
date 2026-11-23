# Cycle 12 自检报告（v1.4.15）

## 用户视角

- 共享 Dice 的多个 Combo 现在能同时显示：
  - `Adrenaline + Berserker + Overheat` 同时出现「狂暴血脉」「狂热」。
- 没有参与 Combo 的 Dice 仍保留在 TAB 标签里。
- `[A]+N` 压缩仍然生效，不会无限变长。
- 68 个 Combo 的触发、效果、数值未改。
- 尚未实机验证；需要重点看共享 Dice 的 TAB 标签、`!rtd tab` 开关、Combo 公告。

## 设计师视角

- 修掉 `ResolveComboNames` 的顺序依赖贪心消费：先收集全部命中 Combo，再决定哪些 Dice 保留。
- 共享 Dice 不再被第一个 Combo 吃掉；一个 Dice 可以参与多个展示 Combo。
- 同名 Combo 仍只展示一次（「超越天堂」重复项未改）。
- 独立 Dice 顺序稳定；Combo 在前，独立 Dice 在后。
- 没有改 Combo 数据、数值、TAB 格式或开关。

## 工程师视角

- `DiceSynergy.ResolveComboNames` 返回类型改为 `List<string>`。
- 使用 `participating` 记录参与 Dice，`comboSeen` 去重显示名。
- `PlayerNameService` 的 `.Select().ToList()` 调用点无需改动。
- 测试新增共享 Dice / 独立 Dice 场景。
- 构建：0 warning / 0 error。
- 配置测试：32/32。
- Registry/UI/Lifecycle/Movement/Synergy/Broadcast/Design：标准 61/61；真实配置 62/62。
- 架构检查：`timers=0`，主类 2721/2900，通过。
- 未部署、未实机；CI 将在提交后运行。

## 外部知识核对

- 数据驱动联动应避免顺序依赖的消费；先计算完整命中集合，再做展示/结算。
- 共享节点的图式联动应展示所有边，而不是只走第一条边。
- UI 侧仍可用条数压缩控制长度，完整集合不等于无限展示。

## 结论

v1.4.15 是 Dice 设计/TAB 联动展示版本，构建、配置测试、61/62 项断言、架构检查均通过。仍未达到“无 Bug”，剩余明确风险：

1. 「超越天堂」重复显示名仍只展示一次，未改名。
2. `NotifyPlayers` 与稀有度 reveal 仍各发一次。
3. `Fate` 仍直接写 MoveType。
4. `Dictionary<ulong, T>` 状态不做自动清理。
5. CSS Reload 属性 setter 中途抛异常仍可能部分更新。
6. 高频 Dice 扫描与伤害 wrapper 性能。
7. 主类 2721/2900，仍有职责可拆。
8. 所有改动尚未实机验证。
