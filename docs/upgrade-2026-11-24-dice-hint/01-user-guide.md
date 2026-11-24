# v1.4.19 用户指南：抽到骰子就能看到效果

## 以前的问题

抽到 Dice 时，屏幕中央会闪一块面板，但只停 5 秒；聊天里只有一句：

```text
🎲 你抽到了 [稀有] 嗜血 用 !rtd status 查看效果。
```

正在跑位、买枪、被打的时候，5 秒根本不够看。想看效果就得记住并输入 `!rtd status`，新人通常不知道这条命令。

## 现在怎么显示

抽到 Dice 后，聊天会出现两行：

```text
🎲 你抽到了 [稀有] 嗜血
└ HP<40% 时攻击力提升 1.4~2.0 倍
```

- 第一行：档位 + 名字
- 第二行：这个 Dice 的具体效果
- 聊天记录是永久的，活着的时候随时上翻就能看，**不用输入任何命令**

屏幕中央的 HUD 也从 5 秒延长到 **8 秒**，内容不变（名字、档位、描述、特效类别）。

## 描述缺失时

如果某个 Dice 没有本地化描述，第二行会退回原来的提示：

```text
用 !rtd status 查看效果。
```

不会出现空行或 `Unknown` 字样。

## 想改 HUD 时长

配置文件：

```text
game\csgo\addons\counterstrikesharp\configs\plugins\RollTheDice\RollTheDice.json
```

找到 `effects`：

```json
"effects": {
  "enabled": true,
  "trails": true,
  "hud": true,
  "hud_duration": 8
}
```

- `hud_duration`：中央 HUD 停留秒数，默认 `8`
- 范围 1–30；写 `0` 或负数会回退到 8
- 设 `hud: false` 可以完全关掉中央 HUD，聊天里仍然有效果描述

改完用 `!rtdfx reload` 或重载配置生效。

## 没有变的东西

- 抽骰概率、权重、冷却、伤害、持续时间、奖励
- 命令和参数：`!rtd`、`!rtd status`、`!rtd list`、`!givedice`
- 传说/组合的全服播报、音效、TAB 标签
- 已有配置 Key 的含义
