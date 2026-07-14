using System.IO;
using System.Reflection;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using RollTheDice.Dices;
using RollTheDice.Enums;
using RollTheDice.Utils;

namespace RollTheDice
{
    public partial class RollTheDice : BasePlugin, IPluginConfig<PluginConfig>
    {
        public override string ModuleName => "Roll The Dice";
        public override string ModuleAuthor => "Kalle <kalle@kandru.de>";

        /// <summary>Singleton instance for other dice to access core functions.</summary>
        public static RollTheDice? Instance { get; private set; }

        /// <summary>Prefix for round backup files created at round start.</summary>
        public const string RoundBackupPrefix = "rtd";

        /// <summary>Current round backup filename (e.g. "rtd_round05.txt") for Rewind/Universe.</summary>
        private static string? _currentRoundBackupFile;

        /// <summary>Returns the current round backup filename (no path, e.g. "rtd_round05.txt").</summary>
        public static string? GetRoundBackupFile() => _currentRoundBackupFile;

        private string _currentMap = "";
        private readonly Dictionary<CCSPlayerController, int> _playersThatRolledTheDice = [];
        private readonly Dictionary<CCSPlayerController, int> _PlayerCooldown = [];
        private readonly List<DiceBlueprint> _dices = [];
        private readonly Dictionary<DiceBlueprint, int> _diceUsageCount = [];
        private readonly Dictionary<CCSPlayerController, string> _originalPlayerNames = [];
        private bool _isDuringRound;
        private readonly Random _random = new(Guid.NewGuid().GetHashCode());

        public override void Load(bool hotReload)
        {
            Instance = this;
            // update configuration
            ReloadConfigFromDisk();
            // register listeners
            RegisterEventHandler<EventRoundStart>(OnRoundStart);
            RegisterEventHandler<EventRoundFreezeEnd>(OnRoundFreezeEnd);
            RegisterEventHandler<EventRoundEnd>(OnRoundEnd);
            RegisterEventHandler<EventPlayerDeath>(OnPlayerDeath);
            RegisterEventHandler<EventPlayerHurt>(OnPlayerHurtReveal);
            RegisterEventHandler<EventPlayerDisconnect>(OnPlayerDisconnect);
            RegisterListener<Listeners.OnMapStart>(OnMapStart);
            RegisterListener<Listeners.OnMapEnd>(OnMapEnd);
            RegisterListener<Listeners.OnServerPrecacheResources>(OnServerPrecacheResources);
            RegisterListener<Listeners.OnPlayerButtonsChanged>(OnPlayerButtonsChanged);
            // print message if hot reload
            if (hotReload)
            {
                Console.WriteLine(Localizer["core.hotreload"]);
                // set current map
                _currentMap = Server.MapName;
                // initialize configuration
                LoadMapConfig(_currentMap);
                _isDuringRound = true;
                // initialize dice modules
                InitializeModules();
            }
        }

        public override void Unload(bool hotReload)
        {
            // reset dice rolls on unload
            DestroyModules();
            // update configuration
            ReloadConfigFromDisk();
            // unregister listeners
            DeregisterEventHandler<EventRoundStart>(OnRoundStart);
            DeregisterEventHandler<EventRoundFreezeEnd>(OnRoundFreezeEnd);
            DeregisterEventHandler<EventRoundEnd>(OnRoundEnd);
            DeregisterEventHandler<EventPlayerDeath>(OnPlayerDeath);
            DeregisterEventHandler<EventPlayerDisconnect>(OnPlayerDisconnect);
            RemoveListener<Listeners.OnMapStart>(OnMapStart);
            RemoveListener<Listeners.OnMapEnd>(OnMapEnd);
            RemoveListener<Listeners.OnServerPrecacheResources>(OnServerPrecacheResources);
            Console.WriteLine(Localizer["core.unload"]);
        }

