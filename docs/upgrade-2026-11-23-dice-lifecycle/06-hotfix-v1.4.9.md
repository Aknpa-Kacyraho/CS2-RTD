# Cycle 6 热修说明（v1.4.9）

## 为什么需要热修

v1.4.8 的清理链路覆盖了：

- controller 已失效的断线（`RemoveDiceForSteamId` → `RemoveBySteamId`）。
- `PruneInvalid` 看到的失效 controller。

但 **controller 仍有效** 的断线走的是 `RemoveDiceForPlayer` → `dix.Remove(player, reason)`。如果某个老 Dice 的 `Remove` 只执行 `_players.Remove(player)`，却留下次级 `Dictionary<CCSPlayerController, T>`，那么之后的 `PruneInvalid` 已经看不到这个 controller，通用兜底也不会执行。v1.4.9 补上这条路径。

## 改动

- 新增 `DiceBlueprint.CleanupHolderState(ulong steamId, DiceRemoveReason reason)`：
  - `OnHolderRemoved`（Dice 特殊清理）
  - `CleanControllerState`（controller 键字典/集合兜底）
- `RemoveBySteamId`、`PruneInvalid` 改为调用该 API。
- `RemoveDiceForPlayer` 在 `reason == Disconnect` 时，执行完 `Remove` 后补跑 `CleanupHolderState`。
- 新增 1 项断言（共享清理 API 存在），总数 29。

## 安全性

- `OnHolderRemoved` 与字典删除都是幂等的；同一个 SteamID 被 `Remove` 和 `CleanupHolderState` 各清一次不会产生额外效果。
- 仍然不自动清 `Dictionary<ulong, T>`。
- 仍然不改变 Dice 概率、伤害、持续时间、奖励、命令、配置 Key。

## 验证

```text
构建：0 warning / 0 error
配置测试：32/32
Registry/UI/Lifecycle：29/29
架构检查：timers=0，主类 2703/2900
```

未实机验证；需要重点回归有效 controller 断线、无效 controller 断线、`PruneInvalid`、Phoenix/Amber 清理。
