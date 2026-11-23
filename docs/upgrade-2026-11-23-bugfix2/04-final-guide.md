# 统一 Bug 修复指南（Cycle 7 / v1.4.10）

## 目标

修复“先破坏、后失败”的三处问题，保持所有 Dice 数值与命令语义不变。

## 本轮范围

1. `ConfigFileValidator`：重载前反序列化临时 `PluginConfig`，类型错误不进入 CSS Reload。
2. `CommandGiveDice`：`MinArgs` 1 → 0，使 `!givedice` 列表分支可达。
3. `AutoRollTick`：空 drawable pool 直接返回，不先移除玩家 Dice。
4. 测试增加 5 项（标准 34 项；带真实配置路径 35 项）。
5. 版本升到 v1.4.10，更新 CHANGELOG/README/自检。

## 明确不做

- 不替代 CSS 的 Reload 实现，不做配置热迁移。
- 不改 Dice 概率、权重、冷却、伤害、持续时间、奖励、命令参数。
- 不改 `!givedice` 权限、玩家匹配、随机逻辑。
- 不改 AutoRoll 的替换策略、间隔、计数。
- 不处理 Fate/Skyline/Void MoveType、ulong 状态清理、Combo 重名。

## 验收门槛

- 构建 0 warning / 0 error。
- 配置测试 32/32；Registry/UI/Lifecycle 34/34。
- 架构检查通过，`timers=0`，主类不超 2900 行。
- 真实部署配置能被 `ConfigFileValidator` 解析（本地 `RTD_CONFIG_PATH` 验证）。
- 静态确认 `CommandHelper(0, ...)` 与 AutoRoll 空池预检。

## 自检

- 用户：重载失败是否保留旧配置，`!givedice` 是否可用，自动抽骰是否不再空手。
- 设计师：是否只做前置校验/预检，没有扩大行为变化。
- 工程师：构建、测试、架构、CI、真实配置验证是否通过。
