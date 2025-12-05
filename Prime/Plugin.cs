using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using Jotunn.Utils;
using Prime.Stats;
using Prime.Core;
using Prime.Config;
using Jotunn.Managers;

namespace Prime
{
    /// <summary>
    /// Prime - Combat and Stats Engine for Valheim Mod Ecosystem.
    /// Provides a unified framework for stats, modifiers, abilities, and damage calculation.
    /// </summary>
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
    [BepInDependency(Jotunn.Main.ModGuid)]
    [NetworkCompatibility(CompatibilityLevel.EveryoneMustHaveMod, VersionStrictness.Minor)]
    public class Plugin : BaseUnityPlugin
    {
        /// <summary>Plugin GUID for BepInEx.</summary>
        public const string PluginGUID = "com.slatyo.prime";
        /// <summary>Plugin display name.</summary>
        public const string PluginName = "Prime";
        /// <summary>Plugin version.</summary>
        public const string PluginVersion = "1.0.0";

        /// <summary>
        /// Logger instance for Prime.
        /// </summary>
        public static ManualLogSource Log { get; private set; }

        /// <summary>
        /// Plugin instance.
        /// </summary>
        public static Plugin Instance { get; private set; }

        /// <summary>
        /// Configuration manager.
        /// </summary>
        public static ConfigManager ConfigManager { get; private set; }

        private Harmony _harmony;

        private void Awake()
        {
            Instance = this;
            Log = Logger;

            Log.LogInfo($"{PluginName} v{PluginVersion} is loading...");

            // Initialize configuration
            ConfigManager = new ConfigManager(Config);

            // Register core stats
            RegisterCoreStats();

            // Add localizations
            AddLocalizations();

            // Register console commands
            CommandManager.Instance.AddConsoleCommand(new Commands.PrimeCommand());

            // Initialize Harmony patches
            _harmony = new Harmony(PluginGUID);
            _harmony.PatchAll();

            Log.LogInfo($"{PluginName} v{PluginVersion} loaded successfully");
            Log.LogInfo($"Registered {StatRegistry.Instance.Count} core stats");
        }

        private void Update()
        {
            // Update all entity stat containers (process timed modifiers)
            EntityManager.Instance.Update();

            // Update all active effects (DoTs, procs, etc.)
            Effects.EffectManager.Update();
        }

        private void OnDestroy()
        {
            // Cleanup
            EntityManager.Instance.Clear();
            Effects.EffectManager.Clear();
            _harmony?.UnpatchSelf();
        }

