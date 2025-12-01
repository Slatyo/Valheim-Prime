using System;
using System.Collections.Generic;
using Prime.Stats;
using Prime.Modifiers;
using Prime.Events;
using Prime.Abilities;
using Prime.Combat;
using Prime.Effects;

namespace Prime
{
    /// <summary>
    /// Main API for the Prime combat and stats engine.
    /// This is the primary interface for other mods to interact with Prime.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Prime provides a unified stat and modifier system for Valheim mods.
    /// All stat access and modification should go through this API.
    /// </para>
    /// <para>
    /// <b>Quick Start:</b>
    /// <code>
    /// // 1. Register a stat (in your mod's Awake)
    /// PrimeAPI.Stats.Register(new StatDefinition("Strength", baseValue: 10f));
    ///
    /// // 2. Get a stat value
    /// float str = PrimeAPI.Get(player, "Strength");
    ///
    /// // 3. Add a modifier
    /// PrimeAPI.AddModifier(player, new Modifier("buff_str", "Strength", ModifierType.Flat, 5f));
    ///
    /// // 4. Subscribe to changes
    /// PrimeEvents.OnStatChanged += (entity, stat, newVal, oldVal) => { };
    /// </code>
    /// </para>
    /// </remarks>
    public static class PrimeAPI
    {
        /// <summary>
        /// Access to the stat registry for registering new stat definitions.
        /// </summary>
        /// <example>
        /// <code>
        /// PrimeAPI.Stats.Register(new StatDefinition("CritChance")
        /// {
        ///     BaseValue = 0.05f,
        ///     MinValue = 0f,
        ///     MaxValue = 1f,
        ///     Category = StatCategory.Offense,
        ///     DisplayType = StatDisplayType.Percent
        /// });
        /// </code>
        /// </example>
        public static StatRegistry Stats => StatRegistry.Instance;

        /// <summary>
        /// Access to the entity manager for advanced entity operations.
        /// Most use cases don't need this - use the convenience methods below instead.
        /// </summary>
        public static Core.EntityManager Entities => Core.EntityManager.Instance;

        // ==================== STAT READING ====================

        /// <summary>
        /// Gets the final calculated value of a stat for an entity.
        /// </summary>
        /// <param name="entity">The entity (Player, Character, etc.)</param>
        /// <param name="statId">The stat ID</param>
        /// <returns>The final stat value including all modifiers, or 0 if not found</returns>
        /// <example>
        /// <code>
        /// float strength = PrimeAPI.Get(player, "Strength");
        /// float armor = PrimeAPI.Get(creature, "Armor");
        /// </code>
        /// </example>
        public static float Get(object entity, string statId)
        {
            var container = Entities.Get(entity);
            return container?.Get(statId) ?? 0f;
        }

        /// <summary>
        /// Gets the base value of a stat (without modifiers).
        /// </summary>
        /// <param name="entity">The entity</param>
        /// <param name="statId">The stat ID</param>
        /// <returns>The base stat value</returns>
        public static float GetBase(object entity, string statId)
        {
            var container = Entities.Get(entity);
            return container?.GetBase(statId) ?? 0f;
        }

        /// <summary>
        /// Sets the base value of a stat.
        /// </summary>
        /// <param name="entity">The entity</param>
        /// <param name="statId">The stat ID</param>
        /// <param name="value">The new base value</param>
        /// <example>
        /// <code>
        /// // Set a creature's base strength
        /// PrimeAPI.SetBase(creature, "Strength", 25f);
        /// </code>
        /// </example>
        public static void SetBase(object entity, string statId, float value)
        {
            var container = Entities.GetOrCreate(entity);
            container.SetBase(statId, value);
        }

        /// <summary>
        /// Gets all stat values for an entity.
        /// </summary>
        /// <param name="entity">The entity</param>
        /// <returns>Dictionary of stat ID to final value</returns>
        public static Dictionary<string, float> GetAll(object entity)
        {
            var result = new Dictionary<string, float>(StringComparer.OrdinalIgnoreCase);
            var container = Entities.Get(entity);

            if (container != null)
            {
                foreach (var statId in Stats.GetAllIds())
                {
                    result[statId] = container.Get(statId);
                }
            }

            return result;
        }

