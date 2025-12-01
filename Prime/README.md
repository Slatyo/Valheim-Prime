# Prime - Combat and Stats Engine

Prime is the foundational combat and stats framework for the Valheim mod ecosystem. It provides a unified system for stats, modifiers, abilities, damage calculation, and effects that works for players, creatures, and NPCs.

## Features

- **Stat System**: Define and track any stats (Strength, Health, Armor, etc.)
- **Modifier System**: Apply flat, percentage, or multiplicative bonuses
- **Timed Modifiers**: Buffs and debuffs that expire automatically
- **Stacking Modifiers**: Stackable effects with configurable max stacks
- **Ability System**: Define abilities with cooldowns, costs, and effects
- **Damage Pipeline**: Full damage calculation with crit, armor, resistances
- **Effect System**: DoTs, procs, on-hit effects, on-damage-taken triggers
- **Formula Overrides**: Mods can override any combat formula
- **Events**: Subscribe to stat changes, combat events, ability events
- **Console Commands**: Debug and test everything in-game

## For Mod Developers

### Quick Start

```csharp
using Prime;
using Prime.Stats;
using Prime.Modifiers;
using Prime.Abilities;
using Prime.Effects;
using Prime.Combat;

// ========== STATS ==========

// 1. Register a custom stat (in Awake)
PrimeAPI.Stats.Register(new StatDefinition("MyCustomStat", baseValue: 50f)
{
    MinValue = 0f,
    MaxValue = 100f,
    Category = StatCategory.Offense
});

// 2. Get stat values
float strength = PrimeAPI.Get(player, "Strength");
float mystat = PrimeAPI.Get(player, "MyCustomStat");

// 3. Add modifiers
PrimeAPI.AddModifier(player, new Modifier("sword_bonus", "Strength", ModifierType.Flat, 10f)
{
    Source = "IronSword",
    Order = ModifierOrder.Equipment
});

// 4. Add timed buff
PrimeAPI.AddModifier(player, new Modifier("str_potion", "Strength", ModifierType.Percent, 25f)
{
    Duration = 120f,  // 2 minutes
    Source = "StrengthPotion"
});

// ========== ABILITIES ==========

// 5. Register an ability
PrimeAPI.RegisterAbility(new AbilityDefinition("Fireball")
{
    BaseCooldown = 5f,
    BaseDamage = 50f,
    DamageType = DamageType.Fire,
    ScalingStat = "Intelligence",
    ScalingFactor = 2.5f,
    Cost = new ResourceCost("Eitr", 25f),
    TargetType = AbilityTargetType.Projectile
});

// 6. Grant and use abilities
PrimeAPI.GrantAbility(player, "Fireball");
PrimeAPI.UseAbility(player, "Fireball", target);

// ========== EFFECTS ==========

// 7. Create an on-hit proc effect
var lifesteal = new EffectDefinition("Lifesteal")
{
    Duration = 30f,
    Trigger = EffectTrigger.OnHit,
    ProcChance = 0.25f,  // 25% chance
    OnProc = (owner, target, damageInfo) =>
    {
        float heal = damageInfo.FinalDamage * 0.1f;
        // Heal the owner
    }
};
PrimeAPI.ApplyEffect(player, lifesteal);

// 8. Create an on-damage-taken proc (like "cast frost nova when hit")
var frostNova = new EffectDefinition("FrostNovaProc")
{
    Duration = 60f,
    Trigger = EffectTrigger.OnDamageTaken,
    Cooldown = 10f,  // Can only proc every 10 seconds
    ProcCondition = (owner, attacker, damage) => damage.FinalDamage > 50,
    OnProc = (owner, attacker, damage) =>
    {
        // Cast frost nova centered on owner
        CastFrostNovaAoE(owner);
    }
};
PrimeAPI.ApplyEffect(player, frostNova);

// ========== EVENTS ==========

// 9. Subscribe to events
PrimeEvents.OnStatChanged += (entity, stat, newVal, oldVal) =>
{
    if (stat == "Strength")
        UpdateUI();
};

PrimeEvents.OnCritical += (damageInfo) =>
{
    SpawnCritVFX(damageInfo.HitPoint);
};

PrimeEvents.OnKill += (killer, victim, damageInfo) =>
{
    GiveExperience(killer, victim);
};
```

### Core Stats (Built-in)

Prime registers these stats automatically:

**Attributes**: Strength, Dexterity, Intelligence, Vitality

**Resources**: MaxHealth, MaxStamina, MaxEitr

**Offense**: PhysicalDamage, AttackSpeed, CritChance, CritDamage

**Defense**: Armor, BlockPower

**Resistances**: FireResist, FrostResist, LightningResist, PoisonResist

**Movement**: MoveSpeed

**Utility**: CarryWeight, CooldownReduction

### Stat Calculation Order

```
Base Value
    ↓
+ Flat modifiers (sum)
    ↓
× (1 + Percent modifiers / 100)
    ↓
× Multiply modifiers (product)
    ↓
= Final Value (clamped to min/max)
```

### Modifier Types

- `ModifierType.Flat`: Added directly (+10)
- `ModifierType.Percent`: Added as percentage (+25%)
- `ModifierType.Multiply`: Multiplicative (×1.5)
- `ModifierType.Override`: Sets final value directly

### Modifier Order

Modifiers are applied in order within each type:

