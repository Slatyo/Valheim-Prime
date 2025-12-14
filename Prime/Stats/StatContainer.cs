using System;
using System.Collections.Generic;
using System.Linq;
using Prime.Modifiers;
using Prime.Events;
using UnityEngine;

namespace Prime.Stats
{
    /// <summary>
    /// Container that holds stat values and modifiers for an entity.
    /// Each entity (player, creature, item) that has stats gets its own StatContainer.
    /// </summary>
    /// <remarks>
    /// StatContainer handles:
    /// - Base stat values
    /// - Active modifiers
    /// - Stat calculation with proper modifier ordering
    /// - Timed modifier expiration
    /// - Caching for performance
    /// </remarks>
    public class StatContainer
    {
        private readonly Dictionary<string, float> _baseValues = new Dictionary<string, float>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, List<Modifier>> _modifiers = new Dictionary<string, List<Modifier>>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, float> _cachedValues = new Dictionary<string, float>(StringComparer.OrdinalIgnoreCase);
        private readonly HashSet<string> _dirtyStats = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Vanilla-synced base values for stats that come from the game's calculation (e.g., food-based health).
        /// These take precedence over hardcoded base values when calculating final stat values.
        /// </summary>
        private readonly Dictionary<string, float> _vanillaSyncedBases = new Dictionary<string, float>(StringComparer.OrdinalIgnoreCase);

        private float _lastUpdateTime;
        private bool _initialized;

        /// <summary>
        /// Optional owner reference for event callbacks.
        /// </summary>
        public object Owner { get; set; }

        /// <summary>
        /// Creates a new stat container.
        /// </summary>
        /// <param name="owner">Optional owner reference</param>
        public StatContainer(object owner = null)
        {
            Owner = owner;
        }

        /// <summary>
        /// Initializes the container with all registered stats at their base values.
        /// Call this when the entity is first created.
        /// </summary>
        public void Initialize()
        {
            if (_initialized) return;

            foreach (var stat in StatRegistry.Instance.GetAll())
            {
                if (!_baseValues.ContainsKey(stat.Id))
                {
                    _baseValues[stat.Id] = stat.BaseValue;
                    _dirtyStats.Add(stat.Id);
                }
            }

            _initialized = true;
        }

        /// <summary>
        /// Gets the final calculated value of a stat, including all modifiers.
        /// </summary>
        /// <param name="statId">The stat ID</param>
        /// <returns>The final calculated value, or 0 if stat doesn't exist</returns>
        public float Get(string statId)
        {
            if (string.IsNullOrEmpty(statId))
                return 0f;

            // Check cache first
            if (!_dirtyStats.Contains(statId) && _cachedValues.TryGetValue(statId, out float cached))
                return cached;

            // Calculate and cache
            float value = Calculate(statId);
            _cachedValues[statId] = value;
            _dirtyStats.Remove(statId);

            return value;
        }

        /// <summary>
        /// Gets the base value of a stat (without modifiers).
        /// Priority: Vanilla-synced value > Explicitly set base > Definition default
        /// </summary>
        /// <param name="statId">The stat ID</param>
        /// <returns>The base value</returns>
        public float GetBase(string statId)
        {
            if (string.IsNullOrEmpty(statId))
                return 0f;

            // Priority 1: Vanilla-synced base (from game's actual calculations like food)
            if (_vanillaSyncedBases.TryGetValue(statId, out float vanillaValue))
                return vanillaValue;

            // Priority 2: Explicitly set base value
            if (_baseValues.TryGetValue(statId, out float value))
                return value;

            // Priority 3: Definition default
            var definition = StatRegistry.Instance.Get(statId);
            return definition?.BaseValue ?? 0f;
        }

        /// <summary>
        /// Syncs a base value from vanilla's game calculation.
        /// This is used for stats like MaxHealth where vanilla calculates the base from food/equipment.
        /// Does NOT trigger events or recalculation to avoid circular loops.
        /// </summary>
        /// <param name="statId">The stat ID</param>
        /// <param name="vanillaValue">The value vanilla calculated (before Prime modifiers)</param>
        public void SyncVanillaBase(string statId, float vanillaValue)
        {
            if (string.IsNullOrEmpty(statId))
                return;

            // Only update if value actually changed (avoid unnecessary cache invalidation)
            if (_vanillaSyncedBases.TryGetValue(statId, out float existing) &&
                Math.Abs(existing - vanillaValue) < 0.001f)
                return;

            _vanillaSyncedBases[statId] = vanillaValue;
            // Mark dirty so next Get() recalculates with new base
            _dirtyStats.Add(statId);
        }

        /// <summary>
        /// Gets the vanilla-synced base value if one exists.
        /// </summary>
        /// <param name="statId">The stat ID</param>
        /// <param name="value">The vanilla base value</param>
        /// <returns>True if a vanilla-synced value exists</returns>
        public bool TryGetVanillaBase(string statId, out float value)
        {
            return _vanillaSyncedBases.TryGetValue(statId, out value);
        }

