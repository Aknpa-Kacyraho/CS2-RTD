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

public class WASDChaos : DiceBlueprint
{
	private readonly Random _random = new Random(Guid.NewGuid().GetHashCode());

	private readonly Dictionary<CCSPlayerController, PlayerButtons> _heldButtons = new Dictionary<CCSPlayerController, PlayerButtons>();

	private readonly Dictionary<CCSPlayerController, float> _chaosOffset = new Dictionary<CCSPlayerController, float>();

	private readonly Dictionary<CCSPlayerController, float> _nextReshuffle = new Dictionary<CCSPlayerController, float>();

	private float _chaosEndTime;

	private static readonly float[] ChaosPresets = new float[8] { 90f, -90f, 180f, 45f, -45f, 135f, -135f, 0f };

	public override string ClassName => "WASDChaos";

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

	public WASDChaos(PluginConfig GlobalConfig, MapConfig Config, IStringLocalizer Localizer)
		: base(GlobalConfig, Config, Localizer)
	{
		RollTheDice.LogDebug(_localizer["dice.class.initialize"].Value.Replace("{name}", ClassName) + "\n");
	}

	public override void Add(CCSPlayerController player)
	{
		if (!((CEntityInstance)(object)player == (CEntityInstance)null) && ((CEntityInstance)player).IsValid && !((CEntityInstance)(object)player.PlayerPawn?.Value == (CEntityInstance)null) && ((CEntityInstance)player.PlayerPawn.Value).IsValid)
		{
			_players.Add(player);
			_heldButtons[player] = (PlayerButtons)0;
			_chaosOffset[player] = ChaosPresets[_random.Next(ChaosPresets.Length)];
			_nextReshuffle[player] = Server.CurrentTime + 2f + (float)(_random.NextDouble() * 3.0);
			if (_chaosEndTime == 0f)
			{
				_chaosEndTime = Server.CurrentTime + 40f;
			}
			NotifyPlayers(player, ClassName, new Dictionary<string, string> { 
			{
				"playerName",
				((CBasePlayerController)player).PlayerName
			} });
			Server.PrintToChatAll(" " + _localizer["command.prefix"].Value + _localizer["dice_WASDChaos_broadcast"].Value.Replace("{playerName}", ((CBasePlayerController)player).PlayerName));
		}
	}

	public override void Remove(CCSPlayerController player, DiceRemoveReason reason = DiceRemoveReason.GameLogic)
	{
		_players.Remove(player);
		_heldButtons.Remove(player);
		_chaosOffset.Remove(player);
		_nextReshuffle.Remove(player);
	}

	public override void Reset()
	{
		_players.Clear();
		_heldButtons.Clear();
		_chaosOffset.Clear();
		_nextReshuffle.Clear();
		_chaosEndTime = 0f;
	}

	public void OnPlayerButtonsChanged(CCSPlayerController player, PlayerButtons pressed, PlayerButtons released)
	{
		//IL_0050: Unknown result type (might be due to invalid IL or missing references)
		//IL_0051: Unknown result type (might be due to invalid IL or missing references)
		//IL_0052: Unknown result type (might be due to invalid IL or missing references)
		//IL_0053: Unknown result type (might be due to invalid IL or missing references)
		//IL_0054: Unknown result type (might be due to invalid IL or missing references)
		//IL_0055: Unknown result type (might be due to invalid IL or missing references)
		//IL_0056: Unknown result type (might be due to invalid IL or missing references)
		//IL_0057: Unknown result type (might be due to invalid IL or missing references)
		//IL_0058: Unknown result type (might be due to invalid IL or missing references)
		//IL_0059: Unknown result type (might be due to invalid IL or missing references)
		//IL_005a: Unknown result type (might be due to invalid IL or missing references)
		//IL_005b: Unknown result type (might be due to invalid IL or missing references)
		//IL_005c: Unknown result type (might be due to invalid IL or missing references)
		//IL_005d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0065: Unknown result type (might be due to invalid IL or missing references)
		//IL_0049: Unknown result type (might be due to invalid IL or missing references)
		if (_players.Count != 0 && !((CEntityInstance)(object)player == (CEntityInstance)null) && ((CEntityInstance)player).IsValid)
		{
			if (!_heldButtons.TryGetValue(player, out var value))
			{
				value = (PlayerButtons)0;
			}
			PlayerButtons val = (PlayerButtons)1560;
			value = (PlayerButtons)(value | (pressed & val));
			value = (PlayerButtons)(value & ~(released & val));
			_heldButtons[player] = value;
		}
	}

