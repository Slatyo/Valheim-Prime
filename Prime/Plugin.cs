using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using Jotunn.Utils;
using Jotunn.Managers;
using Prime.Stats;
using Prime.Core;
using Prime.Config;
using Prime.Abilities;
using Prime.Commands;

namespace Prime
{
    /// <summary>
    /// Prime - Combat and Stats Engine for Valheim Mod Ecosystem.
    /// Provides a unified framework for stats, modifiers, abilities, and damage calculation.
    /// </summary>
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
    [BepInDependency(Jotunn.Main.ModGuid)]
    [BepInDependency("com.slatyo.munin", BepInDependency.DependencyFlags.SoftDependency)]
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

            // Register default abilities
            DefaultAbilities.RegisterAll();

            // Add localizations
            AddLocalizations();

            // Register Munin commands (if Munin is available)
            MuninCommands.Register();

            // Initialize Harmony patches
            _harmony = new Harmony(PluginGUID);
            _harmony.PatchAll();

            Log.LogInfo($"{PluginName} v{PluginVersion} loaded successfully");
            Log.LogInfo($"Registered {StatRegistry.Instance.Count} core stats, {AbilityRegistry.Instance.Count} abilities");
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

            // === REGENERATION ===
            registry.Register(new StatDefinition("HealthRegen", 1f)
            {
                DisplayName = "$stat_healthregen",
                Description = "$stat_healthregen_desc",
                MinValue = 0f,
                Category = StatCategory.Resource,
                DisplayType = StatDisplayType.Multiplier,
                Tags = new[] { "resource", "regen" }
            });

            registry.Register(new StatDefinition("StaminaRegen", 1f)
            {
                DisplayName = "$stat_staminaregen",
                Description = "$stat_staminaregen_desc",
                MinValue = 0f,
                Category = StatCategory.Resource,
                DisplayType = StatDisplayType.Multiplier,
                Tags = new[] { "resource", "regen" }
            });

            registry.Register(new StatDefinition("EitrRegen", 1f)
            {
                DisplayName = "$stat_eitrregen",
                Description = "$stat_eitrregen_desc",
                MinValue = 0f,
                Category = StatCategory.Resource,
                DisplayType = StatDisplayType.Multiplier,
                Tags = new[] { "resource", "regen", "magic" }
            });

            // === STAGGER/COMBAT MECHANICS ===
            registry.Register(new StatDefinition("StaggerDamage", 0f)
            {
                DisplayName = "$stat_staggerdamage",
                Description = "$stat_staggerdamage_desc",
                MinValue = 0f,
                Category = StatCategory.Offense,
                Tags = new[] { "damage", "stagger" }
            });

            registry.Register(new StatDefinition("StaggerDuration", 0f)
            {
                DisplayName = "$stat_staggerduration",
                Description = "$stat_staggerduration_desc",
                MinValue = 0f,
                Category = StatCategory.Offense,
                DisplayType = StatDisplayType.Percent,
                Tags = new[] { "stagger" }
            });

            // === WEAPON-SPECIFIC DAMAGE ===
            registry.Register(new StatDefinition("BluntDamage", 0f)
            {
                DisplayName = "$stat_bluntdamage",
                Category = StatCategory.Offense,
                Tags = new[] { "damage", "physical", "blunt" }
            });

            registry.Register(new StatDefinition("SlashDamage", 0f)
            {
                DisplayName = "$stat_slashdamage",
                Category = StatCategory.Offense,
                Tags = new[] { "damage", "physical", "slash" }
            });

            registry.Register(new StatDefinition("PierceDamage", 0f)
            {
                DisplayName = "$stat_piercedamage",
                Category = StatCategory.Offense,
                Tags = new[] { "damage", "physical", "pierce" }
            });

            registry.Register(new StatDefinition("MeleeDamage", 0f)
            {
                DisplayName = "$stat_meleedamage",
                Category = StatCategory.Offense,
                Tags = new[] { "damage", "melee" }
            });

            registry.Register(new StatDefinition("RangedDamage", 0f)
            {
                DisplayName = "$stat_rangeddamage",
                Category = StatCategory.Offense,
                Tags = new[] { "damage", "ranged" }
            });

            registry.Register(new StatDefinition("BowDamage", 0f)
            {
                DisplayName = "$stat_bowdamage",
                Category = StatCategory.Offense,
                Tags = new[] { "damage", "ranged", "bow" }
            });

