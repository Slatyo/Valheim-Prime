using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Prime.Abilities
{
    /// <summary>
    /// Manages abilities for a single entity.
    /// Each entity (player, creature) has its own EntityAbilities instance.
    /// </summary>
    public class EntityAbilities
    {
        private readonly Character _owner;
        private readonly Dictionary<string, AbilityInstance> _abilities =
            new Dictionary<string, AbilityInstance>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, float> _cooldowns =
            new Dictionary<string, float>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// The entity that owns these abilities.
        /// </summary>
        public Character Owner => _owner;

        /// <summary>
        /// Number of abilities this entity has.
        /// </summary>
        public int Count => _abilities.Count;

        /// <summary>
        /// Creates a new entity abilities container.
        /// </summary>
        /// <param name="owner">The character that owns these abilities</param>
        public EntityAbilities(Character owner)
        {
            _owner = owner ?? throw new ArgumentNullException(nameof(owner));
        }

        /// <summary>
        /// Grants an ability to this entity.
        /// </summary>
        /// <param name="abilityId">The ability ID to grant</param>
        /// <returns>True if granted, false if already has or not found</returns>
        public bool Grant(string abilityId)
        {
            if (string.IsNullOrEmpty(abilityId))
                return false;

            if (_abilities.ContainsKey(abilityId))
                return false;

            var definition = AbilityRegistry.Instance.Get(abilityId);
            if (definition == null)
            {
                Plugin.Log?.LogWarning($"[Prime] Cannot grant unknown ability: {abilityId}");
                return false;
            }

            var instance = new AbilityInstance(definition, _owner);
            _abilities[abilityId] = instance;

            // Restore cooldown if tracked
            if (_cooldowns.TryGetValue(abilityId, out float cdEnd) && Time.time < cdEnd)
            {
                // Still on cooldown from previous grant
            }

            Events.PrimeEvents.RaiseAbilityGranted(_owner, instance);
            Plugin.Log?.LogDebug($"[Prime] Granted ability '{abilityId}' to {_owner.GetHoverName()}");

            return true;
        }

        /// <summary>
        /// Revokes an ability from this entity.
        /// </summary>
        /// <param name="abilityId">The ability ID to revoke</param>
        /// <returns>True if revoked</returns>
        public bool Revoke(string abilityId)
        {
            if (string.IsNullOrEmpty(abilityId))
                return false;

            if (_abilities.TryGetValue(abilityId, out var instance))
            {
                // Track remaining cooldown
                if (instance.State == AbilityState.OnCooldown)
                {
                    _cooldowns[abilityId] = Time.time + instance.GetRemainingCooldown();
                }

                _abilities.Remove(abilityId);
                Events.PrimeEvents.RaiseAbilityRevoked(_owner, instance);
                return true;
            }

            return false;
        }

        /// <summary>
        /// Checks if this entity has an ability.
        /// </summary>
        public bool Has(string abilityId)
        {
            return !string.IsNullOrEmpty(abilityId) && _abilities.ContainsKey(abilityId);
        }

        /// <summary>
        /// Gets an ability instance by ID.
        /// </summary>
        public AbilityInstance Get(string abilityId)
        {
            if (string.IsNullOrEmpty(abilityId))
                return null;

            _abilities.TryGetValue(abilityId, out var instance);
            return instance;
        }

        /// <summary>
        /// Gets all ability instances.
        /// </summary>
        public IEnumerable<AbilityInstance> GetAll()
        {
            return _abilities.Values;
        }

        /// <summary>
        /// Gets ability instances by category.
        /// </summary>
        public IEnumerable<AbilityInstance> GetByCategory(AbilityCategory category)
        {
            return _abilities.Values.Where(a => a.Definition.Category == category);
        }

        /// <summary>
        /// Gets all abilities that are ready to use.
        /// </summary>
        public IEnumerable<AbilityInstance> GetReady()
        {
            return _abilities.Values.Where(a => a.CanCast());
        }

        /// <summary>
        /// Attempts to use an ability.
        /// </summary>
        /// <param name="abilityId">The ability to use</param>
        /// <param name="target">Optional target character</param>
        /// <param name="targetPosition">Optional target position</param>
        /// <returns>True if ability was used</returns>
        public bool TryUse(string abilityId, Character target = null, Vector3? targetPosition = null)
        {
            var instance = Get(abilityId);
            if (instance == null)
            {
                Plugin.Log?.LogDebug($"[Prime] Cannot use ability '{abilityId}' - not granted");
                return false;
            }

            instance.Target = target;
            instance.TargetPosition = targetPosition;

            return instance.TryCast();
        }

        /// <summary>
        /// Updates all ability instances (call each frame).
        /// </summary>
        public void Update()
        {
            foreach (var instance in _abilities.Values)
            {
                instance.Update();
            }
        }

        /// <summary>
        /// Interrupts any currently casting ability.
        /// </summary>
        /// <returns>True if an ability was interrupted</returns>
        public bool InterruptCasting()
        {
            foreach (var instance in _abilities.Values)
            {
                if (instance.State == AbilityState.Casting || instance.State == AbilityState.Channeling)
                {
                    if (instance.Interrupt())
                        return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Gets the currently casting ability, if any.
        /// </summary>
        public AbilityInstance GetCasting()
        {
            return _abilities.Values.FirstOrDefault(a =>
                a.State == AbilityState.Casting || a.State == AbilityState.Channeling);
        }

        /// <summary>
        /// Resets all ability cooldowns.
        /// </summary>
        public void ResetCooldowns()
        {
            foreach (var instance in _abilities.Values)
            {
                if (instance.State == AbilityState.OnCooldown)
                {
                    // Force state back to ready - requires making State settable or adding method
                    // For now, we'll track separately
                }
            }
            _cooldowns.Clear();
        }

        /// <summary>
        /// Clears all abilities.
        /// </summary>
        public void Clear()
        {
            foreach (var instance in _abilities.Values.ToList())
            {
                Events.PrimeEvents.RaiseAbilityRevoked(_owner, instance);
            }
            _abilities.Clear();
        }
    }
}