	public void OnTick()
	{
		//IL_0313: Unknown result type (might be due to invalid IL or missing references)
		//IL_030c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0315: Unknown result type (might be due to invalid IL or missing references)
		//IL_0317: Unknown result type (might be due to invalid IL or missing references)
		//IL_031f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0322: Invalid comparison between Unknown and I8
		//IL_0383: Unknown result type (might be due to invalid IL or missing references)
		//IL_038a: Expected O, but got Unknown
		//IL_038a: Unknown result type (might be due to invalid IL or missing references)
		//IL_03c9: Unknown result type (might be due to invalid IL or missing references)
		//IL_0409: Unknown result type (might be due to invalid IL or missing references)
		//IL_044c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0559: Unknown result type (might be due to invalid IL or missing references)
		//IL_0563: Expected O, but got Unknown
		if (_players.Count == 0)
		{
			return;
		}
		float num = Server.CurrentTime;
		if (_chaosEndTime > 0f && num >= _chaosEndTime)
		{
			_players.Clear();
			_chaosOffset.Clear();
			_heldButtons.Clear();
			_nextReshuffle.Clear();
			_chaosEndTime = 0f;
			Server.PrintToChatAll(" " + _localizer["command.prefix"].Value + "\ud83c\udf00 方向错乱已解除！");
			return;
		}
		int num2;
		if (_players.Count > 0)
		{
			CCSPlayerController obj = _players[0];
			if (obj != null && ((CEntityInstance)obj).IsValid)
			{
				num2 = ((CBaseEntity)_players[0]).TeamNum;
				goto IL_00ee;
			}
		}
		num2 = -1;
		goto IL_00ee;
		IL_00ee:
		int holderTeam = num2;
		if (holderTeam < 2)
		{
			return;
		}
		foreach (CCSPlayerController item in from p in Utilities.GetPlayers()
			where ((CEntityInstance)p).IsValid && !((CBasePlayerController)p).IsHLTV && ((CBaseEntity)p).TeamNum != holderTeam && (CEntityInstance)(object)p.PlayerPawn?.Value != (CEntityInstance)null && ((CEntityInstance)p.PlayerPawn.Value).IsValid && ((CBaseEntity)p.PlayerPawn.Value).LifeState == 0
			select p)
		{
			try
			{
				CCSPlayerPawn value = item.PlayerPawn.Value;
				if (!_chaosOffset.ContainsKey(item))
				{
					_chaosOffset[item] = ChaosPresets[_random.Next(ChaosPresets.Length)];
					_heldButtons[item] = (PlayerButtons)0;
					_nextReshuffle[item] = num + 2f + (float)(_random.NextDouble() * 3.0);
				}
				if (_nextReshuffle.TryGetValue(item, out var value2) && num >= value2)
				{
					float num3 = ChaosPresets[_random.Next(ChaosPresets.Length)];
					_chaosOffset[item] = num3;
					float num4 = _config.Dices.WASDChaos.MinInterval + (float)(_random.NextDouble() * (double)(_config.Dices.WASDChaos.MaxInterval - _config.Dices.WASDChaos.MinInterval));
					_nextReshuffle[item] = num + num4;
					if (1 == 0)
					{
					}
					string text = ((num3 == 90f) ? "W→A / A→S / S→D / D→W" : ((num3 == -90f) ? "W→D / D→S / S→A / A→W" : ((num3 == 180f) ? "W↔S / A↔D" : ((num3 != 45f) ? "方向重新映射!" : "45°旋转"))));
					if (1 == 0)
					{
					}
					string text2 = text;
					item.PrintToCenterAlert("\ud83c\udf00 " + text2);
				}
				PlayerButtons val = (_heldButtons.TryGetValue(item, out var value3) ? value3 : item.Buttons);
				if (((long)val & 0x618L) != 0)
				{
					float y = value.EyeAngles.Y;
					float num5 = y + _chaosOffset.GetValueOrDefault(item, 90f);
					float x = num5 * (float)Math.PI / 180f;
					Vector val2 = new Vector((float?)0f, (float?)0f, (float?)0f);
					if (((Enum)val).HasFlag((Enum)(object)(PlayerButtons)8))
					{
						val2.X += MathF.Cos(x);
						val2.Y += MathF.Sin(x);
					}
					if (((Enum)val).HasFlag((Enum)(object)(PlayerButtons)16))
					{
						val2.X -= MathF.Cos(x);
						val2.Y -= MathF.Sin(x);
					}
					if (((Enum)val).HasFlag((Enum)(object)(PlayerButtons)512))
					{
						val2.X += MathF.Sin(x);
						val2.Y -= MathF.Cos(x);
					}
					if (((Enum)val).HasFlag((Enum)(object)(PlayerButtons)1024))
					{
						val2.X -= MathF.Sin(x);
						val2.Y += MathF.Cos(x);
					}
					if (val2.X != 0f || val2.Y != 0f)
					{
						float num6 = MathF.Sqrt(val2.X * val2.X + val2.Y * val2.Y);
						float num7 = 260f;
						val2.X = val2.X / num6 * num7;
						val2.Y = val2.Y / num6 * num7;
						Vector absVelocity = ((CBaseEntity)value).AbsVelocity;
						float value4 = ((absVelocity != null) ? absVelocity.Z : 0f);
						((CBaseEntity)value).Teleport((Vector)null, (QAngle)null, new Vector((float?)val2.X, (float?)val2.Y, (float?)value4));
					}
				}
			}
			catch
			{
			}
		}
	}
}
