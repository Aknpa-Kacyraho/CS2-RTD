# v1.4.19 统一指南：抽到即见效果

## 一句话

抽到 Dice 后，聊天第二行直接写效果，中央 HUD 从 5 秒延长到 8 秒且可配置；不用再输入 `!rtd status` 才能知道自己中了什么。

## 玩家看到的变化

```text
🎲 你抽到了 [稀有] 嗜血
└ HP<40% 时攻击力提升 1.4~2.0 倍
```

- 聊天记录永久保留，活着时随时上翻
- HUD 8 秒，内容不变
- 描述缺失时退回 `用 !rtd status 查看效果。`

## 管理员看到的变化

`RollTheDice.json` 的 `effects` 多了一个字段：

```json
"effects": {
  "enabled": true,
  "trails": true,
  "hud": true,
  "hud_duration": 8
}
```

- 默认 8，范围 1–30
- `0` 或负数回退 8
- `hud: false` 仍可完全关闭中央面板，聊天描述不受影响

## 为什么这样改

- 中央 HUD 是瞬时的，会被击杀提示和其他插件覆盖
- 聊天是永久的，且不需要任何操作
- 玩家在抽骰瞬间通常正在跑位/交火，5 秒不够读
- Dice 死亡即移除，所以"死后慢慢看"不可行，必须在活着时给到

## 明确没改

- 171 个 Dice、68 个 Combo、所有概率/权重/冷却/伤害/持续时间/奖励
- 命令与参数
- 已有配置 Key
- 传说/组合全服播报、音效、TAB 标签

## 验证

- 构建：0 warning / 0 error
- 配置测试：32/32
- Registry/UI/Lifecycle/Broadcast 测试：标准 76，真实配置 77
- 架构：`timers=0`，主类 2659/2900
- 部署后 `rtd_health` 应仍为 `registry=ok`、171 dice、68 combos

## 回退

部署前目录备份在 `备份\deploy_20261124_*_v1419_live\`；回退时覆盖 `plugins\RollTheDice` 与配置即可。
