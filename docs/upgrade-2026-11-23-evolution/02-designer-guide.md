# 设计师 / 全面进化指南（2026-11-23 Cycle 8）

## 问题

旧实现里每个 Dice 各自写 `MoveType`：

- 天际飞行：写 7（noclip），结束写 2（walk）。
- 虚无：写 7，结束写 2。
- 琥珀/凤凰：已迁移到 `MoveLockManager`，但只支持“锁到 0”。
- 多个效果叠加时，后结束的效果可能把先结束/仍生效的效果覆盖掉。

## 设计决策

1. **命名作用域栈**：`MoveScopeStack` 保存 `key → 目标 MoveType`，最后一个作用域优先；移除最后一个才恢复原始状态。
2. **原始状态快照**：每个玩家第一次加锁时同时记录 `MoveType` 与 `ActualMoveType`，恢复时两者一起写回。
3. **统一入口**：`MoveLockManager.Lock(player, key)` 仍锁 0；新增 `Lock(player, key, MoveType_t target)` 支持 noclip/其他目标。
4. **先迁移两个边界清晰的 Dice**：Skyline、Void。Fate 的 dawn/darktide 逻辑混合复活、冻结与静态状态，留到单独一轮。
5. **死亡/移除显式释放**：Skyline/Void/Phoenix 的 `Remove` 即使 Pawn 已失效，也尝试移除命名作用域，避免重生继承。
6. **可观测**：`!rtd_health` 显示 `move_locks=keys/players`。

## 外部知识依据

- CounterStrikeSharp `MoveType_t`：`MOVETYPE_NONE=0`、`MOVETYPE_WALK=2`、`MOVETYPE_NOCLIP=7`（docs.cssharp.dev）。
- `CBaseEntity.ActualMoveType` 是 ref-return schema 属性，写它可保持 `m_nActualMoveType` 与 `m_MoveType` 同步（docs.cssharp.dev / CSS 源码）。
- CSS `PluginConfigExtensions.Reload` 会更新当前配置实例，所以重载前必须做类型校验（上一轮已落地）。
- CSS 的 OnTick 是服务器每 tick 成本；本轮不增加新的每 tick 全量扫描，作用域只在激活期间维护。

## 不做

- 不迁移 Fate 的 dawn/darktide MoveType。
- 不改任何时长、冷却、概率、伤害、奖励。
- 不做移动状态配置化，不引入新的配置 Key。
- 不重写 `MoveLockManager` 的调用方；保留原有 `Lock/Unlock/IsLocked/Clear/ClearAll` API。

## 验收

- 作用域栈嵌套/更新/移除行为有纯逻辑测试。
- Skyline/Void 源码不再直接写 `.MoveType =`。
- Amber/Phoenix/Skyline/Void 的源码守卫通过。
- `!rtd_health` 能显示移动锁计数。
