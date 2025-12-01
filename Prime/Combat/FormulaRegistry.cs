using System;
using System.Collections.Generic;

namespace Prime.Combat
{
    /// <summary>
    /// Registry for overridable combat formulas.
    /// Mods can register custom formulas to change how damage, crit, etc. are calculated.
    /// </summary>
    public static class FormulaRegistry
    {
        private static readonly Dictionary<string, Delegate> _formulas =
            new Dictionary<string, Delegate>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Registers a custom formula, overriding any existing one.
        /// </summary>
        /// <typeparam name="T">The delegate type for this formula</typeparam>
        /// <param name="formulaId">Unique ID for this formula</param>
        /// <param name="formula">The formula delegate</param>
        /// <param name="source">Source mod for logging</param>
        public static void Register<T>(string formulaId, T formula, string source = null) where T : Delegate
        {
            if (string.IsNullOrEmpty(formulaId))
                throw new ArgumentException("Formula ID cannot be null or empty", nameof(formulaId));

            if (formula == null)
                throw new ArgumentNullException(nameof(formula));

            bool isOverride = _formulas.ContainsKey(formulaId);
            _formulas[formulaId] = formula;

            string action = isOverride ? "Overriding" : "Registering";
            string sourceStr = source != null ? $" from {source}" : "";
            Plugin.Log?.LogDebug($"[Prime] {action} formula '{formulaId}'{sourceStr}");
        }

        /// <summary>
        /// Gets a registered formula.
        /// </summary>
        /// <typeparam name="T">The delegate type</typeparam>
        /// <param name="formulaId">The formula ID</param>
        /// <returns>The formula delegate, or null if not found</returns>
        public static T Get<T>(string formulaId) where T : Delegate
        {
            if (_formulas.TryGetValue(formulaId, out var formula))
            {
                return formula as T;
            }
            return null;
        }

        /// <summary>
        /// Gets a formula or returns a default.
        /// </summary>
        public static T GetOrDefault<T>(string formulaId, T defaultFormula) where T : Delegate
        {
            return Get<T>(formulaId) ?? defaultFormula;
        }

        /// <summary>
        /// Checks if a formula is registered.
        /// </summary>
        public static bool IsRegistered(string formulaId)
        {
            return _formulas.ContainsKey(formulaId);
        }

        /// <summary>
        /// Unregisters a formula.
        /// </summary>
        public static bool Unregister(string formulaId)
        {
            return _formulas.Remove(formulaId);
        }

        /// <summary>
        /// Clears all registered formulas.
        /// </summary>
        internal static void Clear()
        {
            _formulas.Clear();
        }
    }

    /// <summary>
    /// Standard formula IDs used by Prime.
    /// </summary>
    public static class FormulaIds
    {
        /// <summary>
        /// Formula for calculating crit chance.
        /// Signature: Func&lt;Character, DamageInfo, float&gt; (returns crit chance 0-1)
        /// </summary>
        public const string CritChance = "Combat.CritChance";

        /// <summary>
        /// Formula for calculating crit damage multiplier.
        /// Signature: Func&lt;Character, DamageInfo, float&gt; (returns multiplier)
        /// </summary>
        public const string CritDamage = "Combat.CritDamage";

        /// <summary>
        /// Formula for calculating armor reduction.
        /// Signature: Func&lt;float, float, float&gt; (damage, armor, returns reduced damage)
        /// </summary>
        public const string ArmorReduction = "Combat.ArmorReduction";

        /// <summary>
        /// Formula for calculating resistance reduction.
        /// Signature: Func&lt;float, float, float&gt; (damage, resistance, returns reduced damage)
        /// </summary>
        public const string ResistanceReduction = "Combat.ResistanceReduction";

        /// <summary>
        /// Formula for calculating backstab damage.
        /// Signature: Func&lt;Character, Character, float&gt; (returns backstab multiplier)
        /// </summary>
        public const string BackstabDamage = "Combat.BackstabDamage";

        /// <summary>
        /// Formula for calculating block amount.
        /// Signature: Func&lt;Character, float, float&gt; (blocker, incomingDamage, returns blocked amount)
        /// </summary>
        public const string BlockAmount = "Combat.BlockAmount";

        /// <summary>
        /// Formula for calculating stagger threshold.
        /// Signature: Func&lt;Character, float&gt; (returns stagger threshold)
        /// </summary>
        public const string StaggerThreshold = "Combat.StaggerThreshold";

        /// <summary>
        /// Formula for Strength damage scaling.
        /// Signature: Func&lt;Character, float, float&gt; (attacker, baseDamage, returns scaled damage)
        /// </summary>
        public const string StrengthScaling = "Combat.StrengthScaling";

        /// <summary>
        /// Formula for ability cooldown.
        /// Signature: Func&lt;Character, AbilityDefinition, float&gt; (returns cooldown in seconds)
        /// </summary>
        public const string AbilityCooldown = "Combat.AbilityCooldown";

        /// <summary>
        /// Formula for ability resource cost.
        /// Signature: Func&lt;Character, AbilityDefinition, float&gt; (returns resource cost)
        /// </summary>
        public const string AbilityResourceCost = "Combat.AbilityResourceCost";
    }
}
