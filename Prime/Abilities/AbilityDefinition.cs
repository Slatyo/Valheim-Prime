using System;
using System.Collections.Generic;
using UnityEngine;

namespace Prime.Abilities
{
    /// <summary>
    /// Defines an ability that can be used by players, creatures, or items.
    /// Abilities are templates - AbilityInstance represents an active use.
    /// </summary>
    /// <example>
    /// <code>
    /// // Define a fireball ability
    /// var fireball = new AbilityDefinition("Fireball")
    /// {
    ///     DisplayName = "$ability_fireball",
    ///     Description = "$ability_fireball_desc",
    ///     BaseCooldown = 5f,
    ///     ResourceCost = new ResourceCost("Eitr", 25f),
    ///     TargetType = AbilityTargetType.Projectile,
    ///     DamageType = DamageType.Fire,
    ///     BaseDamage = 50f,
    ///     ScalingStat = "Intelligence",
    ///     ScalingFactor = 2.5f  // +2.5 damage per Int
    /// };
    ///
    /// // Define a buff ability
    /// var warcry = new AbilityDefinition("WarCry")
    /// {
    ///     DisplayName = "$ability_warcry",
    ///     TargetType = AbilityTargetType.Self,
    ///     Effects = new[]
    ///     {
    ///         new AbilityEffect("Strength", ModifierType.Percent, 25f, 10f),
    ///         new AbilityEffect("Armor", ModifierType.Percent, 15f, 10f)
    ///     }
    /// };
    /// </code>
    /// </example>
    public class AbilityDefinition
    {
        /// <summary>
        /// Unique identifier for this ability.
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
        /// Base cooldown in seconds before CooldownReduction.
        /// </summary>
        public float BaseCooldown { get; set; } = 1f;

        /// <summary>
        /// Resource cost to use this ability.
        /// </summary>
        public ResourceCost Cost { get; set; }

        /// <summary>
        /// How the ability selects its target.
        /// </summary>
        public AbilityTargetType TargetType { get; set; } = AbilityTargetType.None;

        /// <summary>
        /// Maximum range for targeted abilities.
        /// </summary>
        public float Range { get; set; } = 10f;

        /// <summary>
        /// Radius for AoE abilities.
        /// </summary>
        public float Radius { get; set; } = 0f;

        /// <summary>
        /// Base damage dealt (before scaling).
        /// </summary>
        public float BaseDamage { get; set; } = 0f;

        /// <summary>
        /// Type of damage dealt.
        /// </summary>
        public DamageType DamageType { get; set; } = DamageType.Physical;

        /// <summary>
        /// Stat used for damage scaling.
        /// </summary>
        public string ScalingStat { get; set; }

        /// <summary>
        /// Damage added per point of ScalingStat.
        /// </summary>
        public float ScalingFactor { get; set; } = 0f;

        /// <summary>
        /// Stat used for secondary scaling (optional).
        /// </summary>
        public string SecondaryScalingStat { get; set; }

        /// <summary>
        /// Damage added per point of SecondaryScalingStat.
        /// </summary>
        public float SecondaryScalingFactor { get; set; } = 0f;

        /// <summary>
        /// Can this ability critically strike?
        /// </summary>
        public bool CanCrit { get; set; } = true;

        /// <summary>
        /// Modifiers applied to self when ability is used.
        /// </summary>
        public List<AbilityEffect> SelfEffects { get; set; } = new List<AbilityEffect>();

        /// <summary>
        /// Modifiers applied to target(s) when ability hits.
        /// </summary>
        public List<AbilityEffect> TargetEffects { get; set; } = new List<AbilityEffect>();

        /// <summary>
        /// Projectile prefab name for projectile abilities.
        /// </summary>
        public string ProjectilePrefab { get; set; }

        /// <summary>
        /// Projectile speed.
        /// </summary>
        public float ProjectileSpeed { get; set; } = 20f;

        /// <summary>
        /// Animation trigger name.
        /// </summary>
        public string Animation { get; set; }

        /// <summary>
        /// VFX prefab name to spawn on cast.
        /// </summary>
        public string CastVFX { get; set; }

        /// <summary>
        /// VFX prefab name to spawn on hit.
        /// </summary>
        public string HitVFX { get; set; }

        /// <summary>
        /// Sound effect on cast.
        /// </summary>
        public string CastSFX { get; set; }

        /// <summary>
        /// Sound effect on hit.
        /// </summary>
        public string HitSFX { get; set; }