            registry.Register(new StatDefinition("ElementalDamage", 0f)
            {
                DisplayName = "$stat_elementaldamage",
                Category = StatCategory.Offense,
                Tags = new[] { "damage", "elemental" }
            });

            registry.Register(new StatDefinition("SpellDamage", 0f)
            {
                DisplayName = "$stat_spelldamage",
                Category = StatCategory.Offense,
                Tags = new[] { "damage", "magic", "spell" }
            });

            // === WEAPON-SPECIFIC ATTACK SPEED ===
            registry.Register(new StatDefinition("SwordAttackSpeed", 0f)
            {
                DisplayName = "$stat_swordattackspeed",
                Category = StatCategory.Offense,
                DisplayType = StatDisplayType.Percent,
                Tags = new[] { "attack", "sword" }
            });

            // === DEFENSE/BLOCK ===
            registry.Register(new StatDefinition("BlockEfficiency", 0f)
            {
                DisplayName = "$stat_blockefficiency",
                Category = StatCategory.Defense,
                DisplayType = StatDisplayType.Percent,
                Tags = new[] { "defense", "block" }
            });

            registry.Register(new StatDefinition("PhysicalResist", 0f)
            {
                DisplayName = "$stat_physicalresist",
                MinValue = -1f,
                MaxValue = 1f,
                Category = StatCategory.Resistance,
                DisplayType = StatDisplayType.Percent,
                Tags = new[] { "resistance", "physical" }
            });

            // === LIFESTEAL/HEALING ===
            registry.Register(new StatDefinition("LifeSteal", 0f)
            {
                DisplayName = "$stat_lifesteal",
                Category = StatCategory.Offense,
                DisplayType = StatDisplayType.Percent,
                Tags = new[] { "damage", "healing" }
            });

            registry.Register(new StatDefinition("OverkillHeal", 0f)
            {
                DisplayName = "$stat_overkillheal",
                Category = StatCategory.Offense,
                DisplayType = StatDisplayType.Percent,
                Tags = new[] { "healing" }
            });

            // === EVASION/MOBILITY ===
            registry.Register(new StatDefinition("EvasionChance", 0f)
            {
                DisplayName = "$stat_evasionchance",
                MinValue = 0f,
                MaxValue = 0.75f,
                Category = StatCategory.Defense,
                DisplayType = StatDisplayType.Percent,
                Tags = new[] { "defense", "evasion" }
            });

            registry.Register(new StatDefinition("MovementOnKill", 0f)
            {
                DisplayName = "$stat_movementonkill",
                Category = StatCategory.Movement,
                DisplayType = StatDisplayType.Percent,
                Tags = new[] { "movement", "kill" }
            });

            // === RANGED SPECIFIC ===
            registry.Register(new StatDefinition("ArrowSpeed", 1f)
            {
                DisplayName = "$stat_arrowspeed",
                Category = StatCategory.Offense,
                DisplayType = StatDisplayType.Multiplier,
                Tags = new[] { "ranged", "bow" }
            });

            registry.Register(new StatDefinition("Multishot", 0f)
            {
                DisplayName = "$stat_multishot",
                Category = StatCategory.Offense,
                DisplayType = StatDisplayType.Percent,
                Tags = new[] { "ranged", "bow" }
            });

            // === MAGIC SPECIFIC ===
            registry.Register(new StatDefinition("ManaCost", 0f)
            {
                DisplayName = "$stat_manacost",
                MinValue = -0.5f,
                Category = StatCategory.Utility,
                DisplayType = StatDisplayType.Percent,
                Tags = new[] { "magic", "cost" }
            });

            // === STATUS EFFECTS ===
            registry.Register(new StatDefinition("StatusDuration", 0f)
            {
                DisplayName = "$stat_statusduration",
                Category = StatCategory.Offense,
                DisplayType = StatDisplayType.Percent,
                Tags = new[] { "status" }
            });

            registry.Register(new StatDefinition("StatusDamage", 0f)
            {
                DisplayName = "$stat_statusdamage",
                Category = StatCategory.Offense,
                Tags = new[] { "damage", "status" }
            });

            registry.Register(new StatDefinition("FireStatusChance", 0f)
            {
                DisplayName = "$stat_firestatuschance",
                MinValue = 0f,
                MaxValue = 1f,
                Category = StatCategory.Offense,
                DisplayType = StatDisplayType.Percent,
                Tags = new[] { "status", "fire" }
            });

