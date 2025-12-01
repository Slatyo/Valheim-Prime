using System;
using Prime.Modifiers;
using Prime.Abilities;
using Prime.Combat;

namespace Prime.Events
{
    /// <summary>
    /// Central event hub for the Prime system.
    /// Subscribe to these events to react to stat, modifier, combat, and ability changes.
    /// </summary>
    /// <remarks>
    /// All events are static and fire synchronously.
    /// Always unsubscribe when your mod is unloaded to prevent memory leaks.
    /// </remarks>
    public static class PrimeEvents
    {
        // ==================== STAT EVENTS ====================

        /// <summary>
        /// Fired when a stat's calculated value changes.
        /// Parameters: entity, statId, newValue, oldValue
        /// </summary>
        public static event Action<object, string, float, float> OnStatChanged;

        /// <summary>
        /// Fired when an entity's base stat value is modified.
        /// Parameters: entity, statId, newBase, oldBase
        /// </summary>
        public static event Action<object, string, float, float> OnBaseStatChanged;

        // ==================== MODIFIER EVENTS ====================

        /// <summary>
        /// Fired when a modifier is added to an entity.
        /// Parameters: entity, modifier
        /// </summary>
        public static event Action<object, Modifier> OnModifierAdded;

        /// <summary>
        /// Fired when a modifier is removed from an entity.
        /// Parameters: entity, modifier
        /// </summary>
        public static event Action<object, Modifier> OnModifierRemoved;

        /// <summary>
        /// Fired when a timed modifier expires naturally.
        /// Parameters: entity, modifier
        /// </summary>
        public static event Action<object, Modifier> OnModifierExpired;

        /// <summary>
        /// Fired when a stackable modifier gains a stack.
        /// Parameters: entity, modifier
        /// </summary>
        public static event Action<object, Modifier> OnModifierStacked;

        // ==================== ENTITY EVENTS ====================

        /// <summary>
        /// Fired when a StatContainer is created for an entity.
        /// Parameters: entity, container
        /// </summary>
        public static event Action<object, Stats.StatContainer> OnEntityRegistered;

        /// <summary>
        /// Fired when an entity's StatContainer is removed.
        /// Parameters: entity
        /// </summary>
        public static event Action<object> OnEntityUnregistered;

        // ==================== REGISTRY EVENTS ====================

        /// <summary>
        /// Fired when a new stat definition is registered.
        /// Parameters: statDefinition
        /// </summary>
        public static event Action<Stats.StatDefinition> OnStatRegistered;

        // ==================== COMBAT EVENTS ====================

        /// <summary>
        /// Fired before damage calculation. Can modify or cancel damage.
        /// Parameters: damageInfo
        /// </summary>
        public static event Action<DamageInfo> OnPreDamage;

        /// <summary>
        /// Fired after damage calculation, before it's applied.
        /// Parameters: damageInfo
        /// </summary>
        public static event Action<DamageInfo> OnPostDamage;

        /// <summary>
        /// Fired when a critical hit occurs.
        /// Parameters: damageInfo
        /// </summary>
        public static event Action<DamageInfo> OnCritical;

        /// <summary>
        /// Fired when an entity is killed.
        /// Parameters: killer, victim, damageInfo
        /// </summary>
        public static event Action<Character, Character, DamageInfo> OnKill;

        /// <summary>
        /// Fired when damage is blocked.
        /// Parameters: blocker, attacker, damageInfo
        /// </summary>
        public static event Action<Character, Character, DamageInfo> OnBlock;

        /// <summary>
        /// Fired when an entity is staggered.
        /// Parameters: target, attacker
        /// </summary>
        public static event Action<Character, Character> OnStagger;

        // ==================== ABILITY EVENTS ====================

        /// <summary>
        /// Fired when an ability is registered.
        /// Parameters: abilityDefinition
        /// </summary>
        public static event Action<AbilityDefinition> OnAbilityRegistered;

        /// <summary>
        /// Fired when an ability is granted to an entity.
        /// Parameters: entity, abilityInstance
        /// </summary>
        public static event Action<Character, AbilityInstance> OnAbilityGranted;

        /// <summary>
        /// Fired when an ability is revoked from an entity.
        /// Parameters: entity, abilityInstance
        /// </summary>
        public static event Action<Character, AbilityInstance> OnAbilityRevoked;

        /// <summary>
        /// Fired when an ability starts casting.
        /// Parameters: caster, abilityInstance
        /// </summary>
        public static event Action<Character, AbilityInstance> OnAbilityCastStart;

