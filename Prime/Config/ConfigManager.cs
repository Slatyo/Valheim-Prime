using BepInEx.Configuration;

namespace Prime.Config
{
    /// <summary>
    /// Manages Prime configuration settings.
    /// </summary>
    public class ConfigManager
    {
        private readonly ConfigFile _config;

        // ==================== DEBUG ====================

        /// <summary>
        /// Enable debug logging for stat changes and modifiers.
        /// </summary>
        public ConfigEntry<bool> DebugLogging { get; private set; }

        // ==================== UI ====================

        /// <summary>
        /// Show stat breakdown in tooltips.
        /// </summary>
        public ConfigEntry<bool> ShowStatBreakdown { get; private set; }

        // ==================== PERFORMANCE ====================

        /// <summary>
        /// Update interval for timed modifiers in seconds.
        /// </summary>
        public ConfigEntry<float> ModifierUpdateInterval { get; private set; }

        // ==================== COMBAT SCALING ====================

        /// <summary>
        /// Damage bonus per point of Strength above 10.
        /// 0 = disabled, 0.01 = 1% per point.
        /// </summary>
        public ConfigEntry<float> StrengthScaling { get; private set; }

        /// <summary>
        /// Crit chance bonus per point of Dexterity above 10.
        /// 0 = disabled, 0.005 = 0.5% per point.
        /// </summary>
        public ConfigEntry<float> DexterityCritBonus { get; private set; }

        /// <summary>
        /// Magic damage bonus per point of Intelligence above 10.
        /// 0 = disabled, 0.02 = 2% per point.
        /// </summary>
        public ConfigEntry<float> IntelligenceScaling { get; private set; }

        /// <summary>
        /// Max health bonus per point of Vitality above 10.
        /// 0 = disabled, 2 = +2 HP per point.
        /// </summary>
        public ConfigEntry<float> VitalityHealthBonus { get; private set; }

        public ConfigManager(ConfigFile config)
        {
            _config = config;

            // Debug settings
            DebugLogging = config.Bind(
                "Debug",
                "DebugLogging",
                false,
                "Enable detailed logging of stat changes and modifier operations"
            );

            // UI settings
            ShowStatBreakdown = config.Bind(
                "UI",
                "ShowStatBreakdown",
                true,
                "Show detailed stat breakdown in tooltips"
            );

            // Performance settings
            ModifierUpdateInterval = config.Bind(
                "Performance",
                "ModifierUpdateInterval",
                0.1f,
                new ConfigDescription(
                    "How often to check for expired modifiers (seconds)",
                    new AcceptableValueRange<float>(0.05f, 1f)
                )
            );

            // Combat scaling settings
            StrengthScaling = config.Bind(
                "Combat",
                "StrengthScaling",
                0.01f,
                new ConfigDescription(
                    "Damage bonus per point of Strength above 10 (0.01 = 1% per point, 0 = disabled)",
                    new AcceptableValueRange<float>(0f, 0.1f)
                )
            );

            DexterityCritBonus = config.Bind(
                "Combat",
                "DexterityCritBonus",
                0.005f,
                new ConfigDescription(
                    "Crit chance bonus per point of Dexterity above 10 (0.005 = 0.5% per point, 0 = disabled)",
                    new AcceptableValueRange<float>(0f, 0.05f)
                )
            );

            IntelligenceScaling = config.Bind(
                "Combat",
                "IntelligenceScaling",
                0.02f,
                new ConfigDescription(
                    "Magic damage bonus per point of Intelligence above 10 (0.02 = 2% per point, 0 = disabled)",
                    new AcceptableValueRange<float>(0f, 0.1f)
                )
            );

            VitalityHealthBonus = config.Bind(
                "Combat",
                "VitalityHealthBonus",
                2f,
                new ConfigDescription(
                    "Max health bonus per point of Vitality above 10 (2 = +2 HP per point, 0 = disabled)",
                    new AcceptableValueRange<float>(0f, 10f)
                )
            );
        }
    }
}
