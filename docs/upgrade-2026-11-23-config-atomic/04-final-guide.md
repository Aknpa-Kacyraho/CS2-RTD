# 统一配置原子重载指南（Cycle 13 / v1.4.16）

## 目标

修掉 JSON 配置重载“setter 中途抛异常会留下半套配置”的 Bug：先验证、再快照、再应用、失败回滚；保持 CSS 持有的 Config 对象身份不变。

## 本轮范围

1. 新增 `ConfigApplier.Apply<T>`：快照 + 逐属性应用 + 失败回滚 + 异常解包。
2. `ReloadConfigFromDisk`：JSON 走 `ConfigFileValidator` + `ConfigApplier`；TOML 保持 CSS Reload。
3. 测试新增 4 项：正常复制、回滚、JSON 解析、JSON 应用。
4. 版本升到 v1.4.16，更新 CHANGELOG/README/自检。

## 明确不做

- 不替代 TOML Reload。
- 不改配置 Key、默认值、Dice 数值、命令、播报、TAB、Combo。
- 不做配置版本迁移。
- 不改配置路径/JSON 选项（上一轮已对齐 CSS）。

## 验收门槛

- 构建 0 warning / 0 error。
- 配置测试 32/32；Registry/UI/Lifecycle/Movement/Synergy/Broadcast/Design/Config 65/65。
- 真实部署配置 66/66。
- 架构检查通过，`timers=0`，主类不超 2900。
- 回滚测试：第二个 setter 抛异常后，第一个属性恢复旧值。
- TOML 仍走 CSS Reload。

## 自检

- 用户：失败重载是否保留旧配置，错误是否可读。
- 设计师：是否保持对象身份、只对 JSON 做原子应用、没有扩大范围。
- 工程师：构建/测试/架构/CI、回滚正确、异常解包。
