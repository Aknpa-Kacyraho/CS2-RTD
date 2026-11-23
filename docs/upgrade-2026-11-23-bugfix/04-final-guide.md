# 统一升级优化指南（Cycle 1 / v1.4.3）

## 目标

把当前工作区已有的配置、Timer、生命周期修复固化为一个可发布版本，并完成“剩余生命值 Dice 的断线清理”这一组低风险 P1 修复。

## 本轮范围

### 必须完成

1. **生命值 Dice 清理（继续 2A-1b）**
   - 已覆盖：`Awakener`、`DragonSoul`、`God`。
   - 本轮补齐：`DeathKnightComplete`、`Dragonborn`、`Evolution`、`FireDragon`、`IceDragon`、`Knight`、`Mosquito`、`Paladin`、`PlayAsChicken`、`RoyalBarrier`、`WolfKing`。
   - `Phoenix` 因翅膀/光效实体清理复杂，本轮只记录，不强行改。
   - 每个 override 只清 controller-key 字典和按 SteamID 的 Buff，不恢复失效 Pawn 的血量/护甲。

2. **StackingHealth SteamID 清理**
   - 增加 `UnregisterMultiplier(ulong steamId, string key)`。
   - 无效 controller 不再读取 schema。
   - `DeagleKing`/`Giant` 改为安全调用。

3. **文档与版本**
   - 更新 `VERSION` → `1.4.3`。
   - 更新 `ModuleVersion` → `1.4.3`。
   - 更新 `CHANGELOG.md` 与 `README.md` 的版本说明。
   - 新增本目录指南与自检报告。

### 明确不做

- 不迁移 Phoenix/Amber/Fate/Skyline 的 MoveType（下一轮）。
- 不改 `PruneInvalid`（缺少安全 SteamID 路径）。
- 不做性能索引、主类拆分（下一轮）。
- 不修改概率、冷却、伤害、命令、配置字段。
- 不重写 Dice。

## 执行顺序

1. 生命值 Dice A：`Mosquito`、`RoyalBarrier`、`PlayAsChicken`。
2. 生命值 Dice B：`Knight`、`Paladin`、`Evolution`。
3. 生命值 Dice C：`FireDragon`、`IceDragon`、`DeathKnightComplete`。
4. 生命值 Dice D：`Dragonborn`、`WolfKing`。
5. `StackingHealth` SteamID 重载与调用点。
6. 文档/版本/Changelog。
7. 构建、测试、架构检查、diff 审查。
8. 自检（用户/设计师/工程师）→ 提交 → tag `v1.4.3` → 推送。

## 验收门槛

- `dotnet build RollTheDice.csproj -c Release --no-incremental`：0 warning / 0 error。
- 配置测试：32/32。
- 架构检查：`timers=0`，主类 < 2900。
- 新增 `OnHolderRemoved` 只影响断线兜底路径。
- 无无关文件、无全文换行符变化。
- 未实机验证的部分必须明确写进自检报告。

## 自检维度

- **用户**：断线/重连、回合结束、换图、命令是否正常。
- **设计师**：状态所有权是否更清晰；是否引入过度抽象。
- **工程师**：构建、测试、架构、diff、发布流程是否可复现。

## 下一轮候选

1. Phoenix/Amber/Fate/Skyline MoveType 统一。
2. `PruneInvalid` 安全清理路径。
3. `GetAllDiceForPlayer`/53 伤害 wrapper 性能。
4. 主类职责拆分。
5. `ChildGuard.ClearAll`、配置类型回滚、Combo 唯一名校验。