        private HookResult OnRoundStart(EventRoundStart @event, GameEventInfo info)
        {
            // safety: reset host_timescale in case Izayoi crashed mid-effect
            Server.ExecuteCommand("host_timescale 1.0");

            // reset players that rolled the dice
            _playersThatRolledTheDice.Clear();

            // Process Glutton kills: consume them as extra dice this round, then reset
            // The kill count was accumulated last round and consumed via GetMaxDiceCount()
            // After this round's rolls complete (~4s, after the 3s second pass), clear kill counts
            _ = AddTimer(4f, () =>
            {
                Glutton.KillCounts.Clear();
            });

            // Reset Goddess flag
            Goddess.ActiveThisRound = false;
            Goddess.BlessedPlayers.Clear();

            // Clear World pending rolls
            World.PendingExtraRolls.Clear();

            // Clear Mimic pending copies
            Mimic.PendingCopy.Clear();

            // Clear Trickster pending fake names
            Trickster.PendingFakeNames.Clear();

            // Clear Karma buffed players
            Karma.BuffedPlayers.Clear();

            // Clear Plague infected players
            Plague.InfectedPlayers.Clear();

            // Clear GravityWell active wells
            GravityWell.ActiveWells.Clear();

            // Clear DeathKnightComplete denied list
            DeathKnightComplete.DeniedNextRound.Clear();

            // Clear stacking health modifiers (prevents cross-round corruption)
            StackingHealth.ClearAll();

            // Clear stacking move locks (prevents players staying frozen)
            MoveLockManager.ClearAll();

            // CRITICAL: Set _isDuringRound BEFORE RemoveDicesForPlayers so that
            // if any dice's Reset() throws (e.g. DarkTide Fade field bug),
            // manual !rtd still works after the error is caught.
            _isDuringRound = true;

            // reset dices (necessary after warmup) — wrap in its own try-catch
            // so that Reset() bugs in individual dice don't block auto dice roll.
            try { RemoveDicesForPlayers(); }
            catch (Exception ex)
            {
                File.AppendAllText(Path.Combine(Server.GameDirectory, "csgo/addons/counterstrikesharp/logs/rtd_debug.txt"),
                    $"{DateTime.Now:HH:mm:ss} OnRoundStart: RemoveDicesForPlayers error (non-fatal): {ex.Message}\n");
            }

            // refresh game rules (to avoid caching warmup value)
            GameRules.Refresh();
            // abort if warmup
            object? warmupPeriodObj = GameRules.Get("WarmupPeriod");
            if (!Config.AllowRtdDuringWarmup && warmupPeriodObj is bool warmupPeriod && warmupPeriod)
            {
                _isDuringRound = false;
                return HookResult.Continue;
            }

            try
            {
                // Create round backup for Rewind/Universe time-rewind dice.
                CreateRoundBackup();

                // announce round start
                Server.PrintToChatAll(Localizer["core.announcement"]);
                File.AppendAllText(Path.Combine(Server.GameDirectory, "csgo/addons/counterstrikesharp/logs/rtd_debug.txt"),
                    $"{DateTime.Now:HH:mm:ss} OnRoundStart: TriggerEvent={Config.DiceTrigger.TriggerEvent}, Force={Config.DiceTrigger.ForceAllPlayers}, dices={_dices.Count}\n");
                if (Config.DiceTrigger.TriggerEvent == DiceTriggerEvent.RoundStart)
                {
                    RollTheDiceOnRoundStart(force: Config.DiceTrigger.ForceAllPlayers);
                    if (Config.DiceTrigger.RollTheDiceEveryXSeconds > 0)
                    {
                        RollTheDiceEveryXSeconds(Config.DiceTrigger.RollTheDiceEveryXSeconds);
                    }
                }
            }
            catch (Exception ex)
            {
                File.AppendAllText(Path.Combine(Server.GameDirectory, "csgo/addons/counterstrikesharp/logs/rtd_debug.txt"),
                    $"{DateTime.Now:HH:mm:ss} OnRoundStart dice roll error: {ex.Message}\n");
            }
            return HookResult.Continue;
        }

        private HookResult OnRoundFreezeEnd(EventRoundFreezeEnd @event, GameEventInfo info)
        {
            // abort if warmup
            object? warmupPeriodObj = GameRules.Get("WarmupPeriod");
            if (!Config.AllowRtdDuringWarmup && warmupPeriodObj is bool warmupPeriod && warmupPeriod)
            {
                return HookResult.Continue;
            }
            if (Config.DiceTrigger.TriggerEvent != DiceTriggerEvent.RoundFreezeEnd)
            {
                return HookResult.Continue;
            }
            RollTheDiceOnRoundStart(force: Config.DiceTrigger.ForceAllPlayers);
            // optionally roll the dice every X seconds
            if (Config.DiceTrigger.RollTheDiceEveryXSeconds > 0)
            {
                RollTheDiceEveryXSeconds(Config.DiceTrigger.RollTheDiceEveryXSeconds);
            }
            return HookResult.Continue;
        }

