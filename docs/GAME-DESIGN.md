# Echo Crypt (working title)

The crypt learns how you fight. Your rites fire when you act. When you fall, you become the crypt's next warden.

| | |
|---|---|
| Setting | A living crypt full of skeletons, using the kit's KayKit dungeon and skeleton art |
| You play | A warden-hunter who fights with rites: spells bound to actions |
| Run | 15 minutes. Bosses at 3, 6, 12. Your Echo at 9:00. Final boss at 15. |
| Feel | Dark loot, closest to Halls of Torment |
| Stack | Godot .NET, C#, Jolt physics, 3D top-down |

## Signature features

None of the five games in [RESEARCH.md](RESEARCH.md) do these.

### 1. The Crypt Learns
The game tracks your damage by tag. Each level-up, the crypt gains resistance to your top tag, capped at 50%. You branch out or push through.

The kit already pairs a player buff with an enemy buff on every upgrade card. Swap the random enemy pick for a counter-pick.

```csharp
var top = _damageByTag.MaxBy(kv => kv.Value).Key;
_resist[top] = Mathf.Min(_resist[top] + 0.15f, 0.5f);
// Card shows: "You: Fire Rite +1 | Crypt: Fire resist 30%"
```

### 2. Rites (trigger weapons)
Each rite has a trigger, an effect, and a tag. A Bone Nova fires on dash. A Blood Ring fires when you get hit. A Frost Lance fires after you stand still for 2 seconds. The kit's 4 spells become the first 4 effects.

```csharp
public enum RiteTrigger { Timer, Dash, Hurt, StandStill, KillStreak }
public enum RiteTag { Bone, Blood, Frost, Spirit, Fire }

[GlobalClass]
public partial class RiteDefinition : Resource {
  [Export] public RiteTrigger Trigger;
  [Export] public float TriggerValue;   // seconds or kills
  [Export] public RiteTag Tag;
  [Export] public PackedScene Effect;   // bolt, nova, orbit, zone
  [Export] public float[] DamageByLevel;
}
```

### 3. Your Echo
When you die, the game saves your build. Next run, at 9:00, your Echo spawns as a boss using your rites against you. Kill it and you keep one of its rites to start your next run. Reuse the boss prefab with your saved rites on the enemy team.

## Feature list

Difficulty: Easy / Medium / Hard. Source game in brackets.

### MVP: prove the loop with one map
| Feature | From | Build |
|---|---|---|
| Move + auto-fire, optional mouse aim | Halls of Torment, Brotato | Easy |
| 15-minute run with boss beats | HoloCure, Soulstone | Easy |
| 5 enemy types: chaser, shooter, charger, bomber, healer | HoloCure, Brotato | Medium |
| Spawn formations: ring, wall, horde rush | HoloCure | Easy |
| XP gems with magnet and auto-merge | Soulstone | Easy |
| Level-up: pick 1 of 3, reroll and banish | Soulstone, HoloCure, Halls of Torment | Easy |
| 6 weapon slots, levels 1–7 | HoloCure, Brotato | Easy |
| Tags with set bonuses at 2+ matching | DRG, Soulstone, Brotato | Medium |
| Pooling + spatial grid | Halls of Torment | Done in Phase 1 |
| Debug spawner | Brotato | Easy |
| Effects opacity slider, enemy warnings drawn above effects | Soulstone, Halls of Torment | Easy |
| Damage numbers toggle and cap | HoloCure | Partly done in Phase 1 |

### v1: depth
| Feature | From | Build |
|---|---|---|
| Shop between boss beats, rising reroll cost, free locks | Brotato, DRG | Easy |
| Weapon fusion: two maxed weapons become one | HoloCure | Easy |
| Weapon branch picks at Lv 3 and 6 | DRG | Medium |
| 4 rule-changing characters | Brotato, Soulstone | Medium |
| Dash with cooldown | Soulstone, HoloCure | Easy |
| Loot runners that flee and drop chests | Brotato, HoloCure | Easy |
| Danger levels 0–5 | Brotato | Easy |
| Keep one item per run | Halls of Torment | Medium |
| Unlocks only, no stat grind | Brotato | Easy |

### Later
| Feature | From | Build |
|---|---|---|
| Adaptive difficulty meter | Halls of Torment | Medium |
| Mineable walls (GridMap with cell HP) | DRG | Hard |
| Extraction timer | DRG | Easy |
| Endless mode | Brotato, Soulstone | Easy |
| Gear slots from bosses | Halls of Torment | Medium |
| Local co-op | Brotato | Hard |

### Easy extras from brainstorming
| Feature | How it plays |
|---|---|
| XP gems hatch | Gems left too long turn into enemies |
| Boss wagers | Bet an item before a boss. Win and it upgrades. Lose and it's gone. |
| Last stand | At 0 HP you get 8 seconds where kills heal you |

### Skip
| Feature | Why |
|---|---|
| Gacha unlocks | Duplicate pulls are a top HoloCure complaint |
| Side minigames | Rated as filler |
| 1000+ trait tiers | Too much to balance alone |
| Uncapped screen effects | Soulstone's top complaint |
| 30-minute runs | Players call them too long |

## MVP content scope
1 map · 3 characters · 8 rites · 12 passive items · 5 enemy types · 2 bosses

## Build plan

Part-time pace. Each phase ends with a check you can play.

| When | Phase | Work | Done when |
|---|---|---|---|
| Day 1 | Set up | Godot .NET, fork the kit, translate text | The kit runs |
| Wk 1–2 | Make it fast | Pooling, one enemy loop, spatial grid | 500 enemies at 60 FPS. **Done:** see [PHASE-1-PERFORMANCE.md](PHASE-1-PERFORMANCE.md) |
| Wk 3 | Data-driven | RiteDefinition, auto-load .tres, drop the enum switch, signals over node paths | A new rite needs only a .tres file |
| Wk 4–5 | Core loop | Death + game over, XP gems, carry-over XP, reroll/banish, dash, boss schedule | A full 15-minute run you can win or lose |
| Wk 6–7 | Rites | Trigger system, 8 rites, tag set bonuses | Two runs with different rites play differently |
| Wk 8 | The Crypt Learns | Tag tracker, counter-pick card, 50% cap | A one-tag build feels pressure by minute 10 |
| Wk 9 | Your Echo | Save build on death, Echo boss, reward | You fight your last build and recognize it |
| Wk 10–12 | Content + polish | 3 characters, 12 items, charger and bomber, effects slider | 5 friends play it |

## Starter kit notes

- Opening the project: the skeleton animation FBX uses the FBX2glTF importer. If Godot asks for an FBX2glTF path, download it from the dialog's link. Switching to the built-in importer breaks enemy scene node paths.
- Still to fix in later phases: no death or game over, no XP gems, upgrade code is an enum switch, hard-coded node paths, French upgrade text, 3 s delay after each upgrade pick, extra XP resets on level-up, the Bomb enemy upgrade does nothing.
