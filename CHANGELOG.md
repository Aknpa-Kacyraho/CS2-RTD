# Changelog

## 1.4.17 — 2026-11-23

### Fixed

- TOML config reload now uses Tomlyn + `ConfigApplier`, so JSON and TOML both validate/app/rollback atomically. Other formats keep CSS Reload.
- `Izayoi + Heaven` combo renamed from「超越天堂」to「月华天堂」; all 68 combo display names are now unique.
- Add-time `NotifyPlayers` output is suppressed; `AnnounceDiceRarity` is the single reveal. HUD on: one chat + one center; HUD off: rarity chat + description chat.
- `Fate` dawn freeze/respawn/cleanup now uses `MoveLockManager.ReleaseTo(player, "FateDawn", MOVETYPE_WALK)` instead of writing `MoveType`/`m_nActualMoveType` directly.
- Disconnect cleanup now also removes ulong-keyed primitive/string/enum/`HashSet<ulong>` state for the disconnected SteamID. Entity-owned maps are deliberately skipped.

### Added

- `MoveLockManager.ReleaseTo`.
- Tests: TOML parse/apply, unique combo names, Fate source guard, ulong cleanup, entity-map preservation, add-suppression guards. Standard 73; real config 74.

### Not done

- Performance indexing needs live profiling.
- Main class split (2730/2900) is still within budget.
- In-game regression still pending.

## 1.4.16 — 2026-11-23

### Fixed

- JSON config reload is now all-or-nothing. `ConfigApplier` snapshots the live config, applies the fully deserialized candidate, and rolls back already-applied properties if a setter throws. CSS `Reload` deserializes before copying but has no rollback.
- Setter failures are unwrapped from `TargetInvocationException` so the reload error shows the real message.
- TOML configs still use CSS's native Reload; JSON uses the validator + atomic applier.

### Added

- Atomic-apply tests: normal copy, rollback after a throwing setter, JSON parse, JSON apply. Standard 65; real config 66.

### Unchanged

- Config keys, defaults, dice values, commands, broadcasts, TAB, combos.
- Config path and JSON options remain aligned with CSS.

## 1.4.15 — 2026-11-23

### Fixed

- TAB combo tags no longer consume a shared dice on the first matching combo. `ResolveComboNames` now collects every active combo first, then keeps only dice that participate in no combo.
- Shared-dice example: `Adrenaline + Berserker + Overheat` now shows both「狂暴血脉」and「狂热」instead of hiding the second.
- Standalone dice are preserved after the combo names; combo order stays definition order.

### Added

- Shared-dice and standalone-dice combo tests; standard 61, real config 62.

### Unchanged

- All 68 combo pairings, names, effects and values.
- TAB tag format, `[A]+N` compression and `!rtd tab` toggle.
- Duplicate display name「超越天堂」is still collapsed to one tag and remains a known data item.

## 1.4.14 — 2026-11-23

### Fixed

- Dice reveal HUD now uses the same localized `dice_<Class>_player` description as `!rtd status`, instead of the cached `dice.Description` field (which could be English, `Unknown Dice`, or stale).
- Dynamic HUD text (name, tier, description, effects) is HTML-encoded, so `&`, `<` and `>` cannot break the center panel.
- Legendary/combo broadcasts use the undecorated true player name instead of the TAB `[Dice]`-decorated name.
- `Unknown`-prefixed descriptions are omitted from the HUD.

### Added

- `DiceUi.HtmlEncode`.
- Broadcast tests: HTML encoding, localized description, source guards. Standard 59; real config 60.

### Unchanged

- Reveal text, sound, all-server toggle, HUD toggle.
- Dice `NotifyPlayers` chat/center layer (not merged with the rarity reveal).
- Probabilities, effects, durations, commands and config keys.

## 1.4.13 — 2026-11-23

### Changed

- `DiceSynergy.HasPartner` now resolves the partner dice once and scans its holder list instead of allocating a new `Utilities.GetPlayers()` list and re-querying the dice dictionary for every player. Same self/teammate semantics.
- `RefreshCombos` skips the per-dice `OnDiceSetChanged` loop unless a dice actually overrides it (`HasDiceSetChangedOverrides`); currently none do.
- Combo announcements use the undecorated player name via `GetTruePlayerName`, so TAB `[Dice]` prefixes no longer leak into chat.

