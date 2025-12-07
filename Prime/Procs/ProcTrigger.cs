namespace Prime.Procs
{
    /// <summary>
    /// Defines when an item proc can trigger.
    /// </summary>
    public enum ProcTrigger
    {
        /// <summary>
        /// Triggers when dealing damage to an enemy.
        /// </summary>
        OnHit,

        /// <summary>
        /// Triggers when landing a critical hit.
        /// </summary>
        OnCrit,

        /// <summary>
        /// Triggers when killing an enemy.
        /// </summary>
        OnKill,

        /// <summary>
        /// Triggers when taking damage from an enemy.
        /// </summary>
        OnHitTaken,

        /// <summary>
        /// Triggers when blocking an attack.
        /// </summary>
        OnBlock,

        /// <summary>
        /// Triggers when owner's health drops below threshold.
        /// </summary>
        OnLowHealth
    }
}
