using System.Drawing;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;

namespace RollTheDice.Utils;

public static class GlowUtil
{
	/// <summary>
	/// 造一对"发光替身"prop：<c>val</c> 跟随目标实体，<c>val2</c> 跟随 <c>val</c> 并带发光。
	///
	/// <para>★ 顺序必须是「<c>DispatchSpawn</c> → <c>SetModel</c> → <c>FollowEntity</c>」：
	/// 对还在 staging list 里的实体调 <c>SetModel</c> 会触发引擎
	/// <c>CModelState::SetupModel()</c> 断言（会写 minidump），并让该实体的模型状态变脏，
	/// 之后可能演变成客户端实体表错位（CopyExistingEntity 致命错误）。同工程的
	/// <see cref="Entities.CreatePropEntity"/> 就是"先 spawn 再 SetModel"的正确写法。</para>
	/// </summary>
	public static (CDynamicProp?, CDynamicProp?) CreateGlow(CBaseEntity entity, Color color)
	{
		if (entity == null || !((CEntityInstance)entity).IsValid)
		{
			return (null, null);
		}
		CDynamicProp val = Utilities.CreateEntityByName<CDynamicProp>("prop_dynamic");
		CDynamicProp val2 = Utilities.CreateEntityByName<CDynamicProp>("prop_dynamic");
		if ((CEntityInstance)(object)val == (CEntityInstance)null || (CEntityInstance)(object)val2 == (CEntityInstance)null
			|| !((CEntityInstance)val).IsValid || !((CEntityInstance)val2).IsValid)
		{
			Entities.RemoveEntity((CBaseEntity)val);
			Entities.RemoveEntity((CBaseEntity)val2);
			return (null, null);
		}
		CBodyComponent cBodyComponent = entity.CBodyComponent;
		object obj;
		if (cBodyComponent == null)
		{
			obj = null;
		}
		else
		{
			CGameSceneNode sceneNode = cBodyComponent.SceneNode;
			obj = ((sceneNode != null) ? sceneNode.GetSkeletonInstance() : null);
		}
		CSkeletonInstance val3 = (CSkeletonInstance)obj;
		if (val3 == null)
		{
			Entities.RemoveEntity((CBaseEntity)val);
			Entities.RemoveEntity((CBaseEntity)val2);
			return (null, null);
		}
		string modelName = val3.ModelState.ModelName;
		if (string.IsNullOrEmpty(modelName))
		{
			Entities.RemoveEntity((CBaseEntity)val);
			Entities.RemoveEntity((CBaseEntity)val2);
			return (null, null);
		}
		((CBaseEntity)val).Spawnflags = 256u;
		((CBaseModelEntity)val).RenderMode = (RenderMode_t)2;
		((CBaseEntity)val).DispatchSpawn();
		((CBaseModelEntity)val).SetModel(modelName);
		((CEntityInstance)val).AcceptInput("FollowEntity", (CEntityInstance)(object)entity, (CEntityInstance)(object)val, "!activator", 0);
		((CBaseEntity)val2).Spawnflags = 256u;
		((CBaseModelEntity)val2).RenderMode = (RenderMode_t)0;
		((CBaseEntity)val2).DispatchSpawn();
		((CBaseModelEntity)val2).SetModel(modelName);
		((CEntityInstance)val2).AcceptInput("FollowEntity", (CEntityInstance)(object)val, (CEntityInstance)(object)val2, "!activator", 0);
		((CBaseModelEntity)val2).Render = Color.FromArgb(255, 255, 255, 255);
		((CBaseModelEntity)val2).Glow.GlowColorOverride = color;
		((CBaseModelEntity)val2).Glow.GlowRange = 5000;
		((CBaseModelEntity)val2).Glow.GlowTeam = -1;
		((CBaseModelEntity)val2).Glow.GlowType = 3;
		((CBaseModelEntity)val2).Glow.GlowRangeMin = 20;
		// ★ 父实体（玩家 pawn）会在死亡/换边时被引擎删除：登记进看门狗，父一失效就立刻拆掉替身 prop。
		ChildGuard.Track(val, entity);
		ChildGuard.Track(val2, val);
		return (val, val2);
	}

	public static void RemoveGlow(CBaseEntity? glowProxy, CBaseEntity? glow)
	{
		Entities.RemoveEntity(glowProxy);
		Entities.RemoveEntity(glow);
	}
}
