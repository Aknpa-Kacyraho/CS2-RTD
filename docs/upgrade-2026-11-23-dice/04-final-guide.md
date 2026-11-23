# 统一 Dice 设计指南（Cycle 3 / v1.4.5）

## 目标

在不改变任何 Dice 玩家行为的前提下，给 Dice 的“身份元数据”建立只读登记表，让配置、代码类型、本地化名称、稀有度、Combo 与特效配置之间的错位在启动时可见，而不是等玩家抽到才暴露。

## 本轮范围

1. 新增 `RollTheDice/DiceRegistry.cs`：
   - 统计 Dice 类型、配置项、启用项。
   - 校验类型/配置一一对应。
   - 校验 `ClassName` 与类型名、重复 `ClassName`（仅对已实例化 Dice）。
   - 校验稀有度 tier、`dice_*_name` 本地化 key。
   - 校验 Combo A/B 引用的 Dice 是否存在、Combo 显示名是否重复。
   - 统计缺少 `dicefx.json` profile 的 Dice（仅信息，不报错）。
2. `InitializeModules` 构建 Registry，并在启动日志输出摘要与问题列表。
3. `DestroyModules` 释放 Registry。
4. `!rtd_health` 增加 `registry=ok` 或 `registry=E{errors}/W{warnings}`。
5. 版本升到 v1.4.5，更新 CHANGELOG/README/自检。

## 明确不做

- 不改任何 Dice 的概率、冷却、伤害、持续时间、效果、命令、配置 Key。
- 不实例化未启用的 Dice，不为了校验触发构造副作用。
- 不因为 Registry 报错而卸载插件、禁用 Dice 或阻止抽骰。
- 不修改 `DicesConfig`、`RarityConfig`、`DiceSynergy`、`dicefx.json` 的格式。
- 不做性能索引、MoveType 迁移或 Phoenix 清理（后续 Cycle）。

## 设计约束

- 校验只读，失败隔离：任何一项异常都只记录，不影响 `InitializeModules` 后续流程。
- 启动日志只在存在问题时逐条输出；无问题时只输出一行摘要，避免刷屏。
- 已启用的 Dice 实例用于 `ClassName` 校验；未启用的类型只校验配置/稀有度/名称。
- 本地化 key 的判定沿用项目现有约定：`localizer[key]` 返回 key 本身即视为缺失。
- Combo 显示名重复只警告，不改名、不去重、不改触发逻辑。

## 验收门槛

- 构建 0 warning / 0 error。
- 配置测试 32/32。
- Registry 测试 8/8。
- 架构检查通过，裸 `new Timer(` 仍为 0，主类行数不超 2900。
- 启动日志出现 `dice_registry` 摘要。
- `!rtd_health` 能显示 Registry 状态。
- 抽骰、`!givedice`、TAB 名称、Combo、Dice 特效与 v1.4.4 一致。

## 自检

- 用户：玩法、命令、文案、概率是否完全没变。
- 设计师：Registry 是否只读、是否越界改行为、是否制造重复真相源。
- 工程师：构建/测试/架构/CI 是否通过，日志是否可诊断，发布是否可复现。
