using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using Microsoft.Extensions.Localization;
using RollTheDice.Enums;

namespace RollTheDice.Dices;

public class Singularity : DiceBlueprint
{
	private Vector? _singularityPos;

	private float _singularityEndTime;

	private float _nextUseTime;

	private CParticleSystem? _particle;

	private CBeam? _glowBall;

	private bool _countdown10Shown;

	private bool _countdown5Shown;

	private readonly Random _random = new Random(Guid.NewGuid().GetHashCode());

	public override string ClassName => "Singularity";

	public override List<string> Listeners
	{
		get
		{
			int num = 2;
			List<string> list = new List<string>(num);
			CollectionsMarshal.SetCount(list, num);
			Span<string> span = CollectionsMarshal.AsSpan(list);
			int num2 = 0;
			span[num2] = "OnTick";
			num2++;
			span[num2] = "OnPlayerButtonsChanged";
			return list;
		}
	}

	public override float GetCooldownRemaining(CCSPlayerController player)
	{
		return _players.Contains(player) ? Math.Max(0f, _nextUseTime - Server.CurrentTime) : 0f;
	}

	public Singularity(PluginConfig GlobalConfig, MapConfig Config, IStringLocalizer Localizer)
		: base(GlobalConfig, Config, Localizer)
	{
		Console.WriteLine(_localizer["dice.class.initialize"].Value.Replace("{name}", ClassName));
	}

	public override void Add(CCSPlayerController player)
	{
		if (!((CEntityInstance)(object)player == (CEntityInstance)null) && ((CEntityInstance)player).IsValid && !((CEntityInstance)(object)player.PlayerPawn?.Value == (CEntityInstance)null) && ((CEntityInstance)player.PlayerPawn.Value).IsValid)
		{
			_players.Add(player);
			NotifyPlayers(player, ClassName, new Dictionary<string, string> { 
			{
				"playerName",
				((CBasePlayerController)player).PlayerName
			} });
			player.PrintToCenterAlert("\ud83c\udf00 按E键释放奇点！凝聚坍缩一切！");
		}
	}

	public override void Remove(CCSPlayerController player, DiceRemoveReason reason = DiceRemoveReason.GameLogic)
	{
		_players.Remove(player);
		CleanupSingularity();
	}

	public override void Reset()
	{
		_players.Clear();
		CleanupSingularity();
		_singularityPos = null;
		_singularityEndTime = 0f;
		_nextUseTime = 0f;
		_countdown10Shown = false;
		_countdown5Shown = false;
	}

	public override void Destroy()
	{
		Reset();
	}

	private void CleanupSingularity()
	{
		if ((CEntityInstance)(object)_particle != (CEntityInstance)null && ((CEntityInstance)_particle).IsValid)
		{
			((CEntityInstance)_particle).Remove();
		}
		_particle = null;
		if ((CEntityInstance)(object)_glowBall != (CEntityInstance)null && ((CEntityInstance)_glowBall).IsValid)
		{
			((CEntityInstance)_glowBall).Remove();
		}
		_glowBall = null;
	}

	public void OnPlayerButtonsChanged(CCSPlayerController player, PlayerButtons pressed, PlayerButtons released)
	{
		//IL_0048: Unknown result type (might be due to invalid IL or missing references)
		//IL_0154: Unknown result type (might be due to invalid IL or missing references)
		//IL_015a: Expected O, but got Unknown
		//IL_01c0: Unknown result type (might be due to invalid IL or missing references)
		//IL_01c7: Expected O, but got Unknown
		if (_players.Count == 0 || (CEntityInstance)(object)player == (CEntityInstance)null || !((CEntityInstance)player).IsValid || !_players.Contains(player) || !((Enum)pressed).HasFlag((Enum)(object)(PlayerButtons)32) || (CEntityInstance)(object)player.PlayerPawn?.Value == (CEntityInstance)null || !((CEntityInstance)player.PlayerPawn.Value).IsValid || ((CBaseEntity)player.PlayerPawn.Value).LifeState != 0)
		{
			return;
		}
		float num = Server.CurrentTime;
		if (!(num < _nextUseTime))
		{
			_nextUseTime = num + _config.Dices.Singularity.Cooldown;
			CCSPlayerPawn value = player.PlayerPawn.Value;
			if (((CBaseEntity)value).AbsOrigin != null)
			{
				Vector val = new Vector((float?)((CBaseEntity)value).AbsOrigin.X, (float?)((CBaseEntity)value).AbsOrigin.Y, (float?)(((CBaseEntity)value).AbsOrigin.Z + 64f));
				QAngle eyeAngles = value.EyeAngles;
				float x = eyeAngles.Y * (float)Math.PI / 180f;
				float x2 = eyeAngles.X * (float)Math.PI / 180f;
				Vector val2 = new Vector((float?)(MathF.Cos(x2) * MathF.Cos(x)), (float?)(MathF.Cos(x2) * MathF.Sin(x)), (float?)(0f - MathF.Sin(x2)));
				Vector pos = (_singularityPos = val + val2 * 2000f);
				_singularityEndTime = num + _config.Dices.Singularity.Duration;
				_countdown10Shown = false;
				_countdown5Shown = false;
				SpawnSingularityVisual(pos);
				player.PrintToCenterAlert($"\ud83c\udf00 奇点释放！{_config.Dices.Singularity.Duration:F0}秒");
				Server.PrintToChatAll(" " + _localizer["command.prefix"].Value + _localizer["dice_Singularity_broadcast"].Value.Replace("{playerName}", ((CBasePlayerController)player).PlayerName));
			}
		}
	}

