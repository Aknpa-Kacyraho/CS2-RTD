# CS2 服务器端粒子系统实体机制研究报告

- 日期：2026-09-20
- 环境：Counter-Strike 2（Source 2）+ CounterStrikeSharp（CSS）1.0.373，插件 C# / net10.0
- 目标：搞清楚服务器端如何生成并控制“超位魔法”级别的大型世界粒子（缩放 / 朝向 / 跟随 / 可见性 / 预缓存）
- 性质：**纯调研文档，未改动任何代码**。文中每条结论都尽量给出仓库/文件/行级来源；无法证实的一律标注 **未验证/存疑**。

> ⚠️ 阅读约定：
> - “CP”＝Control Point（控制点）。粒子编辑器（PET）里 CP 是 3 个 float 的向量，可当位置、也可当 3 个独立数值（作者自定义）。
> - “世界空间粒子”＝`m_bScreenSpaceEffect=false`，在 3D 世界里按位置渲染；“屏幕空间粒子”＝`true`，渲染在画面上（HUD），与 `particles/ui/*` 强相关。
> - 所有 `.vpcf` 是否能被“放大/转向/变色”，**取决于 vpcf 作者是否把对应属性接到了 CP 上**。这是本报告最重要的一句话。

---

## 0. 结论速览（TL;DR 可行性表）

| 需求 | 可行机制 | 可行性 | 关键前提 / 备注 |
|---|---|---|---|
| 通用放大任意 vpcf | `CEnvParticleGlow.m_flRadiusScale` | ❌ 基本无效 | 语义存疑，实测“法阵没变大”；非通用倍率 |
| 通用放大任意 vpcf | `CGameSceneNode.Scale` / `SetModelScale` | ❌ 无效 | 粒子不走模型缩放通道（见 §1.1） |
| 放大 | vpcf 尺寸接到某个 CP，服务器写该 CP | ✅ 唯一可靠 | 需作者预留 CP；如 CS2Draw 约定 CP5.X=半径 |
| 缩放/参数 | `AcceptInput("SetControlPoint","N: x y z")` | ✅ | 服务器仅能到 **CP0–CP6**（社区一致结论） |
| 缩放/参数 | `ServerControlPoints[4]` + `Assignments` + `Updated()` | ✅ | 4 个槽可指向 **更高的 CP 索引**（社区用到 16/17/32/33/34） |
| 朝向 | `Teleport(pos, angles, vel)` 设实体朝向 | ⚠️ 部分 | 只影响“以实体坐标系为准”的 vpcf；billboard 类无效 |
| 朝向 | 水平铺地（法阵） | ✅ 需作者 | 靠 vpcf 的 initializer/`m_ConstantNormal`/orientation type，非服务器能改 |
| 跟随移动实体 | `AcceptInput("SetParent", 父, 粒子, "!activator")` | ✅ | `info_particle_system` 与 `env_particle_glow` 都可 |
| 跟随移动实体 | `AcceptInput("FollowEntity", 父, 粒子, "!activator")` | ✅ | 社区对 `env_particle_glow` 常用 |
| 跟随移动实体 | 每 tick `Teleport` 重定位 | ✅ 最稳 | RTD 已实现 `Effects.MoveTo`；无 parent 副作用 |
| `particles/ui/*` 当世界粒子 | — | ❌ 不可靠 | 它们是 screen space / HUD 粒子（§4） |
| 预缓存 | `OnServerPrecacheResources` + `manifest.AddResource(vpcf)` | ✅ | listen server 主机新增粒子需换图/重启才可见 |

一句话方案：**不要指望现成的第三方 vpcf 能被服务器任意放大或转向；自己做一张 vpcf，把尺寸/数量/颜色/朝向接到 CP0–CP6，然后在服务器端写 CP。** 详见 §2.4 / §8。

---

## 1. 缩放：CS2 服务器端如何放大一个 vpcf

### 1.1 为什么 `Scale` / `SetModelScale` 对粒子没用

