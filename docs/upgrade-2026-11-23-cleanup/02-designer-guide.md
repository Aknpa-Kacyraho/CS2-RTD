# 设计师 / 收尾清理指南（2026-11-23 Cycle 14）

##  backlog 对照

| 遗留项 | 本轮处理 |
|---|---|
| TOML 重载无回滚 | 用 Tomlyn 反序列化 + `ConfigApplier` 原子应用 |
| 「超越天堂」重复显示名 | Izayoi+Heaven 改名「月华天堂」 |
| `NotifyPlayers` 与 rarity reveal 重复 | Add 期间抑制 `NotifyPlayers`，reveal 单一发送；HUD 关闭时聊天补效果 |
| `Fate` 直接写 MoveType | 新增 `ReleaseTo`，黎明冻结/复活/清理走作用域 |
| ulong 状态不自动清理 | 通用清理 primitive/string/enum/HashSet<ulong> 值；实体 map 跳过 |
| 性能索引 | 未做：需要先实机 profiling，盲目加索引会增加失效风险 |
| 主类拆分 | 未做：2721→2730，仍低于 2900 预算；本轮改动优先收尾而非搬家 |
| 实机验证 | 无法替代：仍需部署后回归 |

## 关键决策

1. **TOML 也走原子应用**：使用与 CSS 相同的 Tomlyn 选项（属性名恒等映射），候选配置成功后才应用；失败回滚。
2. **Combo 改名**：这是唯一玩家可见文案变更；「月华天堂」与 Izayoi 的月系主题一致，避免与 World+Heaven 重名。
3. **播报单一入口**：Add 期间抑制 `NotifyPlayers`，`AnnounceDiceRarity` 负责 reveal；HUD 关闭时把描述补到聊天，避免丢信息。
4. **Fate 用显式 fallback**：`ReleaseTo(player, key, WALK)` 在最后一个作用域结束时应用 Walk，而不是恢复死亡时快照的 0。
5. **ulong 清理只碰安全值**：primitive/string/enum/HashSet<ulong> 可清；剑/光束/光晕等实体 map 跳过，交给 Dice 自己拆。

## 不做

- 不做性能索引/主类拆分（需要 profiling 与更大重构）。
- 不改 Dice 数值、Combo 效果、配置 Key。
- 不做实机验证（需要服务器）。
- 不清理实体 map 的嵌套引用（例如 `Dictionary<ulong, HashSet<ulong>>` 只删外层 key）。

## 验收

- 六项代码级遗留全部有实现或明确说明。
- 当前 68 个 Combo 显示名唯一。
- 73/74 项断言通过。
