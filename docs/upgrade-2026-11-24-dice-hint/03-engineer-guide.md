# v1.4.19 工程师指南：reveal 聊天与 HUD 时长

## 改动点

### 1. `EffectsConfig.HudDuration`

`RollTheDice.Configs\EffectsConfig.cs`

```csharp
[JsonPropertyName("hud_duration")]
public int HudDuration { get; set; } = 8;
```

- 默认 8 秒
- 旧配置没有这个字段时，反序列化保留属性初始值 8，不需要迁移
- 解析失败或类型写错时由现有配置校验拒绝，不会半应用

### 2. `AnnounceDiceRarity`

`RollTheDice\RollTheDice.Announce.cs`

旧逻辑：

```text
聊天一行：名字 + 用 !rtd status 查看效果
HUD 5 秒
HUD 关闭时才在聊天补描述
```

新逻辑：

```text
聊天第一行：🎲 你抽到了 [档位] 名字
聊天第二行：└ 描述        （描述存在且不是 Unknown）
聊天第二行：旧的 hint     （描述缺失时）
HUD：hud_duration 秒（默认 8）
```

关键变量：

```csharp
bool hasDescription = !string.IsNullOrEmpty(description)
    && !description.StartsWith("Unknown", StringComparison.Ordinal);
```

`hasDescription == true` 时，HUD 里的描述行和聊天第二行共用同一份 `description`，不会出现两处文案不一致。

### 3. 时长钳制

```csharp
int hudDuration = Config?.Effects?.HudDuration ?? 8;
if (hudDuration <= 0) hudDuration = 8;
else if (hudDuration > 30) hudDuration = 30;
```

`Config` 为 null（热重载窗口）时回退 8；`0`/负数视为未配置；上限 30 防止误写 `9999` 导致 HUD 长期占屏。

## 为什么不用 `Math.Clamp`

`Math.Clamp(0, 1, 30)` 会得到 1 秒，比默认值更糟。设计上 `0` 表示"没填对"，应该回退默认，而不是取最小值。

## 未改动

- `DiceUi.DiceDescription` 的占位符清理逻辑
- `NotifyPlayers` 的 Add 抑制
- 传说/组合播报与音效
- 任何 Dice 的数值、概率、冷却、持续时间
- 配置路径与 CSS 序列化选项

## 测试

`tests\RollTheDice.RegistryTests\Program.cs` → `BroadcastChecks`

新增 2 个断言：

```text
reveal chat carries description   → 源码含 hasDescription / ChatColors.Grey
reveal hud duration configurable  → 源码含 HudDuration 且默认值为 8
```

当前断言数：标准 76，设置 `RTD_CONFIG_PATH` 指向真实配置时 77。

## 回归命令

```powershell
dotnet build "插件源文件\cs2-RollTheDice-main\RollTheDice.csproj" -c Release
dotnet run --project "插件源文件\cs2-RollTheDice-main\tests\RollTheDice.ConfigTests\RollTheDice.ConfigTests.csproj" -c Release
dotnet run --project "插件源文件\cs2-RollTheDice-main\tests\RollTheDice.RegistryTests\RollTheDice.RegistryTests.csproj" -c Release
powershell -NoProfile -ExecutionPolicy Bypass -File "scripts\check-rtd-architecture.ps1"
```

## 已知限制

- 聊天一行能容纳的字符受 CS2 限制；描述超过约 40 个汉字时，建议拆短或换行表达
- HUD 仍可能被其他 `PrintToCenterHtml` 覆盖，所以聊天才是可靠回顾入口
- 真人观感需要进服验证，BOT 不触发 RTD
