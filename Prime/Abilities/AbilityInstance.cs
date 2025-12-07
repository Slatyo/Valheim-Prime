using System;
using UnityEngine;

namespace Prime.Abilities
{
    /// <summary>
    /// Represents an active use of an ability.
    /// Tracks casting state, cooldowns, and effects.
    /// </summary>
    public class AbilityInstance
    {
        /// <summary>
        /// The ability definition this instance is based on.
        /// </summary>
        public AbilityDefinition Definition { get; }

        /// <summary>
        /// The character using this ability.
        /// </summary>
        public Character Caster { get; }

        /// <summary>
        /// Target character (if applicable).
        /// </summary>
        public Character Target { get; set; }

        /// <summary>
        /// Target position (for ground-targeted abilities).
        /// </summary>
        public Vector3? TargetPosition { get; set; }

        /// <summary>
        /// Target direction (for directional abilities).
        /// </summary>
        public Vector3? TargetDirection { get; set; }

        /// <summary>
        /// Current state of the ability.
        /// </summary>
        public AbilityState State { get; private set; } = AbilityState.Ready;

        /// <summary>
        /// Time when casting started.
        /// </summary>
        public float CastStartTime { get; private set; }

        /// <summary>
        /// Time when ability goes on cooldown.
        /// </summary>
        public float CooldownStartTime { get; private set; }

        /// <summary>
        /// Calculated cooldown after CooldownReduction.
        /// </summary>
        public float EffectiveCooldown { get; private set; }

        /// <summary>
        /// Unique instance ID for tracking.
        /// </summary>
        public string InstanceId { get; }

        /// <summary>
        /// Custom data for this instance.
        /// </summary>
        public object UserData { get; set; }

        /// <summary>
        /// Damage multiplier for this instance (default 1.0).
        /// Used by proc system to scale damage.
        /// </summary>
        public float DamageMultiplier { get; set; } = 1.0f;

        /// <summary>
        /// If true, skip resource cost when executing.
        /// Used by proc system for free ability procs.
        /// </summary>
        public bool SkipResourceCost { get; set; }

        /// <summary>
        /// Creates a new ability instance.
        /// </summary>
        /// <param name="definition">The ability definition</param>
        /// <param name="caster">The character using the ability</param>
        public AbilityInstance(AbilityDefinition definition, Character caster)
        {
            Definition = definition ?? throw new ArgumentNullException(nameof(definition));
            Caster = caster ?? throw new ArgumentNullException(nameof(caster));
            InstanceId = $"{definition.Id}_{Guid.NewGuid():N}";
        }

        /// <summary>
        /// Attempts to start casting this ability.
        /// </summary>
        /// <returns>True if casting started, false if not possible</returns>
        public bool TryCast()
        {
            if (!CanCast())
                return false;

            // Check custom condition
            if (Definition.UseCondition != null && !Definition.UseCondition(Caster))
                return false;

            // Check and consume resources (unless skipped)
            if (!SkipResourceCost && !TryConsumeResources())
                return false;

            // Calculate effective cooldown with CDR
            float cdr = PrimeAPI.Get(Caster, "CooldownReduction");
            EffectiveCooldown = Definition.BaseCooldown * (1f - cdr);

            // If no cast time, execute immediately
            if (Definition.CastTime <= 0)
            {
                Execute();
            }
            else
            {
                State = AbilityState.Casting;
                CastStartTime = Time.time;
                Events.PrimeEvents.RaiseAbilityCastStart(Caster, this);
            }

            return true;
        }

        /// <summary>
        /// Force execute the ability immediately, bypassing all checks.
        /// Used by proc system for item-triggered abilities.
        /// </summary>
        public void ForceExecute()
        {
            // Calculate effective cooldown with CDR
            float cdr = PrimeAPI.Get(Caster, "CooldownReduction");
            EffectiveCooldown = Definition.BaseCooldown * (1f - cdr);

            // Execute immediately, no cast time
            Execute();
        }

        /// <summary>
        /// Checks if this ability can be cast right now.
        /// </summary>
        public bool CanCast()
        {
            // Check state
            if (State == AbilityState.Casting || State == AbilityState.Channeling)
                return false;

            // Check cooldown
            if (State == AbilityState.OnCooldown && !IsCooldownComplete())
                return false;

            // Check resources (without consuming)
            if (!HasResources())
                return false;

            return true;
        }

