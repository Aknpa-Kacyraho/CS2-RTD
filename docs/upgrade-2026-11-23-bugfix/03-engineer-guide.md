# 工程师 / 发布升级指南（2026-11-23）

## 当前基线

- RTD：`net10.0`，CSS `1.0.380`，v1.4.2。
- 主类：2851 行（预算 2900）。
- 裸 `new Timer`：0（预算 2）。
- 配置测试：32/32。
- CI/tests 目前是工作区未提交文件。
- 部署 DLL 仍为旧构建，源码有未部署修复。

## 构建与验证

```powershell
dotnet build "插件源文件\cs2-RollTheDice-main\RollTheDice.csproj" -c Release --no-incremental

dotnet run --project "插件源文件\cs2-RollTheDice-main\tests\RollTheDice.ConfigTests\RollTheDice.ConfigTests.csproj" -c Release

powershell -NoProfile -ExecutionPolicy Bypass -File "D:\Works\CS2\scripts\check-rtd-architecture.ps1"
```

## 分阶段修复

### 阶段 A：P1

1. 为剩余生命值 Dice 增加 `OnHolderRemoved`，每批 2～3 个文件。
2. `StackingHealth` 增加 SteamID 重载，避免读取失效 controller。
3. Phoenix/Amber/Fate 的 MoveType 迁移，一次一个 Dice。
4. `PruneInvalid` 只做安全清理，不读取失效 controller 的 schema。

### 阶段 B：P2

1. 配置 Reload 类型校验；坏配置不覆盖旧配置。
2. 地图 fallback 日志与行为明确。
3. `ChildGuard.ClearAll` 只在确认实体归属后清理。
4. 为 `GetAllDiceForPlayer` 做 profiling，再决定索引方案。

### 阶段 C：发布

1. 更新 `VERSION`、`ModuleVersion`、`CHANGELOG`、README/指南。
2. 构建产物、测试、架构检查全通过。
3. `git diff` 审查，确认无无关改动、无全文换行符污染。
4. 提交、打 tag、推送；每轮版本独立，不混入下一轮未验证修改。

## 验收门槛

- 0 warning / 0 error。
- 32/32 配置断言。
- `timers=0`，主类 < 2900。
- 无裸 `new Timer`，无直接 `.Kill()` 的 Dice Timer。
- 断线/回合/换图手测通过，或明确标注未测。

## 禁止

- 不修改概率、冷却、伤害、持续时间、命令、配置 Key。
- 不批量重写 Dice。
- 未构建/未测试不得发布。
- 不把真实凭据写入仓库或日志。