### Added

- `DiceRegistry` combo guards: self-pair, duplicate unordered pair, empty display name (plus existing unknown dice / duplicate display name).
- Combo tests: 68-entry table, no self/duplicate pair, known duplicate display name, resolution, capability flag, source guards. Standard 56; real config 57.

### Unchanged

- All 68 combo pairings, names, effects and values.
- `ResolveComboNames` greedy display rule and TAB tags.
- Dice probabilities, cooldowns, damage, durations, rewards, commands and config keys.

## 1.4.12 — 2026-11-23

### Fixed

- Config pre-validation now uses CounterStrikeSharp's exact `JsonSerializerOptions` (priority) with a same-value fallback outside a CSS host. Comments stay valid; trailing commas are rejected, matching CSS.
- Config reload and map-overlay reads now use `Config.GetConfigPath()` instead of a hand-built path, so plugin-directory renames cannot make validation read a different file than `PluginConfigExtensions.Reload`.
- JSON pre-validation is skipped for `.toml` configs; CSS keeps owning TOML reload.

### Documentation

- Corrected the previous assumption: CSS `Reload` fully deserializes before copying writable properties, so type errors already keep the old config. The remaining partial-update window is a property setter throwing mid-copy.

### Tests

- Added trailing-comma rejection; standard suite 46, real deployed config 47.

### Unchanged

- Config keys, defaults, dice values, commands, movement scopes and lifecycle cleanup.

## 1.4.11 — 2026-11-23

### Changed

- `MoveLockManager` now keeps a named movement-scope stack per player instead of a set of lock-to-Walk keys. The last scope wins; releasing the last scope restores the captured `MoveType` + `ActualMoveType`.
- Added `Lock(player, key, MoveType_t target)`; the old `Lock(player, key)` still locks to `MOVETYPE_NONE`.
- `Skyline` and `Void` now use the shared scope manager for noclip instead of writing `MoveType`/`m_nActualMoveType` directly.
- `Skyline`, `Void` and `Phoenix` explicitly release their scope on removal even when the pawn is already invalid, so a respawn cannot inherit a stale movement lock.
- `!rtd_health` reports `move_locks={keys}keys/{players}p`.

### Added

- `MoveScopeStack` pure stack (empty, push, update priority, pop, unknown key, clear).
- Movement source guards: Amber/Phoenix/Skyline/Void no longer contain `.MoveType =`.
- Registry/UI/Lifecycle/Movement tests now 45.

### Unchanged

- Flight/void/freeze/revive durations, cooldowns, damage, rewards, probabilities and config keys.
- `Fate` still owns its dawn/darktide MoveType transitions; it was not migrated.

## 1.4.10 — 2026-11-23

### Fixed

- Config reload now pre-validates the full `PluginConfig` shape before CSS mutates the live object. A wrong type (for example `"enabled": "yes"`) keeps the previous config instead of risking a half-applied reload.
- `!givedice` now accepts zero arguments, making the existing available-dice list reachable; `!givedice <player> [dice]` is unchanged.
- Auto-roll no longer removes a player's current dice when the drawable pool is empty; it returns before the destructive replace.

### Added

- `ConfigFileValidator` with comment/trailing-comma/case-insensitive support and unknown-field tolerance.
- 5 regression assertions (config validation x4, `!givedice` arity x1); standard suite 34, plus 1 optional real-config check via `RTD_CONFIG_PATH`.

### Unchanged

- Draw probabilities, weights, cooldowns, damage, durations, rewards, command semantics and config keys.

## 1.4.9 — 2026-11-23

### Fixed

- Valid-controller disconnect now calls the shared `CleanupHolderState` after each dice's `Remove`. A legacy `Remove` that dropped `_players` but left secondary controller-keyed maps behind could no longer be found by `PruneInvalid`; this closes that gap.
- `CleanupHolderState` centralizes `OnHolderRemoved` + generic controller-state cleanup; `RemoveBySteamId`, `PruneInvalid` and the disconnect path all use it.

### Added

- Lifecycle assertion for the shared cleanup API; Registry/UI/Lifecycle tests now 29.

### Unchanged

- No probability, damage, duration, reward, command or config changes.

