using System.Drawing;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;

namespace RollTheDice.Utils;

public static class GlowUtil
{
	public static (CDynamicProp?, CDynamicProp?) CreateGlow(CBaseEntity entity, Color color)
	{
		CDynamicProp val = Utilities.CreateEntityByName<CDynamicProp>("prop_dynamic");
		CDynamicProp val2 = Utilities.CreateEntityByName<CDynamicProp>("prop_dynamic");
		if ((CEntityInstance)(object)val == (CEntityInstance)null || (CEntityInstance)(object)val2 == (CEntityInstance)null)
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
		((CBaseModelEntity)val).SetModel(modelName);
		((CEntityInstance)val).AcceptInput("FollowEntity", (CEntityInstance)(object)entity, (CEntityInstance)(object)val, "!activator", 0);
		((CBaseEntity)val).DispatchSpawn();
		((CBaseModelEntity)val2).SetModel(modelName);
		((CEntityInstance)val2).AcceptInput("FollowEntity", (CEntityInstance)(object)val, (CEntityInstance)(object)val2, "!activator", 0);
		((CBaseEntity)val2).DispatchSpawn();
		((CBaseModelEntity)val2).Render = Color.FromArgb(255, 255, 255, 255);
		((CBaseModelEntity)val2).Glow.GlowColorOverride = color;
		((CBaseEntity)val2).Spawnflags = 256u;
		((CBaseModelEntity)val2).RenderMode = (RenderMode_t)0;
		((CBaseModelEntity)val2).Glow.GlowRange = 5000;
		((CBaseModelEntity)val2).Glow.GlowTeam = -1;
		((CBaseModelEntity)val2).Glow.GlowType = 3;
		((CBaseModelEntity)val2).Glow.GlowRangeMin = 20;
		return (val, val2);
	}

	public static void RemoveGlow(CBaseEntity? glowProxy, CBaseEntity? glow)
	{
		Entities.RemoveEntity(glowProxy);
		Entities.RemoveEntity(glow);
	}
}
