# Changelog

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