## 1.4.8 — 2026-11-23

### Fixed

- Disconnect and `PruneInvalid` now run the same cleanup: `OnHolderRemoved` is called for pruned controllers, then a generic pass removes leftover controller-keyed dictionary/set entries.
- Legacy dice that never implemented `OnHolderRemoved` no longer keep stale `CCSPlayerController` keys until round/map reset.
- `Phoenix`: added disconnect cleanup for wings, glows, max-health restoration and all nine per-player maps; glow teardown no longer requires a valid pawn.
- `Amber`: removing the dice now clears the freeze and releases its movement lock immediately.

### Changed

- `Amber` and `Phoenix` now use `MoveLockManager` instead of writing `MoveType`/`m_nActualMoveType` directly. Movement during freeze/float is unchanged; release restores the pre-lock MoveType instead of forcing Walk.

### Added

- Lifecycle assertions in `RollTheDice.RegistryTests` (cleanup overrides, controller-state field scanner, ulong-state exclusion, dictionary cast, shared cleanup API), now 29 assertions total.

### Unchanged

- Draw probabilities, weights, damage, durations, rewards, commands and config keys.
- `Fate`, `Skyline` and `Void` still own their noclip/revive MoveType transitions; they were not migrated in this release.

## 1.4.7 — 2026-11-23

### Added

- `!rtd list`: read-only dice catalog with three modes:
  - no filter: total count and legendary/combo/epic/rare/common distribution plus usage;
  - tier filter (Chinese or English alias): names in four columns, held dice marked `✓`, at most 24 names;
  - keyword filter: matches localized name or `ClassName`, up to 8 detailed lines.
- `!rtd help` lists the new subcommand.
- 7 new localization keys; all color tags verified by tests.

### Changed

- None to draw probabilities, cooldowns, damage, durations, dice effects, existing commands, config keys or TAB/combo rules.
- Registry/UI tests extended to 20 assertions (tier aliases, keyword matching, held marker, color-tag substitution).

## 1.4.6 — 2026-11-23

### Changed

- Command UI now shares `DiceUi`: rarity labels/colors/order, dice names/descriptions, dice lines and column packing live in one place.
- `!rtd help` adds one hint line after the existing commands.
- `!rtd status` shows the held-dice count in the title and sorts by rarity (legendary → combo → epic → rare → common), then name.
- `!givedice` without a target prints localized names in three columns plus a count header instead of 171 single-column class names.
- `!rtd_health` prints four short diagnostic lines instead of one long line.

### Added

- UI assertions in `RollTheDice.RegistryTests` (rarity labels/colors/order, column packing, name fallback, line shape, admin list label), now 16 assertions total.

### Unchanged

- Draw probabilities, cooldowns, damage, durations, dice effects, command arguments, config keys and TAB/combo rules.

## 1.4.5 — 2026-11-23

### Added

- Read-only `DiceRegistry`: startup check of dice types, `DicesConfig` entries, rarity tiers, `dice_*_name` localization keys, combo references and `dicefx.json` profiles. Findings are logged only; draw weights, dice instances and config values are untouched.
- `!rtd_health` reports `registry=ok` or `registry=E{errors}/W{warnings}`.
- `tests/RollTheDice.RegistryTests`: 8 assertions covering all 171 dice types, construction, config alignment, combo references and the known duplicate combo-name warning. CI now runs it.

### Changed

- No player-visible behavior change. Draw probabilities, cooldowns, damage, durations, commands and config keys are identical to v1.4.4.

## 1.4.4 — 2026-11-23

### Changed

- Extracted `PlayerNameService` from `RollTheDice`: original names, TAB refresh timers and known dice/combo prefixes now have a single owner. Main class dropped from ~2851 to ~2668 lines.
- Name/TAB behavior is intentionally unchanged: same 1-second reapply, same `ShowDiceInTab`, same combo/tag stripping, same round-end restore.

### Fixed

- No new player-visible behavior changes in this release; this is an architecture/maintenance release built on v1.4.3.

## 1.4.3 — 2026-11-23

### Fixed

