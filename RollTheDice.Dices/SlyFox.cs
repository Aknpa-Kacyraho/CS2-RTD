using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using Microsoft.Extensions.Localization;
using RollTheDice.Enums;

namespace RollTheDice.Dices;

public class SlyFox : DiceBlueprint
{
	private readonly Random _random = new Random(Guid.NewGuid().GetHashCode());

	private float _nextItemTime;

	private readonly HashSet<string> _grenadeProjectiles = new HashSet<string> { "smokegrenade_projectile", "hegrenade_projectile", "molotov_projectile", "decoy_projectile", "flashbang_projectile" };

	private static readonly string[] RandomItems = new string[6] { "weapon_hegrenade", "weapon_flashbang", "weapon_smokegrenade", "weapon_molotov", "weapon_incgrenade", "weapon_decoy" };

	public override string ClassName => "SlyFox";

	public override List<string> Listeners
	{
		get
		{
			int num = 2;
			List<string> list = new List<string>(num);
			CollectionsMarshal.SetCount(list, num);
			Span<string> span = CollectionsMarshal.AsSpan(list);
			int num2 = 0;
			span[num2] = "OnEntitySpawned";
			num2++;
			span[num2] = "OnTick";
			return list;
		}
	}

	public SlyFox(PluginConfig GlobalConfig, MapConfig Config, IStringLocalizer Localizer)
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
		}
	}

	public override void Remove(CCSPlayerController player, DiceRemoveReason reason = DiceRemoveReason.GameLogic)
	{
		_players.Remove(player);
	}

	public override void Reset()
	{
		_players.Clear();
		_nextItemTime = 0f;
	}

	public override void Destroy()
	{
		Reset();
	}

	public void OnEntitySpawned(CEntityInstance entity)
	{
		if (_players.Count == 0 || !_grenadeProjectiles.Contains(entity.DesignerName))
		{
			return;
		}
		Server.NextFrame((Action)delegate
		{
			//IL_0029: Unknown result type (might be due to invalid IL or missing references)
			//IL_002f: Expected O, but got Unknown
			if (((NativeEntity)entity).Handle != (IntPtr)IntPtr.Zero)
			{
				CBaseGrenade val = new CBaseGrenade(((NativeEntity)entity).Handle);
				if (((CEntityInstance)val).IsValid && ((NativeEntity)val).Handle != (IntPtr)IntPtr.Zero)
				{
					CCSPlayerPawn val2 = val.OriginalThrower?.Value;
					if (!((CEntityInstance)(object)val2 == (CEntityInstance)null) && ((CEntityInstance)val2).IsValid)
					{
						CHandle<CBasePlayerController> controller = ((CBasePlayerPawn)val2).Controller;
						object obj;
						if (controller == null)
						{
							obj = null;
						}
						else
						{
							CBasePlayerController value = controller.Value;
							obj = ((value != null) ? ((NativeObject)value).As<CCSPlayerController>() : null);
						}
						CCSPlayerController val3 = (CCSPlayerController)obj;
						if (!((CEntityInstance)(object)val3 == (CEntityInstance)null) && ((CEntityInstance)val3).IsValid && _players.Contains(val3))
						{
							float num = 1f + (float)(_random.NextDouble() * 19.0);
							val.DetonateTime = Server.CurrentTime + num;
						}
					}
				}
			}
		});
	}

	public void OnTick()
	{
		if (_players.Count == 0)
		{
			return;
		}
		float num = Server.CurrentTime;
		if (num < _nextItemTime)
		{
			return;
		}
		_nextItemTime = num + 10f;
		foreach (CCSPlayerController item in _players.ToList())
		{
			if (!((CEntityInstance)(object)item == (CEntityInstance)null) && ((CEntityInstance)item).IsValid && !((CEntityInstance)(object)item.PlayerPawn?.Value == (CEntityInstance)null) && ((CEntityInstance)item.PlayerPawn.Value).IsValid && ((CBaseEntity)item.PlayerPawn.Value).LifeState == 0)
			{
				string text = RandomItems[_random.Next(RandomItems.Length)];
				item.GiveNamedItem(text);
				item.PrintToCenterAlert("\ud83e\udd8a 狡猾狐狸获得了随机道具！");
			}
		}
	}
}
