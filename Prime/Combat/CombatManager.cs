using System;
using System.Collections.Generic;
using Prime.Abilities;
using Prime.Effects;
using UnityEngine;

namespace Prime.Combat
{
    /// <summary>
    /// Central combat system that processes all damage through Prime's pipeline.
    /// Handles damage calculation, crit checks, resistances, and effect procs.
    /// </summary>
    public static class CombatManager
    {
        /// <summary>
        /// Processes damage through the full Prime pipeline.
        /// </summary>
        /// <param name="damageInfo">The damage info to process</param>
        /// <returns>Final damage dealt, or 0 if cancelled</returns>
        public static float ProcessDamage(DamageInfo damageInfo)
        {
            if (damageInfo == null || damageInfo.Target == null)
                return 0f;

            try
            {
                // Phase 1: Pre-damage (can modify or cancel)
                Events.PrimeEvents.RaiseOnPreDamage(damageInfo);
                if (damageInfo.Cancelled)
                {
                    Plugin.Log?.LogDebug($"[Prime] Damage cancelled: {damageInfo.CancelReason}");
                    return 0f;
                }

                // Phase 2: Apply attacker bonuses
                ApplyAttackerBonuses(damageInfo);

                // Phase 3: Crit check
                if (!damageInfo.IsCritical && damageInfo.Attacker != null)
                {
                    CheckCritical(damageInfo);
                }

                // Phase 4: Apply crit multiplier
                if (damageInfo.IsCritical)
                {
                    damageInfo.MultiplyAllDamage(damageInfo.CritMultiplier);
                    Events.PrimeEvents.RaiseOnCritical(damageInfo);
                }

                // Phase 5: Calculate and apply target mitigation
                ApplyTargetMitigation(damageInfo);

                // Phase 6: Final damage calculation
                damageInfo.FinalDamage = damageInfo.GetTotalDamage();

                // Phase 7: Post-damage events
                Events.PrimeEvents.RaiseOnPostDamage(damageInfo);

                // Phase 8: Trigger on-hit effects
                if (damageInfo.CanProcEffects && damageInfo.Attacker != null)
                {
                    EffectManager.TriggerOnHitEffects(damageInfo);
                }

                // Phase 9: Trigger on-damage-taken effects
                if (damageInfo.CanProcEffects)
                {
                    EffectManager.TriggerOnDamageTakenEffects(damageInfo);
                }

                // Phase 10: Notify kill if target died
                if (damageInfo.Target.GetHealth() <= damageInfo.FinalDamage)
                {
                    Events.PrimeEvents.RaiseOnKill(damageInfo.Attacker, damageInfo.Target, damageInfo);
                }

                return damageInfo.FinalDamage;
            }
            catch (Exception ex)
            {
                Plugin.Log?.LogError($"[Prime] Error processing damage: {ex}");
                return damageInfo.GetTotalDamage();
            }
        }

        /// <summary>
        /// Deals damage from an ability.
        /// </summary>
        public static float DealAbilityDamage(Character attacker, Character target, AbilityDefinition ability,
            float damage, bool isCrit)
        {
            var damageInfo = new DamageInfo(attacker, target)
            {
                Source = DamageSource.Ability,
                Ability = ability,
                IsCritical = isCrit
            };

            // Set damage type
            damageInfo.SetDamage(ability.DamageType, damage);

            // Set crit multiplier if critting
            if (isCrit && attacker != null)
            {
                damageInfo.CritMultiplier = PrimeAPI.Get(attacker, "CritDamage");
            }

            return ProcessDamage(damageInfo);
        }

        /// <summary>
        /// Deals direct damage (bypasses some calculations).
        /// </summary>
        public static float DealDirectDamage(Character attacker, Character target, DamageType type,
            float amount, bool canCrit = false)
        {
            var damageInfo = new DamageInfo(attacker, target)
            {
                Source = DamageSource.Unknown
            };
            damageInfo.SetDamage(type, amount);

            if (!canCrit)
            {
                damageInfo.IsCritical = false;
            }

            return ProcessDamage(damageInfo);
        }