- Disconnect fallback now clears player-scoped Invulnerability/MoveLock/StackingHealth/StackingBonus and health-state dictionaries for 13/14 health dice (Phoenix deferred because its wings/glow entities need separate cleanup).
- `MoveLockManager` records and restores the pre-lock MoveType instead of forcing Walk; `Clear/ClearAll` restore when the controller is still valid.
- `StackingHealth` captures the actual base MaxHealth instead of assuming 100; invalid controllers no longer fall back to SteamID 0.
- `DynamicHandlers` no longer registers the raw damage/tick callback when the safety wrapper fails; registration/deregistration failures are isolated per dice/handler.
- `DiceEffects.OnTick` isolates failures per player with rate-limited logging.
- `ChildGuard.Track` de-duplicates child handles; `FogOfWar` removes only the `_playerVisibility` entity it created.
- Round end now clears MoveLock/Stacking/Damage/Speed managers before the next round.
- Map-config reload diagnostics and broader overlay deserialization fallback.
- TEST/dice documentation now covers all 171 dice and corrected rarity counts.

### Added

- `tests/RollTheDice.ConfigTests`: config merge/overlay/reload assertions.
- `.github/workflows/ci.yml`: restore/build/test/JSON validation for Windows net10.0.

## 1.4.2 — 2026-11-22

### Fixed

- **P0 regression from 1.4.0**: empty-holder short-circuit also froze the CBeam
  teardown queues. FinalJudgment/Excalibur/Supernova now expose `NeedsBackgroundTick`
  while `_fade`/`_fading` is non-empty, so beams drain after round end and the
  CopyExistingEntity crash class cannot return.
- Auto-roll now records one roll; a manual `!rtd` can no longer stack a second dice
  on top of the automatic one.
- Map configs are merged as overlays over the global dice config (scalar overrides
  only); disabling one dice in a map no longer resets the other dice to defaults.
- TAB-name stripping only removes known dice/combo tags, preserving real `[CLAN]` tags.
- Warmup branch sets `RoundActive=false`, so delayed damage/revive cannot run in warmup.
- Disconnect with an already-invalid controller removes holders by SteamID instead of
  relying on round-end prune.
- `!rtd status` reads the localized dice description instead of the last notification
  text.
- Map end removes test sigil groups as well as clearing the list.
- Player config writes merge by SteamID instead of replacing the dictionary.
- Config lifecycle (reload/map overlay/OnConfigParsed/player config) moved to
  `RollTheDice.Config.cs`; main class is ~2.76k lines.

## 1.4.1 — 2026-11-22

### Changed

- Split `RollTheDice.cs` into partial `RollTheDice.Help.cs` and
  `RollTheDice.CheatGuard.cs`; main class is now ~2.7k lines (from ~2.9k).
- `RefreshCombos` only calls `OnDiceSetChanged` for dice held by a player in the
  affected team; empty dice are skipped.
- TAB-name combo resolution uses `_diceByName` instead of scanning all 171 dice for
  each combo entry.

### Added

- `scripts/check-rtd-architecture.ps1`: fails on new raw `new Timer(` in dice classes
  or main class growth past 2900 lines; reports pawn casts and empty catches.

## 1.4.0 — 2026-11-22

Architecture/stability release: every dice timer and callback now has an owner.

### Fixed

