using System;
using System.Collections.Generic;
using UnityEngine;

namespace Prime.Effects
{
    /// <summary>
    /// Defines an effect that can be applied to entities.
    /// Effects can be passive buffs, DoTs, procs, auras, etc.
    /// </summary>
    /// <example>
    /// <code>
    /// // Damage over time effect
    /// var bleed = new EffectDefinition("Bleed")
    /// {
    ///     Duration = 6f,
    ///     TickInterval = 1f,
    ///     OnTick = (owner) => CombatManager.DealDirectDamage(null, owner, DamageType.Physical, 5f)
    /// };
    ///
    /// // On-hit proc: "On hit, 20% chance to cast frost nova"
    /// var frostProc = new EffectDefinition("FrostNova_OnHit")
    /// {
    ///     Trigger = EffectTrigger.OnHit,
    ///     ProcChance = 0.20f,
    ///     Cooldown = 5f,
    ///     OnProc = (owner, target, damage) => CastFrostNova(owner)
    /// };
    ///
    /// // Defensive proc: "When taking damage over 50, gain +50% armor for 5s"
    /// var defenseProc = new EffectDefinition("DefensiveStance")
    /// {
    ///     Trigger = EffectTrigger.OnDamageTaken,
    ///     ProcCondition = (owner, attacker, damage) => damage.FinalDamage > 50,
    ///     OnProc = (owner, attacker, damage) => {
    ///         PrimeAPI.ApplyTimedPercent(owner, "Armor", 50f, 5f, "DefensiveStance");
    ///     }
    /// };
    /// </code>
    /// </example>
    public class EffectDefinition
    {
        /// <summary>
        /// Unique identifier for this effect.
        /// </summary>
        public string Id { get; }

        /// <summary>
        /// Localization key for display name.
        /// </summary>
        public string DisplayName { get; set; }

        /// <summary>
        /// Localization key for description.
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Icon for UI display.
        /// </summary>
        public Sprite Icon { get; set; }

        /// <summary>
        /// Duration of the effect. 0 or negative = permanent until removed.
        /// </summary>
        public float Duration { get; set; } = 0f;

        /// <summary>
        /// Time between ticks for periodic effects.
        /// </summary>
        public float TickInterval { get; set; } = 1f;

        /// <summary>
        /// When does this effect trigger?
        /// </summary>
        public EffectTrigger Trigger { get; set; } = EffectTrigger.None;

        /// <summary>
        /// Chance to proc (0-1). 1 = always.
        /// </summary>
        public float ProcChance { get; set; } = 1f;

        /// <summary>
        /// Cooldown between procs.
        /// </summary>
        public float Cooldown { get; set; } = 0f;

        /// <summary>
        /// How this effect stacks with itself.
        /// </summary>
        public EffectStackBehavior StackBehavior { get; set; } = EffectStackBehavior.Refresh;

        /// <summary>
        /// Maximum stacks if StackBehavior is Stack.
        /// </summary>
        public int MaxStacks { get; set; } = 1;

        /// <summary>
        /// VFX prefab to show while effect is active.
        /// </summary>
        public string ActiveVFX { get; set; }

        /// <summary>
        /// VFX prefab to show on proc.
        /// </summary>
        public string ProcVFX { get; set; }

        /// <summary>
        /// Sound to play on proc.
        /// </summary>
        public string ProcSFX { get; set; }

        /// <summary>
        /// Tags for filtering and categorization.
        /// </summary>
        public HashSet<string> Tags { get; set; } = new HashSet<string>();

        /// <summary>
        /// Is this effect considered a buff (beneficial)?
        /// </summary>
        public bool IsBuff { get; set; } = true;

        /// <summary>
        /// Can this effect be dispelled/removed?
        /// </summary>
        public bool Dispellable { get; set; } = true;

        /// <summary>
        /// Priority for determining which effects can be dispelled first.
        /// Higher = harder to dispel.
        /// </summary>
        public int Priority { get; set; } = 0;

        /// <summary>
        /// Custom data storage.
        /// </summary>
        public Dictionary<string, object> CustomData { get; set; } = new Dictionary<string, object>();

        // ==================== HANDLERS ====================

        /// <summary>
        /// Called when effect is first applied.
        /// Parameters: owner
        /// </summary>
        public Action<Character> OnApply { get; set; }