        /// <summary>
        /// Deals true damage (ignores armor/resistances).
        /// </summary>
        public static float DealTrueDamage(Character attacker, Character target, float amount)
        {
            var damageInfo = new DamageInfo(attacker, target)
            {
                Source = DamageSource.Unknown,
                CanProcEffects = false
            };
            damageInfo.SetDamage(DamageType.True, amount);

            // Skip mitigation for true damage
            Events.PrimeEvents.RaiseOnPreDamage(damageInfo);
            if (damageInfo.Cancelled)
                return 0f;

            damageInfo.FinalDamage = amount;
            Events.PrimeEvents.RaiseOnPostDamage(damageInfo);

            return damageInfo.FinalDamage;
        }

        /// <summary>
        /// Applies damage over time.
        /// </summary>
        public static void ApplyDoT(Character attacker, Character target, DamageType type,
            float damagePerTick, float duration, float tickInterval, string dotId)
        {
            // DoTs are handled by the effect system
            var effect = new EffectDefinition(dotId)
            {
                Duration = duration,
                TickInterval = tickInterval,
                OnTick = (owner) =>
                {
                    if (owner != null && owner.GetHealth() > 0)
                    {
                        var damageInfo = new DamageInfo(attacker, owner)
                        {
                            Source = DamageSource.DoT,
                            CanProcEffects = false // DoTs don't proc on-hit effects
                        };
                        damageInfo.SetDamage(type, damagePerTick);
                        ProcessDamage(damageInfo);
                    }
                }
            };

            EffectManager.ApplyEffect(target, effect);
        }

        private static void ApplyAttackerBonuses(DamageInfo damageInfo)
        {
            if (damageInfo.Attacker == null)
                return;

            // Apply PhysicalDamage bonus - add to any physical damage type present
            float physBonus = PrimeAPI.Get(damageInfo.Attacker, "PhysicalDamage");
            if (physBonus > 0)
            {
                var physicalTypes = new[] { DamageType.Physical, DamageType.Blunt, DamageType.Slash, DamageType.Pierce };
                bool hasPhysical = false;
                foreach (var type in physicalTypes)
                {
                    if (damageInfo.Damages.ContainsKey(type))
                    {
                        hasPhysical = true;
                        break;
                    }
                }

                if (hasPhysical)
                {
                    // Distribute bonus across all physical types proportionally
                    foreach (var type in physicalTypes)
                    {
                        if (damageInfo.Damages.TryGetValue(type, out float value))
                        {
                            damageInfo.Damages[type] = value + physBonus;
                        }
                    }
                }
            }

            // Apply elemental damage bonuses - these ADD new damage if the player has the stat
            AddElementalBonus(damageInfo, DamageType.Fire, "FireDamage");
            AddElementalBonus(damageInfo, DamageType.Frost, "FrostDamage");
            AddElementalBonus(damageInfo, DamageType.Lightning, "LightningDamage");
            AddElementalBonus(damageInfo, DamageType.Poison, "PoisonDamage");
            AddElementalBonus(damageInfo, DamageType.Spirit, "SpiritDamage");

            // Apply Strength scaling (optional - configurable)
            if (Plugin.ConfigManager.StrengthScaling.Value > 0)
            {
                float strength = PrimeAPI.Get(damageInfo.Attacker, "Strength");
                float baseStrength = 10f;
                float strengthBonus = (strength - baseStrength) * Plugin.ConfigManager.StrengthScaling.Value;
                if (strengthBonus != 0f)
                {
                    damageInfo.MultiplyAllDamage(1f + strengthBonus);
                }
            }
        }

        /// <summary>
        /// Adds elemental damage from player stats to the damage info.
        /// This is flat additional damage, not a multiplier.
        /// </summary>
        private static void AddElementalBonus(DamageInfo damageInfo, DamageType type, string statId)
        {
            float bonus = PrimeAPI.Get(damageInfo.Attacker, statId);
            if (bonus > 0)
            {
                // Add to existing or create new
                if (damageInfo.Damages.TryGetValue(type, out float existing))
                {
                    damageInfo.Damages[type] = existing + bonus;
                }
                else
                {
                    damageInfo.Damages[type] = bonus;
                }
            }
        }