        private void RollTheDiceOnRoundStart(bool force = false)
        {
            var eligiblePlayers = Utilities.GetPlayers()
                .Where(p => !p.IsHLTV && !p.IsBot
                    && p.Pawn?.Value?.LifeState == (byte)LifeState_t.LIFE_ALIVE
                    && (force || (Config.DiceTrigger.AllowPlayerAutoRtd
                        && _playerConfigs.ContainsKey(p.SteamID)
                        && _playerConfigs[p.SteamID].RtdOnSpawn)))
                .ToList();

            if (eligiblePlayers.Count == 0) return;

            int playerCount = Utilities.GetPlayers().Count(p => !p.IsHLTV && !p.IsBot && p.Pawn?.Value?.LifeState == (byte)LifeState_t.LIFE_ALIVE);
            int rolledCount = eligiblePlayers.Count;

            // Build pools
            var drawablePool = GetDrawablePool();
            if (drawablePool.Count == 0) return;
            var normalPool = GetNormalPool();
            if (normalPool.Count == 0) normalPool = drawablePool; // edge case: all dice are special

            // === ROUND 1: each player draws one die, weighted ===
            Dictionary<CCSPlayerController, DiceBlueprint> firstDraw = [];
            List<DiceBlueprint> specialTypes = []; // distinct special types found

            foreach (var entry in eligiblePlayers)
            {
                if (GetDiceRollCount(entry) >= GetMaxDiceCount(entry)) continue;
                var drawn = WeightedRandomDraw(drawablePool);
                if (drawn == null) continue;
                firstDraw[entry] = drawn;
                if (drawn.IsSpecial && !specialTypes.Any(s => s.ClassName == drawn.ClassName))
                    specialTypes.Add(drawn);
            }

            // === ROUND 2 (only if specials exist) ===
            Dictionary<CCSPlayerController, DiceBlueprint> specialWinners = [];
            Dictionary<CCSPlayerController, DiceBlueprint> remaining = [];
            bool hasSpecials = specialTypes.Count > 0;

            if (hasSpecials)
            {
                // Split: special holders are locked, others enter round 2
                foreach (var kv in firstDraw)
                {
                    if (kv.Value.IsSpecial)
                        specialWinners[kv.Key] = kv.Value;
                    else
                        remaining[kv.Key] = kv.Value;
                }

                // Pull ALL alive players (including non-auto-RTD teammates) into round 2
                // so WolfKing/DragonSoul teammate bonuses actually reach everyone.
                foreach (var p in Utilities.GetPlayers()
                    .Where(p => !p.IsHLTV && !p.IsBot
                        && p.Pawn?.Value?.LifeState == (byte)LifeState_t.LIFE_ALIVE
                        && !specialWinners.ContainsKey(p)
                        && !remaining.ContainsKey(p)))
                {
                    remaining[p] = WeightedRandomDraw(normalPool) ?? WeightedRandomDraw(drawablePool)!;
                }

                // Compute P_total = 1 - ∏(1 - Pi) and probSum = Σ Pi
                double pMiss = 1.0;
                foreach (var st in specialTypes)
                    pMiss *= (1.0 - st.SecondRoundProbability);
                double pTotal = 1.0 - pMiss;
                double probSum = specialTypes.Sum(st => (double)st.SecondRoundProbability);

                foreach (var player in remaining.Keys.ToList())
                {
                    double roll = _random.NextDouble();
                    DiceBlueprint? assigned;

                    if (roll < pTotal && probSum > 0)
                    {
                        // HIT: weighted select among special types by Pi/probSum
                        double selectRoll = _random.NextDouble() * probSum;
                        double selectCumulative = 0;
                        DiceBlueprint? selectedType = specialTypes[0];
                        foreach (var st in specialTypes)
                        {
                            selectCumulative += st.SecondRoundProbability;
                            if (selectRoll < selectCumulative) { selectedType = st; break; }
                        }

                        // Apply RewardId mapping
                        if (!string.IsNullOrEmpty(selectedType.SecondRoundRewardId))
                        {
                            var reward = _dices.FirstOrDefault(d =>
                                d.ClassName.Equals(selectedType.SecondRoundRewardId, StringComparison.OrdinalIgnoreCase));
                            assigned = reward ?? selectedType;
                        }
                        else
                        {
                            assigned = selectedType;
                        }
                    }
                    else
                    {
                        // MISS: weighted draw from normal (non-special) pool
                        assigned = WeightedRandomDraw(normalPool);
                        if (assigned == null) assigned = WeightedRandomDraw(drawablePool);
                    }

                    if (assigned != null)
                        remaining[player] = assigned;
                }
            }
            else
            {
                remaining = firstDraw;
            }

            // === DISTRIBUTE ===
            var finalDice = hasSpecials
                ? specialWinners.Concat(remaining).ToDictionary(kv => kv.Key, kv => kv.Value)
                : remaining;

            foreach (var kv in finalDice)
            {
                var entry = kv.Key;
                string diceName = kv.Value.ClassName;
                if (entry == null || !entry.IsValid) continue;
                if (GetDiceRollCount(entry) >= GetMaxDiceCount(entry)) continue;

                CCSPlayerController capturedEntry = entry;
                string capturedDice = diceName;
                _ = AddTimer(1f, () =>
                {
                    if (capturedEntry == null || !capturedEntry.IsValid) return;
                    if (GetDiceRollCount(capturedEntry) >= GetMaxDiceCount(capturedEntry)) return;
                    (string? rolledDice, string? diceDescription) = RollTheDiceForPlayer(capturedEntry, capturedDice);
                    if (rolledDice is null or "") return;
                    IncrementDiceRollCount(capturedEntry);
                    PlayDiceSoundForPlayer(capturedEntry, rolledDice);
                });
            }

            File.AppendAllText(Path.Combine(Server.GameDirectory, "csgo/addons/counterstrikesharp/logs/rtd_debug.txt"),
                $"{DateTime.Now:HH:mm:ss} RoundStart: {playerCount} alive, {rolledCount} rolling, pool={drawablePool.Count}d, special=[{string.Join(",", specialTypes.Select(d => d.ClassName))}], P_total={1.0 - specialTypes.Aggregate(1.0, (m, st) => m * (1.0 - st.SecondRoundProbability)):F3}\n");

            // Second pass: grant extra dice from Goddess / World / Glutton
            _ = AddTimer(3f, () =>
            {
                // Snapshot max dice counts BEFORE the loop
                Dictionary<CCSPlayerController, int> maxSnapshot = [];
                foreach (CCSPlayerController p in Utilities.GetPlayers()
                    .Where(p => !p.IsHLTV && !p.IsBot && p.Pawn?.Value?.LifeState == (byte)LifeState_t.LIFE_ALIVE))
                {
                    maxSnapshot[p] = GetMaxDiceCount(p);
                }

                foreach (CCSPlayerController entry in Utilities.GetPlayers()
                    .Where(p => !p.IsHLTV && !p.IsBot && p.Pawn?.Value?.LifeState == (byte)LifeState_t.LIFE_ALIVE))
                {
                    int current = GetDiceRollCount(entry);
                    int max = maxSnapshot.TryGetValue(entry, out int snap) ? snap : DefaultMaxDicePerPlayer;
                    for (int i = current; i < max; i++)
                    {
                        (string? extraDice, _) = RollTheDiceForPlayer(entry);
                        if (extraDice is not null and not "")
                        {
                            IncrementDiceRollCount(entry);
                        }
                    }
                    World.PendingExtraRolls.Remove(entry.SteamID);
                    Reincarnation.PendingExtraDice.Remove(entry.SteamID);
                }

                // Process Mimic pending copies
                foreach (var kv in Mimic.PendingCopy.ToList())
                {
                    ulong steamID = kv.Key;
                    string diceClass = kv.Value;
                    if (string.IsNullOrEmpty(diceClass)) continue;

                    CCSPlayerController? mimicPlayer = Utilities.GetPlayers()
                        .FirstOrDefault(p => p.IsValid && !p.IsHLTV && !p.IsBot && p.SteamID == steamID);
                    if (mimicPlayer == null) { Mimic.PendingCopy.Remove(steamID); continue; }
                    if (GetDiceRollCount(mimicPlayer) >= GetMaxDiceCount(mimicPlayer)) { Mimic.PendingCopy.Remove(steamID); continue; }

                    (string? copiedDice, _) = RollTheDiceForPlayer(mimicPlayer, diceClass);
                    if (copiedDice is not null and not "")
                    {
                        IncrementDiceRollCount(mimicPlayer);
                    }
                    Mimic.PendingCopy.Remove(steamID);
                }

                // Re-give Universe to players whose restore just fired
                foreach (ulong steamId in Universe.PendingRestore.ToList())
                {
                    CCSPlayerController? p = Utilities.GetPlayers()
                        .FirstOrDefault(pl => pl.IsValid && !pl.IsHLTV && pl.SteamID == steamId);
                    if (p != null && p.IsValid)
                    {
                        RollTheDiceForPlayer(p, "Universe");
                        p.PrintToCenterAlert($"🌌 宇宙之力已恢复！剩余{Universe.RestoresLeft.GetValueOrDefault(steamId)}次");
                    }
                    Universe.PendingRestore.Remove(steamId);
                }
            });
        }

