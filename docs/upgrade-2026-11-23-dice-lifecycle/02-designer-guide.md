# 设计师 / 生命周期指南（2026-11-23 Cycle 6）

## 问题

老 Dice 普遍用 `Dictionary<CCSPlayerController, T>` / `HashSet<CCSPlayerController>` 保存状态，但只有部分 Dice 实现了 `OnHolderRemoved`。断线时主类走 `RemoveBySteamId`，`Remove` 里的清理不执行；`PruneInvalid` 又只删 `_players`，不触发任何清理。结果是：

- 旧控制器键残留到回合结束。
- 光翼/光晕等实体没人拆。
- 重连/换图后旧状态看似“复活”。

## 设计决策

1. **通用兜底**：`DiceBlueprint` 缓存每个 Dice 类型的 controller-keyed 字典/集合字段；断线和 prune 时统一移除“无效控制器”或“SteamID 匹配”的键。
2. **特殊清理优先**：通过 `CleanupHolderState` 先调用 Dice 自己的 `OnHolderRemoved`（拆 CBeam、光晕、还原血量），再跑通用兜底，避免通用清理提前删掉特殊清理需要的数据。
3. **只清 controller 键**：不自动清 `Dictionary<ulong, T>`，因为 uint64 状态可能跨会话保留（余额、持久配置等），必须由 Dice 自己决定。
4. **Prune 与断线同权**：`PruneInvalid` 对每个失效控制器调用 `OnHolderRemoved`，再跑通用兜底。
5. **两个老 Dice 顺手统一移动状态**：Amber、Phoenix 改用 `MoveLockManager`，不再直接写 `MoveType`；Phoenix 仍保留特殊的光翼/光晕/血量清理。

## 不做

- 不自动清 ulong 键字典，不碰持久化数据。
- 不改 Phoenix/Amber 的概率、时长、伤害、奖励。
- 不一次性迁移 Fate/Skyline/Void 的 noclip/复活 MoveType（机制不同，下一轮单独评估）。
- 不重构 171 个 Dice 的 `Remove`。

## 验收

- 通用兜底能命中 Phoenix/Skyline/Void 的 controller 字典。
- Amber/Phoenix 的 `OnHolderRemoved` 在类型上真实存在。
- 特殊清理顺序正确：Phoenix 先拆光翼/光晕，再删字典。
- 29 项 Registry/UI/Lifecycle 断言通过。
