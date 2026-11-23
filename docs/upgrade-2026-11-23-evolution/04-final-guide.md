# 统一全面进化指南（Cycle 8 / v1.4.11）

## 目标

把临时移动状态从“各 Dice 各写 MoveType”进化为“命名作用域栈 + 原始状态快照 + 可观测计数”，先覆盖 Skyline、Void，保留 Fate 到后续轮次。

## 本轮范围

1. 新增纯逻辑 `MoveScopeStack`。
2. `MoveLockManager` 支持任意 `MoveType_t` 目标，记录并恢复 `MoveType` + `ActualMoveType`。
3. Skyline/Void 迁移到 `Lock/Unlock`；死亡/移除显式释放。
4. Phoenix 的 `Unlock` 不再受 `LifeState == 0` 限制。
5. `!rtd_health` 增加 `move_locks=keys/players`。
6. 测试增加 11 项（45 总计）：栈逻辑 6、启动 0 锁 1、源码守卫 4。
7. 版本升到 v1.4.11，更新 CHANGELOG/README/自检。

## 明确不做

- 不迁移 Fate 的 dawn/darktide。
- 不改时长、冷却、概率、伤害、奖励、命令、配置 Key。
- 不新增配置项或主题文件。
- 不重写调用方 API；`Lock/Unlock/IsLocked/Clear/ClearAll` 保持兼容。
- 不做全量 Dice `Remove` 重构。

## 验收门槛

- 构建 0 warning / 0 error。
- 配置测试 32/32；Registry/UI/Lifecycle/Movement 45/45。
- 架构检查通过，`timers=0`，主类不超 2900。
- 源码守卫确认 Amber/Phoenix/Skyline/Void 不再直接写 `.MoveType =`。
- 纯逻辑测试覆盖嵌套、更新优先级、出栈、清空。
- 未实机验证前，不宣称“移动状态零残留”。

## 自检

- 用户：飞行/虚无结束是否恢复正确，死亡/移除是否残留。
- 设计师：作用域语义是否单一，是否只做安全迁移。
- 工程师：构建/测试/架构/CI、`ActualMoveType` 同步、旧 API 兼容。