        /// <summary>
        /// Checks if the caster has enough resources.
        /// </summary>
        public bool HasResources()
        {
            if (Definition.Cost == null)
                return true;

            float available = GetCurrentResource(Definition.Cost.ResourceType);
            float cost = GetActualCost();

            return available >= cost;
        }

        /// <summary>
        /// Gets the actual resource cost accounting for percentage costs.
        /// </summary>
        public float GetActualCost()
        {
            if (Definition.Cost == null)
                return 0f;

            if (Definition.Cost.IsPercentage)
            {
                float max = GetMaxResource(Definition.Cost.ResourceType);
                return max * (Definition.Cost.Amount / 100f);
            }

            return Definition.Cost.Amount;
        }

        /// <summary>
        /// Updates the ability instance (call each frame while casting).
        /// </summary>
        public void Update()
        {
            switch (State)
            {
                case AbilityState.Casting:
                    UpdateCasting();
                    break;

                case AbilityState.OnCooldown:
                    if (IsCooldownComplete())
                    {
                        State = AbilityState.Ready;
                        Events.PrimeEvents.RaiseAbilityCooldownComplete(Caster, this);
                    }
                    break;
            }
        }

        /// <summary>
        /// Interrupts the current cast.
        /// </summary>
        /// <returns>True if interrupted</returns>
        public bool Interrupt()
        {
            if (State != AbilityState.Casting && State != AbilityState.Channeling)
                return false;

            if (!Definition.Interruptible)
                return false;

            State = AbilityState.Ready;
            Events.PrimeEvents.RaiseAbilityInterrupted(Caster, this);
            return true;
        }

        /// <summary>
        /// Gets remaining cooldown time.
        /// </summary>
        public float GetRemainingCooldown()
        {
            if (State != AbilityState.OnCooldown)
                return 0f;

            float elapsed = Time.time - CooldownStartTime;
            float remaining = EffectiveCooldown - elapsed;
            return Mathf.Max(0f, remaining);
        }

        /// <summary>
        /// Gets cooldown progress (0-1).
        /// </summary>
        public float GetCooldownProgress()
        {
            if (State != AbilityState.OnCooldown || EffectiveCooldown <= 0)
                return 1f;

            float elapsed = Time.time - CooldownStartTime;
            return Mathf.Clamp01(elapsed / EffectiveCooldown);
        }

        /// <summary>
        /// Gets casting progress (0-1).
        /// </summary>
        public float GetCastProgress()
        {
            if (State != AbilityState.Casting || Definition.CastTime <= 0)
                return 1f;

            float elapsed = Time.time - CastStartTime;
            return Mathf.Clamp01(elapsed / Definition.CastTime);
        }

        private void UpdateCasting()
        {
            float elapsed = Time.time - CastStartTime;
            if (elapsed >= Definition.CastTime)
            {
                Execute();
            }
        }

        private void Execute()
        {
            // Call OnUse handler
            if (Definition.OnUse != null)
            {
                if (!Definition.OnUse(Caster, Target, Definition))
                {
                    // Handler cancelled the ability
                    State = AbilityState.Ready;
                    return;
                }
            }

            // Play cast VFX via Spark (if available)
            if (!string.IsNullOrEmpty(Definition.CastVFX))
            {
                Core.VFXHelper.PlayOnCharacter(Definition.CastVFX, Caster);
            }

            // Apply self effects
            foreach (var effect in Definition.SelfEffects)
            {
                ApplyEffect(Caster, effect, "self");
            }

            // Calculate damage if applicable
            if (Definition.BaseDamage > 0 && Target != null)
            {
                float damage = CalculateDamage();

                // Check for crit
                bool isCrit = false;
                if (Definition.CanCrit)
                {
                    float critChance = PrimeAPI.Get(Caster, "CritChance");
                    isCrit = UnityEngine.Random.value < critChance;
                    if (isCrit)
                    {
                        float critDamage = PrimeAPI.Get(Caster, "CritDamage");
                        damage *= critDamage;
                    }
                }

                // Deal damage through combat system
                Combat.CombatManager.DealAbilityDamage(Caster, Target, Definition, damage, isCrit);

                // Play hit VFX on target via Spark (if available)
                if (!string.IsNullOrEmpty(Definition.HitVFX))
                {
                    Core.VFXHelper.PlayOnCharacter(Definition.HitVFX, Target);
                }

                // Apply target effects
                foreach (var effect in Definition.TargetEffects)
                {
                    ApplyEffect(Target, effect, "target");
                }

                // Call OnHit handler
                Definition.OnHit?.Invoke(Caster, Target, damage);
            }

            // Raise event
            Events.PrimeEvents.RaiseAbilityExecuted(Caster, this);

            // Start cooldown
            State = AbilityState.OnCooldown;
            CooldownStartTime = Time.time;
        }

