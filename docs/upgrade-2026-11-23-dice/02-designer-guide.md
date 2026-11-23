# 设计师 / Dice 设计指南（2026-11-23 Cycle 3）

## 设计目标

建立只读 `DiceRegistry`，把 Dice 的“身份元数据”集中描述：

- `ClassName` / 类型名
- `DicesConfig` 属性与 `Enabled`
- 稀有度 tier
- 显示名/玩家文案 key
- Combo 引用
- `dicefx.json` profile 是否存在

## 边界

- Registry 只读，不实例化 Dice，不参与抽骰/权重。
- 不改变 `DicesConfig` JSON、`RarityConfig`、`DiceSynergy`。
- 不要求所有 Dice 继承新基类。
- 不把 `EventPlayerDeath`/`OnPlayerTakeDamagePre` 合并。
- 发现错误只记录 warning/error，不禁用 Dice。

## 验收

- Startup 日志有 registry 摘要。
- `!rtd_health` 显示错误/警告数。
- 现有 171 个 Dice 配置/名称/稀有度无缺失。
- 文档/Combo/DiceFx 异常能被指出。