            registry.Register(new StatDefinition("FrostStatusChance", 0f)
            {
                DisplayName = "$stat_froststatuschance",
                MinValue = 0f,
                MaxValue = 1f,
                Category = StatCategory.Offense,
                DisplayType = StatDisplayType.Percent,
                Tags = new[] { "status", "frost" }
            });

            registry.Register(new StatDefinition("LightningStatusChance", 0f)
            {
                DisplayName = "$stat_lightningstatuschance",
                MinValue = 0f,
                MaxValue = 1f,
                Category = StatCategory.Offense,
                DisplayType = StatDisplayType.Percent,
                Tags = new[] { "status", "lightning" }
            });

            registry.Register(new StatDefinition("PoisonStatusChance", 0f)
            {
                DisplayName = "$stat_poisonstatuschance",
                MinValue = 0f,
                MaxValue = 1f,
                Category = StatCategory.Offense,
                DisplayType = StatDisplayType.Percent,
                Tags = new[] { "status", "poison" }
            });

            registry.Register(new StatDefinition("SpiritStatusChance", 0f)
            {
                DisplayName = "$stat_spiritstatuschance",
                MinValue = 0f,
                MaxValue = 1f,
                Category = StatCategory.Offense,
                DisplayType = StatDisplayType.Percent,
                Tags = new[] { "status", "spirit" }
            });

            // === MISC COMBAT ===
            registry.Register(new StatDefinition("StaggerVsArmored", 0f)
            {
                DisplayName = "$stat_staggervsarmored",
                Category = StatCategory.Offense,
                DisplayType = StatDisplayType.Percent,
                Tags = new[] { "damage", "stagger" }
            });

