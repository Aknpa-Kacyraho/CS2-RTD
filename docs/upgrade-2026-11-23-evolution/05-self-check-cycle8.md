# Cycle 8 自检报告（v1.4.11）

## 用户视角

- 天际飞行结束、虚无结束会恢复到进入前的移动状态，而不是无条件变回步行。
- 多个移动效果重叠时，最后激活者优先；先结束的效果不会覆盖后面的效果。
- 死亡、`!givedice` 替换、回合结束会释放对应移动作用域，减少“重生还在飞/还在虚无”的残留。
- `!rtd_health` 新增 `move_locks=keys/players`。
- 飞行/虚无/冻结/涅槃的时长、冷却、伤害、奖励均未改。
- 尚未实机验证；需要重点看 Skyline 飞行、Void 结束、Phoenix 死亡、Amber 冻结、效果重叠。

## 设计师视角

- 移动状态从“各 Dice 各写 MoveType”收口为命名作用域栈；最后一个作用域优先。
- 首次加锁同时快照 `MoveType` 与 `ActualMoveType`，恢复时两者一起写回。
- `Lock/Unlock/IsLocked/Clear/ClearAll` API 保持兼容；旧调用仍锁 `MOVETYPE_NONE`。
- 只迁移 Skyline/Void；Fate 的 dawn/darktide 混合复活与静态状态，留到后续轮次。
- 死亡/移除显式释放作用域，避免 Pawn 失效时只清字典、不清锁。
- 没有新增配置 Key，没有改任何玩法数值。

## 工程师视角

- 新增 `MoveScopeStack` 纯逻辑栈；`MoveLockManager` 使用 `MoveType` + `ActualMoveType` ref 属性，移除 `Schema.SetSchemaValue` 依赖。
- `ApplyTarget` 在两侧 MoveType 已经匹配时跳过写入，避免每 tick 重复 `SetStateChanged`。
- `Unlock` 对无效 controller 的 SteamID 读取做了异常隔离。
- `!rtd_health` 第 3 行显示移动锁计数。
- 构建：0 warning / 0 error。
- 配置测试：32/32。
- Registry/UI/Lifecycle/Movement：45/45。
- 架构检查：`timers=0`，主类 2709/2900，通过。
- 未部署、未实机；CI 将在提交后运行。

## 外部知识核对

- CounterStrikeSharp `MoveType_t`：`MOVETYPE_NONE=0`、`MOVETYPE_WALK=2`、`MOVETYPE_NOCLIP=7`（docs.cssharp.dev）。
- `CBaseEntity.ActualMoveType` 是 ref-return schema 属性；本轮用它同步 `m_nActualMoveType`，不再手写 schema 常量。
- CSS 的 `PluginConfigExtensions.Reload` 更新当前实例，佐证上一轮“先类型校验再 Reload”的必要性。
- CSS 的 OnTick 是每 tick 成本；本轮不增加全量扫描，`ApplyTarget` 在状态匹配时直接返回。

## 结论

v1.4.11 是移动作用域全面进化版本，构建、配置测试、45 项断言、架构检查均通过。仍未达到“无 Bug”，剩余明确风险：

1. `Fate` 仍直接写 MoveType（dawn/darktide）。
2. `Dictionary<ulong, T>` 状态不做自动清理。
3. «超越天堂» Combo 显示名重复。
4. 配置重载只做前置类型校验，CSS Reload 应用阶段失败仍可能部分更新。
5. 高频 Dice 扫描与伤害 wrapper 性能。
6. 主类 2709/2900，仍有职责可拆。
7. 所有改动尚未实机验证。