        private void RollTheDiceEveryXSeconds(int seconds)
        {
            _ = AddTimer(seconds, () =>
            {
                if (!_isDuringRound)
                {
                    return;
                }

                foreach (CCSPlayerController entry in Utilities.GetPlayers()
                    .Where(p => !p.IsHLTV
                        && !p.IsBot
                        && p.Pawn?.Value?.LifeState == (byte)LifeState_t.LIFE_ALIVE))
                {
                    RemoveDiceForPlayer(entry, reason: DiceRemoveReason.GameLogic);
                    _ = RollTheDiceForPlayer(entry);
                }
                RollTheDiceEveryXSeconds(seconds);
            });
        }

        private HookResult OnRoundEnd(EventRoundEnd @event, GameEventInfo info)
        {
            // safety: reset timescale
            Server.ExecuteCommand("host_timescale 1.0");
            // disallow dice rolls immediately (before RemoveDicesForPlayers may throw)
            _isDuringRound = false;
            try
            {
                RemoveDicesForPlayers();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[RollTheDice] OnRoundEnd RemoveDicesForPlayers error: {ex.Message}");
            }
            // reduct cooldown if applicable
            if (Config.CooldownRounds > 0)
            {
                foreach (KeyValuePair<CCSPlayerController, int> kvp in _PlayerCooldown)
                {
                    // remove one round per player
                    if (_PlayerCooldown[kvp.Key] > 0)
                    {
                        _PlayerCooldown[kvp.Key] -= 1;
                    }
                }
            }
            // continue event
            return HookResult.Continue;
        }

        private HookResult OnPlayerDeath(EventPlayerDeath @event, GameEventInfo info)
        {
            CCSPlayerController? victim = @event.Userid;
            CCSPlayerController? attacker = @event.Attacker;

            // Mimic: attacker kills victim → capture victim's dice name before removal
            if (victim != null && victim.IsValid && attacker != null && attacker.IsValid
                && Mimic.PendingCopy.TryGetValue(attacker.SteamID, out string? pendingVal)
                && string.IsNullOrEmpty(pendingVal))
            {
                foreach (DiceBlueprint dice in _dices)
                {
                    if (dice._players.Contains(victim) && dice.ClassName != "Mimic")
                    {
                        Mimic.PendingCopy[attacker.SteamID] = dice.ClassName;
                        break;
                    }
                }
            }
            RemoveDiceForPlayer(victim, DiceRemoveReason.Death);
            return HookResult.Continue;
        }

        private HookResult OnPlayerDisconnect(EventPlayerDisconnect @event, GameEventInfo info)
        {
            RemoveDiceForPlayer(@event.Userid, DiceRemoveReason.Disconnect);
            _ = _originalPlayerNames.Remove(@event.Userid);
            return HookResult.Continue;
        }

        private HookResult OnPlayerHurtReveal(EventPlayerHurt @event, GameEventInfo info)
        {
            CCSPlayerController? attacker = @event.Attacker;
            CCSPlayerController? victim = @event.Userid;
            if (attacker == null || !attacker.IsValid || victim == null || !victim.IsValid) return HookResult.Continue;
            if (attacker.TeamNum == victim.TeamNum) return HookResult.Continue;

            var dices = GetAllDiceForPlayer(victim);
            if (dices.Count == 0) return HookResult.Continue;

            var diceNames = new List<string>();
            foreach (var d in dices)
            {
                if (d == "Trickster" && Trickster.PendingFakeNames.TryGetValue(victim.SteamID, out var fakeName))
                    diceNames.Add(Localizer[$"dice_{fakeName}_name"].Value);
                else
                {
                    string key = $"dice_{d}_name";
                    string localized = Localizer[key];
                    diceNames.Add(localized == key ? d : localized);
                }
            }
            attacker.PrintToChat($" {Localizer["command.prefix"].Value}🎯 {(_originalPlayerNames.TryGetValue(victim, out var clean) ? clean : victim.PlayerName)}的骰子：{string.Join(" + ", diceNames)}");
            return HookResult.Continue;
        }

        private void OnMapStart(string mapName)
        {
            // update configuration
            ReloadConfigFromDisk();
            // load map config
            LoadMapConfig(mapName);
            // initialize dice modules
            InitializeModules();
            // set current map
            _currentMap = mapName;
        }

        private void OnMapEnd()
        {
            DestroyModules();
            // reset states
            _isDuringRound = false;
            _playersThatRolledTheDice.Clear();
            _PlayerCooldown.Clear();
            // reset dice usage counter for better distribution
            _diceUsageCount.Clear();
        }