        /// <summary>
        /// Gets a detailed breakdown of how a stat is calculated.
        /// Useful for tooltips and debugging.
        /// </summary>
        /// <param name="entity">The entity</param>
        /// <param name="statId">The stat ID</param>
        /// <returns>Breakdown showing base value and all modifiers</returns>
        /// <example>
        /// <code>
        /// var breakdown = PrimeAPI.GetBreakdown(player, "Strength");
        /// Debug.Log(breakdown.ToString());
        /// // Output:
        /// // Strength: 18.00
        /// //   Base: 10.00
        /// //   +5 (Sword)
        /// //   +30% (Potion)
        /// </code>
        /// </example>
        public static StatBreakdown GetBreakdown(object entity, string statId)
        {
            var container = Entities.Get(entity);
            return container?.GetBreakdown(statId) ?? new StatBreakdown
            {
                StatId = statId,
                BaseValue = 0,
                FinalValue = 0,
                Modifiers = Array.Empty<Modifier>()
            };
        }

        // ==================== MODIFIERS ====================

        /// <summary>
        /// Adds a modifier to an entity's stat.
        /// </summary>
        /// <param name="entity">The entity</param>
        /// <param name="modifier">The modifier to add</param>
        /// <returns>True if added successfully</returns>
        /// <example>
        /// <code>
        /// // Flat bonus from equipment
        /// PrimeAPI.AddModifier(player, new Modifier("sword_str", "Strength", ModifierType.Flat, 5f)
        /// {
        ///     Source = "IronSword",
        ///     Order = ModifierOrder.Equipment
        /// });
        ///
        /// // Timed percentage buff
        /// PrimeAPI.AddModifier(player, new Modifier("potion_str", "Strength", ModifierType.Percent, 25f)
        /// {
        ///     Duration = 120f,  // 2 minutes
        ///     Source = "StrengthPotion"
        /// });
        ///
        /// // Stackable debuff
        /// PrimeAPI.AddModifier(target, new Modifier("bleed_dmg", "Damage", ModifierType.Flat, -2f)
        /// {
        ///     StackBehavior = StackBehavior.Stack,
        ///     MaxStacks = 5,
        ///     Duration = 6f
        /// });
        /// </code>
        /// </example>
        public static bool AddModifier(object entity, Modifier modifier)
        {
            var container = Entities.GetOrCreate(entity);
            return container.AddModifier(modifier);
        }

        /// <summary>
        /// Removes a modifier by its ID.
        /// </summary>
        /// <param name="entity">The entity</param>
        /// <param name="modifierId">The modifier ID to remove</param>
        /// <returns>True if removed</returns>
        public static bool RemoveModifier(object entity, string modifierId)
        {
            var container = Entities.Get(entity);
            return container?.RemoveModifier(modifierId) ?? false;
        }

        /// <summary>
        /// Removes all modifiers from a specific source.
        /// </summary>
        /// <param name="entity">The entity</param>
        /// <param name="source">The source to remove (e.g., item name, buff name)</param>
        /// <returns>Number of modifiers removed</returns>
        /// <example>
        /// <code>
        /// // Remove all modifiers from an unequipped sword
        /// PrimeAPI.RemoveModifiersFromSource(player, "IronSword");
        /// </code>
        /// </example>
        public static int RemoveModifiersFromSource(object entity, string source)
        {
            var container = Entities.Get(entity);
            return container?.RemoveModifiersFromSource(source) ?? 0;
        }

        /// <summary>
        /// Gets all modifiers for a specific stat.
        /// </summary>
        /// <param name="entity">The entity</param>
        /// <param name="statId">The stat ID</param>
        /// <returns>List of modifiers</returns>
        public static IReadOnlyList<Modifier> GetModifiers(object entity, string statId)
        {
            var container = Entities.Get(entity);
            return container?.GetModifiers(statId) ?? Array.Empty<Modifier>();
        }

