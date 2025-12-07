using System;
using System.Collections.Generic;
using Prime.Abilities;
using Prime.Combat;
using Prime.Events;
using UnityEngine;

namespace Prime.Procs
{
    /// <summary>
    /// Manages item proc effects for all entities.
    /// Subscribes to combat events and triggers registered procs.
    /// </summary>
    public class ItemProcSystem
    {
        private static ItemProcSystem _instance;

        /// <summary>
        /// Singleton instance.
        /// </summary>
        public static ItemProcSystem Instance => _instance ??= new ItemProcSystem();

        // Registered procs: characterId -> itemId -> config
        private readonly Dictionary<long, Dictionary<string, RegisteredProc>> _procs =
            new Dictionary<long, Dictionary<string, RegisteredProc>>();

        // Cooldown tracking: characterId_itemId -> last proc time
        private readonly Dictionary<string, float> _cooldowns =
            new Dictionary<string, float>();

        // Low health tracking for OnLowHealth trigger
        private readonly Dictionary<long, bool> _wasLowHealth =
            new Dictionary<long, bool>();

        private bool _initialized;

        /// <summary>
        /// Internal structure for tracking a registered proc.
        /// </summary>
        private class RegisteredProc
        {
            public string ItemId;
            public ItemProcConfig Config;
            public Character Owner;
        }

        private ItemProcSystem()
        {
        }

        /// <summary>
        /// Initialize the proc system and subscribe to events.
        /// Call once during plugin startup.
        /// </summary>
        public void Initialize()
        {
            if (_initialized) return;

            // Subscribe to combat events
            PrimeEvents.OnPostDamage += HandleOnPostDamage;
            PrimeEvents.OnCritical += HandleOnCritical;
            PrimeEvents.OnKill += HandleOnKill;
            PrimeEvents.OnBlock += HandleOnBlock;

            _initialized = true;
            Plugin.Log?.LogInfo("[Prime] ItemProcSystem initialized");
        }

        /// <summary>
        /// Cleanup and unsubscribe from events.
        /// Call during plugin shutdown.
        /// </summary>
        public void Shutdown()
        {
            if (!_initialized) return;

            PrimeEvents.OnPostDamage -= HandleOnPostDamage;
            PrimeEvents.OnCritical -= HandleOnCritical;
            PrimeEvents.OnKill -= HandleOnKill;
            PrimeEvents.OnBlock -= HandleOnBlock;

            _procs.Clear();
            _cooldowns.Clear();
            _wasLowHealth.Clear();

            _initialized = false;
            Plugin.Log?.LogInfo("[Prime] ItemProcSystem shutdown");
        }

        /// <summary>
        /// Register a proc for an equipped item.
        /// </summary>
        /// <param name="owner">The character who owns the item</param>
        /// <param name="itemId">Unique identifier for the item</param>
        /// <param name="config">Proc configuration</param>
        public void RegisterProc(Character owner, string itemId, ItemProcConfig config)
        {
            if (owner == null || string.IsNullOrEmpty(itemId) || config == null)
            {
                Plugin.Log?.LogWarning("[Prime] RegisterProc called with null parameters");
                return;
            }

            long charId = GetCharacterId(owner);

            if (!_procs.TryGetValue(charId, out var itemProcs))
            {
                itemProcs = new Dictionary<string, RegisteredProc>();
                _procs[charId] = itemProcs;
            }

            itemProcs[itemId] = new RegisteredProc
            {
                ItemId = itemId,
                Config = config,
                Owner = owner
            };

            Plugin.Log?.LogDebug($"[Prime] Registered proc '{config.AbilityId}' for item '{itemId}' on {owner.GetHoverName()}");
        }

        /// <summary>
        /// Unregister a proc when item is unequipped.
        /// </summary>
        /// <param name="owner">The character who owns the item</param>
        /// <param name="itemId">Unique identifier for the item</param>
        public void UnregisterProc(Character owner, string itemId)
        {
            if (owner == null || string.IsNullOrEmpty(itemId))
                return;

            long charId = GetCharacterId(owner);

            if (_procs.TryGetValue(charId, out var itemProcs))
            {
                if (itemProcs.Remove(itemId))
                {
                    Plugin.Log?.LogDebug($"[Prime] Unregistered proc for item '{itemId}' on {owner.GetHoverName()}");
                }

                // Clean up empty dictionary
                if (itemProcs.Count == 0)
                {
                    _procs.Remove(charId);
                }
            }

            // Remove cooldown tracking
            string cooldownKey = $"{charId}_{itemId}";
            _cooldowns.Remove(cooldownKey);
        }

        /// <summary>
        /// Unregister all procs for a character (on death/logout).
        /// </summary>
        public void UnregisterAllProcs(Character owner)
        {
            if (owner == null) return;

            long charId = GetCharacterId(owner);
            _procs.Remove(charId);
            _wasLowHealth.Remove(charId);

            // Remove all cooldowns for this character
            var keysToRemove = new List<string>();
            string prefix = $"{charId}_";
            foreach (var key in _cooldowns.Keys)
            {
                if (key.StartsWith(prefix))
                    keysToRemove.Add(key);
            }
            foreach (var key in keysToRemove)
            {
                _cooldowns.Remove(key);
            }
        }

        /// <summary>
        /// Check if a character has any procs registered.
        /// </summary>
        public bool HasProcs(Character owner)
        {
            if (owner == null) return false;
            long charId = GetCharacterId(owner);
            return _procs.TryGetValue(charId, out var itemProcs) && itemProcs.Count > 0;
        }