        private (string?, string?) RollTheDiceForPlayer(CCSPlayerController? player, string? diceName = null)
        {
            if (player == null || !player.IsValid) return (null, null);

            if (_dices.Count > 0)
            {
                if (diceName != null)
                {
                    List<DiceBlueprint> matchingDices = [.. _dices.Where(d => d.ClassName.Contains(diceName, StringComparison.OrdinalIgnoreCase))];
                    DiceBlueprint? foundDice = matchingDices.Count == 1 ? matchingDices.First() : _dices.FirstOrDefault(d => string.Equals(d.ClassName, diceName, StringComparison.OrdinalIgnoreCase));
                    if (foundDice != null)
                    {
                        try
                        {
                            File.AppendAllText(Path.Combine(Server.GameDirectory, "csgo/addons/counterstrikesharp/logs/rtd_debug.txt"),
                                $"{DateTime.Now:HH:mm:ss} ➜ Adding {diceName} to {player.PlayerName}\n");
                            if (foundDice._players.Contains(player))
                                foundDice.Remove(player, DiceRemoveReason.NewDice);
                        foundDice.Add(player);
                        RefreshPlayerDiceName(player);
                        File.AppendAllText(Path.Combine(Server.GameDirectory, "csgo/addons/counterstrikesharp/logs/rtd_debug.txt"),
                            $"{DateTime.Now:HH:mm:ss} ✓ {player.PlayerName} ← {diceName}\n");
                            if (!_diceUsageCount.ContainsKey(foundDice))
                                _diceUsageCount[foundDice] = 0;
                            _diceUsageCount[foundDice]++;
                            return (foundDice.ClassName, foundDice.Description);
                        }
                        catch (Exception ex)
                        {
                            File.AppendAllText(Path.Combine(Server.GameDirectory, "csgo/addons/counterstrikesharp/logs/rtd_debug.txt"),
                                $"{DateTime.Now:HH:mm:ss} Error adding dice {diceName}: {ex}\n");
                            // Do NOT remove from _dices — that permanently deletes the dice from
                            // the draw pool while its EventHandler/Listener is still registered,
                            // causing the player to retain the effect forever.
                            return (null, null);
                        }
                    }
                }
                else
                {
                    DiceBlueprint? randomDice = WeightedRandomDraw(GetDrawablePool());
                    if (randomDice == null) return (null, null);
                    try
                    {
                        File.AppendAllText(Path.Combine(Server.GameDirectory, "csgo/addons/counterstrikesharp/logs/rtd_debug.txt"),
                            $"{DateTime.Now:HH:mm:ss} ➜ Adding {randomDice.ClassName} to {player.PlayerName}\n");
                        if (randomDice._players.Contains(player))
                            randomDice.Remove(player, DiceRemoveReason.NewDice);
                        randomDice.Add(player);
                        RefreshPlayerDiceName(player);
                        File.AppendAllText(Path.Combine(Server.GameDirectory, "csgo/addons/counterstrikesharp/logs/rtd_debug.txt"),
                            $"{DateTime.Now:HH:mm:ss} ✓ {player.PlayerName} ← {randomDice.ClassName}\n");
                        if (!_diceUsageCount.ContainsKey(randomDice))
                            _diceUsageCount[randomDice] = 0;
                        _diceUsageCount[randomDice]++;
                        return (randomDice.ClassName, randomDice.Description);
                    }
                    catch (Exception ex)
                    {
                        File.AppendAllText(Path.Combine(Server.GameDirectory, "csgo/addons/counterstrikesharp/logs/rtd_debug.txt"),
                            $"{DateTime.Now:HH:mm:ss} Error adding random dice {randomDice.ClassName}: {ex}\n");
                        // Do NOT remove from _dices — see note above.
                        return (null, null);
                    }
                }
            }
            return (null, null);
        }

        /// <summary>Weighted random draw from a pool. Returns null if pool is empty.</summary>
        private DiceBlueprint? WeightedRandomDraw(List<DiceBlueprint> pool)
        {
            if (pool.Count == 0) return null;
            float totalWeight = 0f;
            foreach (var d in pool) totalWeight += d.Weight;
            if (totalWeight <= 0f) return pool[_random.Next(pool.Count)];
            float roll = (float)_random.NextDouble() * totalWeight;
            float cumulative = 0f;
            foreach (var d in pool)
            {
                cumulative += d.Weight;
                if (roll < cumulative) return d;
            }
            return pool[pool.Count - 1]; // float precision fallback
        }

        /// <summary>Build the drawable pool (CanBeDrawn == true).</summary>
        private List<DiceBlueprint> GetDrawablePool()
            => _dices.Where(d => d.CanBeDrawn).ToList();

        /// <summary>Build the normal (non-special) pool for round 2 miss case.</summary>
        private List<DiceBlueprint> GetNormalPool()
            => _dices.Where(d => d.CanBeDrawn && !d.IsSpecial).ToList();

        private void RemoveDiceForPlayer(CCSPlayerController? player, DiceRemoveReason reason)
        {
            if (player == null || !player.IsValid) return;

            // Don't remove old dice when rolling extra dice — stack them.
            // Only remove for Death, Disconnect, or GameLogic (round end reset).
            if (reason == DiceRemoveReason.NewDice) return;

            bool hadDice = false;
            foreach (DiceBlueprint dice in _dices)
            {
                if (dice._players.Contains(player))
                {
                    dice.Remove(player, reason);
                    hadDice = true;
                }
            }
            if (hadDice) RefreshPlayerDiceName(player);
            if (Config.AllowDiceAfterRespawn)
            {
                _ = _playersThatRolledTheDice.Remove(player);
            }
        }

