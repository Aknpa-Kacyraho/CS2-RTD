# 统一收尾清理指南（Cycle 14 / v1.4.17）

## 目标

把前面遗留的代码级问题一次性收尾：TOML 原子重载、重复 Combo 名、重复播报、Fate MoveType、ulong 状态清理。

## 本轮范围

1. `ConfigFileValidator.TryDeserializeToml` + `ConfigApplier`：TOML 也原子应用。
2. Izayoi+Heaven 改名「月华天堂」，68 个 Combo 显示名唯一。
3. Add 期间抑制 `NotifyPlayers`，`AnnounceDiceRarity` 单一 reveal；HUD 关闭时聊天补描述。
4. `MoveLockManager.ReleaseTo`；Fate 黎明冻结/复活/清理迁移，直接 MoveType 归零。
5. 通用清理扩展 ulong primitive/string/enum/HashSet<ulong> 状态；实体 map 跳过。
6. 测试新增 TOML、显示名、Fate 守卫、ulong 清理、播报抑制（标准 73，真实 74）。
7. 版本升到 v1.4.17，更新 CHANGELOG/README/自检。

## 明确不做

- 性能索引：需要实机 profiling，盲目索引会引入失效风险。
- 主类拆分：2730/2900，预算内；本轮优先收尾。
- 实体 map 的嵌套引用清理：需要 Dice 级专用逻辑。
- 实机验证：需要服务器。
- 不改 Dice 数值、Combo 效果、配置 Key、命令。

## 验收门槛

- 构建 0 warning / 0 error。
- 配置测试 32/32；Registry/UI/Lifecycle/Movement/Synergy/Broadcast/Design/Config/Cleanup 73/73。
- 真实部署配置 74/74。
- 架构检查通过，`timers=0`，主类 2730/2900。
- Fate/Amber/Phoenix/Skyline/Void 均无直接 `.MoveType =`。
- 68 个 Combo 显示名唯一。
- ulong primitive 状态清理、实体 map 保留。

## 自检

- 用户：播报是否去重、Fate 是否正常、配置失败是否回滚。
- 设计师：是否只收尾，没有改数值；是否明确列出仍未做的性能/实机项。
- 工程师：构建/测试/架构/CI、TOML、回滚、ulong 安全边界。
