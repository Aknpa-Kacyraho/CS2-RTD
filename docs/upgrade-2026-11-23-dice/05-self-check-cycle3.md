# Cycle 3 自检报告（v1.4.5）

## 用户视角

- 抽骰概率、冷却、伤害、持续时间、效果、命令、配置 Key、玩家可见文案均未改。
- `!rtd_health` 新增 `registry=ok` 或 `registry=E{错误}/W{警告}`，只是诊断信息。
- 启动日志新增一行 `dice_registry` 摘要；有问题时逐条列出，不影响插件加载或抽骰。
- 尚未实机验证；本轮改动不触碰 Dice 运行路径，部署后仍应回归抽骰、`!givedice`、TAB 名称与 Combo。

## 设计师视角

- `DiceRegistry` 是只读校验器，不是第二套真相源：Dice 类型、`DicesConfig`、`RarityConfig`、`DiceSynergy`、`dicefx.json` 仍是各自唯一来源。
- 不实例化未启用的 Dice；已启用实例只用于 `ClassName`/重复类名校验。
- 单项元数据错误只记录，不禁用 Dice、不阻止抽骰、不改权重。
- 已发现但本轮不修的设计问题：
  - `World + Heaven` 与 `Izayoi + Heaven` 的 Combo 显示名都叫「超越天堂」。改名会改变玩家可见文案，留待单独确认。
  - 源码仓库不含 `dicefx.json`，因此本地 Registry 显示 `fx_missing=171`；部署环境加载 `dicefx.json` 后应显著下降。该计数仅是信息，不算错误。
- 未越界做性能索引、MoveType 迁移或 Phoenix 清理。

## 工程师视角

- 新增 `RollTheDice/DiceRegistry.cs`、`RollTheDice.DiceRegistry`（字段）、`BuildDiceRegistry()`、`tests/RollTheDice.RegistryTests/`。
- 主类行数 2668 → 2694（预算 2900）。
- 构建：0 warning / 0 error。
- 配置测试：32/32。
- Registry 测试：8/8，实际断言全部 171 个 Dice 可构造、类型/配置/启用数一致、Combo 引用有效、无未知警告。
- 架构检查：`timers=0`，主类 2694/2900，通过。
- CI 已增加 Registry 测试步骤。
- 未部署、未实机；不能宣称“零 Bug”。

## 结论

v1.4.5 是行为不变的 Dice 元数据可观测性版本，构建、配置测试、Registry 测试、架构检查均通过。已知剩余问题：

1. Combo 显示名「超越天堂」重复（设计问题，需确认后再改）。
2. Phoenix 断线实体/状态清理。
3. `PruneInvalid` 不触发 `OnHolderRemoved`。
4. Amber/Fate/Phoenix/Skyline 直接写 MoveType。
5. 配置 Reload 类型错误与地图 fallback。
6. `GetAllDiceForPlayer` 与大量伤害 wrapper 的性能。
7. 主类仍有其他职责可拆。

因此仍未达到“无 Bug”，下一轮建议做 Phoenix + `PruneInvalid` + MoveType。