- `CParticleSystem : CBaseModelEntity`，而 `CBaseModelEntity : CBaseEntity`。
  来源：[`CParticleSystem.g.cs`](https://github.com/roflmuffin/CounterStrikeSharp/blob/main/managed/CounterStrikeSharp.API/Generated/Schema/Classes/CParticleSystem.g.cs)、[`CBaseModelEntity.g.cs`](https://github.com/roflmuffin/CounterStrikeSharp/blob/main/managed/CounterStrikeSharp.API/Generated/Schema/Classes/CBaseModelEntity.g.cs)
- `CBaseModelEntity` **没有** skeleton；`CGameSceneNode.GetSkeletonInstance()` 只对真正有 `CBodyComponent` + 骨骼模型的实体有意义。社区里唯一能用的缩放写法（Warcraft）也是先拿 `entity.CBodyComponent.SceneNode.GetSkeletonInstance()` 再改 `Scale`——粒子实体拿不到 skeleton，所以整条路走不通。
  来源：[`Wngui/CS2WarcraftMod/Helpers/Warcraft.cs`](https://github.com/Wngui/CS2WarcraftMod/blob/main/WarcraftPlugin/Helpers/Warcraft.cs)（`SetScale` 扩展）
- 更本质：**粒子的视觉尺寸由 vpcf 里每个 element 的 radius（`m_flConstantRadius` / Radius 算子，单位=世界单位）决定，而不是由实体变换的 scale 决定**。Valve 自带 toolhelp 明确写：`m_flConstantRadius`＝“Radius for each particle, in world units.”
  来源：[`GameTracking-CS2 toolhelp_particles_english.txt`](https://github.com/SteamTracking/GameTracking-CS2/blob/master/game/core/pak01_dir/resource/toolhelp_particles_english.txt)
- 结论：对 `info_particle_system` / `env_particle_glow` 设 `CGameSceneNode.Scale`、`SetModelScale`、`m_flScale` 一律无效——**这不是“没找对写法”，而是机制上不经过该通道**。

### 1.2 `CEnvParticleGlow.m_flRadiusScale` 的真实语义

- 字段确实存在：`CEnvParticleGlow` 继承 `CParticleSystem`，额外有 `AlphaScale` / `RadiusScale` / `SelfIllumScale` / `ColorTint` / `TextureOverride`。
  来源：[`CEnvParticleGlow.g.cs`](https://github.com/roflmuffin/CounterStrikeSharp/blob/main/managed/CounterStrikeSharp.API/Generated/Schema/Classes/CEnvParticleGlow.g.cs)
- `env_particle_glow` 在 Hammer 里确实有一个 `scale` 属性（`VMapRescale` 会按倍数改它）。
  来源：[`LionDoge/VMapRescale/VMapEdit.Cli/VmapEditor.cs`](https://github.com/LionDoge/VMapRescale/blob/master/VMapEdit.Cli/VmapEditor.cs)（`classname == "env_particle_glow"` 分支改 `scale`）
- **但**：找不到 Valve 一手文档说明它是“整体倍率”。社区用法彼此矛盾：有人填 `0.5 / 1.0 / 1.35`，有人填 `110 / 200`（显然不是“1=原尺寸”的倍率）。
  来源：[`DeadSwimek/cs2-specialgrenades/Functions.cs`](https://github.com/DeadSwimek/cs2-specialgrenades/blob/main/Functions.cs)（`RadiusScale=0.5f`）、[`H-AN/HZPTurretS2`](https://github.com/H-AN/HZPTurretS2/blob/main/src/HZPTurretEffectService.cs)（`RadiusScale=110f`）、[`zakriamansoor47/SLAYER_Conquest`](https://github.com/zakriamansoor47/SLAYER_Conquest/blob/main/source/Utils.cs)（默认 200）
- 判断（**推断，未在 CS2 一手文档验证**）：`m_flRadiusScale` 更像 `env_particle_glow` 这个“旧式 glow sprite”的发光半径参数，**只有 vpcf 作者显式去读它时才可能有效**；对任意第三方 vpcf 不会当成“整体尺寸倍率”。这与你实测“法阵没变大”一致。
- 结论：**不要依赖 `RadiusScale` 做通用放大**。

### 1.3 `CParticleSystem` 的控制点字段全解析

以 CSS 生成 schema 为准（[`CParticleSystem.g.cs`](https://github.com/roflmuffin/CounterStrikeSharp/blob/main/managed/CounterStrikeSharp.API/Generated/Schema/Classes/CParticleSystem.g.cs)）：

| 字段 | 类型 | 含义 / 服务器用途 |
|---|---|---|
| `m_vServerControlPoints` | `Vector[4]` | 4 个“服务器控制点槽”。写进去的位置/数值会被复制到粒子系统 |
| `m_iServerControlPointAssignments` | `byte[4]` | 每个槽映射到哪个 CP 索引；**255 = 未分配** |
| `m_hControlPointEnts` | `CHandle<CBaseEntity>[64]` | 让某个 CP 绑定到一个实体（CP 跟随/引用该实体） |
| `m_nDataCP` / `m_vecDataCPValue` | `int` / `Vector` | 一个“可选的数据通道”：把某个 CP 设为一个值。**不可靠**，见下 |
| `m_nTintCP` / `m_clrTint` | `int` / `Color` | 把 `TintCP` 指定的 CP 设成 `m_clrTint`（用于染色），前提是 vpcf 读该 CP 做 color |
| `m_iEffectIndex` / `m_iszEffectName` | — | 资源句柄 / vpcf 路径 |
| `m_bActive` / `m_bFrozen` / `m_flStartTime` / `m_flPreSimTime` | — | 播放控制（`Start` / `Stop` / `Active`） |

关键事实（社区一手结论）：

1. **`AcceptInput("SetControlPoint", "N: x y z")` 只能到 CP0–CP6。**
   来源：[`ShookEagle/CS2Draw/PARTICLES.md`](https://github.com/ShookEagle/CS2Draw/blob/main/PARTICLES.md)：
   > “CS2 only exposes **CP0 through CP6** to server-side code. That's 7 control points total that `AcceptInput("SetControlPoint")` can reach. … Should CounterStrikeSharp add EKV this number would jump to 63”
   以及 [`Letaryat/CS2-CustomTrailAndTracers/src/Utils/Utils.cs`](https://github.com/Letaryat/CS2-CustomTrailAndTracers/blob/main/src/Utils/Utils.cs)（注释：CP4=半径、CP5=Start、CP6=End）、[`DeadSwimek`](https://github.com/DeadSwimek/cs2-specialgrenades/blob/main/Functions.cs)（`SetControlPoint 0/1`）。

2. **`ServerControlPoints[4]` + `ServerControlPointAssignments[4]` 可以指向更高的 CP 索引。**
   来源：`CenterSpeed` 明确用 CP16/17/32/33/34：
   [`Excalibro1/CenterHud/CenterSpeed/src/CenterSpeed.cs`](https://github.com/Excalibro1/CenterHud/blob/main/CenterSpeed/src/CenterSpeed.cs)
   ```csharp
   // Slot 0 = CP16: (R,G,B)  颜色
   // Slot 1 = CP32: (frame,0,0) 帧
   // Slot 2 = CP34: (scale,0,0) 尺寸
   // Slot 3 = CP33: (x,y,0)     屏幕位置
   bool r34 = SetControlPointValue(particle, 34, new Vector(settings.HudScale, 0f, 0f));
   ```
   写完后必须调用 `ServerControlPointsUpdated()` 和 `ServerControlPointAssignmentsUpdated()` 才会同步到客户端。
   `nicedayzhu/SwiftParticleMenuDemo` 用同样的 CP16/17/33/34；`nicedayzhu/SwiftMvpEffect` 用 CP34.x=scale、CP34.z=offset。

3. **`m_nDataCP` / `m_vecDataCPValue` 不可靠。** `CenterSpeed` 直接写注释：
   > “DataCP/DataCPValue does NOT reliably propagate to the client particle system. CP33 (X/Y position offset) MUST be a ServerControlPoint.”
   来源同上。`LCrew/S2-CSRoll` 也踩过：data_cp 是“定义必须显式 opt-in 读取的通用 data 通道，taser 的 wire1a 根本不读它”。
   来源：[`LCrew/S2-CSRoll/src/Modifiers/GameModifierMasterZeus.cs`](https://github.com/LCrew/S2-CSRoll/blob/main/src/Modifiers/GameModifierMasterZeus.cs)

4. **CP 可以绑定到实体**（`m_hControlPointEnts`）：`CenterSpeed` 把 CP17 绑到一个共享 `info_target`。这让 CP 能引用动态实体。

5. **“服务器改 CP ⇒ 粒子变化”的前提永远是 vpcf 作者把该属性接到了这个 CP。**
   `CS2Draw` 的作者在 PET 里把半径接到 `CP5.X`，颜色接到 `CP1`（并用 0.0039=1/255 缩放），代码端才能通过 CP 控制。来源：[`PARTICLES.md`](https://github.com/ShookEagle/CS2Draw/blob/main/PARTICLES.md)。

### 1.4 服务器能否用控制点控制“位置/朝向/缩放”

- **位置**：能，且这是最常用法。
  - `Teleport()` 设实体原点 → 即 CP0（CS2Draw 明确：“CP0 is always the position of the `CParticleSystem` entity in the world”）。
  - CP5/CP6 常被作者拿来做“起点/终点”（激光、连线）：`CS2Draw.DrawService.spawnBeam`、`Letaryat.Utils.CreateParticleBullet`。
- **朝向/缩放**：只有在 vpcf 作者把 orientation/radius 接到某个 CP 时，服务器写该 CP 才有效。没有“通用朝向/缩放的 CP”。
- 所以：**是否要求作者预先在 PET 里接 CP？——是的，绝对要求。** 这是整个机制的核心限制。

### 1.5 推荐做法（缩放）

1. 用 Workshop Tools 粒子编辑器自制 vpcf（CS2 默认禁用 PET，需在 `sdkenginetools.txt` 里取消 `csgo` 排除；来源 [Source2Wiki ParticleEditor](https://source2wiki.github.io/EngineTools/ParticleEditor)）。
2. 按 `CS2Draw` 的 CP 约定接线（见 §8.1）把关键参数暴露到 CP0–CP6：
   - `CP1` = tint（0–255，renderer color blend 处 scale=0.0039）
   - `CP2.X` = 粒子数；`CP5.X` = 半径；`CP5.Y` = 高度
3. 服务器端：
   - 低 CP（0–6）用 `AcceptInput("SetControlPoint", "5: {r} 0 0")`；
   - 高 CP 或需要 4 个以上参数，用 `ServerControlPoints[i]` + `ServerControlPointAssignments[i]` + 两次 `Updated()`。
4. 别在粒子实体上试 `SetModelScale` / `SceneNode.Scale`。

---

## 2. 朝向：如何让粒子水平铺在地面

### 2.1 世界空间 vs 屏幕空间 vs billboard

- `m_bScreenSpaceEffect`（Base Properties）＝“Tells the effect to render in screen space (on the picture plane) rather than in world space.”（Valve toolhelp 原文）
  来源：[toolhelp_particles_english.txt](https://github.com/SteamTracking/GameTracking-CS2/blob/master/game/core/pak01_dir/resource/toolhelp_particles_english.txt)、[`Dingf/Source-2-Decompiler/include/vpcf.h`](https://github.com/Dingf/Source-2-Decompiler/blob/main/include/vpcf.h)（`1 byte: Screen space effect`）
- `m_bViewModelEffect`＝第一人称武器/手部特效（同样 source）。
- Sprite renderer 默认是面向摄像机的 billboard；要让粒子“平铺在地面/固定朝向世界”，靠的是 renderer 的 **orientation type** + `m_ConstantNormal`。toolhelp 原文：
  > `m_ConstantNormal` … “This field is ignored unless the orientation_type property in the effect's renderer is set to **‘Particle Normal Align’** or **‘Screen & Particle Normal Align’**.”

### 2.2 服务器能做什么

- `Teleport(pos, angles, vel)` 会设定粒子实体的世界角度。**只有当 vpcf 内部以“实体坐标系”为基准时，angles 才会改变粒子的世界朝向**；billboard / world-locked / 仅按 CP0 渲染的粒子不会因 angles 改变而转向。
  - `CS2Draw` 的 beacon/trail 都传 `QAngle.Zero`；说明多数情况角度无意义。
  - `CSRoll` 遇到“闪电总是向右弯”，推测是 `C_INIT_CreateSequentialPath` 的 bulge 相对实体局部轴计算，于是把实体朝向目标（`Teleport(origin, angleToTarget)`）来缓解——**作者本人也标注“not fully certain”**。来源：[GameModifierMasterZeus.cs](https://github.com/LCrew/S2-CSRoll/blob/main/src/Modifiers/GameModifierMasterZeus.cs)。
- 所以：**“法阵显示成竖直”不是服务器传错 angles，而是 vpcf 本身的世界朝向/法线设置问题**（多数法阵 vpcf 默认是面向摄像机的竖直圆盘，或 normal=(0,0,1) 但 renderer orientation 没设对）。服务器端没有通用开关能把它“压倒”到地面。

### 2.3 怎么才能水平铺地

- 在 PET 里：
  - 用 `Position Along Ring`（toolhelp 名 `C_INIT_RingWave`）或 `Position Within Sphere Random` 且 `distance_bias` 压扁（如 `1 1 0` 只留 XZ 或 XY 平面）。toolhelp：`m_vecDistanceBias` “1 1 0 will create particles only in the X Y plane”。
  - `m_ConstantNormal = (0,0,1)`，并把 sprite/rope renderer 的 orientation type 设为 `Particle Normal Align`（或 `Screen & Particle Normal Align`）。
  - 需要的话用 `C_OP_RemapCPOrientationToRotations`（toolhelp 提到新的 “Use Quaternions Internally / Write Normal instead of Rotation”）。
- 服务器端：只能选一个“已经平铺”的 vpcf，或自建。`Teleport` 的 angles 只能作为辅助。
- **未验证**：第三方现成法阵 vpcf 若作者没做上述设置，服务器端无解。

---

## 3. 跟随：让粒子跟随移动实体

### 3.1 两条可行路径（社区均有可用实例）

**(A) `FollowEntity`（Source 1 遗留的 MOVETYPE_FOLLOW）** —— `env_particle_glow` 上最常见：
```csharp
// exkludera-cssharp/equipments src/Particles.cs（env_particle_glow 跟随玩家）
particle.Teleport(absOrigin);
particle.DispatchSpawn();
particle.AcceptInput("FollowEntity", player.PlayerPawn.Value, particle, "!activator");
```
来源：[`exkludera-cssharp/equipments`](https://github.com/exkludera-cssharp/equipments/blob/main/src/Particles.cs)、[`exkludera-cssharp/trails`](https://github.com/exkludera-cssharp/trails/blob/main/src/trail.cs)、[`exkludera-cssharp/blockmaker`](https://github.com/exkludera-cssharp/blockmaker/blob/main/src/Blocks/Blocks.cs)、[`zakriamansoor47/SLAYER_Conquest`](https://github.com/zakriamansoor47/SLAYER_Conquest/blob/main/source/Utils.cs)。
实体定义（含 `FollowEntity(CBaseEntity*, bool)`）：[alliedmodders/hl2sdk baseentity.h](https://github.com/alliedmodders/hl2sdk/blob/master/game/server/baseentity.h)。

**(B) `SetParent`** —— `info_particle_system` 与 `env_particle_glow` 都能用：
```csharp
// ShookEagle/CS2Draw DrawService.cs（info_particle_system 挂到 anchor/pawn）
particle.StartActive = true;
particle.DispatchSpawn();
particle.AcceptInput("Start");
particle.AcceptInput("SetParent", builder.Anchor, particle, "!activator");
```
来源：[`CS2Draw/DrawService.cs`](https://github.com/ShookEagle/CS2Draw/blob/main/CS2Draw/DrawService.cs)（beacon / trail / customTrail）。
也见 [`Letaryat/CS2-CustomTrailAndTracers`](https://github.com/Letaryat/CS2-CustomTrailAndTracers/blob/main/src/Utils/Utils.cs)、[`darkerz7/EntWatchSharp Item.cs`](https://github.com/darkerz7/EntWatchSharp/blob/main/EntWatchSharp/Items/Item.cs)（对武器 `SetParent`）。
引擎定义：`SetParent(string_t newParent, CBaseEntity *pActivator, int iAttachment)`——通过 activator 解析父实体，所以传 `"!activator"` + activator=父实体即可。来源：[hl2sdk baseentity.cpp](https://github.com/alliedmodders/hl2sdk/blob/master/game/server/baseentity.cpp)。

### 3.2 你现有写法为什么可能不跟随（分析）

你用的是 `AcceptInput("SetParent", pawn, pawn, "!activator")`（RTD 里是 `SetParent, parent, parent, "!activator"`）。activator=父实体，方向没问题。可能原因：

1. **缺一次 `Teleport`。** `Warcraft.SetParent` 扩展里明确注释：
   > “If not teleported, the childrenEntity will not follow the parentEntity correctly.”
   来源：[`Wngui/CS2WarcraftMod/Helpers/Warcraft.cs`](https://github.com/Wngui/CS2WarcraftMod/blob/main/WarcraftPlugin/Helpers/Warcraft.cs)。
   RTD 的 `Init` 顺序是 `Teleport → DispatchSpawn → SetParent`，SetParent 之后**没有**再 Teleport。可试在 `SetParent` 后再 `Teleport` 一次。
2. `info_particle_system` 的“跟随”在引擎里本质是移动父级；对无模型实体，某些情况下不如 `FollowEntity` 稳。
3. **最稳妥：每 tick `Teleport` 重定位**（无 parent 的副作用，`env_particle_glow`/`info_particle_system` 都适用）。RTD 已经有现成函数：
   [`RollTheDice.Utils/Effects.cs`](https://github.com/Aknpa-Kacyraho/CS2-RTD/blob/main/RollTheDice.Utils/Effects.cs)
   ```csharp
   /// <summary>把已存在的粒子实体移动到新位置（用于"持续附着/环绕"这类不挂 Parent、每 tick 跟随的特效）。</summary>
   public static void MoveTo(CParticleSystem? system, Vector? position)
   { ((CBaseEntity)system).Teleport(position, new QAngle(0f,0f,0f), new Vector(0f,0f,0f)); }
   ```
   `CS2Draw` 的 beacon 干脆每 2s **重新 spawn** 一个（`spawnBeaconTick`）而不是移动同一个。

### 3.3 `info_particle_system` vs `env_particle_glow` 挂 Parent

- 两者都能：`info_particle_system` 用 `SetParent`（CS2Draw/Letaryat），`env_particle_glow` 用 `FollowEntity`（exkludera）或 `SetParent`（EntWatch 对 prop 用 `FollowEntity`）。
- 经验选择：
  - 需要 CP 控制（缩放/染色/连线）→ 用 `info_particle_system`（社区控 CP 的例子几乎全是它）。
  - 只是“贴着一个实体飘个光/尾迹”且不需要 CP → `env_particle_glow` + `FollowEntity` 更省心。
- **未验证**：无 parent 时两者的 tick 跟随差异；建议按 §3.2 的三种方式各做一次小实验。

---

## 4. 渲染可见性 / 粒子家族

- `particles/ui/*`（如 `ui_gold_halo_rays_rot`、`ui_status_level_wings`）属于 **HUD / 屏幕空间** 粒子（Base Properties 的 `m_bScreenSpaceEffect`）。当世界粒子放会渲染异常/不显示，符合其设计（它们本就是给 Panorama UI 画面用的）。来源：[toolhelp_particles_english.txt](https://github.com/SteamTracking/GameTracking-CS2/blob/master/game/core/pak01_dir/resource/toolhelp_particles_english.txt)、[`vpcf.h`](https://github.com/Dingf/Source-2-Decompiler/blob/main/include/vpcf.h)。
  - 社区里有人确实拿 `particles/ui/annotation/ui_annotation_aim.vpcf`、`particles/ui/ui_mainmenu_nav_hover.vpcf` 做世界/HUD 粒子（`nicedayzhu/SwiftParticleMenuDemo`），但那是**刻意当 HUD / overlay** 用，并且配合 `SetTransmitState` 只给特定玩家看。**未验证**：直接当世界粒子能否稳定。
- 较可靠的世界空间粒子家族（社区高频、Valve 地图大量使用）：
  - `particles/explosions_fx/*`（`explosion_c4_short.vpcf`、`explosion_basic.vpcf`）
    来源：[ParticleDemo](https://github.com/qazlll456/ParticleDemo/blob/main/ParticleDemo.cs)、[`Wngui/CS2WarcraftMod`](https://github.com/Wngui/CS2WarcraftMod/blob/main/WarcraftPlugin/Helpers/Warcraft.cs)、[edgegamers/Jailbreak C4Behavior](https://github.com/edgegamers/Jailbreak/blob/main/mod/Jailbreak.Rebel/C4Bomb/C4Behavior.cs)
  - `particles/overhead_icon_fx/*`（`player_ping_ground_rings.vpcf` 做成地上的圈、`player_ping.vpcf`）
    来源：[`darkerz7/EntWatchSharp`](https://github.com/darkerz7/EntWatchSharp/blob/main/EntWatchSharp/Items/Item.cs)、[`SpectralHive/CS2-ZE-EntWatchSharp`](https://github.com/SpectralHive/CS2-ZE-EntWatchSharp/blob/main/EntWatchSharp/Items/Item.cs)、SwiftParticleMenuDemo
  - `particles/burning_fx/*`（`env_fire_medium.vpcf`）
    来源：[blockmaker Action.cs](https://github.com/exkludera-cssharp/blockmaker/blob/main/src/Blocks/Action.cs)
  - `particles/generic_fx/fx_sparks.vpcf`、`particles/numbers/number_x.vpcf`、`particles/environment/glow01.vpcf`（后者是地图里 env_particle_glow 的常客）
    来源：SwiftParticleMenuDemo、CenterSpeed、[fyscs/MapTracking-CS2 entity lump](https://github.com/fyscs/MapTracking-CS2)
- 重要观察（**存疑，需实测**）：`LCrew/S2-CSRoll` 报告**复合粒子**（vpcf 带 `m_Children`/`m_hFallback`，如原生 taser tracer）经**插件** spawn **完全不渲染**，只认简单的单层粒子。
  来源：[GameModifierMasterZeus.cs](https://github.com/LCrew/S2-CSRoll/blob/main/src/Modifiers/GameModifierMasterZeus.cs)（长注释：“composite/wrapper particle definitions apparently can't be dispatched by name through either public API”）。
  但 7ychu5 的 `founding_rainofstars/main.vpcf` **恰恰是复合体**（带 4 个 `m_Children`），却被地图 entitylump 用 `info_particle_system` + `start_active=1` 放置。→ **“地图引擎放置”能渲染 vs “插件 spawn”不渲染** 存在矛盾，务必实测；这是“超位魔法”能否复用的关键分水岭。

---

## 5. 预缓存与 listen server

- 正确用法：在 `OnServerPrecacheResources` 回调里 `manifest.AddResource(vpcf)`。RTD 已经这么做了：
  [`RollTheDice.Utils/Effects.cs`](https://github.com/Aknpa-Kacyraho/CS2-RTD/blob/main/RollTheDice.Utils/Effects.cs) `Precache` / `PrecacheAll`。
  其它例子：[ParticleDemo](https://github.com/qazlll456/ParticleDemo/blob/main/ParticleDemo.cs)（`RegisterListener<Listeners.OnServerPrecacheResources>(manifest => manifest.AddResource(Config.ParticleFile));`）、[CenterSpeed](https://github.com/Excalibro1/CenterHud/blob/main/CenterSpeed/src/CenterSpeed.cs)（还会自动扫插件 `assets/` 下所有 `particles/*`）。
- 触及时机：**在载图/资源清单阶段**（`OnServerPrecacheResources`）触发。因此：
  - 新增粒子后，`css_reload` **不够**；listen server 上**主机（房主）客户端**要看到新粒子，**必须换图或重启服务器**（本仓库 `AGENTS.md` 也记录了同一条）。远程客户端可能因下载/挂载时机不同而表现不同。
  - 路径要写成**不带 `.vpcf_c`** 的形式（RTD `Normalize` 会去掉 `_c` 后缀）——预编译产物是 `*.vpcf_c`，`AddResource` 用逻辑路径。
- Workshop/第三方 addon：要么随地图挂载，要么用 MultiAddonManager 挂 Workshop ID（`mm_extra_addons "ID"`）。来源：[CS2Draw PARTICLES.md](https://github.com/ShookEagle/CS2Draw/blob/main/PARTICLES.md)。
- **未验证**：`ResourceManifest.AddResource` 对 `.vpcf` 之外资源（vtex/vmat）是否都需显式加；社区通常连 vpcf 一起加。

---

## 6. 社区样例（仓库 + 文件 + 关键代码）

### 6.1 ShookEagle/CS2Draw —— 最完整的“服务器控 CP 绘制世界几何”范式 ⭐
- 文件：[`PARTICLES.md`](https://github.com/ShookEagle/CS2Draw/blob/main/PARTICLES.md)、[`CS2Draw/DrawService.cs`](https://github.com/ShookEagle/CS2Draw/blob/main/CS2Draw/DrawService.cs)、[`CS2Draw/ParticleConfigurator.cs`](https://github.com/ShookEagle/CS2Draw/blob/main/CS2Draw/ParticleConfigurator.cs)
- 方式：自制 vpcf + `AcceptInput("SetControlPoint")` 控 CP0–CP6；`Teleport` 定位；`SetParent` 跟随；tint 用 `TintCP`+`Tint`；几何用 rope renderer + CP 运算。
- 缩放/朝向：**全部靠 vpcf 预接 CP**（`CP5.X` 半径、`CP1` 颜色、`m_ConstantNormal=(0,0,1)` + normal align）；服务器不直接改 scale/角度。
- 跟随关键代码：
  ```csharp
  particle.AcceptInput("Start");
  particle.AcceptInput("SetParent", builder.Anchor, particle, "!activator");
  ```

### 6.2 Excalibro1/CenterHud（CenterSpeed）—— `ServerControlPoints` 写高 CP 的权威例子 ⭐
- 文件：[`CenterSpeed/src/CenterSpeed.cs`](https://github.com/Excalibro1/CenterHud/blob/main/CenterSpeed/src/CenterSpeed.cs)
- 方式：`info_particle_system` + `ServerControlPoints[4]`/`ServerControlPointAssignments[4]` + `Updated()`，用 CP16/17/32/33/34 做 HUD；`SetTransmitState` 只给本人看；`ControlPointEnts[17]` 绑 `info_target`。
- 明确否定了 `DataCP/DataCPValue` 的可靠性。

### 6.3 LCrew/S2-CSRoll —— 复合粒子、data_cp、CP 数组踩坑的实战记录 ⭐
- 文件：[`src/Modifiers/GameModifierMasterZeus.cs`](https://github.com/LCrew/S2-CSRoll/blob/main/src/Modifiers/GameModifierMasterZeus.cs)
- 结论：复合粒子经插件不渲染；`data_cp` 需定义 opt-in；改用 `ServerControlPoints` 配 `ServerControlPointAssignments`（255=未分配）驱动 CP0/CP1；`Teleport(origin, direction.ToQAngles())` 缓解局部轴 bulge。

### 6.4 exkludera-cssharp —— `env_particle_glow` + `FollowEntity` 跟随真人
- 文件：[`equipments/src/Particles.cs`](https://github.com/exkludera-cssharp/equipments/blob/main/src/Particles.cs)、[`trails/src/trail.cs`](https://github.com/exkludera-cssharp/trails/blob/main/src/trail.cs)
- 方式：`env_particle_glow`，`Teleport → DispatchSpawn → AcceptInput("FollowEntity", pawn, particle, "!activator")`。**没有**用 CP 缩放。

### 6.5 7ychu5/ze_onliners_2025 —— `founding_rainofstars`（艾尔登法环“陨石雨”）
- 粒子：`particles/7ychu5/elden_ring/founding_rainofstars/main.vpcf`（719B，**复合体**：`m_Children = p1/p2(延迟3s)/p3(4s)/p4(6s)`）。
  [main.vpcf 原文](https://github.com/7ychu5/ze_onliners_2025/blob/main/particles/7ychu5/elden_ring/founding_rainofstars/main.vpcf)
- 用法：在**地图 entitylump** 里以 `info_particle_system` 放置（来自 `fyscs/MapTracking-CS2` 的地图转储）：
  ```json
  { "classname": "info_particle_system",
    "effect_name": "particles/7ychu5/elden_ring/founding_rainofstars/main.vpcf",
    "start_active": "1", "clientSideEntity": "0", "useLocalOffset": "0" }
  ```
  来源：[`fyscs/MapTracking-CS2` entity lump](https://github.com/fyscs/MapTracking-CS2/blob/main/2001/ze_maontain_escape/3464392570/maps/ze_maontain_escape/17253%23entitylumpname.lump)
- **在仓库 vscripts 中未搜到调用该粒子的脚本**（对 `rainofstars` 的 repo 内代码搜索返回 0）。该仓库 vscripts 是 TypeScript 转译的 `.nut`（`scripts/vscripts/7ychu5/*`），主要是玩家/属性/任务系统。
- 结论/存疑：这是**地图侧放置**的用法，不能直接证明“插件能 spawn 这个复合粒子”。要不要复用，先做 §4 的复合粒子实测。

### 6.6 其它可参考
- [`nicedayzhu/SwiftMvpEffect`](https://github.com/nicedayzhu/SwiftMvpEffect/blob/main/src/SwiftMvpEffectPlugin.cs)：`info_particle_system`，CP34.x=scale 的清晰例子；`AcquireInput("StopPlayEndCap"/"DestroyImmediately")` 生命周期。
- [`nicedayzhu/SwiftParticleMenuDemo`](https://github.com/nicedayzhu/SwiftParticleMenuDemo/blob/main/src/SwiftParticleMenuDemoPlugin.cs)：HUD overlay 粒子、`SetTransmitState` 逐玩家显隐、`CP33/CP34`。
- [`DeadSwimek/cs2-specialgrenades`](https://github.com/DeadSwimek/cs2-specialgrenades/blob/main/Functions.cs)：`env_particle_glow` + `SetControlPoint 0/1` 连线。
- [`Letaryat/CS2-CustomTrailAndTracers`](https://github.com/Letaryat/CS2-CustomTrailAndTracers/blob/main/src/Utils/Utils.cs)：注释明确 CP4=半径/CP5=起点/CP6=终点；`SetParent` 跟随。
- [`Wngui/CS2WarcraftMod`](https://github.com/Wngui/CS2WarcraftMod/blob/main/WarcraftPlugin/Helpers/Warcraft.cs)：`SetParent` 后必须 `Teleport` 的注释。

---

## 7. 一手来源清单

- CounterStrikeSharp 生成 Schema：
  - [CParticleSystem.g.cs](https://github.com/roflmuffin/CounterStrikeSharp/blob/main/managed/CounterStrikeSharp.API/Generated/Schema/Classes/CParticleSystem.g.cs)
  - [CEnvParticleGlow.g.cs](https://github.com/roflmuffin/CounterStrikeSharp/blob/main/managed/CounterStrikeSharp.API/Generated/Schema/Classes/CEnvParticleGlow.g.cs)
  - [CBaseModelEntity.g.cs](https://github.com/roflmuffin/CounterStrikeSharp/blob/main/managed/CounterStrikeSharp.API/Generated/Schema/Classes/CBaseModelEntity.g.cs)
- Valve 随游戏发布的粒子属性说明：
  - [GameTracking-CS2 toolhelp_particles_english.txt](https://github.com/SteamTracking/GameTracking-CS2/blob/master/game/core/pak01_dir/resource/toolhelp_particles_english.txt)
  - [Source-2-Decompiler vpcf.h](https://github.com/Dingf/Source-2-Decompiler/blob/main/include/vpcf.h)
- Source2 Wiki：
  - [Particle Editor](https://source2wiki.github.io/EngineTools/ParticleEditor)
  - [Particle Editor Guide](https://source2wiki.github.io/EngineTools/ParticleEditor/particle-editor-guide)
- CS2Draw 粒子制作指南（社区一手，最实用）：[PARTICLES.md](https://github.com/ShookEagle/CS2Draw/blob/main/PARTICLES.md)
- 引擎输入语义：[hl2sdk baseentity.h](https://github.com/alliedmodders/hl2sdk/blob/master/game/server/baseentity.h) / [baseentity.cpp](https://github.com/alliedmodders/hl2sdk/blob/master/game/server/baseentity.cpp)（`SetParent` / `FollowEntity` / `AcceptInput`）

> 注：`developer.valvesoftware.com` 目前由 Anubis 反爬保护，本次无法直接抓取；相关结论以上面的 Valve 官方 toolhelp / Source2Wiki / 引擎源码替代。

---

## 8. 给本项目的实现建议

### 8.1 vpcf 侧（决定性）
自制“超位魔法”粒子，按 `CS2Draw` 约定把可控项接到 CP：
| CP | 分量 | 用途 |
|---|---|---|
| CP0 | XYZ | 实体原点（`Teleport` 自动设，**不要**在 vpcf 里覆写） |
| CP1 | XYZ | tint（0–255；renderer color blend 处 scale=0.0039） |
| CP2 | X | 粒子数 / 发射密度 |
| CP3 | X/Y/Z | 半径 / alpha / 自定义 |
| CP5 | X / Y | 主尺寸（半径）/ 次尺寸（高度） |
| CP6 | XYZ | 终点 / 自定义 |
- renderer orientation 需设成 `Particle Normal Align`（或 `Screen & Particle Normal Align`），`m_ConstantNormal=(0,0,1)`，法阵才会水平铺地。
- 避免不必要的 `m_Children`/`m_hFallback`（复合体经插件 spawn 存疑），或先实测确认。

### 8.2 C# 侧（可直接写）
```csharp
// 1) 建实体（低 CP 用 AcceptInput，高 CP/多参数用 ServerControlPoints）
var p = Utilities.CreateEntityByName<CParticleSystem>("info_particle_system");
p.EffectName = "particles/<你的addon>/<魔法阵>.vpcf";
p.Teleport(pos, new QAngle(0, 0, 0), new Vector(0, 0, 0)); // 设 CP0
p.StartActive = true;

// 2) 控 CP0–CP6（半径、数量等）
p.AcceptInput("SetControlPoint", value: "5: 800 0 0"); // CP5.X = 半径=800
p.AcceptInput("SetControlPoint", value: "2: 64 0 0");  // CP2.X = 粒子数

// 3) 染色（vpcf 读 CP1 做 color 时）
p.TintCP = 1;
p.Tint = Color.FromArgb(255, 120, 180, 255);

p.DispatchSpawn();
p.AcceptInput("Start");

// 4) 跟随：三选一（优先每 tick Teleport）
p.AcceptInput("FollowEntity", pawn, p, "!activator");      // A
// p.AcceptInput("SetParent", pawn, p, "!activator");      // B（必要时之后补一次 Teleport）
// Effects.MoveTo(p, pawn.AbsOrigin);                      // C：每 tick

// 5) 预缓存（载图阶段）
RegisterListener<Listeners.OnServerPrecacheResources>(m => m.AddResource("particles/<你的addon>/<魔法阵>.vpcf"));
```

### 8.3 验证步骤（务必实测，别只看文档）
1. 自建一个最小 vpcf（一个 emit + 一个 sprite，半径接 CP5.X），确认 `SetControlPoint 5: R 0 0` 能实时改大小。
2. 用 §3.2 的 A/B/C 三种跟随各做 10 秒测试，记录哪种在本机 listen server 下稳定。
3. 拿 `founding_rainofstars/main.vpcf` 做一次“插件 spawn 复合粒子是否渲染”的对照实验（`cs_reload` 后**换图**再看）。
4. 用 `particles/overlay_fx`、`particles/explosions_fx` 等世界粒子做可见性对照。

---

## 9. 明确未验证 / 存疑清单

1. `CEnvParticleGlow.m_flRadiusScale` 是否对**某些** vpcf 生效——**未验证**；现有证据偏向“非通用倍率”。
2. 复合粒子（`m_Children`）经**插件** spawn 是否渲染——**存疑**（CSRoll 说不渲染；地图 entitylump 能放同类资源）。
3. `m_vServerControlPoints` 4 个槽能指向的 CP 索引上限——社区用到 34；理论到 63（CS2Draw 语），**未验证**。
4. `particles/ui/*` 直接当世界粒子是否稳定——**未验证**（设计上是 screen space）。
5. `Teleport` 的 angles 对“世界空间但非 billboard”粒子的实际影响——**部分验证**，需作者支持。
6. `SetParent` 后是否必须补 `Teleport`——社区注释提示需要，**未在本版本 1.0.373 复现验证**。
7. `ResourceManifest.AddResource` 对 vtex/vmat 依赖是否需显式添加——**未验证**。