- Dice timers: added `ScopedTimer`/`KillTimers`; replaced 33 `new Timer` call sites
  (ThunderChain's two static 0.5s cleanup timers stay raw). `DestroyModules` and round
  end kill every scoped timer, so delayed callbacks cannot outlive a reload/round.
- `SafeOnPlayerTakeDamagePre` returns immediately when the dice has no holder.
- `DynamicHandlers` wraps `OnTick`; a dice without a holder no longer enters its tick.
- TAB-name refresh keeps at most one pending timer per SteamID instead of stacking one
  per dice change.
- `ReloadConfigFromDisk` no longer writes the in-memory config back to disk; map start
  and reload cannot overwrite a manual edit.
- `RoundActive` is initialized on Load/OnMapStart for mid-round hot reloads.
- Map end clears `GameRules` entity caches, `_sigilTests`, and the fx precache set;
  round start clears Reincarnation pending state.

### Performance

- `_diceByName` index + cached drawable/normal pools; `HasDiceActive` is O(1).
- `GetDrawablePool`/`GetNormalPool` no longer allocate on every roll.
- `rtd_health` now reports roundActive, fx profile count, and auto/reminder/TAB timers.

### Diagnostics

- `DynamicHandlers` logs unknown listener/event names and missing methods instead of
  silently disabling a dice feature.

## 1.3.1 — 2026-11-22

### Added

- `!rtd tab`: toggle the dice prefix in the TAB name. Persisted per SteamID in
  `player_configs`, default on. `!rtd status` shows the current switch.
- `!rtd help` lists the new command.

## 1.3.0 — 2026-11-22

### Added

- `!rtd help` (`!rtd ?`) player help card: roll, auto-roll, status, WeaponPaints entry,
  and an admin-only line when the player has `@rollthedice/admin`.
- `!rtd status` (`!rtd info`): every held dice with tier and description, auto-roll
  state, cooldown (rounds/seconds), and rolls used this round.
- The roll HUD now includes the dice description and stays 5 seconds.

### Changed

- Unknown `!rtd <arg>` no longer silently rolls a dice; it prints the help card.
- `!rtd auto` is persisted to the plugin config, so it survives reconnect/reload.
- `combo` dice are labelled **组合** instead of **传说**; the global broadcast uses the
  same label.
- The old every-round global announcement is now a private reminder sent 20 seconds
  after round start, only to alive players who have not rolled.
- “Already rolled” lists every held dice name, not only the first one.
- Player-facing English error replaced with Chinese.

## 1.2.2 — 2026-11-22

### Fixed

- Added `DiceBlueprint.RoundActive`, set false on round end/map end/unload and true on
  round start. Delayed callbacks that can damage or revive (BlackHole's 10s collapse,
  Necromancer's 1s weapon restore) no longer leak into the next round.

### Performance

- `PlayerResolver.FromEntity` now caches pawn-handle → controller for the current
  `Server.TickCount`. Damage hooks do O(1) lookups instead of scanning every player
  twice per dice per hit.

## 1.2.1 — 2026-11-22

### Fixed

- Unload/map-end no longer synchronously flushes thousands of CBeams. That one-frame
  teardown is the known `CopyExistingEntity: missing client entity` crash path.
  Residual beams are left for the engine to clear on changelevel.
- `Supernova.Reset` (round end) no longer calls `Flush`; it uses the frame-budgeted
  teardown queue driven by `OnTick`.
- Every dice damage hook is wrapped by `DynamicHandlers`. Non-player victim or attacker
  entities are rejected before any dice can cast them to `CCSPlayerPawn`. This is a
  coverage fix on top of the v1.2.0 central-hook change.

## 1.2.0 — 2026-11-22

Stability and lifecycle release. Target: CS2 1.41.9.7 / CounterStrikeSharp 1.0.380.

### Fixed

- `rollthedice` admin command had no permission attribute; any player could run
  `!rollthedice disable|reload|createmapconfig`. It now requires `@rollthedice/admin`.
- The central damage hook cast every damaged entity to `CCSPlayerPawn`, including
  props, the world and grenades. It now resolves players by entity handle
  (`PlayerResolver.FromEntity`), which avoids pawn-schema reads on non-player entities.
- `RollTheDiceEveryXSeconds` scheduled another timer from inside its own callback and
  started a new chain every round. It is now one repeating timer with a stored handle,
  killed on round end, map end and unload.
- Dice holders were tracked by controller reference. Add now de-duplicates by SteamID;
  remove resolves holders by SteamID; invalid controllers are pruned on disconnect.
- `rollthedice reload` did not rebuild dice, so per-dice values and map overrides stayed
  stale. Reload and map start now run `DestroyModules → LoadMapConfig → InitializeModules`.
- `Config.Enabled=false` did not stop automatic rolling. Round start/freeze-end now
  check the global and map-level switch.
- Unload called `ReloadConfigFromDisk()`, which writes the in-memory config back to disk
  and could overwrite manual edits. Unload no longer touches config files.
- A single dice constructor failure could leave the plugin permanently half-initialized.
  Module construction is now collected and committed atomically; failures are logged.

### Added

- `!rtd_health` admin command: version, map, enabled state, dice count, holder count,
  round state and effects switch.
- Startup summary line in the server console.
- `VERSION` file (single release version for tags and docs).

### Notes

- `!rollthedice reload` rebuilds all dice, so current dice effects are cleared. Run it
  between rounds.
