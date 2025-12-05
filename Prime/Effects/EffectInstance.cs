using System;
using UnityEngine;

namespace Prime.Effects
{
    /// <summary>
    /// Represents an active effect on an entity.
    /// </summary>
    public class EffectInstance
    {
        /// <summary>
        /// The effect definition.
        /// </summary>
        public EffectDefinition Definition { get; }

        /// <summary>
        /// The entity this effect is on.
        /// </summary>
        public Character Owner { get; }

        /// <summary>
        /// Unique instance ID.
        /// </summary>
        public string InstanceId { get; }

        /// <summary>
        /// Current stack count.
        /// </summary>
        public int Stacks { get; internal set; } = 1;

        /// <summary>
        /// Time when effect was applied.
        /// </summary>
        public float AppliedTime { get; private set; }

        /// <summary>
        /// Time of last tick.
        /// </summary>
        public float LastTickTime { get; private set; }

        /// <summary>
        /// Time of last proc.
        /// </summary>
        public float LastProcTime { get; private set; }

        /// <summary>
        /// Is the effect still active?
        /// </summary>
        public bool IsActive { get; internal set; } = true;

        /// <summary>
        /// Custom data for this instance.
        /// </summary>
        public object UserData { get; set; }

        /// <summary>
        /// Creates a new effect instance.
        /// </summary>
        /// <param name="definition">The effect definition</param>
        /// <param name="owner">The character affected by this effect</param>
        public EffectInstance(EffectDefinition definition, Character owner)
        {
            Definition = definition ?? throw new ArgumentNullException(nameof(definition));
            Owner = owner ?? throw new ArgumentNullException(nameof(owner));
            InstanceId = $"{definition.Id}_{Guid.NewGuid():N}";
            AppliedTime = Time.time;
            LastTickTime = Time.time;
            LastProcTime = 0f;
        }

        /// <summary>
        /// Gets remaining duration, or -1 if permanent.
        /// </summary>
        public float GetRemainingDuration()
        {
            if (Definition.Duration <= 0)
                return -1f;

            float elapsed = Time.time - AppliedTime;
            return Mathf.Max(0f, Definition.Duration - elapsed);
        }

        /// <summary>
        /// Checks if the effect has expired.
        /// </summary>
        public bool IsExpired()
        {
            if (Definition.Duration <= 0)
                return false;

            return Time.time - AppliedTime >= Definition.Duration;
        }

        /// <summary>
        /// Refreshes the effect duration.
        /// </summary>
        public void Refresh()
        {
            AppliedTime = Time.time;
        }

        /// <summary>
        /// Checks if proc is off cooldown.
        /// </summary>
        public bool CanProc()
        {
            if (Definition.Cooldown <= 0)
                return true;

            return Time.time - LastProcTime >= Definition.Cooldown;
        }

        /// <summary>
        /// Attempts to trigger the proc.
        /// </summary>
        /// <param name="target">Target of the trigger (may be null)</param>
        /// <param name="damageInfo">Damage info if applicable (may be null)</param>
        /// <returns>True if proc occurred</returns>
        public bool TryProc(Character target, Combat.DamageInfo damageInfo)
        {
            if (Definition.OnProc == null)
                return false;

            if (!CanProc())
                return false;

            // Check proc condition
            if (Definition.ProcCondition != null)
            {
                if (!Definition.ProcCondition(Owner, target, damageInfo))
                    return false;
            }

            // Check proc chance
            if (Definition.ProcChance < 1f && UnityEngine.Random.value > Definition.ProcChance)
                return false;

            // Proc!
            LastProcTime = Time.time;

            try
            {
                Definition.OnProc(Owner, target, damageInfo);
            }
            catch (Exception ex)
            {
                Plugin.Log?.LogError($"[Prime] Error in effect proc {Definition.Id}: {ex}");
            }

            return true;
        }

        /// <summary>
        /// Updates the effect (call each frame).
        /// </summary>
        public void Update(float deltaTime)
        {
            if (!IsActive)
                return;

            // Check expiration
            if (IsExpired())
            {
                IsActive = false;
                return;
            }

            // Per-frame update
            if (Definition.OnUpdate != null)
            {
                try
                {
                    Definition.OnUpdate(Owner, deltaTime);
                }
                catch (Exception ex)
                {
                    Plugin.Log?.LogError($"[Prime] Error in effect update {Definition.Id}: {ex}");
                }
            }

            // Tick update
            if (Definition.OnTick != null && Definition.TickInterval > 0)
            {
                if (Time.time - LastTickTime >= Definition.TickInterval)
                {
                    LastTickTime = Time.time;
                    try
                    {
                        Definition.OnTick(Owner);
                    }
                    catch (Exception ex)
                    {
                        Plugin.Log?.LogError($"[Prime] Error in effect tick {Definition.Id}: {ex}");
                    }
                }
            }
        }

        /// <summary>
        /// Removes this effect.
        /// </summary>
        public void Remove()
        {
            if (!IsActive)
                return;

            IsActive = false;

            if (Definition.OnRemove != null)
            {
                try
                {
                    Definition.OnRemove(Owner);
                }
                catch (Exception ex)
                {
                    Plugin.Log?.LogError($"[Prime] Error in effect remove {Definition.Id}: {ex}");
                }
            }
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            string stackStr = Stacks > 1 ? $" x{Stacks}" : "";
            string durationStr = Definition.Duration > 0 ? $" ({GetRemainingDuration():F1}s)" : "";
            return $"EffectInstance({Definition.Id}{stackStr}{durationStr})";
        }
    }
}
