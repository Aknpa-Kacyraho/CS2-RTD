# 设计师 / Dice 播报优化指南（2026-11-23 Cycle 11）

## 现状问题

`AnnounceDiceRarity` 的中央 HUD 使用：

```csharp
Regex.Replace(dice.Description ?? "", "\\{[^}]*\\}", "").Trim()
```

但 `dice.Description` 是 Dice 在 `NotifyPlayers`/`NotifyStatus` 中**被动写入**的字段：

- 两个通知开关都关闭时，它保持 `"Unknown Dice"`。
- 可能保留上一次状态文本，和当前 Dice 不一致。
- 可能是英文/未本地化内容。

而 `!rtd status` 已经用 `DiceUi.DiceDescription` 读取 `dice_<Class>_player`，两者不一致。

另外：

- HUD 直接把名称/描述/特效拼进 HTML，描述里的 `&`、`<` 会破坏版面。
- 传说播报用 `player.PlayerName`，会带 TAB `[Dice]` 前缀。

## 设计决策

1. **单一描述来源**：HUD 改用 `DiceUi.DiceDescription`，与 `!rtd status` 完全一致；`dice.Description` 不再作为 reveal 来源。
2. **输出编码**：所有动态 HUD 文本（名称、稀有度、描述、特效）经 `DiceUi.HtmlEncode`。
3. **真名播报**：传说/组合全服播报用 `GetTruePlayerName`，与 Combo 公告一致。
4. **名称复用**：Dice 名用 `DiceUi.DiceName`，缺 key 时回退类名。
5. **不改层级**：Dice 自身 `NotifyPlayers` 仍在 Add 时发一次效果提示，reveal 再发一次稀有度/HUD；这是既有播报层级，本轮不合并，避免改变 156 个 Dice 的 Add 行为。

## 外部知识依据

- CounterStrikeSharp `PrintToCenterHtml` 接受 HTML 子集；动态文本应先做 HTML 输出编码（OWASP Output Encoding）。
- 社区插件（如 spawn protection）在 `PrintToCenterHtml` 前显式转义 `&`/`<`/`>`，避免 HUD 被用户/配置文本破坏。
- 本地化文本优先从资源 key 读取，不要依赖“上次写入的字段”。

## 不做

- 不改播报文案、音效、全服播报开关、HUD 开关。
- 不合并 `NotifyPlayers` 与 `AnnounceDiceRarity` 的层级。
- 不改概率、效果、持续时间、命令、配置 Key。
- 不做播报冷却/去重。

## 验收

- HUD 描述 = `DiceUi.DiceDescription`。
- HUD 动态文本全部编码。
- 传说播报真名。
- `DiceUi.HtmlEncode` 单测通过。