        private void RemoveDicesForPlayers()
        {
            // reset all dices for all players
            foreach (DiceBlueprint dice in _dices)
                dice.Reset();
            // Strip dice prefix from player names (e.g. "[骰子名] PlayerName")
            foreach (var kv in _originalPlayerNames.ToList())
            {
                if (kv.Key != null && kv.Key.IsValid)
                {
                    string current = kv.Key.PlayerName;
                    int lastBracket = current.LastIndexOf("] ");
                    if (lastBracket > 0)
                        current = current[(lastBracket + 2)..].Trim();
                    kv.Key.PlayerName = current;
                    Utilities.SetStateChanged(kv.Key, "CBasePlayerController", "m_iszPlayerName");
                }
            }
            _originalPlayerNames.Clear();
        }

        private void RefreshPlayerDiceName(CCSPlayerController player)
        {
            if (player == null || !player.IsValid) return;
            try
            {
            string cleanName = player.PlayerName;

            // Store original name on first call
            if (!_originalPlayerNames.TryGetValue(player, out string? original))
            {
                // Strip any existing prefix to get clean name
                int lastBracket = cleanName.LastIndexOf("] ");
                if (lastBracket > 0)
                    cleanName = cleanName[(lastBracket + 2)..].Trim();
                _originalPlayerNames[player] = cleanName;
            }
            else
                cleanName = original;

            // Collect active dice class names, replace combo pairs with combo name
            var activeClassNames = new HashSet<string>(
                _dices.Where(d => d._players.Contains(player)).Select(d =>
                    d.ClassName == "Trickster" && Trickster.PendingFakeNames.TryGetValue(player.SteamID, out var fakeName)
                        ? fakeName
                        : d.ClassName));
            var displayNames = DiceSynergy.ResolveComboNames(activeClassNames)
                .Select(cn =>
                {
                    // cn is either a dice ClassName or a combo name
                    // Combo names are pure display text (Chinese), dice ClassNames need localization
                    string key = $"dice_{cn}_name";
                    string localized = Localizer[key];
                    // If the localized string equals the raw key, the resource was not found
                    return localized == key ? cn : localized;
                })
                .ToList();

            string newName;
            if (displayNames.Count > 2)
                newName = $"[{displayNames[0]}]+{displayNames.Count - 1} " + cleanName;
            else if (displayNames.Count > 0)
                newName = string.Join(" ", displayNames.Select(n => $"[{n}]")) + " " + cleanName;
            else
                newName = cleanName;

            player.PlayerName = newName;
            Utilities.SetStateChanged(player, "CBasePlayerController", "m_iszPlayerName");

            // Re-apply after 1s (engine resists name changes)
            CCSPlayerController captured = player;
            string capturedName = newName;
            _ = AddTimer(1f, () =>
            {
                if (captured != null && captured.IsValid)
                {
                    captured.PlayerName = capturedName;
                    Utilities.SetStateChanged(captured, "CBasePlayerController", "m_iszPlayerName");
                }
            });
            }
            catch (Exception ex)
            {
                File.AppendAllText(Path.Combine(Server.GameDirectory, "csgo/addons/counterstrikesharp/logs/rtd_debug.txt"),
                    $"{DateTime.Now:HH:mm:ss} RefreshPlayerDiceName error for {player.PlayerName}: {ex.Message}\n");
            }
        }

        private void InitializeModules()
        {
            if (_dices.Count > 0)
            {
                return;
            }
            // skip if globally disabled
            if (!_currentMapConfig.Enabled)
            {
                return;
            }

            // Get all dice types that are not abstract and are subclasses of DiceBlueprint
            Dictionary<string, Type> diceTypes = typeof(DiceBlueprint).Assembly.GetTypes()
                .Where(static t => t.IsSubclassOf(typeof(DiceBlueprint)) && !t.IsAbstract)
                .ToDictionary(static t => t.Name, static t => t);

            // Iterate through properties in DicesConfig and instantiate enabled dices
            foreach (PropertyInfo property in typeof(DicesConfig).GetProperties())
            {
                if (diceTypes.TryGetValue(property.Name, out Type? diceType))
                {
                    object? configValue = property.GetValue(_currentMapConfig.Dices);
                    if (configValue != null)
                    {
                        PropertyInfo? enabledProperty = configValue.GetType().GetProperty("Enabled");
                        if (enabledProperty != null && enabledProperty.GetValue(configValue) is bool enabled && enabled)
                        {
                            DiceBlueprint instance = (DiceBlueprint)Activator.CreateInstance(diceType, Config, _currentMapConfig, Localizer)!;
                            _dices.Add(instance);
                        }
                    }
                }
            }

            // set current rolled dices to 0
            foreach (DiceBlueprint entry in _dices)
            {
                _diceUsageCount.Add(entry, 0);
            }
            // register listeners
            RegisterListeners();
            RegisterEventHandlers();
            RegisterUserMessageHooks();
        }

        private void DestroyModules()
        {
            // deregister listeners
            try { DeregisterListeners(); } catch { }
            try { DeregisterEventHandlers(); } catch { }
            try { DeregisterUserMessageHooks(); } catch { }
            // destroy all modules - wrap each in try-catch so one bad dice doesn't block cleanup
            foreach (DiceBlueprint module in _dices)
            {
                try { module.Destroy(); }
                catch (Exception ex)
                {
                    Console.WriteLine($"[RollTheDice] DestroyModules: error destroying {module.ClassName}: {ex.Message}");
                }
            }
            _dices.Clear();
        }