        /// <summary>
        /// Called when effect is removed (expires or dispelled).
        /// Parameters: owner
        /// </summary>
        public Action<Character> OnRemove { get; set; }

        /// <summary>
        /// Called each tick for periodic effects.
        /// Parameters: owner
        /// </summary>
        public Action<Character> OnTick { get; set; }

        /// <summary>
        /// Called when effect stacks.
        /// Parameters: owner, newStackCount
        /// </summary>
        public Action<Character, int> OnStack { get; set; }

        /// <summary>
        /// Called when effect procs (for triggered effects).
        /// Parameters: owner, target (may be null), damageInfo (may be null)
        /// </summary>
        public Action<Character, Character, Combat.DamageInfo> OnProc { get; set; }

        /// <summary>
        /// Condition that must be true for proc to trigger.
        /// Parameters: owner, target, damageInfo
        /// Return: true to allow proc
        /// </summary>
        public Func<Character, Character, Combat.DamageInfo, bool> ProcCondition { get; set; }

        /// <summary>
        /// Called each frame while effect is active.
        /// Parameters: owner, deltaTime
        /// </summary>
        public Action<Character, float> OnUpdate { get; set; }

        public EffectDefinition(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentException("Effect ID cannot be null or empty", nameof(id));

            Id = id;
            DisplayName = $"$effect_{id.ToLowerInvariant()}";
            Description = $"$effect_{id.ToLowerInvariant()}_desc";
        }

        /// <summary>
        /// Creates a copy of this effect with a new ID.
        /// </summary>
        public EffectDefinition Clone(string newId)
        {
            return new EffectDefinition(newId)
            {
                DisplayName = DisplayName,
                Description = Description,
                Icon = Icon,
                Duration = Duration,
                TickInterval = TickInterval,
                Trigger = Trigger,
                ProcChance = ProcChance,
                Cooldown = Cooldown,
                StackBehavior = StackBehavior,
                MaxStacks = MaxStacks,
                ActiveVFX = ActiveVFX,
                ProcVFX = ProcVFX,
                ProcSFX = ProcSFX,
                Tags = new HashSet<string>(Tags),
                IsBuff = IsBuff,
                Dispellable = Dispellable,
                Priority = Priority,
                CustomData = new Dictionary<string, object>(CustomData),
                OnApply = OnApply,
                OnRemove = OnRemove,
                OnTick = OnTick,
                OnStack = OnStack,
                OnProc = OnProc,
                ProcCondition = ProcCondition,
                OnUpdate = OnUpdate
            };
        }

        public override string ToString() => $"Effect({Id})";
    }

    /// <summary>
    /// When an effect is triggered.
    /// </summary>
    public enum EffectTrigger
    {
        /// <summary>No trigger - passive effect.</summary>
        None,

        /// <summary>Triggers when dealing damage.</summary>
        OnHit,

        /// <summary>Triggers when dealing a critical hit.</summary>
        OnCrit,

        /// <summary>Triggers when killing an enemy.</summary>
        OnKill,

        /// <summary>Triggers when taking damage.</summary>
        OnDamageTaken,

        /// <summary>Triggers when blocking damage.</summary>
        OnBlock,

        /// <summary>Triggers when dodging an attack.</summary>
        OnDodge,

        /// <summary>Triggers when using an ability.</summary>
        OnAbilityUse,

        /// <summary>Triggers when health falls below threshold.</summary>
        OnLowHealth,

        /// <summary>Triggers when entering combat.</summary>
        OnCombatStart,

        /// <summary>Triggers when leaving combat.</summary>
        OnCombatEnd,

        /// <summary>Triggers when healing.</summary>
        OnHeal,

        /// <summary>Triggers when receiving a buff.</summary>
        OnBuffReceived,

        /// <summary>Triggers when receiving a debuff.</summary>
        OnDebuffReceived
    }

    /// <summary>
    /// How an effect behaves when applied multiple times.
    /// </summary>
    public enum EffectStackBehavior
    {
        /// <summary>New application replaces the old one.</summary>
        Replace,

        /// <summary>New application is ignored.</summary>
        Ignore,

        /// <summary>Duration is refreshed but other properties stay the same.</summary>
        Refresh,

        /// <summary>Stack count increases, multiplying the effect.</summary>
        Stack,

        /// <summary>Multiple independent instances can exist.</summary>
        Independent
    }
}
