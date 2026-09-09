using System;
using System.Drawing;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Memory;
using CounterStrikeSharp.API.Modules.Utils;

namespace RollTheDice.Utils;

public static class Entities
{
	public static void SetSchemaValue<T>(CBaseEntity entity, string className, string propertyName, T value)
	{
		Schema.SetSchemaValue<T>(((NativeEntity)entity).Handle, className, propertyName, value);
		if (Schema.IsSchemaFieldNetworked(className, propertyName))
		{
			Utilities.SetStateChanged(entity, className, propertyName, 0);
		}
	}

	public static CDynamicProp? CreatePlayerEntity(Vector position, QAngle rotation, string model, string animation, bool loop = false)
	{
		CDynamicProp val = Utilities.CreateEntityByName<CDynamicProp>("prop_dynamic");
		if ((CEntityInstance)(object)val == (CEntityInstance)null)
		{
			return null;
		}
		CBodyComponent cBodyComponent = ((CBaseEntity)val).CBodyComponent;
		object obj;
		if (cBodyComponent == null)
		{
			obj = null;
		}
		else
		{
			CGameSceneNode sceneNode = cBodyComponent.SceneNode;
			if (sceneNode == null)
			{
				obj = null;
			}
			else
			{
				CEntityInstance owner = sceneNode.Owner;
				obj = ((owner != null) ? owner.Entity : null);
			}
		}
		if (obj != null)
		{
			((CBaseEntity)val).CBodyComponent.SceneNode.Owner.Entity.Flags = (uint)(((CBaseEntity)val).CBodyComponent.SceneNode.Owner.Entity.Flags & -5);
		}
		val.UseAnimGraph = false;
		val.IdleAnim = animation;
		val.IdleAnimLoopMode = (AnimLoopMode_t)(loop ? 1 : 0);
		((CBaseEntity)val).DispatchSpawn();
		((CBaseModelEntity)val).SetModel(model);
		((CEntityInstance)val).AcceptInput("Enable", (CEntityInstance)null, (CEntityInstance)null, "", 0);
		((CBaseEntity)val).Teleport(position, rotation, (Vector)null);
		return val;
	}

	public static CDynamicProp? CreatePropEntity(Vector position, QAngle rotation, string model, float scale = 1f, CEntityInstance? parent = null)
	{
		//IL_0096: Unknown result type (might be due to invalid IL or missing references)
		//IL_009c: Expected O, but got Unknown
		CDynamicProp val = Utilities.CreateEntityByName<CDynamicProp>("prop_dynamic_override");
		if ((CEntityInstance)(object)val == (CEntityInstance)null)
		{
			return null;
		}
		CBodyComponent cBodyComponent = ((CBaseEntity)val).CBodyComponent;
		object obj;
		if (cBodyComponent == null)
		{
			obj = null;
		}
		else
		{
			CGameSceneNode sceneNode = cBodyComponent.SceneNode;
			if (sceneNode == null)
			{
				obj = null;
			}
			else
			{
				CEntityInstance owner = sceneNode.Owner;
				obj = ((owner != null) ? owner.Entity : null);
			}
		}
		if (obj != null)
		{
			((CBaseEntity)val).CBodyComponent.SceneNode.Owner.Entity.Flags = (uint)(((CBaseEntity)val).CBodyComponent.SceneNode.Owner.Entity.Flags & -5);
		}
		val.UseAnimGraph = true;
		CEntityKeyValues val2 = new CEntityKeyValues();
		val2.SetFloat("modelscale", scale);
		((CBaseEntity)val).DispatchSpawn(val2);
		((CBaseModelEntity)val).SetModel(model);
		((CEntityInstance)val).AcceptInput("Enable", (CEntityInstance)null, (CEntityInstance)null, "", 0);
		((CBaseEntity)val).Teleport(position, rotation, (Vector)null);
		if (parent != (CEntityInstance)null)
		{
			((CEntityInstance)val).AcceptInput("SetParent", parent, parent, "!activator", 0);
		}
		return val;
	}