        /// <summary>
        /// Gets all modifiers on an entity across all stats.
        /// </summary>
        /// <param name="entity">The entity</param>
        /// <returns>All modifiers</returns>
        public static IEnumerable<Modifier> GetAllModifiers(object entity)
        {
            var container = Entities.Get(entity);
            return container?.GetAllModifiers() ?? Array.Empty<Modifier>();
        }

        /// <summary>
        /// Clears all modifiers from an entity.
        /// </summary>
        /// <param name="entity">The entity</param>
        public static void ClearAllModifiers(object entity)
        {
            var container = Entities.Get(entity);
            container?.ClearAllModifiers();
        }

        // ==================== CONVENIENCE METHODS ====================

        /// <summary>
        /// Checks if an entity has any Prime stats registered.
        /// </summary>
        /// <param name="entity">The entity to check</param>
        /// <returns>True if the entity has a StatContainer</returns>
        public static bool HasStats(object entity)
        {
            return Entities.Has(entity);
        }

        /// <summary>
        /// Initializes an entity with Prime stats.
        /// Usually called automatically when first accessing stats.
        /// </summary>
        /// <param name="entity">The entity to initialize</param>
        /// <returns>The entity's StatContainer</returns>
        public static StatContainer InitializeEntity(object entity)
        {
            return Entities.GetOrCreate(entity);
        }

        /// <summary>
        /// Removes an entity from Prime tracking.
        /// Call this when an entity is destroyed.
        /// </summary>
        /// <param name="entity">The entity to remove</param>
        public static void RemoveEntity(object entity)
        {
            Entities.Remove(entity);
        }

        // ==================== HELPER METHODS ====================

        /// <summary>
        /// Applies a temporary flat modifier that expires after a duration.
        /// Convenience method for common buff/debuff pattern.
        /// </summary>
        /// <param name="entity">The entity</param>
        /// <param name="statId">The stat to modify</param>
        /// <param name="value">The flat value to add</param>
        /// <param name="duration">Duration in seconds</param>
        /// <param name="source">Optional source name</param>
        /// <returns>The modifier ID</returns>
        /// <example>
        /// <code>
        /// // Apply a +10 Strength buff for 30 seconds
        /// string modId = PrimeAPI.ApplyTimedFlat(player, "Strength", 10f, 30f, "WarCry");
        /// </code>
        /// </example>
        public static string ApplyTimedFlat(object entity, string statId, float value, float duration, string source = null)
        {
            string id = $"{source ?? "buff"}_{statId}_{Guid.NewGuid():N}";
            var modifier = new Modifier(id, statId, ModifierType.Flat, value)
            {
                Duration = duration,
                Source = source,
                Order = ModifierOrder.Buff
            };
            AddModifier(entity, modifier);
            return id;
        }

        /// <summary>
        /// Applies a temporary percentage modifier that expires after a duration.
        /// </summary>
        /// <param name="entity">The entity</param>
        /// <param name="statId">The stat to modify</param>
        /// <param name="percent">The percentage to add (e.g., 25 for +25%)</param>
        /// <param name="duration">Duration in seconds</param>
        /// <param name="source">Optional source name</param>
        /// <returns>The modifier ID</returns>
        public static string ApplyTimedPercent(object entity, string statId, float percent, float duration, string source = null)
        {
            string id = $"{source ?? "buff"}_{statId}_{Guid.NewGuid():N}";
            var modifier = new Modifier(id, statId, ModifierType.Percent, percent)
            {
                Duration = duration,
                Source = source,
                Order = ModifierOrder.Buff
            };
            AddModifier(entity, modifier);
            return id;
        }

        /// <summary>
        /// Applies or refreshes a stackable modifier.
        /// </summary>
        /// <param name="entity">The entity</param>
        /// <param name="modifierId">The modifier ID (used for stacking)</param>
        /// <param name="statId">The stat to modify</param>
        /// <param name="valuePerStack">Value per stack</param>
        /// <param name="maxStacks">Maximum stacks</param>
        /// <param name="duration">Duration (refreshed on each stack)</param>
        /// <param name="source">Optional source name</param>
        /// <returns>True if applied/stacked</returns>
        /// <example>
        /// <code>
        /// // Apply stacking bleed that does 3 damage per stack, up to 5 stacks
        /// PrimeAPI.ApplyStackingModifier(target, "bleed", "DamageOverTime", 3f, 5, 6f, "BleedingWound");
        /// </code>
        /// </example>
        public static bool ApplyStackingModifier(object entity, string modifierId, string statId,
            float valuePerStack, int maxStacks, float duration, string source = null)
        {
            var modifier = new Modifier(modifierId, statId, ModifierType.Flat, valuePerStack)
            {
                Duration = duration,
                Source = source,
                StackBehavior = StackBehavior.Stack,
                MaxStacks = maxStacks,
                Order = ModifierOrder.Debuff
            };
            return AddModifier(entity, modifier);
        }