        /// <summary>
        /// Cast time in seconds (0 = instant).
        /// </summary>
        public float CastTime { get; set; } = 0f;

        /// <summary>
        /// Can the ability be interrupted during cast?
        /// </summary>
        public bool Interruptible { get; set; } = true;

        /// <summary>
        /// Can the caster move while casting?
        /// </summary>
        public bool CanMoveWhileCasting { get; set; } = false;

        /// <summary>
        /// Tags for filtering and categorization.
        /// </summary>
        public HashSet<string> Tags { get; set; } = new HashSet<string>();

        /// <summary>
        /// Category for UI grouping.
        /// </summary>
        public AbilityCategory Category { get; set; } = AbilityCategory.Active;

        /// <summary>
        /// Custom data for mod-specific extensions.
        /// </summary>
        public Dictionary<string, object> CustomData { get; set; } = new Dictionary<string, object>();

        /// <summary>
        /// Handler called when ability is used. Return false to cancel.
        /// Parameters: caster, target (may be null), ability
        /// </summary>
        public Func<Character, Character, AbilityDefinition, bool> OnUse { get; set; }

        /// <summary>
        /// Handler called when ability hits a target.
        /// Parameters: caster, target, damage dealt
        /// </summary>
        public Action<Character, Character, float> OnHit { get; set; }

        /// <summary>
        /// Condition that must be true to use this ability.
        /// Parameters: caster
        /// </summary>
        public Func<Character, bool> UseCondition { get; set; }

        /// <summary>
        /// Creates a new ability definition with the specified ID.
        /// </summary>
        /// <param name="id">Unique identifier for this ability</param>
        public AbilityDefinition(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentException("Ability ID cannot be null or empty", nameof(id));

            Id = id;
            DisplayName = $"$ability_{id.ToLowerInvariant()}";
            Description = $"$ability_{id.ToLowerInvariant()}_desc";
        }

        /// <summary>
        /// Creates a copy of this definition with a new ID.
        /// Useful for creating variants.
        /// </summary>
        public AbilityDefinition Clone(string newId)
        {
            var clone = new AbilityDefinition(newId)
            {
                DisplayName = DisplayName,
                Description = Description,
                Icon = Icon,
                BaseCooldown = BaseCooldown,
                Cost = Cost?.Clone(),
                TargetType = TargetType,
                Range = Range,
                Radius = Radius,
                BaseDamage = BaseDamage,
                DamageType = DamageType,
                ScalingStat = ScalingStat,
                ScalingFactor = ScalingFactor,
                SecondaryScalingStat = SecondaryScalingStat,
                SecondaryScalingFactor = SecondaryScalingFactor,
                CanCrit = CanCrit,
                ProjectilePrefab = ProjectilePrefab,
                ProjectileSpeed = ProjectileSpeed,
                Animation = Animation,
                CastVFX = CastVFX,
                HitVFX = HitVFX,
                CastSFX = CastSFX,
                HitSFX = HitSFX,
                CastTime = CastTime,
                Interruptible = Interruptible,
                CanMoveWhileCasting = CanMoveWhileCasting,
                Category = Category,
                OnUse = OnUse,
                OnHit = OnHit,
                UseCondition = UseCondition
            };

            clone.SelfEffects.AddRange(SelfEffects);
            clone.TargetEffects.AddRange(TargetEffects);
            clone.Tags = new HashSet<string>(Tags);
            clone.CustomData = new Dictionary<string, object>(CustomData);

            return clone;
        }

        /// <inheritdoc/>
        public override string ToString() => $"Ability({Id})";
    }

    /// <summary>
    /// Effect applied by an ability (buff/debuff).
    /// </summary>
    public class AbilityEffect
    {
        /// <summary>
        /// The stat to modify.
        /// </summary>
        public string StatId { get; set; }

        /// <summary>
        /// How the modifier is applied.
        /// </summary>
        public Modifiers.ModifierType ModifierType { get; set; }

        /// <summary>
        /// The modifier value.
        /// </summary>
        public float Value { get; set; }

        /// <summary>
        /// Duration in seconds (0 = instant/permanent).
        /// </summary>
        public float Duration { get; set; }

        /// <summary>
        /// Stack behavior for repeated applications.
        /// </summary>
        public Modifiers.StackBehavior StackBehavior { get; set; } = Modifiers.StackBehavior.Refresh;

        /// <summary>
        /// Maximum stacks if StackBehavior is Stack.
        /// </summary>
        public int MaxStacks { get; set; } = 1;

