# Cycle 14 自检报告（v1.4.17）

## 用户视角

- JSON 与 TOML 配置重载都变成“全有或全无”：失败回滚，旧配置继续生效。
- Izayoi + Heaven 的 Combo 改名「月华天堂」，不再与 World + Heaven 重名。
- 抽到 Dice 不再同时收到 Dice 自身效果提示 + 稀有度 reveal：
  - HUD 开：一条聊天 + 一条中央 HUD；
  - HUD 关：稀有度聊天 + 效果描述聊天，不丢信息。
- Fate 黎明冻结/复活不再直接写 MoveType，结束时回到 Walk。
- 断线时 ulong 冷却/计时/标记状态会随 SteamID 清理；剑/光束/光晕等实体 map 不会被误删。
- Dice 概率、Combo 效果、持续时间、冷却、伤害、奖励未改。
- 尚未实机验证。

## 设计师视角

- 六项代码级遗留全部处理：
  1. TOML 原子重载；
  2. 重复 Combo 名；
  3. 重复播报；
  4. Fate MoveType；
  5. ulong 状态清理；
  6. 测试覆盖。
- 性能索引未做：需要实机 profiling，盲目加索引会引入失效风险。
- 主类拆分未做：2730/2900，仍在预算内；本轮优先收尾。
- 实体 map 嵌套引用未做通用清理：需要 Dice 级专用逻辑，避免丢实体。
- 实机验证仍待部署。

## 工程师视角

- `ConfigFileValidator.TryDeserializeToml` + `ConfigApplier`：TOML 原子应用。
- `DiceSynergy`：`Izayoi+Heaven` → 「月华天堂」；Combo 显示名唯一测试。
- `DiceBlueprint.AddWithoutHolderNotification` + suppression depth；`NotifyPlayers` 早退；`RollTheDiceForPlayer` 两处接入。
- `MoveLockManager.ReleaseTo`；Fate 三处迁移；Dice 源码守卫加入 Fate。
- `CleanControllerState` 扩展 ulong primitive/string/enum/`HashSet<ulong>`；实体值 map 跳过；新增 probe 测试。
- 构建：0 warning / 0 error。
- 配置测试：32/32。
- Registry/UI/Lifecycle/Movement/Synergy/Broadcast/Design/Config/Cleanup：标准 73/73；真实配置 74/74。
- 架构检查：`timers=0`，主类 2730/2900，通过。
- 未部署、未实机；CI 将在提交后运行。

##  backlog 对照

| 遗留项 | 结果 |
|---|---|
| TOML 无回滚 | 已修 |
| 重复 Combo 名 | 已修 |
| 重复播报 | 已修 |
| Fate MoveType | 已迁移 |
| ulong primitive 状态 | 已通用清理 |
| 实体 map 嵌套引用 | 明确保留，避免丢实体 |
| 性能索引 | 未做，需 profiling |
| 主类拆分 | 未做，2730/2900 |
| 实机验证 | 未做，需服务器 |

## 结论

v1.4.17 是收尾清理版本，构建、配置测试、73/74 项断言、架构检查均通过。剩余风险：

1. 性能索引与主类拆分仍未做（非阻塞，需 profiling/更大重构）。
2. 实体 map 嵌套引用仍可能残留（避免误删实体的取舍）。
3. 所有改动尚未实机回归。
4. `NotifyPlayers` 抑制依赖 Add 路径；若未来有 Add 之外的玩家通知，需确认是否应被抑制。
5. `ReleaseTo` 的 fallback 语义是新 API，Fate 之外的 Dice 尚未使用。
