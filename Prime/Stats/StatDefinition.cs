using System;

namespace Prime.Stats
{
    /// <summary>
    /// Defines a stat type that can be registered with the Prime system.
    /// Stats are identified by a unique string ID and have configurable base values and bounds.
    /// </summary>
    /// <example>
    /// <code>
    /// // Register a new stat
    /// PrimeStats.Register(new StatDefinition("Strength")
    /// {
    ///     DisplayName = "$stat_strength",
    ///     Description = "$stat_strength_desc",
    ///     BaseValue = 10f,
    ///     MinValue = 0f,
    ///     MaxValue = 999f,
    ///     Category = StatCategory.Attribute
    /// });
    /// </code>
    /// </example>
    public class StatDefinition
    {
        /// <summary>
        /// Unique identifier for this stat. Used in all API calls.
        /// Convention: PascalCase, no spaces (e.g., "Strength", "CritChance", "FireResist")
        /// </summary>
        public string Id { get; }

        /// <summary>
        /// Localization key for display name. Should start with $ for Valheim localization.
        /// </summary>
        public string DisplayName { get; set; }

        /// <summary>
        /// Localization key for description. Should start with $ for Valheim localization.
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Default value when an entity first gets this stat.
        /// </summary>
        public float BaseValue { get; set; }

        /// <summary>
        /// Minimum allowed final value after all modifiers. Null means no minimum.
        /// </summary>
        public float? MinValue { get; set; }

        /// <summary>
        /// Maximum allowed final value after all modifiers. Null means no maximum.
        /// </summary>
        public float? MaxValue { get; set; }

        /// <summary>
        /// Category for UI grouping and filtering.
        /// </summary>
        public StatCategory Category { get; set; } = StatCategory.Misc;

        /// <summary>
        /// How this stat's value should be displayed (percentage, flat number, etc.)
        /// </summary>
        public StatDisplayType DisplayType { get; set; } = StatDisplayType.Number;

        /// <summary>
        /// Number of decimal places to show in UI. Default is 0 for whole numbers.
        /// </summary>
        public int DecimalPlaces { get; set; } = 0;

        /// <summary>
        /// Optional icon for UI display.
        /// </summary>
        public UnityEngine.Sprite Icon { get; set; }

        /// <summary>
        /// If true, higher values are considered better (for color coding in UI).
        /// </summary>
        public bool HigherIsBetter { get; set; } = true;

        /// <summary>
        /// If true, this stat is hidden from standard UI displays.
        /// Useful for internal calculation stats.
        /// </summary>
        public bool Hidden { get; set; } = false;

        /// <summary>
        /// Optional tags for filtering and querying stats.
        /// </summary>
        public string[] Tags { get; set; } = Array.Empty<string>();

        /// <summary>
        /// Creates a new stat definition with the specified ID.
        /// </summary>
        /// <param name="id">Unique identifier for this stat</param>
        /// <exception cref="ArgumentException">Thrown if id is null or whitespace</exception>
        public StatDefinition(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentException("Stat ID cannot be null or empty", nameof(id));

            Id = id;
            DisplayName = $"$stat_{id.ToLowerInvariant()}";
            Description = $"$stat_{id.ToLowerInvariant()}_desc";
        }

        /// <summary>
        /// Creates a new stat definition with the specified ID and base value.
        /// </summary>
        /// <param name="id">Unique identifier for this stat</param>
        /// <param name="baseValue">Default value for this stat</param>
        public StatDefinition(string id, float baseValue) : this(id)
        {
            BaseValue = baseValue;
        }

        /// <summary>
        /// Creates a new stat definition with ID, base value, and bounds.
        /// </summary>
        /// <param name="id">Unique identifier for this stat</param>
        /// <param name="baseValue">Default value for this stat</param>
        /// <param name="min">Minimum allowed value (null for no minimum)</param>
        /// <param name="max">Maximum allowed value (null for no maximum)</param>
        public StatDefinition(string id, float baseValue, float? min, float? max = null) : this(id, baseValue)
        {
            MinValue = min;
            MaxValue = max;
        }

        /// <summary>
        /// Clamps a value to this stat's min/max bounds.
        /// </summary>
        /// <param name="value">Value to clamp</param>
        /// <returns>Clamped value</returns>
        public float Clamp(float value)
        {
            if (MinValue.HasValue && value < MinValue.Value)
                return MinValue.Value;
            if (MaxValue.HasValue && value > MaxValue.Value)
                return MaxValue.Value;
            return value;
        }

        public override string ToString() => $"StatDef({Id}, base={BaseValue})";
    }

    /// <summary>
    /// Categories for grouping stats in UI and queries.
    /// </summary>
    public enum StatCategory
    {
        /// <summary>Core attributes like Strength, Dexterity, Intelligence</summary>
        Attribute,
        /// <summary>Resource pools like Health, Stamina, Eitr</summary>
        Resource,
        /// <summary>Offensive stats like Damage, CritChance, AttackSpeed</summary>
        Offense,
        /// <summary>Defensive stats like Armor, BlockPower, Resistances</summary>
        Defense,
        /// <summary>Movement stats like MoveSpeed, JumpHeight</summary>
        Movement,
        /// <summary>Utility stats like CarryWeight, SkillGain</summary>
        Utility,
        /// <summary>Elemental resistances</summary>
        Resistance,
        /// <summary>Uncategorized stats</summary>
        Misc
    }

    /// <summary>
    /// How a stat value should be displayed in UI.
    /// </summary>
    public enum StatDisplayType
    {
        /// <summary>Display as a flat number (e.g., "150")</summary>
        Number,
        /// <summary>Display as percentage (e.g., "15%")</summary>
        Percent,
        /// <summary>Display as multiplier (e.g., "x1.5")</summary>
        Multiplier,
        /// <summary>Display as time in seconds (e.g., "4.5s")</summary>
        Seconds,
        /// <summary>Display as boolean (e.g., "Yes/No")</summary>
        Boolean
    }
}
