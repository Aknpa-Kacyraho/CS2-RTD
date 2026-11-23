# Changelog

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