        /// <summary>
        /// VFX to show while effect is active.
        /// </summary>
        public string ActiveVFX { get; set; }

        /// <summary>
        /// Creates an empty ability effect.
        /// </summary>
        public AbilityEffect() { }

        /// <summary>
        /// Creates an ability effect with the specified parameters.
        /// </summary>
        /// <param name="statId">The stat to modify</param>
        /// <param name="type">How the modifier is applied</param>
        /// <param name="value">The modifier value</param>
        /// <param name="duration">Duration in seconds (0 = instant)</param>
        public AbilityEffect(string statId, Modifiers.ModifierType type, float value, float duration = 0f)
        {
            StatId = statId;
            ModifierType = type;
            Value = value;
            Duration = duration;
        }
    }

    /// <summary>
    /// Resource cost for using an ability.
    /// </summary>
    public class ResourceCost
    {
        /// <summary>
        /// Resource type: "Health", "Stamina", "Eitr", or custom.
        /// </summary>
        public string ResourceType { get; set; }

        /// <summary>
        /// Amount of resource consumed.
        /// </summary>
        public float Amount { get; set; }

        /// <summary>
        /// If true, cost is a percentage of max resource.
        /// </summary>
        public bool IsPercentage { get; set; }

        /// <summary>
        /// Creates an empty resource cost.
        /// </summary>
        public ResourceCost() { }

        /// <summary>
        /// Creates a resource cost with the specified parameters.
        /// </summary>
        /// <param name="resourceType">Resource type (Health, Stamina, Eitr, or custom)</param>
        /// <param name="amount">Amount of resource consumed</param>
        /// <param name="isPercentage">If true, cost is a percentage of max resource</param>
        public ResourceCost(string resourceType, float amount, bool isPercentage = false)
        {
            ResourceType = resourceType;
            Amount = amount;
            IsPercentage = isPercentage;
        }

        /// <summary>
        /// Creates a copy of this resource cost.
        /// </summary>
        /// <returns>A new ResourceCost with the same values</returns>
        public ResourceCost Clone() => new ResourceCost(ResourceType, Amount, IsPercentage);
    }

    /// <summary>
    /// How an ability selects its target.
    /// </summary>
    public enum AbilityTargetType
    {
        /// <summary>No targeting required (passive or toggle).</summary>
        None,
        /// <summary>Affects the caster.</summary>
        Self,
        /// <summary>Single enemy target.</summary>
        Enemy,
        /// <summary>Single friendly target.</summary>
        Friendly,
        /// <summary>Any character.</summary>
        Any,
        /// <summary>Point on ground.</summary>
        Ground,
        /// <summary>Direction from caster.</summary>
        Direction,
        /// <summary>Fires a projectile.</summary>
        Projectile,
        /// <summary>Cone in front of caster.</summary>
        Cone,
        /// <summary>Area around caster.</summary>
        AroundSelf,
        /// <summary>Area around target point.</summary>
        AreaOfEffect
    }

    /// <summary>
    /// Category for ability UI grouping.
    /// </summary>
    public enum AbilityCategory
    {
        /// <summary>Active ability with cooldown.</summary>
        Active,
        /// <summary>Passive always-on effect.</summary>
        Passive,
        /// <summary>Toggle on/off ability.</summary>
        Toggle,
        /// <summary>Ultimate/signature ability.</summary>
        Ultimate,
        /// <summary>Racial or class trait.</summary>
        Trait,
        /// <summary>Item-granted ability.</summary>
        Item
    }

    /// <summary>
    /// Types of damage that can be dealt.
    /// </summary>
    public enum DamageType
    {
        /// <summary>Generic physical damage.</summary>
        Physical,
        /// <summary>Fire elemental damage.</summary>
        Fire,
        /// <summary>Frost elemental damage.</summary>
        Frost,
        /// <summary>Lightning elemental damage.</summary>
        Lightning,
        /// <summary>Poison damage over time.</summary>
        Poison,
        /// <summary>Spirit/holy damage.</summary>
        Spirit,
        /// <summary>Blunt physical damage.</summary>
        Blunt,
        /// <summary>Slashing physical damage.</summary>
        Slash,
        /// <summary>Piercing physical damage.</summary>
        Pierce,
        /// <summary>Chopping damage for trees.</summary>
        Chop,
        /// <summary>Pickaxe damage for mining.</summary>
        Pickaxe,
        /// <summary>True damage that ignores armor and resistances.</summary>
        True
    }
}
