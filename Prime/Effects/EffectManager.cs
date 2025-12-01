using System;
using System.Collections.Generic;
using System.Linq;
using Prime.Combat;
using UnityEngine;

namespace Prime.Effects
{
    /// <summary>
    /// Manages active effects on all entities.
    /// Handles effect application, removal, updates, and triggered procs.
    /// </summary>
    public static class EffectManager
    {
        private static readonly Dictionary<Character, List<EffectInstance>> _activeEffects =
            new Dictionary<Character, List<EffectInstance>>();

        private static readonly object _lock = new object();

        /// <summary>
        /// Applies an effect to an entity.
        /// </summary>
        /// <param name="target">The entity to apply the effect to</param>
        /// <param name="effect">The effect to apply</param>
        /// <returns>The effect instance, or null if rejected</returns>
        public static EffectInstance ApplyEffect(Character target, EffectDefinition effect)
        {
            if (target == null || effect == null)
                return null;

            lock (_lock)
            {
                if (!_activeEffects.TryGetValue(target, out var effects))
                {
                    effects = new List<EffectInstance>();
                    _activeEffects[target] = effects;
                }

                // Check for existing effect with same ID
                var existing = effects.FirstOrDefault(e => e.Definition.Id == effect.Id && e.IsActive);
                if (existing != null)
                {
                    switch (effect.StackBehavior)
                    {
                        case EffectStackBehavior.Ignore:
                            return null;

                        case EffectStackBehavior.Replace:
                            existing.Remove();
                            effects.Remove(existing);
                            break;

                        case EffectStackBehavior.Refresh:
                            existing.Refresh();
                            return existing;

                        case EffectStackBehavior.Stack:
                            if (existing.Stacks < effect.MaxStacks)
                            {
                                existing.Stacks++;
                                existing.Refresh();
                                effect.OnStack?.Invoke(target, existing.Stacks);
                            }
                            return existing;

                        case EffectStackBehavior.Independent:
                            // Allow new instance
                            break;
                    }
                }

                // Create new instance
                var instance = new EffectInstance(effect, target);
                effects.Add(instance);

                // Call OnApply handler
                if (effect.OnApply != null)
                {
                    try
                    {
                        effect.OnApply(target);
                    }
                    catch (Exception ex)
                    {
                        Plugin.Log?.LogError($"[Prime] Error in effect apply {effect.Id}: {ex}");
                    }
                }

                Plugin.Log?.LogDebug($"[Prime] Applied effect '{effect.Id}' to {target.GetHoverName()}");
                return instance;
            }
        }

        /// <summary>
        /// Removes an effect from an entity.
        /// </summary>
        /// <param name="target">The entity</param>
        /// <param name="effectId">The effect ID to remove</param>
        /// <returns>True if removed</returns>
        public static bool RemoveEffect(Character target, string effectId)
        {
            if (target == null || string.IsNullOrEmpty(effectId))
                return false;

            lock (_lock)
            {
                if (!_activeEffects.TryGetValue(target, out var effects))
                    return false;

                var toRemove = effects.Where(e => e.Definition.Id == effectId && e.IsActive).ToList();
                foreach (var instance in toRemove)
                {
                    instance.Remove();
                    effects.Remove(instance);
                }

                return toRemove.Count > 0;
            }
        }

        /// <summary>
        /// Removes all effects from an entity.
        /// </summary>
        public static void RemoveAllEffects(Character target)
        {
            if (target == null)
                return;

            lock (_lock)
            {
                if (!_activeEffects.TryGetValue(target, out var effects))
                    return;

                foreach (var instance in effects.Where(e => e.IsActive))
                {
                    instance.Remove();
                }

                effects.Clear();
            }
        }

        /// <summary>
        /// Gets all active effects on an entity.
        /// </summary>
        public static IEnumerable<EffectInstance> GetEffects(Character target)
        {
            if (target == null)
                return Enumerable.Empty<EffectInstance>();

            lock (_lock)
            {
                if (!_activeEffects.TryGetValue(target, out var effects))
                    return Enumerable.Empty<EffectInstance>();

                return effects.Where(e => e.IsActive).ToList();
            }
        }

        /// <summary>
        /// Checks if an entity has a specific effect.
        /// </summary>
        public static bool HasEffect(Character target, string effectId)
        {
            if (target == null || string.IsNullOrEmpty(effectId))
                return false;

            lock (_lock)
            {
                if (!_activeEffects.TryGetValue(target, out var effects))
                    return false;

                return effects.Any(e => e.Definition.Id == effectId && e.IsActive);
            }
        }

        /// <summary>
        /// Gets an effect instance by ID.
        /// </summary>
        public static EffectInstance GetEffect(Character target, string effectId)
        {
            if (target == null || string.IsNullOrEmpty(effectId))
                return null;

            lock (_lock)
            {
                if (!_activeEffects.TryGetValue(target, out var effects))
                    return null;

                return effects.FirstOrDefault(e => e.Definition.Id == effectId && e.IsActive);
            }
        }