        /// <summary>
        /// Fired when an ability is executed (finishes casting).
        /// Parameters: caster, abilityInstance
        /// </summary>
        public static event Action<Character, AbilityInstance> OnAbilityExecuted;

        /// <summary>
        /// Fired when an ability cast is interrupted.
        /// Parameters: caster, abilityInstance
        /// </summary>
        public static event Action<Character, AbilityInstance> OnAbilityInterrupted;

        /// <summary>
        /// Fired when an ability's cooldown completes.
        /// Parameters: caster, abilityInstance
        /// </summary>
        public static event Action<Character, AbilityInstance> OnAbilityCooldownComplete;

        // ==================== INTERNAL RAISERS ====================

        internal static void RaiseStatChanged(object entity, string statId, float newValue, float oldValue)
        {
            try
            {
                OnStatChanged?.Invoke(entity, statId, newValue, oldValue);
            }
            catch (Exception ex)
            {
                Plugin.Log?.LogError($"[Prime] Error in OnStatChanged handler: {ex}");
            }
        }

        internal static void RaiseBaseStatChanged(object entity, string statId, float newBase, float oldBase)
        {
            try
            {
                OnBaseStatChanged?.Invoke(entity, statId, newBase, oldBase);
            }
            catch (Exception ex)
            {
                Plugin.Log?.LogError($"[Prime] Error in OnBaseStatChanged handler: {ex}");
            }
        }

        internal static void RaiseModifierAdded(object entity, Modifier modifier)
        {
            try
            {
                OnModifierAdded?.Invoke(entity, modifier);
            }
            catch (Exception ex)
            {
                Plugin.Log?.LogError($"[Prime] Error in OnModifierAdded handler: {ex}");
            }
        }

        internal static void RaiseModifierRemoved(object entity, Modifier modifier)
        {
            try
            {
                OnModifierRemoved?.Invoke(entity, modifier);
            }
            catch (Exception ex)
            {
                Plugin.Log?.LogError($"[Prime] Error in OnModifierRemoved handler: {ex}");
            }
        }

        internal static void RaiseModifierExpired(object entity, Modifier modifier)
        {
            try
            {
                OnModifierExpired?.Invoke(entity, modifier);
            }
            catch (Exception ex)
            {
                Plugin.Log?.LogError($"[Prime] Error in OnModifierExpired handler: {ex}");
            }
        }

        internal static void RaiseModifierStacked(object entity, Modifier modifier)
        {
            try
            {
                OnModifierStacked?.Invoke(entity, modifier);
            }
            catch (Exception ex)
            {
                Plugin.Log?.LogError($"[Prime] Error in OnModifierStacked handler: {ex}");
            }
        }

        internal static void RaiseEntityRegistered(object entity, Stats.StatContainer container)
        {
            try
            {
                OnEntityRegistered?.Invoke(entity, container);
            }
            catch (Exception ex)
            {
                Plugin.Log?.LogError($"[Prime] Error in OnEntityRegistered handler: {ex}");
            }
        }

        internal static void RaiseEntityUnregistered(object entity)
        {
            try
            {
                OnEntityUnregistered?.Invoke(entity);
            }
            catch (Exception ex)
            {
                Plugin.Log?.LogError($"[Prime] Error in OnEntityUnregistered handler: {ex}");
            }
        }

        internal static void RaiseStatRegistered(Stats.StatDefinition definition)
        {
            try
            {
                OnStatRegistered?.Invoke(definition);
            }
            catch (Exception ex)
            {
                Plugin.Log?.LogError($"[Prime] Error in OnStatRegistered handler: {ex}");
            }
        }

        // ==================== COMBAT EVENT RAISERS ====================

        internal static void RaiseOnPreDamage(DamageInfo damageInfo)
        {
            try
            {
                OnPreDamage?.Invoke(damageInfo);
            }
            catch (Exception ex)
            {
                Plugin.Log?.LogError($"[Prime] Error in OnPreDamage handler: {ex}");
            }
        }

        internal static void RaiseOnPostDamage(DamageInfo damageInfo)
        {
            try
            {
                OnPostDamage?.Invoke(damageInfo);
            }
            catch (Exception ex)
            {
                Plugin.Log?.LogError($"[Prime] Error in OnPostDamage handler: {ex}");
            }
        }