        private static void CheckCritical(DamageInfo damageInfo)
        {
            float critChance = PrimeAPI.Get(damageInfo.Attacker, "CritChance");

            // Dexterity can add to crit chance (optional - configurable)
            if (Plugin.ConfigManager.DexterityCritBonus.Value > 0)
            {
                float dex = PrimeAPI.Get(damageInfo.Attacker, "Dexterity");
                float baseDex = 10f;
                critChance += (dex - baseDex) * Plugin.ConfigManager.DexterityCritBonus.Value;
            }

            if (UnityEngine.Random.value < critChance)
            {
                damageInfo.IsCritical = true;
                damageInfo.CritMultiplier = PrimeAPI.Get(damageInfo.Attacker, "CritDamage");

                // Backstab bonus stacks with crit
                if (damageInfo.IsBackstab)
                {
                    damageInfo.CritMultiplier *= damageInfo.BackstabMultiplier;
                }
            }
        }

        private static void ApplyTargetMitigation(DamageInfo damageInfo)
        {
            if (damageInfo.Target == null)
                return;

            // True damage ignores mitigation
            if (damageInfo.Damages.ContainsKey(DamageType.True))
            {
                // Only process true damage
                float trueDamage = damageInfo.GetDamage(DamageType.True);
                damageInfo.Damages.Clear();
                damageInfo.SetDamage(DamageType.True, trueDamage);
                return;
            }

            // Apply armor to physical damage
            float armor = PrimeAPI.Get(damageInfo.Target, "Armor");
            var physicalTypes = new[] { DamageType.Physical, DamageType.Blunt, DamageType.Slash, DamageType.Pierce };
            if (armor > 0)
            {
                foreach (var type in physicalTypes)
                {
                    if (damageInfo.Damages.TryGetValue(type, out float value))
                    {
                        // Armor reduces physical damage: damage = damage * (1 - armor/(armor+100))
                        // This gives diminishing returns: 100 armor = 50% reduction, 300 armor = 75%
                        float reduction = armor / (armor + 100f);
                        damageInfo.Damages[type] = value * (1f - reduction);
                    }
                }
            }

            // Apply PhysicalResist as additional % reduction (stacks with armor)
            float physResist = PrimeAPI.Get(damageInfo.Target, "PhysicalResist");
            if (physResist != 0f)
            {
                // Clamp to prevent immunity or more than 2x damage
                physResist = Mathf.Clamp(physResist, -1f, 0.9f);
                foreach (var type in physicalTypes)
                {
                    if (damageInfo.Damages.TryGetValue(type, out float value))
                    {
                        damageInfo.Damages[type] = value * (1f - physResist);
                    }
                }
            }

            // Apply elemental resistances
            ApplyResistance(damageInfo, DamageType.Fire, "FireResist");
            ApplyResistance(damageInfo, DamageType.Frost, "FrostResist");
            ApplyResistance(damageInfo, DamageType.Lightning, "LightningResist");
            ApplyResistance(damageInfo, DamageType.Poison, "PoisonResist");
            ApplyResistance(damageInfo, DamageType.Spirit, "SpiritResist");
        }

        private static void ApplyResistance(DamageInfo damageInfo, DamageType type, string resistStat)
        {
            if (!damageInfo.Damages.TryGetValue(type, out float damage))
                return;

            float resist = PrimeAPI.Get(damageInfo.Target, resistStat);

            // Resistance is -1 to 1 (-100% to 100%)
            // Negative = vulnerable, positive = resistant
            // Clamp to prevent immunity or more than 2x damage
            resist = Mathf.Clamp(resist, -1f, 0.9f);

            damageInfo.Damages[type] = damage * (1f - resist);
        }

    }
}