            registry.Register(new StatDefinition("ArcherDamage", 0f)
            {
                DisplayName = "$stat_archerdamage",
                Category = StatCategory.Offense,
                Tags = new[] { "damage", "ranged" }
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

                // === WARRIOR ABILITIES ===
                { "ability_warcry", "War Cry" },
                { "ability_warcry_desc", "Let out a fearsome war cry, boosting your strength and speed." },
                { "ability_whirlwind", "Whirlwind" },
                { "ability_whirlwind_desc", "Spin with your weapon, hitting all enemies around you." },
                { "ability_shieldbash", "Shield Bash" },
                { "ability_shieldbash_desc", "Bash an enemy with your shield, stunning them briefly." },
                { "ability_berserkerrage", "Berserker Rage" },
                { "ability_berserkerrage_desc", "Enter a berserker rage, massively increasing damage but lowering defense." },
                { "ability_groundslam", "Ground Slam" },
                { "ability_groundslam_desc", "Slam the ground, damaging and knocking back nearby enemies." },
                { "ability_execute", "Execute" },
                { "ability_execute_desc", "A devastating finisher that deals massive damage to weakened foes." },

                // === RANGER ABILITIES ===
                { "ability_powershot", "Power Shot" },
                { "ability_powershot_desc", "Draw back and release a powerful arrow that pierces through enemies." },
                { "ability_multishot", "Multishot" },
                { "ability_multishot_desc", "Fire multiple arrows in a spread pattern." },
                { "ability_evasiveroll", "Evasive Roll" },
                { "ability_evasiveroll_desc", "Roll in a direction, briefly becoming invulnerable." },
                { "ability_huntersmark", "Hunter's Mark" },
                { "ability_huntersmark_desc", "Mark a target, making them vulnerable to your attacks." },
                { "ability_poisonarrow", "Poison Arrow" },
                { "ability_poisonarrow_desc", "Fire an arrow coated in poison that damages over time." },
                { "ability_beartrap", "Bear Trap" },
                { "ability_beartrap_desc", "Place a trap that snares and damages enemies who step on it." },

                // === SORCERER ABILITIES ===
                { "ability_fireball", "Fireball" },
                { "ability_fireball_desc", "Hurl a ball of fire that explodes on impact." },
                { "ability_frostnova", "Frost Nova" },
                { "ability_frostnova_desc", "Release a burst of frost that damages and slows nearby enemies." },
                { "ability_lightningbolt", "Lightning Bolt" },
                { "ability_lightningbolt_desc", "Strike a target with a bolt of lightning." },
                { "ability_chainlightning", "Chain Lightning" },
                { "ability_chainlightning_desc", "Lightning that bounces between multiple targets." },
                { "ability_manashield", "Mana Shield" },
                { "ability_manashield_desc", "Surround yourself with magical energy that absorbs damage." },
                { "ability_meteor", "Meteor" },
                { "ability_meteor_desc", "Call down a massive meteor that devastates an area." },
                { "ability_teleport", "Teleport" },
                { "ability_teleport_desc", "Instantly teleport to a nearby location." },

                // === GUARDIAN ABILITIES ===
                { "ability_healingtouch", "Healing Touch" },
                { "ability_healingtouch_desc", "Channel healing energy to restore an ally's health." },
                { "ability_healingaura", "Healing Aura" },
                { "ability_healingaura_desc", "Create an aura that heals you and nearby allies over time." },
                { "ability_divineshield", "Divine Shield" },
                { "ability_divineshield_desc", "Protect an ally with a divine barrier that absorbs damage." },
                { "ability_taunt", "Taunt" },
                { "ability_taunt_desc", "Draw enemy attention to yourself while increasing your defenses." },
                { "ability_consecration", "Consecration" },
                { "ability_consecration_desc", "Sanctify the ground, damaging enemies and healing allies." },
                { "ability_resurrection", "Resurrection" },
                { "ability_resurrection_desc", "Bring a fallen ally back from the dead." },

                // === CREATURE ABILITIES ===
                { "ability_frostbreath", "Frost Breath" },
                { "ability_frostbreath_desc", "Exhale a cone of freezing cold." },
                { "ability_firebreath", "Fire Breath" },
                { "ability_firebreath_desc", "Exhale a cone of searing flames." },
                { "ability_enrage", "Enrage" },
                { "ability_enrage_desc", "Enter a rage, greatly increasing damage and speed." },
                { "ability_summonminions", "Summon Minions" },
                { "ability_summonminions_desc", "Call forth creatures to fight alongside you." },
                { "ability_poisonspit", "Poison Spit" },
                { "ability_poisonspit_desc", "Spit a glob of poison at a target." },
                { "ability_icespikes", "Ice Spikes" },
                { "ability_icespikes_desc", "Cause spikes of ice to erupt from the ground." },
                { "ability_groundpound", "Ground Pound" },
                { "ability_groundpound_desc", "Slam the ground with tremendous force." },
                { "ability_roar", "Roar" },
                { "ability_roar_desc", "Let out a terrifying roar that weakens enemies." },
                { "ability_lightningstrike", "Lightning Strike" },
                { "ability_lightningstrike_desc", "Call down lightning on a target." },
                { "ability_spiritdrain", "Spirit Drain" },
                { "ability_spiritdrain_desc", "Drain life force from a target to heal yourself." },

                // === CONSUMABLE ABILITIES ===
                { "ability_strengthpotion", "Strength Potion" },
                { "ability_strengthpotion_desc", "A potion that temporarily increases strength." },
                { "ability_swiftnesselixir", "Swiftness Elixir" },
                { "ability_swiftnesselixir_desc", "An elixir that increases movement and attack speed." },
                { "ability_ironskinpotion", "Iron Skin Potion" },
                { "ability_ironskinpotion_desc", "A potion that hardens your skin, increasing armor." },
                { "ability_fireresistpotion", "Fire Resistance Potion" },
                { "ability_fireresistpotion_desc", "A potion that grants resistance to fire." },
                { "ability_frostresistpotion", "Frost Resistance Potion" },
                { "ability_frostresistpotion_desc", "A potion that grants resistance to frost." },
                { "ability_manapotion", "Mana Potion" },
                { "ability_manapotion_desc", "A potion that rapidly restores eitr." },
                { "ability_berserkermushroom", "Berserker Mushroom" },
                { "ability_berserkermushroom_desc", "A mushroom that grants strength but lowers defense." },
                { "ability_criticalscroll", "Critical Scroll" },
                { "ability_criticalscroll_desc", "A scroll that increases critical strike chance and damage." },

                // === UTILITY ABILITIES ===
                { "ability_sprint", "Sprint" },
                { "ability_sprint_desc", "A burst of speed." },
                { "ability_secondwind", "Second Wind" },
                { "ability_secondwind_desc", "Catch your breath, rapidly regenerating stamina." },
                { "ability_battlefocus", "Battle Focus" },
                { "ability_battlefocus_desc", "Focus your mind, increasing critical strike chance." },
                { "ability_fortify", "Fortify" },
                { "ability_fortify_desc", "Take a defensive stance, trading mobility for protection." },
                { "ability_lifetap", "Life Tap" },
                { "ability_lifetap_desc", "Sacrifice health to restore eitr." },
                { "ability_bloodlust", "Bloodlust" },
                { "ability_bloodlust_desc", "Enter a frenzy, healing from damage you deal." },
            });

