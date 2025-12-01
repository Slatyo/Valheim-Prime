using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Logging;

namespace Prime.Stats
{
    /// <summary>
    /// Central registry for all stat definitions.
    /// Stats must be registered here before they can be used on entities.
    /// </summary>
    /// <remarks>
    /// The registry is a singleton that persists across scene loads.
    /// Other mods should register their stats during plugin Awake().
    /// </remarks>
    public class StatRegistry
    {
        private static StatRegistry _instance;
        private static readonly object _lock = new object();

        /// <summary>
        /// Gets the singleton instance of the stat registry.
        /// </summary>
        public static StatRegistry Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        _instance ??= new StatRegistry();
                    }
                }
                return _instance;
            }
        }

        private readonly Dictionary<string, StatDefinition> _stats = new Dictionary<string, StatDefinition>(StringComparer.OrdinalIgnoreCase);
        private readonly List<Action<StatDefinition>> _onStatRegistered = new List<Action<StatDefinition>>();

        private StatRegistry() { }

        /// <summary>
        /// Registers a new stat definition. The stat ID must be unique.
        /// </summary>
        /// <param name="definition">The stat definition to register</param>
        /// <returns>True if registered successfully, false if ID already exists</returns>
        /// <exception cref="ArgumentNullException">Thrown if definition is null</exception>
        /// <example>
        /// <code>
        /// StatRegistry.Instance.Register(new StatDefinition("Strength", baseValue: 10f));
        /// </code>
        /// </example>
        public bool Register(StatDefinition definition)
        {
            if (definition == null)
                throw new ArgumentNullException(nameof(definition));

            lock (_lock)
            {
                if (_stats.ContainsKey(definition.Id))
                {
                    Plugin.Log?.LogWarning($"[Prime] Stat '{definition.Id}' is already registered. Skipping.");
                    return false;
                }

                _stats[definition.Id] = definition;
                Plugin.Log?.LogDebug($"[Prime] Registered stat: {definition.Id}");

                // Notify listeners
                foreach (var callback in _onStatRegistered)
                {
                    try
                    {
                        callback(definition);
                    }
                    catch (Exception ex)
                    {
                        Plugin.Log?.LogError($"[Prime] Error in stat registration callback: {ex}");
                    }
                }

                return true;
            }
        }

        /// <summary>
        /// Registers multiple stat definitions at once.
        /// </summary>
        /// <param name="definitions">The stat definitions to register</param>
        /// <returns>Number of stats successfully registered</returns>
        public int RegisterAll(IEnumerable<StatDefinition> definitions)
        {
            if (definitions == null)
                throw new ArgumentNullException(nameof(definitions));

            int count = 0;
            foreach (var def in definitions)
            {
                if (Register(def))
                    count++;
            }
            return count;
        }

        /// <summary>
        /// Gets a stat definition by ID.
        /// </summary>
        /// <param name="statId">The stat ID to look up</param>
        /// <returns>The stat definition, or null if not found</returns>
        public StatDefinition Get(string statId)
        {
            if (string.IsNullOrEmpty(statId))
                return null;

            _stats.TryGetValue(statId, out var definition);
            return definition;
        }

        /// <summary>
        /// Checks if a stat is registered.
        /// </summary>
        /// <param name="statId">The stat ID to check</param>
        /// <returns>True if the stat is registered</returns>
        public bool IsRegistered(string statId)
        {
            return !string.IsNullOrEmpty(statId) && _stats.ContainsKey(statId);
        }

        /// <summary>
        /// Gets all registered stat definitions.
        /// </summary>
        /// <returns>Read-only collection of all stat definitions</returns>
        public IReadOnlyCollection<StatDefinition> GetAll()
        {
            return _stats.Values.ToList().AsReadOnly();
        }

        /// <summary>
        /// Gets all stats in a specific category.
        /// </summary>
        /// <param name="category">The category to filter by</param>
        /// <returns>Collection of stats in the specified category</returns>
        public IEnumerable<StatDefinition> GetByCategory(StatCategory category)
        {
            return _stats.Values.Where(s => s.Category == category);
        }

        /// <summary>
        /// Gets all stats with a specific tag.
        /// </summary>
        /// <param name="tag">The tag to filter by</param>
        /// <returns>Collection of stats with the specified tag</returns>
        public IEnumerable<StatDefinition> GetByTag(string tag)
        {
            if (string.IsNullOrEmpty(tag))
                return Enumerable.Empty<StatDefinition>();

            return _stats.Values.Where(s => s.Tags != null && s.Tags.Contains(tag, StringComparer.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Gets the IDs of all registered stats.
        /// </summary>
        /// <returns>Collection of stat IDs</returns>
        public IEnumerable<string> GetAllIds()
        {
            return _stats.Keys.ToList();
        }

        /// <summary>
        /// Gets the number of registered stats.
        /// </summary>
        public int Count => _stats.Count;

        /// <summary>
        /// Registers a callback to be invoked when a new stat is registered.
        /// Useful for mods that need to react to stat registration.
        /// </summary>
        /// <param name="callback">Action to invoke with the new stat definition</param>
        public void OnStatRegistered(Action<StatDefinition> callback)
        {
            if (callback != null)
                _onStatRegistered.Add(callback);
        }

        /// <summary>
        /// Unregisters a stat registration callback.
        /// </summary>
        /// <param name="callback">The callback to remove</param>
        public void RemoveStatRegisteredCallback(Action<StatDefinition> callback)
        {
            if (callback != null)
                _onStatRegistered.Remove(callback);
        }

        /// <summary>
        /// Clears all registered stats. Used for testing only.
        /// </summary>
        internal void Clear()
        {
            _stats.Clear();
        }
    }
}