        private void RegisterListeners()
        {
            foreach (DiceBlueprint module in _dices)
            {
                foreach (string listenerName in module.Listeners)
                {
                    DynamicHandlers.RegisterModuleListener(this, listenerName, module);
                }
            }
        }

        private void DeregisterListeners()
        {
            foreach (DiceBlueprint module in _dices)
            {
                //DebugPrint($"Destroying listener for module {module.GetType().Name}");
                foreach (string listenerName in module.Listeners)
                {
                    //DebugPrint($"- {listenerName}");
                    DynamicHandlers.DeregisterModuleListener(this, listenerName, module);
                }
            }
        }

        private void RegisterEventHandlers()
        {
            foreach (DiceBlueprint module in _dices)
            {
                //DebugPrint($"Initializing event handlers for module {module.GetType().Name}");
                foreach (string eventName in module.Events)
                {
                    //DebugPrint($"- {eventName}");
                    DynamicHandlers.RegisterModuleEventHandler(this, eventName, module);
                }
            }
        }

        private void DeregisterEventHandlers()
        {
            foreach (DiceBlueprint module in _dices)
            {
                //DebugPrint($"Destroying event handlers for module {module.GetType().Name}");
                foreach (string eventName in module.Events)
                {
                    //DebugPrint($"- {eventName}");
                    DynamicHandlers.DeregisterModuleEventHandler(this, eventName, module);
                }
            }
        }

        private void RegisterUserMessageHooks()
        {
            foreach (DiceBlueprint module in _dices)
            {
                //DebugPrint($"Registering user messages for module {module.GetType().Name}");
                foreach ((int userMessageId, HookMode hookMode) in module.UserMessages)
                {
                    //DebugPrint($"- UserMessage ID: {userMessageId}, HookMode: {hookMode}");
                    DynamicHandlers.RegisterUserMessageHook(this, userMessageId, module, hookMode);
                }
            }
        }

        private void DeregisterUserMessageHooks()
        {
            foreach (DiceBlueprint module in _dices)
            {
                //DebugPrint($"Deregistering user messages for module {module.GetType().Name}");
                foreach ((int userMessageId, HookMode hookMode) in module.UserMessages)
                {
                    //DebugPrint($"- UserMessage ID: {userMessageId}, HookMode: {hookMode}");
                    DynamicHandlers.DeregisterUserMessageHook(this, userMessageId, module, hookMode);
                }
            }
        }

        private void PlayDiceSoundForPlayer(CCSPlayerController? player, string rolledDice, bool command = false)
        {
            if (player == null
                || !player.IsValid
                || (!command && Config.Sounds.PlayOnCommandOnly)
                || string.IsNullOrEmpty(Config.Sounds.DiceRollSound))
            {
                return;
            }
            string sound = Config.Sounds.DiceRollSound.Replace("{dice}", rolledDice);
            RecipientFilter filter = [player];
            _ = player.EmitSound(sound, filter);
        }

        // --- Multi-dice support ---
        private const int DefaultMaxDicePerPlayer = 1;

        private int GetDiceRollCount(CCSPlayerController player)
        {
            return _playersThatRolledTheDice.TryGetValue(player, out int count) ? count : 0;
        }

        private void IncrementDiceRollCount(CCSPlayerController player)
        {
            if (_playersThatRolledTheDice.ContainsKey(player))
                _playersThatRolledTheDice[player]++;
            else
                _playersThatRolledTheDice[player] = 1;
        }

        /// <summary>Check if a player currently has a specific dice class active. Used by DiceSynergy combos.</summary>
        public bool HasDiceActive(CCSPlayerController player, string diceClassName)
        {
            // Use SteamID comparison instead of CCSPlayerController reference equality.
            // CS2 can recycle controller handles after disconnect — a stale entry in
            // some dice's _players list could falsely match a new player with the same handle.
            ulong steamId = player.SteamID;
            return _dices.Any(d => string.Equals(d.ClassName, diceClassName, StringComparison.OrdinalIgnoreCase)
                                && d._players.Any(p => p.SteamID == steamId));
        }

        /// <summary>Public entry point for dice that force a roll for any player (e.g. DiceWholesale).</summary>
        public bool ForceDiceForPlayer(CCSPlayerController player)
        {
            if (player == null || !player.IsValid) return false;
            if (GetDiceRollCount(player) >= GetMaxDiceCount(player)) return false;
            (string? rolled, _) = RollTheDiceForPlayer(player);
            if (rolled is null or "") return false;
            IncrementDiceRollCount(player);
            PlayDiceSoundForPlayer(player, rolled);
            return true;
        }

        /// <summary>Force an extra dice for a player, bypassing max dice count check.
        /// Used by Trickster (kill → random dice) and other mid-round extra dice grants.</summary>
        public bool ForceExtraDiceForPlayer(CCSPlayerController player)
        {
            if (player == null || !player.IsValid) return false;
            (string? rolled, _) = RollTheDiceForPlayer(player);
            if (rolled is null or "") return false;
            IncrementDiceRollCount(player);
            PlayDiceSoundForPlayer(player, rolled);
            return true;
        }

        public bool ForceDiceForPlayer(CCSPlayerController player, string diceClassName)
        {
            if (player == null || !player.IsValid) return false;
            if (GetDiceRollCount(player) >= GetMaxDiceCount(player)) return false;
            (string? rolled, _) = RollTheDiceForPlayer(player, diceClassName);
            if (rolled is null or "") return false;
            IncrementDiceRollCount(player);
            PlayDiceSoundForPlayer(player, rolled);
            return true;
        }

