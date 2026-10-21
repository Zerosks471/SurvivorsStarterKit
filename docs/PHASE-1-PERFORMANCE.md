# Phase 1: Performance

Goal: 500 enemies at 60 FPS. The original kit slowed down past 200.

## Results

Measured headless in Godot 4.6.2 .NET with skeleton models loaded. CPU time only, no GPU rendering. Average of 2 runs, 600 frames each.

| Enemies | Original avg frame | New avg frame | Original worst | New worst |
|---|---|---|---|---|
| 200 | 12.2 ms (82 FPS) | 5.3 ms (188 FPS) | 128 ms | 19 ms |
| 500 | 36.9 ms (27 FPS) | 15.9 ms (63 FPS) | 1,035 ms | 34 ms |

Weapon hits per second stayed within 5% of the original, so damage output did not change.

## What changed

| Change | Files | Why |
|---|---|---|
| Spatial grid for enemies | `EnemyGrid.cs`, `EnemyManager.cs` | Weapons look up nearby enemies by grid cell instead of sorting every enemy on every shot |
| One loop moves every enemy | `Enemy.cs`, `EnemyManager.cs`, `GameManager.cs` | Removes two engine-to-C# calls per enemy per frame |
| Enemy physics turned off | `Enemy.cs` | Enemies no longer need collision. Hits come from the grid. This removed the worst frame spikes. |
| Area weapons use the grid | `LifestealAttack.cs`, `SpiritWater.cs`, `FloatingSphereAttack.cs`, `WeaponShapes.cs` | Same hit rules as before. Sizes still come from the collision shapes in each weapon scene. |
| Pooled bullets | `Shooting.cs`, `bullet.tscn` | Bullets are reused, not created per shot. Missed bullets now expire after 4 s. They used to fly forever. |
| Pooled enemies | `Enemy.cs`, `EnemyManager.cs` | Creating a skeleton enemy costs about 8 ms. Dead enemies are hidden and reused. |
| Staggered animation | `Enemy.cs` | Enemies animate every 2nd frame near the player, every 4th frame beyond 20 units |
| Cheaper hits | `Enemy.cs`, `DamageLabelManager.cs` | Direct call instead of a Godot signal per hit. Particle bursts limited to one per 0.3 s per enemy. Max 40 damage numbers on screen. |
| Spawn cap raised to 500 | `GameManager.cs` | `MaxEnemies` constant |

## Fixes found along the way

- Spirit Water cooldown upgrades did nothing. The timer never picked up the new cooldown. Fixed.
- Spirit Water could hit enemies that were already freed. Fixed.

## Debug keys

| Key | Does |
|---|---|
| F3 | Show or hide the overlay: FPS, enemy count, frame times. On by default in debug builds. |
| F4 | Stress test. Spawns 15x faster, one enemy per tick, up to 500. |
| B | Spawn a boss (from the original kit) |

## How to test on your PC

1. Open the project in Godot .NET and press Build.
2. Run the main scene.
3. Press F4 and wait about 35 seconds for 500 enemies.
4. Watch the F3 overlay. Pass: FPS stays at or above 60.

GPU cost was not measured here. If FPS drops on your PC but the overlay's Process and Physics times are low, the GPU is the limit. The next step then is fewer skeleton draw calls, such as simpler models for far enemies.