        internal static void RaiseOnCritical(DamageInfo damageInfo)
        {
            try
            {
                OnCritical?.Invoke(damageInfo);
            }
            catch (Exception ex)
            {
                Plugin.Log?.LogError($"[Prime] Error in OnCritical handler: {ex}");
            }
        }

        internal static void RaiseOnKill(Character killer, Character victim, DamageInfo damageInfo)
        {
            try
            {
                OnKill?.Invoke(killer, victim, damageInfo);
            }
            catch (Exception ex)
            {
                Plugin.Log?.LogError($"[Prime] Error in OnKill handler: {ex}");
            }
        }

        internal static void RaiseOnBlock(Character blocker, Character attacker, DamageInfo damageInfo)
        {
            try
            {
                OnBlock?.Invoke(blocker, attacker, damageInfo);
            }
            catch (Exception ex)
            {
                Plugin.Log?.LogError($"[Prime] Error in OnBlock handler: {ex}");
            }
        }

        internal static void RaiseOnStagger(Character target, Character attacker)
        {
            try
            {
                OnStagger?.Invoke(target, attacker);
            }
            catch (Exception ex)
            {
                Plugin.Log?.LogError($"[Prime] Error in OnStagger handler: {ex}");
            }
        }

        // ==================== ABILITY EVENT RAISERS ====================

        internal static void RaiseAbilityRegistered(AbilityDefinition ability)
        {
            try
            {
                OnAbilityRegistered?.Invoke(ability);
            }
            catch (Exception ex)
            {
                Plugin.Log?.LogError($"[Prime] Error in OnAbilityRegistered handler: {ex}");
            }
        }

        internal static void RaiseAbilityGranted(Character entity, AbilityInstance instance)
        {
            try
            {
                OnAbilityGranted?.Invoke(entity, instance);
            }
            catch (Exception ex)
            {
                Plugin.Log?.LogError($"[Prime] Error in OnAbilityGranted handler: {ex}");
            }
        }

        internal static void RaiseAbilityRevoked(Character entity, AbilityInstance instance)
        {
            try
            {
                OnAbilityRevoked?.Invoke(entity, instance);
            }
            catch (Exception ex)
            {
                Plugin.Log?.LogError($"[Prime] Error in OnAbilityRevoked handler: {ex}");
            }
        }

        internal static void RaiseAbilityCastStart(Character caster, AbilityInstance instance)
        {
            try
            {
                OnAbilityCastStart?.Invoke(caster, instance);
            }
            catch (Exception ex)
            {
                Plugin.Log?.LogError($"[Prime] Error in OnAbilityCastStart handler: {ex}");
            }
        }

        internal static void RaiseAbilityExecuted(Character caster, AbilityInstance instance)
        {
            try
            {
                OnAbilityExecuted?.Invoke(caster, instance);
            }
            catch (Exception ex)
            {
                Plugin.Log?.LogError($"[Prime] Error in OnAbilityExecuted handler: {ex}");
            }
        }

        internal static void RaiseAbilityInterrupted(Character caster, AbilityInstance instance)
        {
            try
            {
                OnAbilityInterrupted?.Invoke(caster, instance);
            }
            catch (Exception ex)
            {
                Plugin.Log?.LogError($"[Prime] Error in OnAbilityInterrupted handler: {ex}");
            }
        }

        internal static void RaiseAbilityCooldownComplete(Character caster, AbilityInstance instance)
        {
            try
            {
                OnAbilityCooldownComplete?.Invoke(caster, instance);
            }
            catch (Exception ex)
            {
                Plugin.Log?.LogError($"[Prime] Error in OnAbilityCooldownComplete handler: {ex}");
            }
        }

        /// <summary>
        /// Clears all event subscriptions. Used for testing and cleanup.
        /// </summary>
        internal static void ClearAll()
        {
            // Stat events
            OnStatChanged = null;
            OnBaseStatChanged = null;
            OnModifierAdded = null;
            OnModifierRemoved = null;
            OnModifierExpired = null;
            OnModifierStacked = null;
            OnEntityRegistered = null;
            OnEntityUnregistered = null;
            OnStatRegistered = null;

            // Combat events
            OnPreDamage = null;
            OnPostDamage = null;
            OnCritical = null;
            OnKill = null;
            OnBlock = null;
            OnStagger = null;

            // Ability events
            OnAbilityRegistered = null;
            OnAbilityGranted = null;
            OnAbilityRevoked = null;
            OnAbilityCastStart = null;
            OnAbilityExecuted = null;
            OnAbilityInterrupted = null;
            OnAbilityCooldownComplete = null;
        }
    }
}