        /// <summary>
        /// Sets the base value of a stat.
        /// </summary>
        /// <param name="statId">The stat ID</param>
        /// <param name="value">The new base value</param>
        public void SetBase(string statId, float value)
        {
            if (string.IsNullOrEmpty(statId))
                return;

            float oldValue = GetBase(statId);
            _baseValues[statId] = value;
            InvalidateStat(statId);

            if (Math.Abs(oldValue - value) > 0.0001f)
            {
                PrimeEvents.RaiseStatChanged(Owner, statId, Get(statId), oldValue);
            }
        }

        /// <summary>
        /// Adds a modifier to this container.
        /// </summary>
        /// <param name="modifier">The modifier to add</param>
        /// <returns>True if added, false if rejected (e.g., duplicate with Ignore behavior)</returns>
        public bool AddModifier(Modifier modifier)
        {
            if (modifier == null)
                throw new ArgumentNullException(nameof(modifier));

            if (!_modifiers.TryGetValue(modifier.StatId, out var modList))
            {
                modList = new List<Modifier>();
                _modifiers[modifier.StatId] = modList;
            }

            // Handle stack behavior
            var existing = modList.FirstOrDefault(m => m.Id == modifier.Id);
            if (existing != null)
            {
                switch (modifier.StackBehavior)
                {
                    case StackBehavior.Ignore:
                        return false;

                    case StackBehavior.Replace:
                        modList.Remove(existing);
                        break;

                    case StackBehavior.Refresh:
                        existing.AppliedTime = Time.time;
                        InvalidateStat(modifier.StatId);
                        return true;

                    case StackBehavior.Stack:
                        if (existing.Stacks < existing.MaxStacks)
                        {
                            existing.Stacks++;
                            existing.AppliedTime = Time.time; // Refresh duration on stack
                            InvalidateStat(modifier.StatId);
                            PrimeEvents.RaiseModifierStacked(Owner, existing);
                        }
                        return true;

                    case StackBehavior.Independent:
                        // Allow multiple, use a unique suffix
                        modifier = modifier.Clone($"{modifier.Id}_{Guid.NewGuid():N}");
                        break;
                }
            }

            modifier.AppliedTime = Time.time;
            modList.Add(modifier);
            InvalidateStat(modifier.StatId);

            PrimeEvents.RaiseModifierAdded(Owner, modifier);
            return true;
        }

