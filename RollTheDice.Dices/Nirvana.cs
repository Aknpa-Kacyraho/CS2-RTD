using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using Microsoft.Extensions.Localization;
using RollTheDice.Enums;
using RollTheDice.Utils;

namespace RollTheDice.Dices;

public class Nirvana : DiceBlueprint
{
	private bool _comboActive;

	private readonly Random _random = new Random(Guid.NewGuid().GetHashCode());

	private readonly Dictionary<CCSPlayerController, float> _cooldowns = new Dictionary<CCSPlayerController, float>();

	private CBaseEntity[] _playerSpawnEntities = Array.Empty<CBaseEntity>();

	private CBaseEntity[] _ctSpawnEntities = Array.Empty<CBaseEntity>();

	private CBaseEntity[] _tSpawnEntities = Array.Empty<CBaseEntity>();

	public override string ClassName => "Nirvana";

	public override List<string> Listeners
	{
		get
		{
			int num = 1;
			List<string> list = new List<string>(num);
			CollectionsMarshal.SetCount(list, num);
			Span<string> span = CollectionsMarshal.AsSpan(list);
			int index = 0;
			span[index] = "OnPlayerTakeDamagePre";
			return list;
		}
	}

	public Nirvana(PluginConfig GlobalConfig, MapConfig Config, IStringLocalizer Localizer)
		: base(GlobalConfig, Config, Localizer)
	{
		RollTheDice.LogDebug(_localizer["dice.class.initialize"].Value.Replace("{name}", ClassName) + "\n");
	}

	public override void Add(CCSPlayerController player)
	{
		if ((CEntityInstance)(object)player == (CEntityInstance)null || !((CEntityInstance)player).IsValid || (CEntityInstance)(object)player.PlayerPawn?.Value == (CEntityInstance)null || !((CEntityInstance)player.PlayerPawn.Value).IsValid)
		{
			return;
		}
		_players.Add(player);
		_cooldowns[player] = 0f;
		_comboActive = DiceSynergy.HasPartner(player, "GuardianAngel");
		if (_comboActive)
		{
			RollTheDice instance = RollTheDice.Instance;
			if (instance != null && instance.HasDiceActive(player, "GuardianAngel"))
			{
				DiceSynergy.AnnounceCombo(player, "菲尼克斯", "涅槃+守护天使合成为菲尼克斯！");
				CCSPlayerController captured = player;
				Server.NextFrame((Action)delegate
				{
					if (instance != null && ((CEntityInstance)captured).IsValid)
					{
						instance.RemoveDiceFromPlayer(captured, "Nirvana");
						instance.RemoveDiceFromPlayer(captured, "GuardianAngel");
						instance.ForceDiceForPlayer(captured, "Phoenix");
					}
				});
				return;
			}
			DiceSynergy.AnnounceCombo(player, "菲尼克斯", "团队联动！涅槃与守护天使共鸣！");
		}
		FindSpawnPoints();
		NotifyPlayers(player, ClassName, new Dictionary<string, string> { 
		{
			"playerName",
			((CBasePlayerController)player).PlayerName
		} });
	}

	public override void Remove(CCSPlayerController player, DiceRemoveReason reason = DiceRemoveReason.GameLogic)
	{
		_players.Remove(player);
		_cooldowns.Remove(player);
	}

	public override void Reset()
	{
		_players.Clear();
		_cooldowns.Clear();
	}

	public override void Destroy()
	{
		Reset();
	}

	private void FindSpawnPoints()
	{
		if (_ctSpawnEntities.Length == 0 && _tSpawnEntities.Length == 0)
		{
			_playerSpawnEntities = Utilities.FindAllEntitiesByDesignerName<CBaseEntity>("info_player_terrorist").Concat(Utilities.FindAllEntitiesByDesignerName<CBaseEntity>("info_player_counterterrorist")).ToArray();
			_ctSpawnEntities = Utilities.FindAllEntitiesByDesignerName<CBaseEntity>("info_player_counterterrorist").ToArray();
			_tSpawnEntities = Utilities.FindAllEntitiesByDesignerName<CBaseEntity>("info_player_terrorist").ToArray();
		}
	}

