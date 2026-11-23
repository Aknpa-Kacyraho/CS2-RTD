# 设计师 / Dice 联动优化指南（2026-11-23 Cycle 10）

## 现状

- `DiceSynergy` 有 68 个 Combo，很多 Dice 在 `OnTick`/伤害回调里调用 `HasPartner`。
- 旧 `HasPartner` 每次调用都会：
  1. `HasDiceActive` 查一次字典并扫持有者；
  2. `Utilities.GetPlayers()` 新建一个全服玩家列表；
  3. 对每个玩家再查一次 Dice 字典。
- `RefreshCombos` 会遍历 171 个 Dice 调用 `OnDiceSetChanged`，但当前没有任何 Dice 覆写它。
- `AnnounceCombo` 直接用 `player.PlayerName`，会带上 TAB 的 `[Dice]` 装饰前缀。

## 设计决策

1. **按伙伴 Dice 的持有者列表查**：`HasPartner` 只解析一次伙伴 Dice，然后遍历它的小持有者列表，判断自己/同队；不再分配全服玩家列表。
2. **能力缓存**：`DiceBlueprint.HasDiceSetChangedOverrides` 扫描一次程序集；没有覆写时，`RefreshCombos` 跳过 171 个空虚调用。保留扩展点，未来有 Dice 覆写会自动恢复调用。
3. **真名公告**：新增 `GetTruePlayerName`，`AnnounceCombo` 使用未装饰真名，避免 `[Dice] 名字 触发了...`。
4. **数据校验进 Registry**：自联动、重复无序配对、空显示名、未知 Dice、重复显示名都在启动时只读检查，不改变 Combo。
5. **不改 Combo 数据**：68 个 Combo 的配对、名称、效果全部保持；重复显示名「超越天堂」仍然保留并作为已知警告。

## 外部知识依据

- CounterStrikeSharp `Utilities.GetPlayers()` 每次调用都会 `new List<CCSPlayerController>()` 并遍历 `Server.MaxPlayers`；在每 tick 的 Combo 检查里应避免。
- 数据驱动联动系统需要唯一配对/显示名校验，避免复制粘贴产生隐形重复。
- `HashSet`/`Dictionary` 的 O(1) 查找优于反复线性扫描；本轮把“全服扫描”换成“伙伴 Dice 持有者扫描”。

## 不做

- 不新增/删除/改数值任何 Combo。
- 不改 `ResolveComboNames` 的贪心去重规则（TAB 标签仍是“一个 Dice 只显示一个联动”）。
- 不改 Combo 公告文案、颜色、触发条件。
- 不做 Combo 冷却/去重/收藏。

## 验收

- `HasPartner` 不再出现 `Utilities.GetPlayers()`。
- `RefreshCombos` 在无覆写时跳过 Dice 循环。
- Combo 公告使用真名。
- Registry 能发现自联动/重复配对/空名。