        private float CalculateDamage()
        {
            float damage = Definition.BaseDamage;

            // Primary stat scaling
            if (!string.IsNullOrEmpty(Definition.ScalingStat))
            {
                float statValue = PrimeAPI.Get(Caster, Definition.ScalingStat);
                damage += statValue * Definition.ScalingFactor;
            }

            // Secondary stat scaling
            if (!string.IsNullOrEmpty(Definition.SecondaryScalingStat))
            {
                float statValue = PrimeAPI.Get(Caster, Definition.SecondaryScalingStat);
                damage += statValue * Definition.SecondaryScalingFactor;
            }

            // Apply damage type bonuses (e.g., FireDamage stat)
            string damageTypeStat = $"{Definition.DamageType}Damage";
            if (Stats.StatRegistry.Instance.IsRegistered(damageTypeStat))
            {
                float damageBonus = PrimeAPI.Get(Caster, damageTypeStat);
                damage *= (1f + damageBonus);
            }

            // Apply instance damage multiplier (from proc system)
            damage *= DamageMultiplier;

            return damage;
        }

        private void ApplyEffect(Character target, AbilityEffect effect, string suffix)
        {
            if (target == null || effect == null)
                return;

            string modId = $"{Definition.Id}_{effect.StatId}_{suffix}";

            var modifier = new Modifiers.Modifier(modId, effect.StatId, effect.ModifierType, effect.Value)
            {
                Duration = effect.Duration > 0 ? effect.Duration : (float?)null,
                Source = Definition.Id,
                StackBehavior = effect.StackBehavior,
                MaxStacks = effect.MaxStacks,
                Order = Modifiers.ModifierOrder.Buff
            };

            PrimeAPI.AddModifier(target, modifier);
        }

        private bool TryConsumeResources()
        {
            if (Definition.Cost == null)
                return true;

            float cost = GetActualCost();
            return ConsumeResource(Definition.Cost.ResourceType, cost);
        }

        private float GetCurrentResource(string resourceType)
        {
            return resourceType.ToLowerInvariant() switch
            {
                "health" => (Caster as Humanoid)?.GetHealth() ?? 0f,
                "stamina" => (Caster as Player)?.GetStamina() ?? 0f,
                "eitr" => (Caster as Player)?.GetEitr() ?? 0f,
                _ => 0f // Custom resources would need extension
            };
        }

        private float GetMaxResource(string resourceType)
        {
            return resourceType.ToLowerInvariant() switch
            {
                "health" => (Caster as Humanoid)?.GetMaxHealth() ?? 0f,
                "stamina" => (Caster as Player)?.GetMaxStamina() ?? 0f,
                "eitr" => (Caster as Player)?.GetMaxEitr() ?? 0f,
                _ => 0f
            };
        }

        private bool ConsumeResource(string resourceType, float amount)
        {
            switch (resourceType.ToLowerInvariant())
            {
                case "health":
                    // Don't consume health if it would kill
                    var health = Caster.GetHealth();
                    if (health <= amount) return false;
                    // Health consumption would need a Harmony patch
                    return true;

                case "stamina":
                    if (Caster is Player player1)
                    {
                        if (player1.GetStamina() < amount) return false;
                        player1.UseStamina(amount);
                        return true;
                    }
                    return false;

                case "eitr":
                    if (Caster is Player player2)
                    {
                        if (player2.GetEitr() < amount) return false;
                        player2.UseEitr(amount);
                        return true;
                    }
                    return false;

                default:
                    return true; // Custom resources always succeed for now
            }
        }

        private bool IsCooldownComplete()
        {
            return Time.time - CooldownStartTime >= EffectiveCooldown;
        }
    }

    /// <summary>
    /// State of an ability instance.
    /// </summary>
    public enum AbilityState
    {
        /// <summary>Ability is ready to be used.</summary>
        Ready,
        /// <summary>Ability is being cast.</summary>
        Casting,
        /// <summary>Ability is channeling (continuous effect).</summary>
        Channeling,
        /// <summary>Ability is on cooldown.</summary>
        OnCooldown,
        /// <summary>Ability is disabled.</summary>
        Disabled
    }
}