        /// <summary>
        /// Get all procs for a character.
        /// </summary>
        public IEnumerable<ItemProcConfig> GetProcs(Character owner)
        {
            if (owner == null) yield break;

            long charId = GetCharacterId(owner);
            if (_procs.TryGetValue(charId, out var itemProcs))
            {
                foreach (var proc in itemProcs.Values)
                {
                    yield return proc.Config;
                }
            }
        }

        // ==================== EVENT HANDLERS ====================

        private void HandleOnPostDamage(DamageInfo info)
        {
            if (info?.Attacker == null) return;

            // OnHit for attacker
            TriggerProcs(info.Attacker, ProcTrigger.OnHit, info.Target, info);

            // OnHitTaken for target
            if (info.Target != null)
            {
                TriggerProcs(info.Target, ProcTrigger.OnHitTaken, info.Attacker, info);
                CheckLowHealthTrigger(info.Target);
            }
        }

        private void HandleOnCritical(DamageInfo info)
        {
            if (info?.Attacker == null) return;

            TriggerProcs(info.Attacker, ProcTrigger.OnCrit, info.Target, info);
        }

        private void HandleOnKill(Character killer, Character victim, DamageInfo info)
        {
            if (killer == null) return;

            TriggerProcs(killer, ProcTrigger.OnKill, victim, info);
        }

        private void HandleOnBlock(Character blocker, Character attacker, DamageInfo info)
        {
            if (blocker == null) return;

            TriggerProcs(blocker, ProcTrigger.OnBlock, attacker, info);
        }

        private void CheckLowHealthTrigger(Character character)
        {
            if (character == null) return;

            long charId = GetCharacterId(character);
            float healthPercent = character.GetHealthPercentage();

            // Check each proc for OnLowHealth trigger
            if (_procs.TryGetValue(charId, out var itemProcs))
            {
                foreach (var proc in itemProcs.Values)
                {
                    if (proc.Config.Trigger == ProcTrigger.OnLowHealth &&
                        proc.Config.OwnerHealthThreshold > 0)
                    {
                        bool isLowNow = healthPercent <= proc.Config.OwnerHealthThreshold;
                        bool wasLowBefore = _wasLowHealth.TryGetValue(charId, out var val) && val;

                        // Trigger when crossing threshold from above to below
                        if (isLowNow && !wasLowBefore)
                        {
                            TryExecuteProc(proc, null, null);
                        }

                        _wasLowHealth[charId] = isLowNow;
                    }
                }
            }
        }

        // ==================== PROC EXECUTION ====================

        private void TriggerProcs(Character owner, ProcTrigger trigger, Character target, DamageInfo damageInfo)
        {
            if (owner == null) return;

            long charId = GetCharacterId(owner);
            if (!_procs.TryGetValue(charId, out var itemProcs))
                return;

            foreach (var proc in itemProcs.Values)
            {
                if (proc.Config.Trigger != trigger)
                    continue;

                TryExecuteProc(proc, target, damageInfo);
            }
        }

        private void TryExecuteProc(RegisteredProc proc, Character target, DamageInfo damageInfo)
        {
            var config = proc.Config;
            var owner = proc.Owner;

            // Check cooldown
            string cooldownKey = $"{GetCharacterId(owner)}_{proc.ItemId}";
            if (_cooldowns.TryGetValue(cooldownKey, out float lastProc))
            {
                if (Time.time - lastProc < config.InternalCooldown)
                    return;
            }

            // Check proc chance
            if (config.ProcChance > 0 && config.ProcChance < 1)
            {
                if (UnityEngine.Random.value > config.ProcChance)
                    return;
            }

            // Check target health threshold
            if (config.TargetHealthThreshold > 0 && target != null)
            {
                float targetHp = target.GetHealthPercentage();
                if (targetHp > config.TargetHealthThreshold)
                    return;
            }

            // Check owner health threshold (for non-OnLowHealth triggers)
            if (config.OwnerHealthThreshold > 0 && config.Trigger != ProcTrigger.OnLowHealth)
            {
                float ownerHp = owner.GetHealthPercentage();
                if (ownerHp > config.OwnerHealthThreshold)
                    return;
            }

            // Execute the ability
            ExecuteProcAbility(owner, target, config);

            // Set cooldown
            _cooldowns[cooldownKey] = Time.time;

            Plugin.Log?.LogDebug($"[Prime] Proc '{config.AbilityId}' triggered for {owner.GetHoverName()}");
        }

        private void ExecuteProcAbility(Character owner, Character target, ItemProcConfig config)
        {
            // Get the ability definition
            var definition = AbilityRegistry.Instance.Get(config.AbilityId);
            if (definition == null)
            {
                Plugin.Log?.LogWarning($"[Prime] Proc ability not found: {config.AbilityId}");
                return;
            }

            // Create a temporary ability instance for this proc
            var instance = new AbilityInstance(definition, owner);
            instance.Target = target;
            instance.TargetPosition = target?.transform.position;

            // Apply damage multiplier if set
            if (config.DamageMultiplier != 1.0f)
            {
                instance.DamageMultiplier = config.DamageMultiplier;
            }

            // Skip resource cost check by forcing execution
            if (config.SkipResourceCost)
            {
                instance.ForceExecute();
            }
            else
            {
                instance.TryCast();
            }
        }

        // ==================== HELPERS ====================

        private static long GetCharacterId(Character character)
        {
            if (character is Player player)
            {
                return player.GetPlayerID();
            }
            // For non-players, use instance ID
            return character.GetInstanceID();
        }
    }
}