        /// <summary>
        /// Registers the core stats that Prime provides out of the box.
        /// Other mods can register additional stats in their Awake methods.
        /// </summary>
        private void RegisterCoreStats()
        {
            var registry = StatRegistry.Instance;

            // === ATTRIBUTES ===
            registry.Register(new StatDefinition("Strength", 10f)
            {
                DisplayName = "$stat_strength",
                Description = "$stat_strength_desc",
                MinValue = 0f,
                Category = StatCategory.Attribute,
                Tags = new[] { "attribute", "primary" }
            });

            registry.Register(new StatDefinition("Dexterity", 10f)
            {
                DisplayName = "$stat_dexterity",
                Description = "$stat_dexterity_desc",
                MinValue = 0f,
                Category = StatCategory.Attribute,
                Tags = new[] { "attribute", "primary" }
            });

            registry.Register(new StatDefinition("Intelligence", 10f)
            {
                DisplayName = "$stat_intelligence",
                Description = "$stat_intelligence_desc",
                MinValue = 0f,
                Category = StatCategory.Attribute,
                Tags = new[] { "attribute", "primary" }
            });

            registry.Register(new StatDefinition("Vitality", 10f)
            {
                DisplayName = "$stat_vitality",
                Description = "$stat_vitality_desc",
                MinValue = 0f,
                Category = StatCategory.Attribute,
                Tags = new[] { "attribute", "primary" }
            });

            // === RESOURCES ===
            registry.Register(new StatDefinition("MaxHealth", 100f)
            {
                DisplayName = "$stat_maxhealth",
                Description = "$stat_maxhealth_desc",
                MinValue = 1f,
                Category = StatCategory.Resource,
                Tags = new[] { "resource" }
            });

            registry.Register(new StatDefinition("MaxStamina", 100f)
            {
                DisplayName = "$stat_maxstamina",
                Description = "$stat_maxstamina_desc",
                MinValue = 0f,
                Category = StatCategory.Resource,
                Tags = new[] { "resource" }
            });

            registry.Register(new StatDefinition("MaxEitr", 0f)
            {
                DisplayName = "$stat_maxeitr",
                Description = "$stat_maxeitr_desc",
                MinValue = 0f,
                Category = StatCategory.Resource,
                Tags = new[] { "resource", "magic" }
            });

            // === OFFENSE ===
            registry.Register(new StatDefinition("PhysicalDamage", 0f)
            {
                DisplayName = "$stat_physicaldamage",
                Description = "$stat_physicaldamage_desc",
                MinValue = 0f,
                Category = StatCategory.Offense,
                Tags = new[] { "damage", "physical" }
            });

            registry.Register(new StatDefinition("FireDamage", 0f)
            {
                DisplayName = "$stat_firedamage",
                Description = "$stat_firedamage_desc",
                MinValue = 0f,
                Category = StatCategory.Offense,
                Tags = new[] { "damage", "elemental", "fire" }
            });

            registry.Register(new StatDefinition("FrostDamage", 0f)
            {
                DisplayName = "$stat_frostdamage",
                Description = "$stat_frostdamage_desc",
                MinValue = 0f,
                Category = StatCategory.Offense,
                Tags = new[] { "damage", "elemental", "frost" }
            });

            registry.Register(new StatDefinition("LightningDamage", 0f)
            {
                DisplayName = "$stat_lightningdamage",
                Description = "$stat_lightningdamage_desc",
                MinValue = 0f,
                Category = StatCategory.Offense,
                Tags = new[] { "damage", "elemental", "lightning" }
            });

            registry.Register(new StatDefinition("PoisonDamage", 0f)
            {
                DisplayName = "$stat_poisondamage",
                Description = "$stat_poisondamage_desc",
                MinValue = 0f,
                Category = StatCategory.Offense,
                Tags = new[] { "damage", "elemental", "poison" }
            });

            registry.Register(new StatDefinition("SpiritDamage", 0f)
            {
                DisplayName = "$stat_spiritdamage",
                Description = "$stat_spiritdamage_desc",
                MinValue = 0f,
                Category = StatCategory.Offense,
                Tags = new[] { "damage", "elemental", "spirit" }
            });

            registry.Register(new StatDefinition("AttackSpeed", 1f)
            {
                DisplayName = "$stat_attackspeed",
                Description = "$stat_attackspeed_desc",
                MinValue = 0.1f,
                MaxValue = 3f,
                Category = StatCategory.Offense,
                DisplayType = StatDisplayType.Multiplier,
                Tags = new[] { "attack" }
            });

            registry.Register(new StatDefinition("CritChance", 0f)
            {
                DisplayName = "$stat_critchance",
                Description = "$stat_critchance_desc",
                MinValue = 0f,
                MaxValue = 1f,
                Category = StatCategory.Offense,
                DisplayType = StatDisplayType.Percent,
                DecimalPlaces = 1,
                Tags = new[] { "crit" }
            });

            registry.Register(new StatDefinition("CritDamage", 1.5f)
            {
                DisplayName = "$stat_critdamage",
                Description = "$stat_critdamage_desc",
                MinValue = 1f,
                Category = StatCategory.Offense,
                DisplayType = StatDisplayType.Multiplier,
                Tags = new[] { "crit" }
            });

            // === DEFENSE ===
            registry.Register(new StatDefinition("Armor", 0f)
            {
                DisplayName = "$stat_armor",
                Description = "$stat_armor_desc",
                MinValue = 0f,
                Category = StatCategory.Defense,
                Tags = new[] { "defense", "physical" }
            });

            registry.Register(new StatDefinition("BlockPower", 0f)
            {
                DisplayName = "$stat_blockpower",
                Description = "$stat_blockpower_desc",
                MinValue = 0f,
                Category = StatCategory.Defense,
                Tags = new[] { "defense", "block" }
            });

            // === RESISTANCES ===
            registry.Register(new StatDefinition("FireResist", 0f)
            {
                DisplayName = "$stat_fireresist",
                Description = "$stat_fireresist_desc",
                MinValue = -1f,
                MaxValue = 1f,
                Category = StatCategory.Resistance,
                DisplayType = StatDisplayType.Percent,
                Tags = new[] { "resistance", "elemental", "fire" }
            });

            registry.Register(new StatDefinition("FrostResist", 0f)
            {
                DisplayName = "$stat_frostresist",
                Description = "$stat_frostresist_desc",
                MinValue = -1f,
                MaxValue = 1f,
                Category = StatCategory.Resistance,
                DisplayType = StatDisplayType.Percent,
                Tags = new[] { "resistance", "elemental", "frost" }
            });

            registry.Register(new StatDefinition("LightningResist", 0f)
            {
                DisplayName = "$stat_lightningresist",
                Description = "$stat_lightningresist_desc",
                MinValue = -1f,
                MaxValue = 1f,
                Category = StatCategory.Resistance,
                DisplayType = StatDisplayType.Percent,
                Tags = new[] { "resistance", "elemental", "lightning" }
            });

            registry.Register(new StatDefinition("PoisonResist", 0f)
            {
                DisplayName = "$stat_poisonresist",
                Description = "$stat_poisonresist_desc",
                MinValue = -1f,
                MaxValue = 1f,
                Category = StatCategory.Resistance,
                DisplayType = StatDisplayType.Percent,
                Tags = new[] { "resistance", "poison" }
            });

            registry.Register(new StatDefinition("SpiritResist", 0f)
            {
                DisplayName = "$stat_spiritresist",
                Description = "$stat_spiritresist_desc",
                MinValue = -1f,
                MaxValue = 1f,
                Category = StatCategory.Resistance,
                DisplayType = StatDisplayType.Percent,
                Tags = new[] { "resistance", "spirit" }
            });

            // === MOVEMENT ===
            registry.Register(new StatDefinition("MoveSpeed", 1f)
            {
                DisplayName = "$stat_movespeed",
                Description = "$stat_movespeed_desc",
                MinValue = 0.1f,
                MaxValue = 5f,
                Category = StatCategory.Movement,
                DisplayType = StatDisplayType.Multiplier,
                Tags = new[] { "movement" }
            });

            // === UTILITY ===
            registry.Register(new StatDefinition("CarryWeight", 300f)
            {
                DisplayName = "$stat_carryweight",
                Description = "$stat_carryweight_desc",
                MinValue = 0f,
                Category = StatCategory.Utility,
                Tags = new[] { "utility" }
            });

            registry.Register(new StatDefinition("CooldownReduction", 0f)
            {
                DisplayName = "$stat_cooldownreduction",
                Description = "$stat_cooldownreduction_desc",
                MinValue = 0f,
                MaxValue = 0.75f, // Cap at 75% CDR
                Category = StatCategory.Utility,
                DisplayType = StatDisplayType.Percent,
                Tags = new[] { "utility", "cooldown" }
            });
        }