        public bool RemoveDiceFromPlayer(CCSPlayerController player, string diceClassName)
        {
            if (player == null || !player.IsValid) return false;
            var dice = _dices.FirstOrDefault(d => d.ClassName == diceClassName);
            if (dice == null) return false;
            if (!dice._players.Contains(player)) return false;
            dice.Remove(player, DiceRemoveReason.GameLogic);
            // Reset roll count so ForceDiceForPlayer can replace the dice
            _ = _playersThatRolledTheDice.Remove(player);
            RefreshPlayerDiceName(player);
            return true;
        }

        public List<string> GetAllDiceForPlayer(CCSPlayerController player)
        {
            var result = new List<string>();
            if (player == null || !player.IsValid) return result;
            foreach (var dice in _dices)
                if (dice._players.Contains(player))
                    result.Add(dice.ClassName);
            return result;
        }

        private int GetMaxDiceCount(CCSPlayerController player)
        {
            if (DeathKnightComplete.DeniedNextRound.Contains(player.SteamID))
                return 0;

            int max = DefaultMaxDicePerPlayer;

            // Check Glutton - stored kills carry over to next round (keyed by SteamID)
            if (Glutton.KillCounts.TryGetValue(player.SteamID, out int kills))
            {
                int extra = Math.Min(kills, 2); // Max 2 extra from Glutton
                max += extra;
            }

            // Check Reincarnation - deaths last round grant extra dice this round
            if (Reincarnation.PendingExtraDice.TryGetValue(player.SteamID, out int reincExtra) && reincExtra > 0)
            {
                max += reincExtra;
            }

            // Check World dice - grants +3 extra dice this round (keyed by SteamID)
            if (World.PendingExtraRolls.TryGetValue(player.SteamID, out int worldExtra) && worldExtra > 0)
            {
                max += worldExtra;
            }

            // Check Goddess - only blessed players get +1
            if (Goddess.ActiveThisRound && Goddess.BlessedPlayers.Contains(player.SteamID))
            {
                max += 1;
            }

            return max;
        }

        private string GetLastDiceDescription(CCSPlayerController player)
        {
            // Find the last dice description for this player from their active dice
            foreach (DiceBlueprint dice in _dices)
            {
                if (dice._players.Contains(player))
                {
                    return dice.Description;
                }
            }
            return "Unknown";
        }

        /// <summary>Create a round backup file for Rewind/Universe. Call at round start.</summary>
        private void CreateRoundBackup()
        {
            try
            {
                // Get current round number from game rules to construct the filename
                // mp_backup_round_file "rtd" creates "rtd_round{N}.txt"
                int round = GameRules.Get("TotalRoundsPlayed", forceRefresh: true) as int? ?? 1;
                string fileName = $"{RoundBackupPrefix}_round{round:D2}.txt";

                // Delete previous round's backup (construct its name too)
                string prevFile = Path.Combine(Server.GameDirectory, $"{RoundBackupPrefix}_round{round - 1:D2}.txt");
                try { File.Delete(prevFile); } catch { }

                // Store filename BEFORE creating backup (so Rewind/Universe can use it immediately)
                _currentRoundBackupFile = fileName;

                // Create new backup (same prefix, same filename every round — overwrites)
                Server.ExecuteCommand($"mp_backup_round_file {RoundBackupPrefix}");

                Console.WriteLine($"[RollTheDice] Round backup: {fileName}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[RollTheDice] Failed to create round backup: {ex.Message}");
            }
        }

        private static int _heartbeatCount;
        public static int DiceTickCounter;
        public void OnHeartbeat()
        {
            _heartbeatCount++;
            if (_heartbeatCount % 128 == 1)
            {
                try
                {
                    // Count dice with active OnTick listeners and players
                    int onTickDice = 0, activePlayers = 0;
                    foreach (var d in _dices)
                    {
                        if (d.Listeners.Contains("OnTick")) onTickDice++;
                        if (d._players.Count > 0) activePlayers++;
                    }
                    var sb = new System.Text.StringBuilder();
                    sb.Append($"{DateTime.Now:HH:mm:ss} <3 #{_heartbeatCount} alive=[");
                    foreach (var p in Utilities.GetPlayers().Where(p => p.IsValid && !p.IsHLTV))
                        sb.Append($"{p.PlayerName}({p.TeamNum}) ");
                    sb.Append($"] dices={_dices.Count} onTickDice={onTickDice} activeDice={activePlayers}");
                    sb.Append($" | tickTest={_tickTest}");
                    File.AppendAllText(Path.Combine(Server.GameDirectory, "csgo/addons/counterstrikesharp/logs/rtd_debug.txt"), sb + "\n");
                }
                catch { }
            }
            _tickTest++; // increment every tick to prove OnTick works
        }
        private static int _tickTest;

        private void OnPlayerButtonsChanged(CCSPlayerController player, PlayerButtons pressed, PlayerButtons released)
        {
            if (!pressed.HasFlag(PlayerButtons.Use)) return;
            if (player == null || !player.IsValid || player.IsHLTV || player.IsBot) return;

            File.AppendAllText(Path.Combine(Server.GameDirectory, "csgo/addons/counterstrikesharp/logs/rtd_debug.txt"),
                $"{DateTime.Now:HH:mm:ss} [E] {player.PlayerName} pressed E\n");

            var cdMessages = new List<string>();
            foreach (var dice in _dices)
            {
                if (dice._players.Contains(player))
                {
                    float cd = dice.GetCooldownRemaining(player);
                    if (cd > 0)
                    {
                        string name = Localizer[$"dice_{dice.ClassName}_name"].Value;
                        cdMessages.Add($"[{name}] {cd:F1}s");
                    }
                }
            }
            if (cdMessages.Count > 0)
                player.PrintToCenterAlert($"⏳ {string.Join("  ", cdMessages)}");
        }
    }
}
