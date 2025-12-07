namespace Prime.Procs
{
    /// <summary>
    /// Configuration for an item proc effect.
    /// Defines what ability to trigger, when, and under what conditions.
    /// </summary>
    public class ItemProcConfig
    {
        /// <summary>
        /// The Prime ability ID to execute when proc triggers.
        /// </summary>
        public string AbilityId { get; set; }

        /// <summary>
        /// What combat event triggers this proc.
        /// </summary>
        public ProcTrigger Trigger { get; set; }

        /// <summary>
        /// Chance to proc (0-1). 0 = always proc if conditions are met.
        /// </summary>
        public float ProcChance { get; set; }

        /// <summary>
        /// Minimum seconds between procs. Prevents spam.
        /// </summary>
        public float InternalCooldown { get; set; }

        /// <summary>
        /// Target health threshold (0-1). Proc only if target HP below this.
        /// 0 = no threshold check.
        /// </summary>
        public float TargetHealthThreshold { get; set; }

        /// <summary>
        /// Owner health threshold (0-1). Proc only if owner HP below this.
        /// 0 = no threshold check.
        /// </summary>
        public float OwnerHealthThreshold { get; set; }

        /// <summary>
        /// Multiplier for ability damage (1.0 = normal, 0.5 = half damage).
        /// </summary>
        public float DamageMultiplier { get; set; } = 1.0f;

        /// <summary>
        /// If true, ability costs no resources (stamina/eitr) when procced.
        /// </summary>
        public bool SkipResourceCost { get; set; } = true;

        /// <summary>
        /// Optional display name for UI.
        /// </summary>
        public string DisplayName { get; set; }

        /// <summary>
        /// Optional description for tooltips.
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Creates a default proc config.
        /// </summary>
        public ItemProcConfig()
        {
        }

        /// <summary>
        /// Creates a proc config with common settings.
        /// </summary>
        public ItemProcConfig(string abilityId, ProcTrigger trigger, float procChance, float internalCooldown)
        {
            AbilityId = abilityId;
            Trigger = trigger;
            ProcChance = procChance;
            InternalCooldown = internalCooldown;
        }

        /// <summary>
        /// Creates a copy of this config.
        /// </summary>
        public ItemProcConfig Clone()
        {
            return new ItemProcConfig
            {
                AbilityId = AbilityId,
                Trigger = Trigger,
                ProcChance = ProcChance,
                InternalCooldown = InternalCooldown,
                TargetHealthThreshold = TargetHealthThreshold,
                OwnerHealthThreshold = OwnerHealthThreshold,
                DamageMultiplier = DamageMultiplier,
                SkipResourceCost = SkipResourceCost,
                DisplayName = DisplayName,
                Description = Description
            };
        }
    }
}
