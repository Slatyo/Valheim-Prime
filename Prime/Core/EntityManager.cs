using System;
using System.Collections.Generic;
using Prime.Stats;
using Prime.Events;
using UnityEngine;

namespace Prime.Core
{
    /// <summary>
    /// Manages stat containers for game entities (players, creatures, etc.).
    /// Provides the mapping between Unity objects and their Prime stats.
    /// </summary>
    /// <remarks>
    /// EntityManager handles:
    /// - Creating StatContainers for new entities
    /// - Looking up containers by entity reference
    /// - Cleaning up containers when entities are destroyed
    /// - Periodic updates for timed modifiers
    /// </remarks>
    public class EntityManager
    {
        private static EntityManager _instance;
        private static readonly object _lock = new object();

        /// <summary>
        /// Gets the singleton instance of the entity manager.
        /// </summary>
        public static EntityManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        _instance ??= new EntityManager();
                    }
                }
                return _instance;
            }
        }

        // Use object reference for flexibility - can be Character, Player, GameObject, or any custom entity
        private readonly Dictionary<object, StatContainer> _containers = new Dictionary<object, StatContainer>();
        private readonly List<object> _pendingRemoval = new List<object>();

        private EntityManager() { }

        /// <summary>
        /// Gets or creates a StatContainer for an entity.
        /// </summary>
        /// <param name="entity">The entity (Player, Character, GameObject, etc.)</param>
        /// <returns>The entity's StatContainer</returns>
        /// <exception cref="ArgumentNullException">Thrown if entity is null</exception>
        /// <example>
        /// <code>
        /// // Get stats for a player
        /// var container = EntityManager.Instance.GetOrCreate(player);
        /// float strength = container.Get("Strength");
        /// </code>
        /// </example>
        public StatContainer GetOrCreate(object entity)
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entity));

            lock (_lock)
            {
                if (_containers.TryGetValue(entity, out var existing))
                    return existing;

                var container = new StatContainer(entity);
                container.Initialize();
                _containers[entity] = container;

                PrimeEvents.RaiseEntityRegistered(entity, container);
                Plugin.Log?.LogDebug($"[Prime] Created StatContainer for {GetEntityName(entity)}");

                return container;
            }
        }

        /// <summary>
        /// Gets a StatContainer for an entity if it exists.
        /// </summary>
        /// <param name="entity">The entity to look up</param>
        /// <returns>The StatContainer, or null if not registered</returns>
        public StatContainer Get(object entity)
        {
            if (entity == null)
                return null;

            lock (_lock)
            {
                _containers.TryGetValue(entity, out var container);
                return container;
            }
        }

        /// <summary>
        /// Checks if an entity has a registered StatContainer.
        /// </summary>
        /// <param name="entity">The entity to check</param>
        /// <returns>True if the entity has stats</returns>
        public bool Has(object entity)
        {
            if (entity == null)
                return false;

            lock (_lock)
            {
                return _containers.ContainsKey(entity);
            }
        }

        /// <summary>
        /// Removes and cleans up a StatContainer for an entity.
        /// </summary>
        /// <param name="entity">The entity to unregister</param>
        /// <returns>True if removed, false if not found</returns>
        public bool Remove(object entity)
        {
            if (entity == null)
                return false;

            lock (_lock)
            {
                if (_containers.TryGetValue(entity, out var container))
                {
                    container.ClearAllModifiers();
                    _containers.Remove(entity);
                    PrimeEvents.RaiseEntityUnregistered(entity);
                    Plugin.Log?.LogDebug($"[Prime] Removed StatContainer for {GetEntityName(entity)}");
                    return true;
                }
                return false;
            }
        }

        /// <summary>
        /// Updates all stat containers (processes timed modifiers).
        /// Call this from the plugin's Update method.
        /// </summary>
        public void Update()
        {
            lock (_lock)
            {
                _pendingRemoval.Clear();

                foreach (var kvp in _containers)
                {
                    // Check if Unity object was destroyed
                    if (kvp.Key is UnityEngine.Object unityObj && unityObj == null)
                    {
                        _pendingRemoval.Add(kvp.Key);
                        continue;
                    }

                    kvp.Value.Update();
                }

                // Clean up destroyed objects
                foreach (var entity in _pendingRemoval)
                {
                    if (_containers.TryGetValue(entity, out var container))
                    {
                        container.ClearAllModifiers();
                        _containers.Remove(entity);
                        PrimeEvents.RaiseEntityUnregistered(entity);
                    }
                }
            }
        }

        /// <summary>
        /// Gets all registered entities.
        /// </summary>
        /// <returns>Collection of all entities with stat containers</returns>
        public IEnumerable<object> GetAllEntities()
        {
            lock (_lock)
            {
                return new List<object>(_containers.Keys);
            }
        }

        /// <summary>
        /// Gets the number of registered entities.
        /// </summary>
        public int Count
        {
            get
            {
                lock (_lock)
                {
                    return _containers.Count;
                }
            }
        }

        /// <summary>
        /// Clears all registered entities. Used for scene transitions or testing.
        /// </summary>
        public void Clear()
        {
            lock (_lock)
            {
                foreach (var kvp in _containers)
                {
                    kvp.Value.ClearAllModifiers();
                    PrimeEvents.RaiseEntityUnregistered(kvp.Key);
                }
                _containers.Clear();
            }
        }

        /// <summary>
        /// Gets a display name for an entity for logging purposes.
        /// </summary>
        private string GetEntityName(object entity)
        {
            return entity switch
            {
                Character character => character.GetHoverName(),
                GameObject go => go.name,
                _ => entity.GetType().Name
            };
        }
    }
}
