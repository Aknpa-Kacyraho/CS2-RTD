# 用户 / 管理员收尾清理指南（2026-11-23 Cycle 14）

## 目标

把前面遗留的代码级尾巴一次性收掉：TOML 重载、重复 Combo 名、重复播报、Fate 移动状态、ulong 玩家状态清理。

## 你会看到的变化

- JSON 和 TOML 配置重载都变成“先验证、再应用、失败回滚”。
- Izayoi + Heaven 的 Combo 从「超越天堂」改名为「月华天堂」，不再与 World + Heaven 重名。
- 抽到 Dice 时，Dice 自身的 `NotifyPlayers` 不再和稀有度 reveal 重复发聊天/中央提示：
  - HUD 开启：一条聊天 + 一条中央 HUD；
  - HUD 关闭：两条聊天（稀有度 + 效果描述），不会丢效果信息。
- Fate 的黎明冻结/复活不再直接写 MoveType，统一走移动作用域。
- 断线时，ulong 键的原始/字符串状态（冷却、计时、标记等）会随 SteamID 清理；带实体所有权的复杂 map 不做通用清理。

## 不会变化

- Dice 概率、Combo 效果、持续时间、冷却、伤害、奖励。
- 配置 Key、命令、TAB 格式、播报音效开关。
- `!rtd status`、`!rtd list`、`!rtd tab`。
- 68 个 Combo 的配对与效果（仅一个显示名改变）。

## 验收

- JSON/TOML 正常与失败重载都保留旧配置。
- 抽到 Dice 不再出现“效果提示 + 稀有度提示”两套聊天/中央提示。
- Fate 黎明结束回到 Walk，不再直接写 MoveType。
- 断线后 ulong 冷却/计时状态不残留；实体 map 不被误删。
- 未实机验证前，不宣称“全部零残留”。