        /// <summary>
        /// Removes a modifier by ID.
        /// </summary>
        /// <param name="modifierId">The modifier ID to remove</param>
        /// <returns>True if removed, false if not found</returns>
        public bool RemoveModifier(string modifierId)
        {
            if (string.IsNullOrEmpty(modifierId))
                return false;

            foreach (var kvp in _modifiers)
            {
                var modifier = kvp.Value.FirstOrDefault(m => m.Id == modifierId);
                if (modifier != null)
                {
                    kvp.Value.Remove(modifier);
                    InvalidateStat(kvp.Key);
                    PrimeEvents.RaiseModifierRemoved(Owner, modifier);
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Removes all modifiers from a specific source.
        /// </summary>
        /// <param name="source">The source to remove modifiers from</param>
        /// <returns>Number of modifiers removed</returns>
        public int RemoveModifiersFromSource(string source)
        {
            if (string.IsNullOrEmpty(source))
                return 0;

            int count = 0;
            foreach (var kvp in _modifiers)
            {
                var toRemove = kvp.Value.Where(m => m.Source == source).ToList();
                foreach (var modifier in toRemove)
                {
                    kvp.Value.Remove(modifier);
                    PrimeEvents.RaiseModifierRemoved(Owner, modifier);
                    count++;
                }

                if (toRemove.Count > 0)
                    InvalidateStat(kvp.Key);
            }

            return count;
        }

        /// <summary>
        /// Gets all modifiers for a specific stat.
        /// </summary>
        /// <param name="statId">The stat ID</param>
        /// <returns>List of modifiers, or empty list if none</returns>
        public IReadOnlyList<Modifier> GetModifiers(string statId)
        {
            if (string.IsNullOrEmpty(statId))
                return Array.Empty<Modifier>();

            if (_modifiers.TryGetValue(statId, out var modList))
                return modList.AsReadOnly();

            return Array.Empty<Modifier>();
        }

        /// <summary>
        /// Gets all modifiers in this container.
        /// </summary>
        /// <returns>All modifiers across all stats</returns>
        public IEnumerable<Modifier> GetAllModifiers()
        {
            return _modifiers.Values.SelectMany(list => list);
        }

        /// <summary>
        /// Gets a breakdown of how a stat's value is calculated.
        /// Useful for tooltips and debugging.
        /// </summary>
        /// <param name="statId">The stat ID</param>
        /// <returns>Detailed breakdown of the calculation</returns>
        public StatBreakdown GetBreakdown(string statId)
        {
            var breakdown = new StatBreakdown
            {
                StatId = statId,
                BaseValue = GetBase(statId),
                FinalValue = Get(statId)
            };

            if (_modifiers.TryGetValue(statId, out var modList))
            {
                breakdown.Modifiers = modList
                    .Where(m => m.IsConditionMet())
                    .OrderBy(m => m.Type)
                    .ThenBy(m => m.Order)
                    .ToList()
                    .AsReadOnly();
            }
            else
            {
                breakdown.Modifiers = Array.Empty<Modifier>();
            }

            return breakdown;
        }

        /// <summary>
        /// Updates timed modifiers and removes expired ones.
        /// Call this periodically (e.g., in Update).
        /// </summary>
        public void Update()
        {
            float currentTime = Time.time;

            // Throttle updates to every 0.1s for performance
            if (currentTime - _lastUpdateTime < 0.1f)
                return;

            _lastUpdateTime = currentTime;

            foreach (var kvp in _modifiers)
            {
                var expired = kvp.Value.Where(m => m.IsExpired(currentTime)).ToList();
                foreach (var modifier in expired)
                {
                    kvp.Value.Remove(modifier);
                    InvalidateStat(kvp.Key);
                    PrimeEvents.RaiseModifierExpired(Owner, modifier);
                }
            }
        }

        /// <summary>
        /// Clears all modifiers from this container.
        /// </summary>
        public void ClearAllModifiers()
        {
            var allModifiers = GetAllModifiers().ToList();
            _modifiers.Clear();

            foreach (var modifier in allModifiers)
            {
                InvalidateStat(modifier.StatId);
                PrimeEvents.RaiseModifierRemoved(Owner, modifier);
            }
        }

        /// <summary>
        /// Marks a stat as needing recalculation.
        /// </summary>
        /// <param name="statId">The stat to invalidate</param>
        private void InvalidateStat(string statId)
        {
            _dirtyStats.Add(statId);
        }

        /// <summary>
        /// Calculates the final value of a stat with all modifiers.
        /// </summary>
        private float Calculate(string statId)
        {
            float baseValue = GetBase(statId);
            var definition = StatRegistry.Instance.Get(statId);

            if (!_modifiers.TryGetValue(statId, out var modList) || modList.Count == 0)
            {
                return definition?.Clamp(baseValue) ?? baseValue;
            }

            // Filter active modifiers and sort by type then order
            var activeModifiers = modList
                .Where(m => m.IsConditionMet() && !m.IsExpired(Time.time))
                .OrderBy(m => m.Type)
                .ThenBy(m => m.Order)
                .ToList();

            float value = baseValue;

            // Calculate in phases: Flat → Percent → Multiply → Override
            // Phase 1: Add all flat modifiers
            float flatSum = activeModifiers
                .Where(m => m.Type == ModifierType.Flat)
                .Sum(m => m.GetEffectiveValue());
            value += flatSum;

            // Phase 2: Add all percent modifiers
            float percentSum = activeModifiers
                .Where(m => m.Type == ModifierType.Percent)
                .Sum(m => m.GetEffectiveValue());
            value *= (1f + percentSum / 100f);

            // Phase 3: Apply all multiply modifiers (multiplicative)
            foreach (var mod in activeModifiers.Where(m => m.Type == ModifierType.Multiply))
            {
                value *= mod.GetEffectiveValue();
            }

            // Phase 4: Override (last one wins)
            var overrideMod = activeModifiers
                .Where(m => m.Type == ModifierType.Override)
                .OrderBy(m => m.Order)
                .LastOrDefault();

            if (overrideMod != null)
            {
                value = overrideMod.GetEffectiveValue();
            }

            // Clamp to stat bounds
            return definition?.Clamp(value) ?? value;
        }
    }

    /// <summary>
    /// Detailed breakdown of a stat's calculation for tooltips and debugging.
    /// </summary>
    public class StatBreakdown
    {
        /// <summary>The stat this breakdown is for.</summary>
        public string StatId { get; set; }

        /// <summary>The base value before modifiers.</summary>
        public float BaseValue { get; set; }

        /// <summary>The final calculated value.</summary>
        public float FinalValue { get; set; }

        /// <summary>All active modifiers affecting this stat.</summary>
        public IReadOnlyList<Modifier> Modifiers { get; set; }

        /// <summary>
        /// Gets a human-readable string representation of this breakdown.
        /// </summary>
        public override string ToString()
        {
            var lines = new List<string>
            {
                $"{StatId}: {FinalValue:F2}",
                $"  Base: {BaseValue:F2}"
            };

            foreach (var mod in Modifiers ?? Enumerable.Empty<Modifier>())
            {
                string symbol = mod.Type switch
                {
                    ModifierType.Flat => "+",
                    ModifierType.Percent => "+",
                    ModifierType.Multiply => "×",
                    ModifierType.Override => "=",
                    _ => "?"
                };

                string valueStr = mod.Type == ModifierType.Percent
                    ? $"{mod.GetEffectiveValue()}%"
                    : mod.GetEffectiveValue().ToString("F2");

                string source = string.IsNullOrEmpty(mod.Source) ? "" : $" ({mod.Source})";
                lines.Add($"  {symbol}{valueStr}{source}");
            }

            return string.Join("\n", lines);
        }
    }
}