	private Vector? GetSpawnPos(CCSPlayerController player)
	{
		CBaseEntity[] first = (((int)player.Team == 3) ? _ctSpawnEntities : _tSpawnEntities);
		List<CBaseEntity> list = (from _ in first
			orderby _random.Next()
			select _).ToList();
		foreach (CBaseEntity item in list)
		{
			if (((item != null) ? item.AbsOrigin : null) != null)
			{
				return item.AbsOrigin;
			}
		}
		return null;
	}

	public HookResult OnPlayerTakeDamagePre(CBaseEntity entity, CTakeDamageInfo info)
	{
		//IL_0023: Unknown result type (might be due to invalid IL or missing references)
		//IL_0042: Unknown result type (might be due to invalid IL or missing references)
		//IL_01bf: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e3: Unknown result type (might be due to invalid IL or missing references)
		//IL_0144: Unknown result type (might be due to invalid IL or missing references)
		//IL_01bb: Unknown result type (might be due to invalid IL or missing references)
		//IL_016a: Unknown result type (might be due to invalid IL or missing references)
		if (_players.Count == 0)
		{
			return (HookResult)0;
		}
		if (info.Damage <= 0f)
		{
			return (HookResult)0;
		}
		CCSPlayerPawn obj = ((NativeObject)entity).As<CCSPlayerPawn>();
		object obj2;
		if (obj == null)
		{
			obj2 = null;
		}
		else
		{
			CHandle<CBasePlayerController> controller = ((CBasePlayerPawn)obj).Controller;
			if (controller == null)
			{
				obj2 = null;
			}
			else
			{
				CBasePlayerController value = controller.Value;
				obj2 = ((value != null) ? ((NativeObject)value).As<CCSPlayerController>() : null);
			}
		}
		CCSPlayerController victim = (CCSPlayerController)obj2;
		if ((CEntityInstance)(object)victim == (CEntityInstance)null || !((CEntityInstance)victim).IsValid || !_players.Contains(victim))
		{
			return (HookResult)0;
		}
		float num = Server.CurrentTime;
		if (_cooldowns.TryGetValue(victim, out var value2) && num < value2)
		{
			RollTheDice.LogDebug($"[Nirvana] cooldown: sid={((CBasePlayerController)victim).SteamID} remain={value2 - num:F1}s -> continue\n");
			return (HookResult)0;
		}
		float minChance = _config.Dices.Nirvana.MinChance;
		float maxChance = _config.Dices.Nirvana.MaxChance;
		float num2 = minChance + (float)(_random.NextDouble() * (double)(maxChance - minChance));
		if (_random.NextDouble() > (double)num2)
		{
			return (HookResult)0;
		}
		Vector spawnPos = GetSpawnPos(victim);
		if (spawnPos == null)
		{
			return (HookResult)0;
		}
		bool lethal = entity.Health - (int)float.Round(info.Damage) <= 0;
		if (lethal)
		{
			info.Damage = 0f;
		}
		_cooldowns[victim] = num + _config.Dices.Nirvana.Cooldown;
		string playerName = ((CBasePlayerController)victim).PlayerName;
		Server.NextFrame((Action)delegate
		{
			CCSPlayerController obj3 = victim;
			if (!((CEntityInstance)(object)((obj3 == null) ? null : obj3.PlayerPawn?.Value) == (CEntityInstance)null) && ((CEntityInstance)victim.PlayerPawn.Value).IsValid)
			{
				CCSPlayerPawn value3 = victim.PlayerPawn.Value;
				((CBaseEntity)value3).Teleport(spawnPos, new QAngle((float?)0f, (float?)0f, (float?)0f), new Vector((float?)0f, (float?)0f, (float?)0f));
				((CBaseEntity)value3).Health = ((CBaseEntity)value3).MaxHealth;
				value3.ArmorValue = 100;
				Utilities.SetStateChanged((CBaseEntity)(object)value3, "CBaseEntity", "m_iHealth", 0);
				Utilities.SetStateChanged((CBaseEntity)(object)value3, "CCSPlayerPawn", "m_ArmorValue", 0);
				victim.PrintToCenterAlert("\ud83c\udf38 彼岸花开！回到出生点！");
				Server.PrintToChatAll(" " + _localizer["command.prefix"].Value + _localizer["dice_Nirvana_broadcast"].Value.Replace("{playerName}", playerName));
			}
		});
		return lethal ? (HookResult)1 : (HookResult)0;
	}
}