            // === GERMAN TRANSLATIONS ===
            loc.AddTranslation("German", new System.Collections.Generic.Dictionary<string, string>
            {
                // Attributes
                { "stat_strength", "Stärke" },
                { "stat_strength_desc", "Erhöht physischen Schaden und Tragkraft" },
                { "stat_dexterity", "Geschicklichkeit" },
                { "stat_dexterity_desc", "Erhöht Angriffsgeschwindigkeit und kritische Trefferchance" },
                { "stat_intelligence", "Intelligenz" },
                { "stat_intelligence_desc", "Erhöht magischen Schaden und Eitr-Pool" },
                { "stat_vitality", "Vitalität" },
                { "stat_vitality_desc", "Erhöht Gesundheit und Gesundheitsregeneration" },

                // Resources
                { "stat_maxhealth", "Maximale Gesundheit" },
                { "stat_maxhealth_desc", "Maximale Gesundheitspunkte" },
                { "stat_maxstamina", "Maximale Ausdauer" },
                { "stat_maxstamina_desc", "Maximale Ausdauerpunkte" },
                { "stat_maxeitr", "Maximales Eitr" },
                { "stat_maxeitr_desc", "Maximale Eitr (Magie) Punkte" },

                // Offense
                { "stat_physicaldamage", "Physischer Schaden" },
                { "stat_physicaldamage_desc", "Zusätzlicher physischer Schaden" },
                { "stat_firedamage", "Feuerschaden" },
                { "stat_firedamage_desc", "Zusätzlicher Feuerschaden" },
                { "stat_frostdamage", "Frostschaden" },
                { "stat_frostdamage_desc", "Zusätzlicher Frostschaden" },
                { "stat_lightningdamage", "Blitzschaden" },
                { "stat_lightningdamage_desc", "Zusätzlicher Blitzschaden" },
                { "stat_poisondamage", "Giftschaden" },
                { "stat_poisondamage_desc", "Zusätzlicher Giftschaden" },
                { "stat_spiritdamage", "Geisterschaden" },
                { "stat_spiritdamage_desc", "Zusätzlicher Geisterschaden" },
                { "stat_attackspeed", "Angriffsgeschwindigkeit" },
                { "stat_attackspeed_desc", "Multiplikator für Angriffsgeschwindigkeit" },
                { "stat_critchance", "Kritische Trefferchance" },
                { "stat_critchance_desc", "Chance auf kritischen Schaden" },
                { "stat_critdamage", "Kritischer Schaden" },
                { "stat_critdamage_desc", "Schadensmultiplikator bei kritischen Treffern" },

                // Defense
                { "stat_armor", "Rüstung" },
                { "stat_armor_desc", "Reduziert erlittenen physischen Schaden" },
                { "stat_blockpower", "Blockkraft" },
                { "stat_blockpower_desc", "Blockierter Schaden beim Parieren" },

                // Resistances
                { "stat_fireresist", "Feuerwiderstand" },
                { "stat_fireresist_desc", "Widerstand gegen Feuerschaden" },
                { "stat_frostresist", "Frostwiderstand" },
                { "stat_frostresist_desc", "Widerstand gegen Frostschaden" },
                { "stat_lightningresist", "Blitzwiderstand" },
                { "stat_lightningresist_desc", "Widerstand gegen Blitzschaden" },
                { "stat_poisonresist", "Giftwiderstand" },
                { "stat_poisonresist_desc", "Widerstand gegen Giftschaden" },
                { "stat_spiritresist", "Geisterwiderstand" },
                { "stat_spiritresist_desc", "Widerstand gegen Geisterschaden" },

                // Movement
                { "stat_movespeed", "Bewegungsgeschwindigkeit" },
                { "stat_movespeed_desc", "Multiplikator für Bewegungsgeschwindigkeit" },

                // Utility
                { "stat_carryweight", "Tragkraft" },
                { "stat_carryweight_desc", "Maximales Gewicht das du tragen kannst" },
                { "stat_cooldownreduction", "Abklingzeitverkürzung" },
                { "stat_cooldownreduction_desc", "Reduziert Fähigkeiten-Abklingzeiten" },
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