	private void SpawnSingularityVisual(Vector pos)
	{
		//IL_0069: Unknown result type (might be due to invalid IL or missing references)
		//IL_0089: Unknown result type (might be due to invalid IL or missing references)
		//IL_0093: Expected O, but got Unknown
		//IL_0093: Expected O, but got Unknown
		//IL_0117: Unknown result type (might be due to invalid IL or missing references)
		//IL_0137: Unknown result type (might be due to invalid IL or missing references)
		//IL_0141: Expected O, but got Unknown
		//IL_0141: Expected O, but got Unknown
		CleanupSingularity();
		_particle = Utilities.CreateEntityByName<CParticleSystem>("info_particle_system");
		if ((CEntityInstance)(object)_particle != (CEntityInstance)null)
		{
			_particle.EffectName = "particles/ui/status_effects/speed_boost.vpcf";
			_particle.StartActive = true;
			((CBaseEntity)_particle).Teleport(pos, new QAngle((float?)null, (float?)null, (float?)null), new Vector((float?)null, (float?)null, (float?)null));
			((CBaseEntity)_particle).DispatchSpawn();
		}
		_glowBall = Utilities.CreateEntityByName<CBeam>("beam");
		if ((CEntityInstance)(object)_glowBall != (CEntityInstance)null)
		{
			((CBaseModelEntity)_glowBall).Render = Color.FromArgb(200, 100, 50, 200);
			_glowBall.Width = 8f;
			((CBaseEntity)_glowBall).Teleport(pos, new QAngle((float?)null, (float?)null, (float?)null), new Vector((float?)null, (float?)null, (float?)null));
			((CBaseEntity)_glowBall).DispatchSpawn();
		}
	}

	public void OnTick()
	{
		//IL_02bc: Unknown result type (might be due to invalid IL or missing references)
		//IL_02c3: Expected O, but got Unknown
		if (_singularityPos == null)
		{
			return;
		}
		float num = Server.CurrentTime;
		if (num >= _singularityEndTime)
		{
			CleanupSingularity();
			_singularityPos = null;
			_countdown10Shown = false;
			_countdown5Shown = false;
			return;
		}
		float num2 = _singularityEndTime - num;
		if (!_countdown10Shown && num2 <= 10f)
		{
			_countdown10Shown = true;
			foreach (CCSPlayerController item in from p in Utilities.GetPlayers()
				where ((CEntityInstance)p).IsValid && !((CBasePlayerController)p).IsHLTV
				select p)
			{
				item.PrintToCenterAlert("\ud83c\udf00 奇点释放！10秒");
			}
		}
		if (!_countdown5Shown && num2 <= 5f)
		{
			_countdown5Shown = true;
			foreach (CCSPlayerController item2 in from p in Utilities.GetPlayers()
				where ((CEntityInstance)p).IsValid && !((CBasePlayerController)p).IsHLTV
				select p)
			{
				item2.PrintToCenterAlert("\ud83c\udf00 奇点释放！5秒");
			}
		}
		if (_singularityPos == null)
		{
			return;
		}
		Vector singularityPos = _singularityPos;
		float pullStrength = _config.Dices.Singularity.PullStrength;
		foreach (CCSPlayerController item3 in from p in Utilities.GetPlayers()
			where ((CEntityInstance)p).IsValid && !((CBasePlayerController)p).IsHLTV && (CEntityInstance)(object)p.PlayerPawn?.Value != (CEntityInstance)null && ((CEntityInstance)p.PlayerPawn.Value).IsValid && ((CBaseEntity)p.PlayerPawn.Value).LifeState == 0 && ((CBaseEntity)p.PlayerPawn.Value).AbsOrigin != null
			select p)
		{
			CCSPlayerPawn value = item3.PlayerPawn.Value;
			Vector absOrigin = ((CBaseEntity)value).AbsOrigin;
			float num3 = singularityPos.X - absOrigin.X;
			float num4 = singularityPos.Y - absOrigin.Y;
			float num5 = singularityPos.Z - absOrigin.Z;
			float num6 = MathF.Sqrt(num3 * num3 + num4 * num4 + num5 * num5);
			if (!(num6 < 20f))
			{
				float num7 = num3 / num6;
				float num8 = num4 / num6;
				float num9 = num5 / num6;
				float num10 = pullStrength * Server.TickInterval * (1000f / (num6 + 200f));
				if (num10 > num6)
				{
					num10 = num6;
				}
				Vector val = new Vector((float?)(num7 * num10 / Server.TickInterval), (float?)(num8 * num10 / Server.TickInterval), (float?)(num9 * num10 / Server.TickInterval));
				((CBaseEntity)value).Teleport((Vector)null, (QAngle)null, val);
			}
		}
	}
}