```csharp
ModifierOrder.Inherent = 0     // Racial traits
ModifierOrder.Equipment = 100  // Gear stats
ModifierOrder.Default = 200    // Unspecified
ModifierOrder.Buff = 300       // Abilities, potions
ModifierOrder.Enchant = 400    // Enchantments
ModifierOrder.Affix = 500      // Random item affixes
ModifierOrder.SetBonus = 600   // Set bonuses
ModifierOrder.Combat = 700     // Temporary combat effects
ModifierOrder.Debuff = 800     // Penalties
ModifierOrder.Final = 1000     // Final adjustments
```

### Stack Behaviors

- `StackBehavior.Replace`: New replaces old
- `StackBehavior.Ignore`: New is ignored if already exists
- `StackBehavior.Refresh`: Duration refreshed, value stays
- `StackBehavior.Stack`: Stack count increases (value × stacks)
- `StackBehavior.Independent`: Multiple instances allowed

### Damage Pipeline

All damage flows through Prime's pipeline:

```
1. OnPreDamage event (can modify or cancel)
2. Apply attacker stat bonuses
3. Crit check (CritChance stat)
4. Apply crit multiplier (CritDamage stat)
5. Apply target armor (reduces physical)
6. Apply resistances (reduces elemental)
7. OnPostDamage event
8. Trigger on-hit effects (attacker)
9. Trigger on-damage-taken effects (target)
```

### Effect Triggers

```csharp
EffectTrigger.None           // Passive effect
EffectTrigger.OnHit          // When dealing damage
EffectTrigger.OnCrit         // When critting
EffectTrigger.OnKill         // When killing
EffectTrigger.OnDamageTaken  // When taking damage
EffectTrigger.OnBlock        // When blocking
EffectTrigger.OnDodge        // When dodging
EffectTrigger.OnAbilityUse   // When using ability
EffectTrigger.OnLowHealth    // Below health threshold
```

### Formula Overrides

Mods can override Prime's default formulas:

```csharp
// Override crit calculation for Assassin class
PrimeAPI.RegisterFormula<Func<Character, DamageInfo, float>>(
    FormulaIds.CritChance,
    (attacker, damageInfo) =>
    {
        float baseCrit = PrimeAPI.Get(attacker, "CritChance");

        // Assassins get +50% crit from behind
        if (IsAttackingFromBehind(attacker, damageInfo.Target))
            baseCrit += 0.5f;

        return baseCrit;
    },
    "Viking"  // Your mod name
);
```

### Events

```csharp
// Stat events
PrimeEvents.OnStatChanged += (entity, statId, newValue, oldValue) => { };
PrimeEvents.OnModifierAdded += (entity, modifier) => { };
PrimeEvents.OnModifierRemoved += (entity, modifier) => { };
PrimeEvents.OnModifierExpired += (entity, modifier) => { };

// Combat events
PrimeEvents.OnPreDamage += (damageInfo) => { };   // Can modify damage
PrimeEvents.OnPostDamage += (damageInfo) => { };  // After calculation
PrimeEvents.OnCritical += (damageInfo) => { };
PrimeEvents.OnKill += (killer, victim, damageInfo) => { };
PrimeEvents.OnBlock += (blocker, attacker, damageInfo) => { };

// Ability events
PrimeEvents.OnAbilityCastStart += (caster, instance) => { };
PrimeEvents.OnAbilityExecuted += (caster, instance) => { };
PrimeEvents.OnAbilityInterrupted += (caster, instance) => { };
```

## Console Commands

```
# Stats
prime stats              - Show all stats
prime get Strength       - Get specific stat
prime set Strength 25    - Set base value
prime breakdown Strength - Show calculation breakdown
prime registered         - List all registered stats

# Modifiers
prime mod add Strength flat 10        - Add flat modifier
prime mod add Strength percent 25 60  - Add 25% for 60 seconds
prime mod list                        - List all modifiers
prime mod remove <id>                 - Remove by ID
prime mod clear                       - Remove all modifiers

# Abilities
prime ability test                    - Register test abilities
prime ability list                    - List registered abilities
prime ability grant Fireball          - Grant ability to self
prime ability revoke Fireball         - Revoke ability
prime ability use Fireball            - Use ability
prime ability granted                 - List your abilities

# Effects
prime effect test                     - Apply test effects
prime effect list                     - List active effects
prime effect apply <id>               - Apply effect
prime effect remove <id>              - Remove effect
prime effect clear                    - Clear all effects

# Combat testing
prime damage 50                       - Deal 50 physical to self
prime damage 50 fire                  - Deal 50 fire damage to self
```

## Configuration

Config file: `BepInEx/config/com.prime.valheim.cfg`

```ini
[Debug]
DebugLogging = false

[UI]
ShowStatBreakdown = true

[Performance]
ModifierUpdateInterval = 0.1

[Combat]
StrengthScaling = 0.01       # 1% damage per Strength point above 10
DexterityCritBonus = 0.005   # 0.5% crit per Dexterity point above 10
IntelligenceScaling = 0.02   # 2% magic damage per Int point above 10
VitalityHealthBonus = 2      # +2 max HP per Vitality point above 10
```

## Dependencies

- BepInEx 5.4.2333+
- Jotunn 2.26.1+

## Ecosystem

Prime is part of the Valheim mod ecosystem:

- **Viking**: Uses Prime for player classes and talents
- **Denizen**: Uses Prime for creature stats and AI abilities
- **Loot**: Uses Prime for item affixes and legendary effects
- **Enchanting**: Uses Prime for enchantment stat bonuses
- **Rift**: Uses Prime for dungeon scaling and boss mechanics

## Links

- [Source Code](https://github.com/Slatyo/Valheim-Prime)
- [Issues](https://github.com/Slatyo/Valheim-Prime/issues)
