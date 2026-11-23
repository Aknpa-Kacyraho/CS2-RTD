# 设计师 / 架构升级指南（2026-11-23 Cycle 2）

## 目标

建立 `PlayerNameService`，把以下职责从 `RollTheDice` 主类移出：

- `_originalPlayerNames`
- `_nameRefreshTimers`
- `_knownDicePrefixes`
- TAB 名称计算、前缀剥离、1 秒重算
- 回合恢复、断线清理、换图/卸载清空

## 边界

`PlayerNameService` 拥有名称状态；不拥有 Dice 列表、玩家配置或 Timer 调度器，这些由主类注入：

- `getDices`
- `getDiceByName`
- `getPlayerConfig`
- `localizer`
- `addTimer`
- `logError`

Combo 解析与 `Trickster` 假名属于显示数据源，允许服务直接引用同程序集静态数据。

## 不做

- 不把名称逻辑继续拆成更多小类。
- 不改 `ShowDiceInTab` 配置。
- 不改 `StripDiceNameDecoration` 规则。
- 不顺手拆 DicePool/PlayerState（下一轮）。
- 不改玩家可见文案。

## 验收

- 主类不再持有三个名称字段。
- 主类只调用 `Refresh/RestoreAll/Forget/Clear` 等 API。
- 名称行为与 1.4.3 一致。