        /// <summary>
        /// Updates all effects (call each frame from Plugin.Update).
        /// </summary>
        public static void Update()
        {
            float deltaTime = Time.deltaTime;

            lock (_lock)
            {
                var toCleanup = new List<Character>();

                foreach (var kvp in _activeEffects)
                {
                    // Check if character is destroyed
                    if (kvp.Key == null)
                    {
                        toCleanup.Add(kvp.Key);
                        continue;
                    }

                    var expiredEffects = new List<EffectInstance>();

                    foreach (var instance in kvp.Value)
                    {
                        if (!instance.IsActive)
                        {
                            expiredEffects.Add(instance);
                            continue;
                        }

                        instance.Update(deltaTime);

                        if (!instance.IsActive)
                        {
                            expiredEffects.Add(instance);
                        }
                    }

                    // Remove expired effects
                    foreach (var expired in expiredEffects)
                    {
                        expired.Remove();
                        kvp.Value.Remove(expired);
                    }

                    if (kvp.Value.Count == 0)
                    {
                        toCleanup.Add(kvp.Key);
                    }
                }

                // Clean up empty entries
                foreach (var character in toCleanup)
                {
                    _activeEffects.Remove(character);
                }
            }
        }

        /// <summary>
        /// Triggers on-hit effects for the attacker.
        /// Called by CombatManager when damage is dealt.
        /// </summary>
        public static void TriggerOnHitEffects(DamageInfo damageInfo)
        {
            if (damageInfo.Attacker == null)
                return;

            var effects = GetEffects(damageInfo.Attacker)
                .Where(e => e.Definition.Trigger == EffectTrigger.OnHit);

            foreach (var effect in effects)
            {
                effect.TryProc(damageInfo.Target, damageInfo);
            }

            // Also trigger OnCrit if applicable
            if (damageInfo.IsCritical)
            {
                var critEffects = GetEffects(damageInfo.Attacker)
                    .Where(e => e.Definition.Trigger == EffectTrigger.OnCrit);

                foreach (var effect in critEffects)
                {
                    effect.TryProc(damageInfo.Target, damageInfo);
                }
            }
        }

        /// <summary>
        /// Triggers on-damage-taken effects for the target.
        /// Called by CombatManager when damage is received.
        /// </summary>
        public static void TriggerOnDamageTakenEffects(DamageInfo damageInfo)
        {
            if (damageInfo.Target == null)
                return;

            var effects = GetEffects(damageInfo.Target)
                .Where(e => e.Definition.Trigger == EffectTrigger.OnDamageTaken);

            foreach (var effect in effects)
            {
                effect.TryProc(damageInfo.Attacker, damageInfo);
            }
        }

        /// <summary>
        /// Triggers on-kill effects for the killer.
        /// </summary>
        public static void TriggerOnKillEffects(Character killer, Character victim, DamageInfo damageInfo)
        {
            if (killer == null)
                return;

            var effects = GetEffects(killer)
                .Where(e => e.Definition.Trigger == EffectTrigger.OnKill);

            foreach (var effect in effects)
            {
                effect.TryProc(victim, damageInfo);
            }
        }

        /// <summary>
        /// Triggers on-block effects for the blocker.
        /// </summary>
        public static void TriggerOnBlockEffects(Character blocker, Character attacker, DamageInfo damageInfo)
        {
            if (blocker == null)
                return;

            var effects = GetEffects(blocker)
                .Where(e => e.Definition.Trigger == EffectTrigger.OnBlock);

            foreach (var effect in effects)
            {
                effect.TryProc(attacker, damageInfo);
            }
        }

        /// <summary>
        /// Dispels effects from an entity.
        /// </summary>
        /// <param name="target">The entity</param>
        /// <param name="dispelBuffs">Remove buffs?</param>
        /// <param name="dispelDebuffs">Remove debuffs?</param>
        /// <param name="maxPriority">Only dispel effects with priority <= this</param>
        /// <returns>Number of effects dispelled</returns>
        public static int Dispel(Character target, bool dispelBuffs = true, bool dispelDebuffs = false, int maxPriority = int.MaxValue)
        {
            if (target == null)
                return 0;

            lock (_lock)
            {
                if (!_activeEffects.TryGetValue(target, out var effects))
                    return 0;

                var toDispel = effects.Where(e =>
                    e.IsActive &&
                    e.Definition.Dispellable &&
                    e.Definition.Priority <= maxPriority &&
                    ((e.Definition.IsBuff && dispelBuffs) || (!e.Definition.IsBuff && dispelDebuffs))
                ).ToList();

                foreach (var instance in toDispel)
                {
                    instance.Remove();
                    effects.Remove(instance);
                }

                return toDispel.Count;
            }
        }

        /// <summary>
        /// Clears all tracked effects. Use for scene cleanup.
        /// </summary>
        internal static void Clear()
        {
            lock (_lock)
            {
                foreach (var kvp in _activeEffects)
                {
                    foreach (var instance in kvp.Value)
                    {
                        instance.Remove();
                    }
                }
                _activeEffects.Clear();
            }
        }
    }
}
