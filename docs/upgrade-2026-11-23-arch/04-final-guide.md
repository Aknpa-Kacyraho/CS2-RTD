# 统一架构升级指南（Cycle 2 / v1.4.4）

## 目标

在不改变玩家可见名称的前提下，把名称/TAB 职责从 `RollTheDice` 主类抽成独立 `PlayerNameService`。

## 本轮范围

1. 新增 `RollTheDice/PlayerNameService.cs`，拥有：
   - `_originalNames`
   - `_refreshTimers`
   - `_knownPrefixes`
   - `Refresh`/`RestoreAll`/`Forget`/`Clear`/`GetTrueName` 等 API
2. 主类删除对应字段/方法，只保留服务调用。
3. `InitializeModules` 配置服务并重建前缀。
4. `DestroyModules` 清空前缀。
5. 断线/回合/换图/卸载迁移到服务 API。
6. 更新 v1.4.4、CHANGELOG、README、架构指南与自检。

## 明确不做

- 不改 `ShowDiceInTab`、名称前缀格式、Combo 显示、1 秒重算时长。
- 不改 Dice/配置/Timer 其他逻辑。
- 不拆 DicePool/PlayerState（下一轮）。
- 不做性能索引或 MoveType 迁移。

## 验收门槛

- 主类行数明显下降。
- 主类无名称三字段残留。
- 构建 0 warning / 0 error。
- 配置测试 32/32。
- 架构检查通过。
- 实机/静态验证 TAB 开关、抽骰、移除、回合恢复、断线、换图。

## 自检

- 用户：名称/TAB 是否完全一致。
- 设计师：职责边界是否清晰，是否过度抽象。
- 工程师：构建、测试、架构、diff、发布可复现。