        // ==================== ABILITIES ====================

        /// <summary>
        /// Access to the ability registry for registering new abilities.
        /// </summary>
        public static AbilityRegistry Abilities => AbilityRegistry.Instance;

        /// <summary>
        /// Ability managers per entity.
        /// </summary>
        private static readonly Dictionary<Character, EntityAbilities> _entityAbilities =
            new Dictionary<Character, EntityAbilities>();

        /// <summary>
        /// Registers a new ability definition.
        /// </summary>
        /// <param name="ability">The ability to register</param>
        /// <returns>True if registered</returns>
        /// <example>
        /// <code>
        /// PrimeAPI.RegisterAbility(new AbilityDefinition("Fireball")
        /// {
        ///     BaseCooldown = 5f,
        ///     BaseDamage = 50f,
        ///     DamageType = DamageType.Fire,
        ///     ScalingStat = "Intelligence",
        ///     ScalingFactor = 2.5f
        /// });
        /// </code>
        /// </example>
        public static bool RegisterAbility(AbilityDefinition ability)
        {
            return Abilities.Register(ability);
        }

        /// <summary>
        /// Gets an ability definition by ID.
        /// </summary>
        public static AbilityDefinition GetAbility(string abilityId)
        {
            return Abilities.Get(abilityId);
        }

        /// <summary>
        /// Grants an ability to a character.
        /// </summary>
        /// <param name="character">The character</param>
        /// <param name="abilityId">The ability to grant</param>
        /// <returns>True if granted</returns>
        public static bool GrantAbility(Character character, string abilityId)
        {
            if (character == null)
                return false;

            if (!_entityAbilities.TryGetValue(character, out var abilities))
            {
                abilities = new EntityAbilities(character);
                _entityAbilities[character] = abilities;
            }

            return abilities.Grant(abilityId);
        }

        /// <summary>
        /// Revokes an ability from a character.
        /// </summary>
        public static bool RevokeAbility(Character character, string abilityId)
        {
            if (character == null)
                return false;

            if (!_entityAbilities.TryGetValue(character, out var abilities))
                return false;

            return abilities.Revoke(abilityId);
        }

        /// <summary>
        /// Uses an ability.
        /// </summary>
        /// <param name="character">The caster</param>
        /// <param name="abilityId">The ability to use</param>
        /// <param name="target">Optional target</param>
        /// <returns>True if ability was used</returns>
        public static bool UseAbility(Character character, string abilityId, Character target = null)
        {
            if (character == null)
                return false;

            if (!_entityAbilities.TryGetValue(character, out var abilities))
                return false;

            return abilities.TryUse(abilityId, target);
        }

        /// <summary>
        /// Gets all abilities granted to a character.
        /// </summary>
        public static IEnumerable<AbilityInstance> GetGrantedAbilities(Character character)
        {
            if (character == null)
                return Array.Empty<AbilityInstance>();

            if (!_entityAbilities.TryGetValue(character, out var abilities))
                return Array.Empty<AbilityInstance>();

            return abilities.GetAll();
        }

        /// <summary>
        /// Checks if a character has an ability.
        /// </summary>
        public static bool HasAbility(Character character, string abilityId)
        {
            if (character == null)
                return false;

            if (!_entityAbilities.TryGetValue(character, out var abilities))
                return false;

            return abilities.Has(abilityId);
        }

        // ==================== COMBAT ====================

