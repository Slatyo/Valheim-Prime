using System;

namespace Prime.Modifiers
{
    /// <summary>
    /// Represents a modifier that changes a stat's value.
    /// Modifiers can be permanent, timed, or conditional.
    /// </summary>
    /// <example>
    /// <code>
    /// // Flat +10 Strength from equipment
    /// var mod = new Modifier("sword_strength", "Strength", ModifierType.Flat, 10f)
    /// {
    ///     Source = "IronSword",
    ///     Order = ModifierOrder.Equipment
    /// };
    ///
    /// // Percentage buff from potion
    /// var buff = new Modifier("potion_str", "Strength", ModifierType.Percent, 15f)
    /// {
    ///     Duration = 120f,  // 2 minutes
    ///     Source = "StrengthPotion"
    /// };
    ///
    /// // Complex proc: "On damage taken, +50% armor for 5s"
    /// var proc = new Modifier("defensive_proc", "Armor", ModifierType.Percent, 50f)
    /// {
    ///     Duration = 5f,
    ///     Source = "DefensiveEnchant",
    ///     StackBehavior = StackBehavior.Refresh
    /// };
    /// </code>
    /// </example>
    public class Modifier
    {
        /// <summary>
        /// Unique identifier for this modifier instance.
        /// Used to update or remove specific modifiers.
        /// </summary>
        public string Id { get; }

        /// <summary>
        /// The stat this modifier affects.
        /// </summary>
        public string StatId { get; }

        /// <summary>
        /// How the modifier's value is applied (Flat, Percent, Multiply, Override).
        /// </summary>
        public ModifierType Type { get; }

        /// <summary>
        /// The modifier's value. Interpretation depends on Type:
        /// - Flat: Added directly (Value = 10 adds 10)
        /// - Percent: Added as percentage (Value = 15 adds 15%)
        /// - Multiply: Multiplied (Value = 1.5 multiplies by 1.5)
        /// - Override: Sets final value directly
        /// </summary>
        public float Value { get; set; }

        /// <summary>
        /// Determines calculation order. Lower values are applied first.
        /// Use ModifierOrder constants for consistency.
        /// </summary>
        public int Order { get; set; } = ModifierOrder.Default;

        /// <summary>
        /// Optional source identifier (mod name, item name, ability name).
        /// Useful for debugging and removing all modifiers from a source.
        /// </summary>
        public string Source { get; set; }

        /// <summary>
        /// Optional duration in seconds. Null means permanent.
        /// Timed modifiers are automatically removed when expired.
        /// </summary>
        public float? Duration { get; set; }

        /// <summary>
        /// Time when this modifier was applied. Used for duration tracking.
        /// </summary>
        public float AppliedTime { get; internal set; }

        /// <summary>
        /// How this modifier behaves when applied multiple times.
        /// </summary>
        public StackBehavior StackBehavior { get; set; } = StackBehavior.Replace;

        /// <summary>
        /// Maximum number of stacks if StackBehavior is Stack.
        /// </summary>
        public int MaxStacks { get; set; } = 1;

        /// <summary>
        /// Current stack count if StackBehavior is Stack.
        /// </summary>
        public int Stacks { get; internal set; } = 1;

        /// <summary>
        /// Optional condition that must be true for this modifier to apply.
        /// Evaluated each time the stat is calculated.
        /// </summary>
        public Func<bool> Condition { get; set; }

        /// <summary>
        /// Optional tags for filtering and querying modifiers.
        /// </summary>
        public string[] Tags { get; set; }

        /// <summary>
        /// If true, this modifier is hidden from UI displays.
        /// </summary>
        public bool Hidden { get; set; }

        /// <summary>
        /// Creates a new modifier.
        /// </summary>
        /// <param name="id">Unique identifier</param>
        /// <param name="statId">Target stat ID</param>
        /// <param name="type">How to apply the value</param>
        /// <param name="value">The modifier value</param>
        public Modifier(string id, string statId, ModifierType type, float value)
        {
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentException("Modifier ID cannot be null or empty", nameof(id));
            if (string.IsNullOrWhiteSpace(statId))
                throw new ArgumentException("Stat ID cannot be null or empty", nameof(statId));

            Id = id;
            StatId = statId;
            Type = type;
            Value = value;
        }

