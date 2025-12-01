# Prime

The foundational combat and stats engine for the Valheim mod ecosystem. Provides a unified system for stats, modifiers, abilities, damage calculation, and effects.

## Features

- **Stat System** - Define and track any stats (Strength, Health, Armor, etc.)
- **Modifier System** - Apply flat, percentage, or multiplicative bonuses with stacking
- **Timed Modifiers** - Buffs and debuffs that expire automatically
- **Ability System** - Define abilities with cooldowns, costs, and effects
- **Damage Pipeline** - Full damage calculation with crit, armor, resistances
- **Effect System** - DoTs, procs, on-hit effects, on-damage-taken triggers
- **Formula Overrides** - Mods can override any combat formula
- **Events** - Subscribe to stat changes, combat events, ability events

## Built-in Stats

**Attributes**: Strength, Dexterity, Intelligence, Vitality

**Resources**: MaxHealth, MaxStamina, MaxEitr

**Offense**: PhysicalDamage, AttackSpeed, CritChance, CritDamage

**Defense**: Armor, BlockPower

**Resistances**: FireResist, FrostResist, LightningResist, PoisonResist

**Movement**: MoveSpeed

**Utility**: CarryWeight, CooldownReduction

## Console Commands

```
prime stats              - Show all stats
prime get Strength       - Get specific stat
prime set Strength 25    - Set base value
prime breakdown Strength - Show calculation breakdown

prime mod add Strength flat 10        - Add flat modifier
prime mod add Strength percent 25 60  - Add 25% for 60 seconds
prime mod list                        - List all modifiers
prime mod clear                       - Remove all modifiers

prime ability test       - Register test abilities
prime ability list       - List registered abilities
prime ability grant X    - Grant ability to self
prime ability use X      - Use ability

prime effect test        - Apply test effects
prime effect list        - List active effects
prime effect clear       - Clear all effects

prime damage 50          - Deal 50 physical damage to self
prime damage 50 fire     - Deal 50 fire damage to self
```

## Configuration

Config file: `BepInEx/config/com.prime.valheim.cfg`

```ini
[Debug]
DebugLogging = false

[Combat]
StrengthScaling = 0.01       # 1% damage per Strength above 10
DexterityCritBonus = 0.005   # 0.5% crit per Dexterity above 10
IntelligenceScaling = 0.02   # 2% magic damage per Int above 10
VitalityHealthBonus = 2      # +2 max HP per Vitality above 10
```

## For Mod Developers

Prime is designed as the foundation for other mods. See the [API documentation](https://github.com/Slatyo/Valheim-Prime) for:

- Registering custom stats
- Adding modifiers with stacking behaviors
- Creating abilities with cooldowns and costs
- Implementing on-hit and on-damage-taken effects
- Overriding combat formulas
- Subscribing to events

### Quick Example

```csharp
using Prime;

// Get a stat
float str = PrimeAPI.Get(player, "Strength");

// Add a timed buff
PrimeAPI.AddModifier(player, new Modifier("buff_id", "Strength", ModifierType.Percent, 25f)
{
    Duration = 60f,
    Source = "MyMod"
});

// Subscribe to kills
PrimeEvents.OnKill += (killer, victim, damage) => GiveXP(killer);
```

## Ecosystem

Prime is part of the Valheim mod ecosystem:

- **Viking** - Player classes and talents
- **Denizen** - Creature stats and AI abilities  
- **Loot** - Item affixes and legendary effects
- **Enchanting** - Enchantment stat bonuses
- **Rift** - Dungeon scaling and boss mechanics

## Requirements

- BepInEx 5.4.2333+
- Jotunn 2.26.1+

## Changelog

### 1.0.0
- Initial release
- Stat system with modifiers
- Ability system with cooldowns
- Damage pipeline with crit, armor, resistances
- Effect system with proc triggers
- Combat events
- Console commands for testing