        /// <summary>
        /// Deals damage through Prime's combat pipeline.
        /// </summary>
        /// <param name="attacker">The attacker (can be null for environmental damage)</param>
        /// <param name="target">The target</param>
        /// <param name="damageType">Type of damage</param>
        /// <param name="amount">Damage amount</param>
        /// <param name="canCrit">Can this damage crit?</param>
        /// <returns>Final damage dealt</returns>
        public static float DealDamage(Character attacker, Character target, DamageType damageType, float amount, bool canCrit = true)
        {
            return CombatManager.DealDirectDamage(attacker, target, damageType, amount, canCrit);
        }

        /// <summary>
        /// Deals true damage (ignores armor and resistances).
        /// </summary>
        public static float DealTrueDamage(Character attacker, Character target, float amount)
        {
            return CombatManager.DealTrueDamage(attacker, target, amount);
        }

        /// <summary>
        /// Applies a damage over time effect.
        /// </summary>
        /// <param name="attacker">Who applied the DoT</param>
        /// <param name="target">Target of the DoT</param>
        /// <param name="damageType">Type of damage</param>
        /// <param name="damagePerTick">Damage per tick</param>
        /// <param name="duration">Total duration</param>
        /// <param name="tickInterval">Seconds between ticks</param>
        /// <param name="dotId">Unique ID for this DoT (for stacking)</param>
        public static void ApplyDoT(Character attacker, Character target, DamageType damageType,
            float damagePerTick, float duration, float tickInterval, string dotId)
        {
            CombatManager.ApplyDoT(attacker, target, damageType, damagePerTick, duration, tickInterval, dotId);
        }

        // ==================== EFFECTS ====================

        /// <summary>
        /// Applies an effect to a character.
        /// </summary>
        /// <param name="target">The target</param>
        /// <param name="effect">The effect to apply</param>
        /// <returns>The effect instance, or null if rejected</returns>
        public static EffectInstance ApplyEffect(Character target, EffectDefinition effect)
        {
            return EffectManager.ApplyEffect(target, effect);
        }

        /// <summary>
        /// Removes an effect from a character.
        /// </summary>
        public static bool RemoveEffect(Character target, string effectId)
        {
            return EffectManager.RemoveEffect(target, effectId);
        }

        /// <summary>
        /// Checks if a character has an effect.
        /// </summary>
        public static bool HasEffect(Character target, string effectId)
        {
            return EffectManager.HasEffect(target, effectId);
        }

        /// <summary>
        /// Gets all active effects on a character.
        /// </summary>
        public static IEnumerable<EffectInstance> GetEffects(Character target)
        {
            return EffectManager.GetEffects(target);
        }

        /// <summary>
        /// Dispels effects from a character.
        /// </summary>
        /// <param name="target">The target</param>
        /// <param name="dispelBuffs">Remove beneficial effects?</param>
        /// <param name="dispelDebuffs">Remove harmful effects?</param>
        /// <returns>Number of effects removed</returns>
        public static int Dispel(Character target, bool dispelBuffs = true, bool dispelDebuffs = false)
        {
            return EffectManager.Dispel(target, dispelBuffs, dispelDebuffs);
        }

        // ==================== FORMULA OVERRIDES ====================

        /// <summary>
        /// Registers a custom combat formula.
        /// </summary>
        /// <typeparam name="T">The delegate type</typeparam>
        /// <param name="formulaId">The formula ID (use FormulaIds constants)</param>
        /// <param name="formula">The formula implementation</param>
        /// <param name="source">Your mod name for logging</param>
        public static void RegisterFormula<T>(string formulaId, T formula, string source = null) where T : Delegate
        {
            FormulaRegistry.Register(formulaId, formula, source);
        }

        /// <summary>
        /// Gets a registered formula.
        /// </summary>
        public static T GetFormula<T>(string formulaId) where T : Delegate
        {
            return FormulaRegistry.Get<T>(formulaId);
        }

        // ==================== INTERNAL CLEANUP ====================

        /// <summary>
        /// Clears ability tracking for a character.
        /// Called internally when character is destroyed.
        /// </summary>
        internal static void CleanupCharacterAbilities(Character character)
        {
            if (character != null)
            {
                _entityAbilities.Remove(character);
            }
        }
    }
}
