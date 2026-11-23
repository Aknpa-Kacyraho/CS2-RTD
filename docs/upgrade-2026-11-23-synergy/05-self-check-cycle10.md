# Cycle 10 自检报告（v1.4.13）

## 用户视角

- Combo 仍按原条件触发，公告文案不变，只是玩家名不再带 TAB 的 `[Dice]` 前缀。
- 高玩家数/多 Dice 时，Combo 检查不再每次新建全服玩家列表。
- 没有 Dice 需要联动刷新回调时，系统不再空跑 171 个 Dice。
- 启动时新增联动数据校验：自联动、重复配对、空显示名会被指出。
- 68 个 Combo 的配对、名称、效果、数值均未改。
- 尚未实机验证；需要重点看 Combo 公告、TAB 联动标签、同队联动、重复 Dice。

## 设计师视角

- `HasPartner` 从“全服扫描 + 每玩家查字典”改为“伙伴 Dice 持有者扫描”，语义保持：自己或同队持有伙伴 Dice 即成立。
- `RefreshCombos` 保留 `OnDiceSetChanged` 扩展点，用能力缓存避免空调用。
- Combo 公告走真名，避免装饰名进聊天。
- Registry 增加数据完整性守卫，不修改 Combo 表。
- 已知未修：
  - 「超越天堂」仍由 World+Heaven 与 Izayoi+Heaven 共用；改名属于玩家可见文案，未擅自改。
  - `ResolveComboNames` 仍是贪心去重：一个 Dice 只展示一个联动；共享 Dice 的多个 Combo 不会全部出现在 TAB 标签里。

## 工程师视角

- `DiceSynergy.HasPartner` 不再出现 `Utilities.GetPlayers`；改为 `GetDiceByName` + 伙伴 `_players` 扫描。
- 新增 `RollTheDice.GetDiceByName` / `GetTruePlayerName`。
- `DiceBlueprint.HasDiceSetChangedOverrides` 程序集扫描一次并缓存；当前无覆写。
- `DiceRegistry` 新增自联动、重复无序配对、空显示名警告。
- 构建：0 warning / 0 error。
- 配置测试：32/32。
- Registry/UI/Lifecycle/Movement/Synergy：标准 56/56；真实配置 57/57。
- 架构检查：`timers=0`，主类 2724/2900，通过。
- 未部署、未实机；CI 将在提交后运行。

## 外部知识核对

- CounterStrikeSharp `Utilities.GetPlayers()` 每次都 `new List<CCSPlayerController>()` 并遍历 `Server.MaxPlayers`；Combo 检查在每 tick 路径上应避免。
- 数据驱动联动需要唯一配对/显示名校验；本轮把校验放进只读 Registry。
- `HashSet`/`Dictionary` 的 O(1) 查找优于反复线性扫描；伙伴 Dice 持有者列表通常远小于全服玩家列表。

## 结论

v1.4.13 是 Dice 联动性能与数据完整性优化版本，构建、配置测试、56/57 项断言、架构检查均通过。仍未达到“无 Bug”，剩余明确风险：

1. 「超越天堂」重复显示名（未改，需确认文案）。
2. `ResolveComboNames` 贪心去重，共享 Dice 的多个 Combo 不会全部展示。
3. `Fate` 仍直接写 MoveType（dawn/darktide）。
4. `Dictionary<ulong, T>` 状态不做自动清理。
5. CSS Reload 属性 setter 中途抛异常仍可能部分更新。
6. 高频 Dice 扫描与伤害 wrapper 性能。
7. 主类 2724/2900，仍有职责可拆。
8. 所有改动尚未实机验证。