        /// <summary>
        /// Adds localization entries for Prime stats and UI.
        /// </summary>
        private void AddLocalizations()
        {
            var loc = LocalizationManager.Instance.GetLocalization();

            loc.AddTranslation("English", new System.Collections.Generic.Dictionary<string, string>
            {
                // Attributes
                { "stat_strength", "Strength" },
                { "stat_strength_desc", "Increases physical damage and carry weight" },
                { "stat_dexterity", "Dexterity" },
                { "stat_dexterity_desc", "Increases attack speed and critical chance" },
                { "stat_intelligence", "Intelligence" },
                { "stat_intelligence_desc", "Increases magic damage and eitr pool" },
                { "stat_vitality", "Vitality" },
                { "stat_vitality_desc", "Increases health and health regeneration" },

                // Resources
                { "stat_maxhealth", "Max Health" },
                { "stat_maxhealth_desc", "Maximum health points" },
                { "stat_maxstamina", "Max Stamina" },
                { "stat_maxstamina_desc", "Maximum stamina points" },
                { "stat_maxeitr", "Max Eitr" },
                { "stat_maxeitr_desc", "Maximum eitr (magic) points" },

                // Offense
                { "stat_physicaldamage", "Physical Damage" },
                { "stat_physicaldamage_desc", "Bonus physical damage dealt" },
                { "stat_attackspeed", "Attack Speed" },
                { "stat_attackspeed_desc", "Multiplier for attack speed" },
                { "stat_critchance", "Critical Chance" },
                { "stat_critchance_desc", "Chance to deal critical damage" },
                { "stat_critdamage", "Critical Damage" },
                { "stat_critdamage_desc", "Damage multiplier on critical hits" },

                // Defense
                { "stat_armor", "Armor" },
                { "stat_armor_desc", "Reduces physical damage taken" },
                { "stat_blockpower", "Block Power" },
                { "stat_blockpower_desc", "Damage blocked when parrying" },

                // Resistances
                { "stat_fireresist", "Fire Resistance" },
                { "stat_fireresist_desc", "Resistance to fire damage" },
                { "stat_frostresist", "Frost Resistance" },
                { "stat_frostresist_desc", "Resistance to frost damage" },
                { "stat_lightningresist", "Lightning Resistance" },
                { "stat_lightningresist_desc", "Resistance to lightning damage" },
                { "stat_poisonresist", "Poison Resistance" },
                { "stat_poisonresist_desc", "Resistance to poison damage" },

                // Movement
                { "stat_movespeed", "Movement Speed" },
                { "stat_movespeed_desc", "Multiplier for movement speed" },

                // Utility
                { "stat_carryweight", "Carry Weight" },
                { "stat_carryweight_desc", "Maximum weight you can carry" },
                { "stat_cooldownreduction", "Cooldown Reduction" },
                { "stat_cooldownreduction_desc", "Reduces ability cooldowns" },
            });
        }

        /// <summary>
        /// Helper method to check if running on server.
        /// </summary>
        public static bool IsServer() => ZNet.instance != null && ZNet.instance.IsServer();

        /// <summary>
        /// Helper method to check if running on client.
        /// </summary>
        public static bool IsClient() => ZNet.instance != null && !ZNet.instance.IsServer();
    }
}