	public static string GetModel(CBaseEntity entity)
	{
		object obj;
		if (entity == null)
		{
			obj = null;
		}
		else
		{
			CBodyComponent cBodyComponent = entity.CBodyComponent;
			if (cBodyComponent == null)
			{
				obj = null;
			}
			else
			{
				CGameSceneNode sceneNode = cBodyComponent.SceneNode;
				if (sceneNode == null)
				{
					obj = null;
				}
				else
				{
					CSkeletonInstance skeletonInstance = sceneNode.GetSkeletonInstance();
					obj = ((skeletonInstance != null) ? skeletonInstance.ModelState.ModelName : null);
				}
			}
		}
		if (obj == null)
		{
			obj = string.Empty;
		}
		return (string)obj;
	}

	public static void RemoveEntity(CBaseEntity? entity)
	{
		if ((CEntityInstance)(object)entity != (CEntityInstance)null && ((CEntityInstance)entity).IsValid)
		{
			((CEntityInstance)entity).AcceptInput("Kill", (CEntityInstance)null, (CEntityInstance)null, "", 0);
			((CEntityInstance)entity).Remove();
		}
	}

	public static string? PlayerWeaponName(CBasePlayerWeapon weapon)
	{
		if (!((CEntityInstance)weapon).IsValid)
		{
			return null;
		}
		try
		{
			CCSWeaponBaseVData vData = ((CBaseEntity)weapon).GetVData<CCSWeaponBaseVData>();
			return ((vData != null) ? vData.Name : null) ?? null;
		}
		catch
		{
			return null;
		}
	}

	public static Vector GetForwardVector(QAngle angles)
	{
		//IL_0069: Unknown result type (might be due to invalid IL or missing references)
		//IL_0070: Expected O, but got Unknown
		float num = angles.X * (float)Math.PI / 180f;
		float num2 = angles.Y * (float)Math.PI / 180f;
		float value = (float)(Math.Cos(num) * Math.Cos(num2));
		float value2 = (float)(Math.Cos(num) * Math.Sin(num2));
		float value3 = (float)(0.0 - Math.Sin(num));
		return new Vector((float?)value, (float?)value2, (float?)value3);
	}

	public static Vector GetRightVector(QAngle angles)
	{
		//IL_00d1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d8: Expected O, but got Unknown
		float num = angles.X * (float)Math.PI / 180f;
		float num2 = angles.Y * (float)Math.PI / 180f;
		float num3 = angles.Z * (float)Math.PI / 180f;
		float num4 = (float)Math.Sin(num);
		float num5 = (float)Math.Cos(num);
		float num6 = (float)Math.Sin(num2);
		float num7 = (float)Math.Cos(num2);
		float num8 = (float)Math.Sin(num3);
		float num9 = (float)Math.Cos(num3);
		float value = -1f * num8 * num4 * num7 + -1f * num9 * (0f - num6);
		float value2 = -1f * num8 * num4 * num6 + -1f * num9 * num7;
		float value3 = -1f * num8 * num5;
		return new Vector((float?)value, (float?)value2, (float?)value3);
	}

	public static void SetPlayerAlpha(CCSPlayerController? player, int alpha)
	{
		if (!((CEntityInstance)(object)((player == null) ? null : ((CBasePlayerController)player).Pawn?.Value) == (CEntityInstance)null) && ((CEntityInstance)((CBasePlayerController)player).Pawn.Value).IsValid)
		{
			((CBaseModelEntity)((CBasePlayerController)player).Pawn.Value).Render = Color.FromArgb(alpha, 255, 255, 255);
			Utilities.SetStateChanged((CBaseEntity)(object)((CBasePlayerController)player).Pawn.Value, "CBaseModelEntity", "m_clrRender", 0);
		}
	}

	public static Vector GetUpVector(QAngle angles)
	{
		//IL_00b5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bc: Expected O, but got Unknown
		float num = angles.X * (float)Math.PI / 180f;
		float num2 = angles.Y * (float)Math.PI / 180f;
		float num3 = angles.Z * (float)Math.PI / 180f;
		float num4 = (float)Math.Sin(num);
		float num5 = (float)Math.Cos(num);
		float num6 = (float)Math.Sin(num2);
		float num7 = (float)Math.Cos(num2);
		float num8 = (float)Math.Sin(num3);
		float num9 = (float)Math.Cos(num3);
		float value = num9 * num4 * num7 + (0f - num8) * (0f - num6);
		float value2 = num9 * num4 * num6 + (0f - num8) * num7;
		float value3 = num9 * num5;
		return new Vector((float?)value, (float?)value2, (float?)value3);
	}
}