        /// <summary>
        /// Checks if this modifier has expired based on duration.
        /// </summary>
        /// <param name="currentTime">Current game time</param>
        /// <returns>True if expired and should be removed</returns>
        public bool IsExpired(float currentTime)
        {
            if (!Duration.HasValue)
                return false;

            return currentTime - AppliedTime >= Duration.Value;
        }

        /// <summary>
        /// Gets remaining duration in seconds, or null if permanent.
        /// </summary>
        /// <param name="currentTime">Current game time</param>
        /// <returns>Remaining seconds, or null if permanent</returns>
        public float? GetRemainingDuration(float currentTime)
        {
            if (!Duration.HasValue)
                return null;

            float remaining = Duration.Value - (currentTime - AppliedTime);
            return remaining > 0 ? remaining : 0;
        }

        /// <summary>
        /// Checks if this modifier's condition is met.
        /// </summary>
        /// <returns>True if condition is null or returns true</returns>
        public bool IsConditionMet()
        {
            return Condition == null || Condition();
        }

        /// <summary>
        /// Gets the effective value considering stacks.
        /// </summary>
        /// <returns>Value multiplied by stack count</returns>
        public float GetEffectiveValue()
        {
            return Value * Stacks;
        }

        /// <summary>
        /// Creates a copy of this modifier with a new ID.
        /// </summary>
        /// <param name="newId">ID for the copy</param>
        /// <returns>A new modifier with the same properties</returns>
        public Modifier Clone(string newId)
        {
            return new Modifier(newId, StatId, Type, Value)
            {
                Order = Order,
                Source = Source,
                Duration = Duration,
                StackBehavior = StackBehavior,
                MaxStacks = MaxStacks,
                Condition = Condition,
                Tags = Tags != null ? (string[])Tags.Clone() : null,
                Hidden = Hidden
            };
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            string typeSymbol = Type switch
            {
                ModifierType.Flat => "+",
                ModifierType.Percent => "%",
                ModifierType.Multiply => "x",
                ModifierType.Override => "=",
                _ => "?"
            };

            string valueStr = Type == ModifierType.Percent ? $"{typeSymbol}{Value}%" : $"{typeSymbol}{Value}";
            string durationStr = Duration.HasValue ? $" ({Duration.Value}s)" : "";

            return $"Modifier({Id}: {StatId} {valueStr}{durationStr})";
        }
    }

    /// <summary>
    /// How a modifier's value is applied to the stat.
    /// </summary>
    public enum ModifierType
    {
        /// <summary>Value is added directly to base. Applied first.</summary>
        Flat,
        /// <summary>Value is added as a percentage of (base + flat). Applied second.</summary>
        Percent,
        /// <summary>Result is multiplied by value. Applied third.</summary>
        Multiply,
        /// <summary>Overrides final value completely. Applied last. Use sparingly.</summary>
        Override
    }

    /// <summary>
    /// How a modifier behaves when applied multiple times with the same ID.
    /// </summary>
    public enum StackBehavior
    {
        /// <summary>New modifier replaces the old one.</summary>
        Replace,
        /// <summary>New application is ignored if modifier already exists.</summary>
        Ignore,
        /// <summary>Duration is refreshed but value stays the same.</summary>
        Refresh,
        /// <summary>Stack count increases up to MaxStacks, multiplying the effect.</summary>
        Stack,
        /// <summary>Multiple instances can exist independently.</summary>
        Independent
    }

    /// <summary>
    /// Standard modifier order values for consistent calculation.
    /// Lower values are applied first within each ModifierType.
    /// </summary>
    public static class ModifierOrder
    {
        /// <summary>Inherent bonuses, racial traits</summary>
        public const int Inherent = 0;

        /// <summary>Base equipment stats</summary>
        public const int Equipment = 100;

        /// <summary>Default order for unspecified modifiers</summary>
        public const int Default = 200;

        /// <summary>Buffs from abilities, potions</summary>
        public const int Buff = 300;

        /// <summary>Enchantment bonuses</summary>
        public const int Enchant = 400;

        /// <summary>Random affixes from loot</summary>
        public const int Affix = 500;

        /// <summary>Set bonuses, synergies</summary>
        public const int SetBonus = 600;

        /// <summary>Temporary combat effects</summary>
        public const int Combat = 700;

        /// <summary>Debuffs, penalties</summary>
        public const int Debuff = 800;

        /// <summary>Final adjustments, caps</summary>
        public const int Final = 1000;
    }
}
