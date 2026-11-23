# Cycle 11 自检报告（v1.4.14）

## 用户视角

- 抽到 Dice 的中央 HUD 描述现在与 `!rtd status` 一致，不再出现英文/`Unknown Dice`/过期描述。
- HUD 里的 `&`、`<`、`>` 不再破坏版面。
- 传说/组合全服播报使用真名，不再带 TAB `[Dice]` 前缀。
- 聊天里的“你抽到了 [稀有度] 名称”保持不变。
- 播报音效、全服播报开关、HUD 开关未改。
- 尚未实机验证；需要重点看抽到 Dice 的聊天/HUD、传说播报、含 `&` 的描述、HUD 关闭。

## 设计师视角

- HUD 描述从“上次 NotifyPlayers 写入的 `dice.Description`”改为“按 ClassName 读取 `_player`”，解决来源不一致。
- 动态文本统一 HTML 编码，符合输出编码原则。
- 传说播报与 Combo 公告统一使用真名。
- 没有合并 `NotifyPlayers` 与稀有度 reveal 的层级：Dice 自身仍会在 Add 时发一次效果提示，reveal 再发稀有度/HUD；这是既有播报设计，本轮未改 156 个 Add。
- 没有改播报文案、音效、开关或 Dice 数值。

## 工程师视角

- `DiceUi.HtmlEncode` 使用 `WebUtility.HtmlEncode`。
- `AnnounceDiceRarity`：
  - `DiceUi.DiceName` 取名；
  - `DiceUi.DiceDescription` 取本地化描述；
  - 名称/稀有度/描述/特效全部编码；
  - `Unknown` 开头描述不显示；
  - 传说播报 `GetTruePlayerName`。
- 构建：0 warning / 0 error。
- 配置测试：32/32。
- Registry/UI/Lifecycle/Movement/Synergy/Broadcast：标准 59/59；真实配置 60/60。
- 架构检查：`timers=0`，主类 2721/2900，通过。
- 未部署、未实机；CI 将在提交后运行。

## 外部知识核对

- CounterStrikeSharp `PrintToCenterHtml` 接受 HTML 子集；动态文本应先做输出编码。
- 社区插件在 `PrintToCenterHtml` 前显式转义 `&`/`<`/`>`。
- OWASP 输出编码原则：在输出点对动态内容做上下文编码。
- 本地化文本应直接读资源 key，不依赖“上次写入的字段”。

## 结论

v1.4.14 是 Dice 播报一致性与安全编码版本，构建、配置测试、59/60 项断言、架构检查均通过。仍未达到“无 Bug”，剩余明确风险：

1. `NotifyPlayers` 与稀有度 reveal 仍会各发一次聊天/中央提示，未合并。
2. 「超越天堂」重复显示名。
3. `ResolveComboNames` 贪心去重。
4. `Fate` 仍直接写 MoveType。
5. `Dictionary<ulong, T>` 状态不做自动清理。
6. CSS Reload 属性 setter 中途抛异常仍可能部分更新。
7. 高频 Dice 扫描与伤害 wrapper 性能。
8. 主类 2721/2900，仍有职责可拆。
9. 所有改动尚未实机验证。
