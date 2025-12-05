using System;
using System.Collections.Generic;
using System.Linq;

namespace Prime.Abilities
{
    /// <summary>
    /// Central registry for all ability definitions.
    /// Other mods register their abilities here.
    /// </summary>
    public class AbilityRegistry
    {
        private static AbilityRegistry _instance;
        private static readonly object _lock = new object();

        /// <summary>
        /// Gets the singleton instance of the ability registry.
        /// </summary>
        public static AbilityRegistry Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        _instance ??= new AbilityRegistry();
                    }
                }
                return _instance;
            }
        }

        private readonly Dictionary<string, AbilityDefinition> _abilities =
            new Dictionary<string, AbilityDefinition>(StringComparer.OrdinalIgnoreCase);

        private AbilityRegistry() { }

        /// <summary>
        /// Registers a new ability definition.
        /// </summary>
        /// <param name="ability">The ability to register</param>
        /// <returns>True if registered, false if ID already exists</returns>
        public bool Register(AbilityDefinition ability)
        {
            if (ability == null)
                throw new ArgumentNullException(nameof(ability));

            lock (_lock)
            {
                if (_abilities.ContainsKey(ability.Id))
                {
                    Plugin.Log?.LogWarning($"[Prime] Ability '{ability.Id}' already registered, skipping");
                    return false;
                }

                _abilities[ability.Id] = ability;
                Plugin.Log?.LogDebug($"[Prime] Registered ability: {ability.Id}");

                Events.PrimeEvents.RaiseAbilityRegistered(ability);
                return true;
            }
        }

        /// <summary>
        /// Unregisters an ability by ID.
        /// </summary>
        /// <param name="abilityId">The ability ID to remove</param>
        /// <returns>True if removed</returns>
        public bool Unregister(string abilityId)
        {
            if (string.IsNullOrEmpty(abilityId))
                return false;

            lock (_lock)
            {
                return _abilities.Remove(abilityId);
            }
        }

        /// <summary>
        /// Gets an ability definition by ID.
        /// </summary>
        /// <param name="abilityId">The ability ID</param>
        /// <returns>The ability definition, or null if not found</returns>
        public AbilityDefinition Get(string abilityId)
        {
            if (string.IsNullOrEmpty(abilityId))
                return null;

            lock (_lock)
            {
                _abilities.TryGetValue(abilityId, out var ability);
                return ability;
            }
        }

        /// <summary>
        /// Checks if an ability is registered.
        /// </summary>
        public bool IsRegistered(string abilityId)
        {
            if (string.IsNullOrEmpty(abilityId))
                return false;

            lock (_lock)
            {
                return _abilities.ContainsKey(abilityId);
            }
        }

        /// <summary>
        /// Gets all registered abilities.
        /// </summary>
        public IEnumerable<AbilityDefinition> GetAll()
        {
            lock (_lock)
            {
                return _abilities.Values.ToList();
            }
        }

        /// <summary>
        /// Gets all ability IDs.
        /// </summary>
        public IEnumerable<string> GetAllIds()
        {
            lock (_lock)
            {
                return _abilities.Keys.ToList();
            }
        }

        /// <summary>
        /// Gets abilities by category.
        /// </summary>
        public IEnumerable<AbilityDefinition> GetByCategory(AbilityCategory category)
        {
            lock (_lock)
            {
                return _abilities.Values.Where(a => a.Category == category).ToList();
            }
        }

        /// <summary>
        /// Gets abilities by tag.
        /// </summary>
        public IEnumerable<AbilityDefinition> GetByTag(string tag)
        {
            if (string.IsNullOrEmpty(tag))
                return Enumerable.Empty<AbilityDefinition>();

            lock (_lock)
            {
                return _abilities.Values.Where(a => a.Tags.Contains(tag)).ToList();
            }
        }

        /// <summary>
        /// Gets abilities by damage type.
        /// </summary>
        public IEnumerable<AbilityDefinition> GetByDamageType(DamageType damageType)
        {
            lock (_lock)
            {
                return _abilities.Values.Where(a => a.DamageType == damageType).ToList();
            }
        }

        /// <summary>
        /// Number of registered abilities.
        /// </summary>
        public int Count
        {
            get
            {
                lock (_lock)
                {
                    return _abilities.Count;
                }
            }
        }

        /// <summary>
        /// Clears all registered abilities. Use for testing only.
        /// </summary>
        internal void Clear()
        {
            lock (_lock)
            {
                _abilities.Clear();
            }
        }
    }
}
