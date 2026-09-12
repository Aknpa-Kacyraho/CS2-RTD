# RollTheDice — Buff Stacking System + 平衡加强 + 新 dice「羁绊」设计

日期：2026-09-08
状态：已获用户批准（设计内容），进入实施

## 目标

1. 统一 buff 加成：同类 buff（伤害/移速/减伤）多来源应叠加而非覆盖，提供可配置的叠加规则。
2. 加强一批偏弱的 dice，并新增一个救场型 dice。

## A. Buff Stacking System

### 规则（已确认）

- 数据：`steamId -> source -> { amount, cap?, expireAt? }`
- **同 source 反复触发 = 叠层**（`AddStack`），受该 source 可选 `cap` 限制，`cap=null` 表示无上限。
- **跨 source = 求和**：各 source 先各自封顶，再累加为总加成。
- 可选 `duration`：到期自动失效；dice 仍可主动 `Unregister`。
- 保留 `Register`（覆盖/刷新语义）以兼容需要"刷新而非叠层"的调用（如 Berserker 每次受击按当前血量刷新）。
- 全局上限可选（默认无）。

### 统一应用点

- 伤害加成/减伤由**主插件单一的 `OnPlayerTakeDamagePre`** 集中应用：
  - `info.Damage *= (1 + totalDamageBonus(attacker))`
  - `info.Damage *= (1 - totalDamageReduction(victim))`
- 删除各 dice 内分散的伤害乘法与 `IsHighest` 防重逻辑（否则求和后会被重复放大）。
- 移速/减伤现有 OnTick 集中读取点，改为读"总合"。

### 涉及改造

- 新增核心 `StackingBonusManager`；`DamageBonusManager` / `SpeedBonusManager` / `DamageReductionManager` 改建为其薄封装。
- 审计并迁移 17 处伤害注册、15 处移速注册、5 处减伤注册：
  - 叠层（`AddStack`）：Glutton 每杀、Overheat 每层、RouletteGambler 每枪、Evolution、Awakener 等
  - 刷新（`Register`）：Bugle 等限时团队光环

### 备选（未采用）

各 dice 只应用自身 source 后连乘：改动小但不满足"求和/统一"，且保留 `IsHighest` 乱账。

## B. 平衡加强

| dice | 现状 | 改为 |
|------|------|------|
| RouletteGambler 轮盘赌徒 | 每枪 2% 死亡；+1%速/+1%伤上限 50% | **每枪 1% 死亡；去掉 50% 上限（无上限叠层）** |
| Awakener 觉醒者 | 起手 ×0.5 伤/×0.7 速，2 杀满级 ×2/×1.5 | **起手 ×1.0/×1.0；kills_to_max=3；满级 ×2.5 伤/×1.8 速** |
| HangedMan 倒吊者 | 自损 1/s，击杀逆转回 4/s | **回血 4 → 8/s**（自损仍 1/s） |
| Jester 小丑 | 移动自损 2/s，回血 3/s | **自损 2 → 1/s；回血 3 → 4/s** |
| JumpHeal 跳跳糖 | 跳跃回 5~8 | **10~15** |
| Regeneration 生命之泉 | `heal_per_tick=2` | **4**（每 2s）；纳入叠层系统；与 JumpHeal 组合仍翻倍 |

## C. 新 dice「羁绊 Kinship」

- 队友（同队**存活真人**）阵亡 → 自己获得 **1s 无敌**。
- 自己阵亡 → 所有同队存活队友获得 **1s 无敌**。
- 无敌实现：`TakesDamage=false` 短 Timer（沿用 GuardianAngel 手法，到期恢复）。
- 无冷却；每名队友死亡各触发一次；排除 bot/HLTV。
- 落地：`RollTheDice.Dices/Kinship.cs` + `RollTheDice.Configs/KinshipConfig.cs`（`invuln_seconds=1`，`enabled`）注册进 `DicesConfig`；`lang/en.json` 增 `dice_Kinship_name/_player/_other`；服务器 `RollTheDice.json` 增 `kinship.enabled=true`。

## D. 验证与交付

1. 编译 0 错（`dotnet build RollTheDice.csproj -c Release`）。
2. 回归：确认所有 manager 调用点迁移正确、无重复伤害应用。
3. lang 键完整（脚本核对）。
4. 部署到 `plugins\RollTheDice\`（已启用），`css_reload` 或重启。
5. push 到 GitHub `CS2-RTD` main，并更新 Release `v2026.09.08-fixed` 及 dll 附件。
