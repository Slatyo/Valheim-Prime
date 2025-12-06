using Prime.Combat;
using Prime.Modifiers;
using UnityEngine;

namespace Prime.Abilities
{
    /// <summary>
    /// Registers default abilities that all ecosystem mods can use.
    /// Viking grants these via talents, Denizen uses them for creatures,
    /// Tome links consumables to them.
    /// </summary>
    public static class DefaultAbilities
    {
        /// <summary>
        /// Register all default abilities.
        /// </summary>
        public static void RegisterAll()
        {
            var registry = AbilityRegistry.Instance;

            // === WARRIOR ABILITIES ===
            RegisterWarriorAbilities(registry);

            // === RANGER ABILITIES ===
            RegisterRangerAbilities(registry);

            // === SORCERER ABILITIES ===
            RegisterSorcererAbilities(registry);

            // === GUARDIAN ABILITIES ===
            RegisterGuardianAbilities(registry);

            // === CREATURE ABILITIES (for Denizen) ===
            RegisterCreatureAbilities(registry);

            // === CONSUMABLE/BUFF ABILITIES (for Tome) ===
            RegisterConsumableAbilities(registry);

            // === UTILITY ABILITIES ===
            RegisterUtilityAbilities(registry);

            Plugin.Log?.LogInfo($"Registered {registry.Count} default abilities");
        }

        #region Warrior Abilities

        private static void RegisterWarriorAbilities(AbilityRegistry registry)
        {
            // === WAR CRY ===
            // Boost damage and intimidate nearby enemies
            registry.Register(new AbilityDefinition("WarCry")
            {
                DisplayName = "$ability_warcry",
                Description = "$ability_warcry_desc",
                BaseCooldown = 45f,
                Cost = new ResourceCost("Stamina", 30f),
                TargetType = AbilityTargetType.AroundSelf,
                Radius = 10f,
                Category = AbilityCategory.Active,
                CastVFX = "spark_warcry",
                CastSFX = "sfx_warcry",
                Animation = "emote_roar",
                SelfEffects = new()
                {
                    new AbilityEffect("Strength", ModifierType.Percent, 25f, 15f),
                    new AbilityEffect("PhysicalDamage", ModifierType.Flat, 10f, 15f),
                    new AbilityEffect("MoveSpeed", ModifierType.Percent, 10f, 15f)
                },
                Tags = { "warrior", "buff", "shout" }
            });

            // === WHIRLWIND ===
            // Spin attack hitting all nearby enemies
            registry.Register(new AbilityDefinition("Whirlwind")
            {
                DisplayName = "$ability_whirlwind",
                Description = "$ability_whirlwind_desc",
                BaseCooldown = 12f,
                Cost = new ResourceCost("Stamina", 40f),
                TargetType = AbilityTargetType.AroundSelf,
                Radius = 4f,
                BaseDamage = 35f,
                DamageType = DamageType.Slash,
                ScalingStat = "Strength",
                ScalingFactor = 1.5f,
                CanCrit = true,
                Category = AbilityCategory.Active,
                CastVFX = "spark_whirlwind",
                CastSFX = "sfx_sword_swing",
                Animation = "attack_spin",
                Tags = { "warrior", "melee", "aoe" }
            });

            // === SHIELD BASH ===
            // Stun and damage a single target
            registry.Register(new AbilityDefinition("ShieldBash")
            {
                DisplayName = "$ability_shieldbash",
                Description = "$ability_shieldbash_desc",
                BaseCooldown = 8f,
                Cost = new ResourceCost("Stamina", 25f),
                TargetType = AbilityTargetType.Enemy,
                Range = 3f,
                BaseDamage = 20f,
                DamageType = DamageType.Blunt,
                ScalingStat = "Strength",
                ScalingFactor = 1.0f,
                CanCrit = true,
                Category = AbilityCategory.Active,
                CastVFX = "spark_impact_blunt",
                CastSFX = "sfx_shield_bash",
                Animation = "attack_shield",
                TargetEffects = new()
                {
                    new AbilityEffect("MoveSpeed", ModifierType.Percent, -50f, 2f),
                    new AbilityEffect("AttackSpeed", ModifierType.Percent, -30f, 2f)
                },
                Tags = { "warrior", "melee", "stun", "control" }
            });

            // === BERSERKER RAGE ===
            // Massive damage boost but take more damage
            registry.Register(new AbilityDefinition("BerserkerRage")
            {
                DisplayName = "$ability_berserkerrage",
                Description = "$ability_berserkerrage_desc",
                BaseCooldown = 90f,
                Cost = new ResourceCost("Health", 10f, true), // 10% max health
                TargetType = AbilityTargetType.Self,
                Category = AbilityCategory.Active,
                CastVFX = "spark_rage",
                CastSFX = "sfx_rage",
                Animation = "emote_flex",
                SelfEffects = new()
                {
                    new AbilityEffect("PhysicalDamage", ModifierType.Percent, 50f, 20f),
                    new AbilityEffect("AttackSpeed", ModifierType.Percent, 25f, 20f),
                    new AbilityEffect("Armor", ModifierType.Percent, -25f, 20f), // Drawback
                    new AbilityEffect("LifeSteal", ModifierType.Flat, 0.1f, 20f) // 10% lifesteal
                },
                Tags = { "warrior", "buff", "berserker" }
            });

            // === GROUND SLAM ===
            // AoE knockback and damage
            registry.Register(new AbilityDefinition("GroundSlam")
            {
                DisplayName = "$ability_groundslam",
                Description = "$ability_groundslam_desc",
                BaseCooldown = 18f,
                Cost = new ResourceCost("Stamina", 50f),
                TargetType = AbilityTargetType.AroundSelf,
                Radius = 6f,
                BaseDamage = 45f,
                DamageType = DamageType.Blunt,
                ScalingStat = "Strength",
                ScalingFactor = 2.0f,
                CanCrit = true,
                Category = AbilityCategory.Active,
                CastVFX = "spark_groundslam",
                CastSFX = "sfx_ground_slam",
                Animation = "attack_slam",
                Tags = { "warrior", "melee", "aoe", "knockback" }
            });

            // === GLADIATOR'S GLORY ===
            // Kill to heal and gain stacking damage buff
            registry.Register(new AbilityDefinition("GladiatorsGlory")
            {
                DisplayName = "$ability_gladiatorsglory",
                Description = "$ability_gladiatorsglory_desc",
                BaseCooldown = 0f, // Passive - triggered on kill
                TargetType = AbilityTargetType.Self,
                Category = AbilityCategory.Passive,
                CastVFX = "spark_gladiator",
                CastSFX = "sfx_victory",
                SelfEffects = new()
                {
                    new AbilityEffect("HealthRegen", ModifierType.Percent, 5f, 1f), // 5% heal on kill
                    new AbilityEffect("PhysicalDamage", ModifierType.Percent, 15f, 5f) // Stacking buff
                },
                CustomData = { ["maxStacks"] = 3 },
                Tags = { "warrior", "passive", "keystone", "sustain" }
            });

            // === IRON SKIN ===
            // Defensive keystone - toggle damage reduction
            registry.Register(new AbilityDefinition("IronSkin")
            {
                DisplayName = "$ability_ironskin",
                Description = "$ability_ironskin_desc",
                BaseCooldown = 60f,
                Cost = new ResourceCost("Stamina", 20f),
                TargetType = AbilityTargetType.Self,
                Category = AbilityCategory.Toggle,
                CastVFX = "spark_iron_skin",
                CastSFX = "sfx_armor_up",
                SelfEffects = new()
                {
                    new AbilityEffect("PhysicalResist", ModifierType.Flat, 0.2f, -1f), // Permanent while active
                    new AbilityEffect("AllDamage", ModifierType.Percent, -15f, -1f) // Damage penalty
                },
                Tags = { "warrior", "toggle", "keystone", "defensive" }
            });

            // === JUGGERNAUT ===
            // Unstoppable advance - immunity to staggers
            registry.Register(new AbilityDefinition("Juggernaut")
            {
                DisplayName = "$ability_juggernaut",
                Description = "$ability_juggernaut_desc",
                BaseCooldown = 90f,
                Cost = new ResourceCost("Stamina", 40f),
                TargetType = AbilityTargetType.Self,
                Category = AbilityCategory.Active,
                CastVFX = "spark_juggernaut",
                CastSFX = "sfx_heavy_step",
                Animation = "emote_flex",
                SelfEffects = new()
                {
                    new AbilityEffect("StaggerImmune", ModifierType.Flat, 1f, 10f),
                    new AbilityEffect("MaxHealth", ModifierType.Percent, 20f, 10f),
                    new AbilityEffect("MoveSpeed", ModifierType.Percent, -25f, 10f)
                },
                Tags = { "warrior", "keystone", "tank", "unstoppable" }
            });

            // === WARLORD ===
            // Melee mastery - larger attack area
            registry.Register(new AbilityDefinition("Warlord")
            {
                DisplayName = "$ability_warlord",
                Description = "$ability_warlord_desc",
                BaseCooldown = 0f, // Passive
                TargetType = AbilityTargetType.Self,
                Category = AbilityCategory.Passive,
                SelfEffects = new()
                {
                    new AbilityEffect("MeleeArea", ModifierType.Percent, 25f, -1f),
                    new AbilityEffect("MeleeDamage", ModifierType.Percent, 20f, -1f),
                    new AbilityEffect("AttackSpeed", ModifierType.Percent, -10f, -1f)
                },
                Tags = { "warrior", "passive", "keystone", "melee" }
            });

            // === EXECUTE ===
            // High damage to low health targets
            registry.Register(new AbilityDefinition("Execute")
            {
                DisplayName = "$ability_execute",
                Description = "$ability_execute_desc",
                BaseCooldown = 15f,
                Cost = new ResourceCost("Stamina", 35f),
                TargetType = AbilityTargetType.Enemy,
                Range = 3f,
                BaseDamage = 100f, // High base, meant for finishing
                DamageType = DamageType.Slash,
                ScalingStat = "Strength",
                ScalingFactor = 3.0f,
                CanCrit = true,
                Category = AbilityCategory.Active,
                CastVFX = "spark_execute",
                CastSFX = "sfx_heavy_swing",
                Animation = "attack_heavy",
                UseCondition = (caster) =>
                {
                    // Only usable on targets below 30% health - checked by user
                    return true;
                },
                Tags = { "warrior", "melee", "execute", "finisher" }
            });
        }

        #endregion

        #region Ranger Abilities

        private static void RegisterRangerAbilities(AbilityRegistry registry)
        {
            // === POWER SHOT ===
            // High damage single arrow
            registry.Register(new AbilityDefinition("PowerShot")
            {
                DisplayName = "$ability_powershot",
                Description = "$ability_powershot_desc",
                BaseCooldown = 10f,
                Cost = new ResourceCost("Stamina", 30f),
                TargetType = AbilityTargetType.Projectile,
                Range = 50f,
                BaseDamage = 60f,
                DamageType = DamageType.Pierce,
                ScalingStat = "Dexterity",
                ScalingFactor = 2.0f,
                CanCrit = true,
                ProjectileSpeed = 60f,
                Category = AbilityCategory.Active,
                CastVFX = "spark_powershot",
                HitVFX = "spark_arrow_impact",
                CastSFX = "sfx_bow_draw",
                HitSFX = "sfx_arrow_hit",
                Animation = "attack_bow",
                Tags = { "ranger", "ranged", "bow" }
            });

            // === MULTISHOT ===
            // Fire 3 arrows in a spread
            registry.Register(new AbilityDefinition("Multishot")
            {
                DisplayName = "$ability_multishot",
                Description = "$ability_multishot_desc",
                BaseCooldown = 15f,
                Cost = new ResourceCost("Stamina", 45f),
                TargetType = AbilityTargetType.Cone,
                Range = 40f,
                Radius = 30f, // Cone angle
                BaseDamage = 25f, // Per arrow
                DamageType = DamageType.Pierce,
                ScalingStat = "Dexterity",
                ScalingFactor = 1.0f,
                CanCrit = true,
                Category = AbilityCategory.Active,
                CastVFX = "spark_multishot",
                CastSFX = "sfx_bow_multishot",
                Animation = "attack_bow",
                Tags = { "ranger", "ranged", "bow", "aoe" }
            });

            // === EVASIVE ROLL ===
            // Dodge roll with brief invulnerability
            registry.Register(new AbilityDefinition("EvasiveRoll")
            {
                DisplayName = "$ability_evasiveroll",
                Description = "$ability_evasiveroll_desc",
                BaseCooldown = 6f,
                Cost = new ResourceCost("Stamina", 20f),
                TargetType = AbilityTargetType.Direction,
                Category = AbilityCategory.Active,
                CastVFX = "spark_dodge",
                CastSFX = "sfx_dodge_roll",
                Animation = "dodge",
                SelfEffects = new()
                {
                    new AbilityEffect("EvasionChance", ModifierType.Flat, 1.0f, 0.3f), // 100% evasion for 0.3s
                    new AbilityEffect("MoveSpeed", ModifierType.Percent, 30f, 1.5f)
                },
                Tags = { "ranger", "mobility", "defensive" }
            });

            // === HUNTER'S MARK ===
            // Mark target for increased damage
            registry.Register(new AbilityDefinition("HuntersMark")
            {
                DisplayName = "$ability_huntersmark",
                Description = "$ability_huntersmark_desc",
                BaseCooldown = 25f,
                Cost = new ResourceCost("Stamina", 20f),
                TargetType = AbilityTargetType.Enemy,
                Range = 30f,
                Category = AbilityCategory.Active,
                CastVFX = "spark_huntersmark",
                CastSFX = "sfx_mark",
                TargetEffects = new()
                {
                    new AbilityEffect("PhysicalResist", ModifierType.Flat, -0.25f, 15f), // -25% resist
                    new AbilityEffect("EvasionChance", ModifierType.Flat, -0.2f, 15f) // -20% evasion
                },
                Tags = { "ranger", "debuff", "mark" }
            });

            // === POISON ARROW ===
            // Apply poison DoT
            registry.Register(new AbilityDefinition("PoisonArrow")
            {
                DisplayName = "$ability_poisonarrow",
                Description = "$ability_poisonarrow_desc",
                BaseCooldown = 12f,
                Cost = new ResourceCost("Stamina", 25f),
                TargetType = AbilityTargetType.Projectile,
                Range = 40f,
                BaseDamage = 20f,
                DamageType = DamageType.Poison,
                ScalingStat = "Dexterity",
                ScalingFactor = 1.0f,
                ProjectileSpeed = 50f,
                Category = AbilityCategory.Active,
                CastVFX = "spark_poisonarrow",
                HitVFX = "spark_poison_cloud",
                CastSFX = "sfx_bow_draw",
                Animation = "attack_bow",
                // DoT applied via OnHit handler
                Tags = { "ranger", "ranged", "bow", "poison", "dot" }
            });

            // === DEADEYE ===
            // Keystone - ranged damage boost with attack speed penalty
            registry.Register(new AbilityDefinition("Deadeye")
            {
                DisplayName = "$ability_deadeye",
                Description = "$ability_deadeye_desc",
                BaseCooldown = 0f, // Passive
                TargetType = AbilityTargetType.Self,
                Category = AbilityCategory.Passive,
                SelfEffects = new()
                {
                    new AbilityEffect("RangedDamage", ModifierType.Percent, 50f, -1f),
                    new AbilityEffect("AttackSpeed", ModifierType.Percent, -30f, -1f),
                    new AbilityEffect("HeadshotDamage", ModifierType.Percent, 100f, -1f)
                },
                Tags = { "ranger", "passive", "keystone", "ranged" }
            });

            // === WIND WALKER ===
            // Keystone - extreme mobility, no heavy armor
            registry.Register(new AbilityDefinition("WindWalker")
            {
                DisplayName = "$ability_windwalker",
                Description = "$ability_windwalker_desc",
                BaseCooldown = 0f, // Passive
                TargetType = AbilityTargetType.Self,
                Category = AbilityCategory.Passive,
                SelfEffects = new()
                {
                    new AbilityEffect("MoveSpeed", ModifierType.Percent, 50f, -1f),
                    new AbilityEffect("DodgeDistance", ModifierType.Percent, 20f, -1f)
                },
                Tags = { "ranger", "passive", "keystone", "mobility" }
            });

            // === SNIPER ===
            // Keystone - stealth and range damage bonuses
            registry.Register(new AbilityDefinition("Sniper")
            {
                DisplayName = "$ability_sniper",
                Description = "$ability_sniper_desc",
                BaseCooldown = 0f, // Passive
                TargetType = AbilityTargetType.Self,
                Category = AbilityCategory.Passive,
                SelfEffects = new()
                {
                    new AbilityEffect("StealthDamage", ModifierType.Percent, 100f, -1f),
                    new AbilityEffect("MaxRangeDamage", ModifierType.Percent, 30f, -1f)
                },
                Tags = { "ranger", "passive", "keystone", "stealth", "ranged" }
            });

            // === SHADOW STRIKE ===
            // Keystone ability - backstab damage and crit
            registry.Register(new AbilityDefinition("ShadowStrike")
            {
                DisplayName = "$ability_shadowstrike",
                Description = "$ability_shadowstrike_desc",
                BaseCooldown = 20f,
                Cost = new ResourceCost("Stamina", 35f),
                TargetType = AbilityTargetType.Enemy,
                Range = 5f,
                BaseDamage = 80f,
                DamageType = DamageType.Pierce,
                ScalingStat = "Dexterity",
                ScalingFactor = 3.0f,
                CanCrit = true,
                Category = AbilityCategory.Active,
                CastVFX = "spark_shadow_strike",
                CastSFX = "sfx_backstab",
                Animation = "attack_knife",
                CustomData = { ["backstabMultiplier"] = 1.4f },
                Tags = { "ranger", "keystone", "melee", "stealth", "assassin" }
            });

            // === TRAP ===
            // Place a trap on the ground
            registry.Register(new AbilityDefinition("BearTrap")
            {
                DisplayName = "$ability_beartrap",
                Description = "$ability_beartrap_desc",
                BaseCooldown = 30f,
                Cost = new ResourceCost("Stamina", 35f),
                TargetType = AbilityTargetType.Ground,
                Range = 10f,
                BaseDamage = 30f,
                DamageType = DamageType.Pierce,
                Category = AbilityCategory.Active,
                CastVFX = "spark_trap_place",
                HitVFX = "spark_trap_trigger",
                CastSFX = "sfx_trap_place",
                HitSFX = "sfx_trap_snap",
                Animation = "crouch",
                Tags = { "ranger", "trap", "control" }
            });
        }

        #endregion

        #region Sorcerer Abilities

        private static void RegisterSorcererAbilities(AbilityRegistry registry)
        {
            // === FIREBALL ===
            // Classic fire projectile
            registry.Register(new AbilityDefinition("Fireball")
            {
                DisplayName = "$ability_fireball",
                Description = "$ability_fireball_desc",
                BaseCooldown = 8f,
                Cost = new ResourceCost("Eitr", 25f),
                TargetType = AbilityTargetType.Projectile,
                Range = 30f,
                Radius = 3f, // Explosion radius
                BaseDamage = 50f,
                DamageType = DamageType.Fire,
                ScalingStat = "Intelligence",
                ScalingFactor = 2.5f,
                CanCrit = true,
                ProjectileSpeed = 25f,
                Category = AbilityCategory.Active,
                CastVFX = "spark_fireball_cast",
                HitVFX = "spark_fire_explosion",
                CastSFX = "sfx_fire_cast",
                HitSFX = "sfx_explosion",
                Animation = "attack_magic",
                Tags = { "sorcerer", "magic", "fire", "projectile", "aoe" }
            });

            // === FROST NOVA ===
            // AoE frost damage and slow
            registry.Register(new AbilityDefinition("FrostNova")
            {
                DisplayName = "$ability_frostnova",
                Description = "$ability_frostnova_desc",
                BaseCooldown = 15f,
                Cost = new ResourceCost("Eitr", 35f),
                TargetType = AbilityTargetType.AroundSelf,
                Radius = 8f,
                BaseDamage = 40f,
                DamageType = DamageType.Frost,
                ScalingStat = "Intelligence",
                ScalingFactor = 2.0f,
                CanCrit = true,
                Category = AbilityCategory.Active,
                CastVFX = "spark_frost_nova",
                CastSFX = "sfx_frost_burst",
                Animation = "attack_magic_aoe",
                TargetEffects = new()
                {
                    new AbilityEffect("MoveSpeed", ModifierType.Percent, -40f, 4f),
                    new AbilityEffect("AttackSpeed", ModifierType.Percent, -20f, 4f)
                },
                Tags = { "sorcerer", "magic", "frost", "aoe", "control" }
            });

            // === LIGHTNING BOLT ===
            // Instant high damage single target
            registry.Register(new AbilityDefinition("LightningBolt")
            {
                DisplayName = "$ability_lightningbolt",
                Description = "$ability_lightningbolt_desc",
                BaseCooldown = 6f,
                Cost = new ResourceCost("Eitr", 20f),
                TargetType = AbilityTargetType.Enemy,
                Range = 25f,
                BaseDamage = 55f,
                DamageType = DamageType.Lightning,
                ScalingStat = "Intelligence",
                ScalingFactor = 2.2f,
                CanCrit = true,
                Category = AbilityCategory.Active,
                CastVFX = "spark_lightning_bolt",
                HitVFX = "spark_lightning_strike",
                CastSFX = "sfx_lightning_cast",
                HitSFX = "sfx_lightning_strike",
                Animation = "attack_magic",
                Tags = { "sorcerer", "magic", "lightning" }
            });

            // === CHAIN LIGHTNING ===
            // Bounces between targets
            registry.Register(new AbilityDefinition("ChainLightning")
            {
                DisplayName = "$ability_chainlightning",
                Description = "$ability_chainlightning_desc",
                BaseCooldown = 12f,
                Cost = new ResourceCost("Eitr", 40f),
                TargetType = AbilityTargetType.Enemy,
                Range = 25f,
                Radius = 10f, // Chain range
                BaseDamage = 35f, // Per bounce
                DamageType = DamageType.Lightning,
                ScalingStat = "Intelligence",
                ScalingFactor = 1.5f,
                CanCrit = true,
                Category = AbilityCategory.Active,
                CastVFX = "spark_chain_lightning",
                CastSFX = "sfx_lightning_chain",
                Animation = "attack_magic",
                CustomData = { ["bounces"] = 4 },
                Tags = { "sorcerer", "magic", "lightning", "chain" }
            });

            // === MANA SHIELD ===
            // Absorb damage with eitr
            registry.Register(new AbilityDefinition("ManaShield")
            {
                DisplayName = "$ability_manashield",
                Description = "$ability_manashield_desc",
                BaseCooldown = 45f,
                Cost = new ResourceCost("Eitr", 30f),
                TargetType = AbilityTargetType.Self,
                Category = AbilityCategory.Toggle,
                CastVFX = "spark_mana_shield",
                CastSFX = "sfx_shield_up",
                SelfEffects = new()
                {
                    new AbilityEffect("PhysicalResist", ModifierType.Flat, 0.3f, 10f),
                    new AbilityEffect("FireResist", ModifierType.Flat, 0.3f, 10f),
                    new AbilityEffect("FrostResist", ModifierType.Flat, 0.3f, 10f),
                    new AbilityEffect("LightningResist", ModifierType.Flat, 0.3f, 10f)
                },
                Tags = { "sorcerer", "magic", "defensive", "shield" }
            });

            // === METEOR ===
            // Ultimate - massive AoE fire damage
            registry.Register(new AbilityDefinition("Meteor")
            {
                DisplayName = "$ability_meteor",
                Description = "$ability_meteor_desc",
                BaseCooldown = 120f,
                Cost = new ResourceCost("Eitr", 80f),
                TargetType = AbilityTargetType.AreaOfEffect,
                Range = 30f,
                Radius = 8f,
                BaseDamage = 150f,
                DamageType = DamageType.Fire,
                ScalingStat = "Intelligence",
                ScalingFactor = 4.0f,
                CanCrit = true,
                CastTime = 2f, // Channel time
                Interruptible = true,
                CanMoveWhileCasting = false,
                Category = AbilityCategory.Ultimate,
                CastVFX = "spark_meteor_channel",
                HitVFX = "spark_meteor_impact",
                CastSFX = "sfx_meteor_channel",
                HitSFX = "sfx_meteor_impact",
                Animation = "attack_magic_channel",
                Tags = { "sorcerer", "magic", "fire", "aoe", "ultimate" }
            });

            // === ARCHMAGE ===
            // Keystone - spell damage boost with eitr cost penalty
            registry.Register(new AbilityDefinition("Archmage")
            {
                DisplayName = "$ability_archmage",
                Description = "$ability_archmage_desc",
                BaseCooldown = 0f, // Passive
                TargetType = AbilityTargetType.Self,
                Category = AbilityCategory.Passive,
                SelfEffects = new()
                {
                    new AbilityEffect("SpellDamage", ModifierType.Percent, 40f, -1f),
                    new AbilityEffect("SpellCost", ModifierType.Percent, 30f, -1f),
                    new AbilityEffect("Intelligence", ModifierType.Flat, 20f, -1f)
                },
                Tags = { "sorcerer", "passive", "keystone", "magic" }
            });

            // === BATTLE MAGE ===
            // Keystone - melee attacks restore eitr
            registry.Register(new AbilityDefinition("BattleMage")
            {
                DisplayName = "$ability_battlemage",
                Description = "$ability_battlemage_desc",
                BaseCooldown = 0f, // Passive - on melee hit
                TargetType = AbilityTargetType.Self,
                Category = AbilityCategory.Passive,
                SelfEffects = new()
                {
                    new AbilityEffect("MeleeEitrRestore", ModifierType.Percent, 5f, -1f),
                    new AbilityEffect("MeleeDamage", ModifierType.Percent, 15f, -1f),
                    new AbilityEffect("SpellDamage", ModifierType.Percent, 15f, -1f)
                },
                Tags = { "sorcerer", "passive", "keystone", "hybrid" }
            });

            // === ELEMENTAL ROTATION ===
            // Keystone - casting one element buffs the next different element
            registry.Register(new AbilityDefinition("ElementalRotation")
            {
                DisplayName = "$ability_elementalrotation",
                Description = "$ability_elementalrotation_desc",
                BaseCooldown = 0f, // Passive - triggered on spell cast
                TargetType = AbilityTargetType.Self,
                Category = AbilityCategory.Passive,
                CustomData = { ["elementalBonus"] = 30f },
                Tags = { "sorcerer", "passive", "keystone", "elemental" }
            });

            // === PYROMANCER ===
            // Keystone - fire mastery with frost weakness
            registry.Register(new AbilityDefinition("Pyromancer")
            {
                DisplayName = "$ability_pyromancer",
                Description = "$ability_pyromancer_desc",
                BaseCooldown = 0f, // Passive
                TargetType = AbilityTargetType.Self,
                Category = AbilityCategory.Passive,
                SelfEffects = new()
                {
                    new AbilityEffect("FireDamage", ModifierType.Percent, 50f, -1f),
                    new AbilityEffect("FrostResist", ModifierType.Flat, -0.15f, -1f)
                },
                CustomData = { ["fireExplosion"] = true },
                Tags = { "sorcerer", "passive", "keystone", "fire" }
            });

            // === CRYOMANCER ===
            // Keystone - frost mastery with fire weakness
            registry.Register(new AbilityDefinition("Cryomancer")
            {
                DisplayName = "$ability_cryomancer",
                Description = "$ability_cryomancer_desc",
                BaseCooldown = 0f, // Passive
                TargetType = AbilityTargetType.Self,
                Category = AbilityCategory.Passive,
                SelfEffects = new()
                {
                    new AbilityEffect("FrostDamage", ModifierType.Percent, 50f, -1f),
                    new AbilityEffect("FireResist", ModifierType.Flat, -0.15f, -1f)
                },
                CustomData = { ["frostShatter"] = true },
                Tags = { "sorcerer", "passive", "keystone", "frost" }
            });

            // === TELEPORT ===
            // Blink to target location
            registry.Register(new AbilityDefinition("Teleport")
            {
                DisplayName = "$ability_teleport",
                Description = "$ability_teleport_desc",
                BaseCooldown = 20f,
                Cost = new ResourceCost("Eitr", 30f),
                TargetType = AbilityTargetType.Ground,
                Range = 15f,
                Category = AbilityCategory.Active,
                CastVFX = "spark_teleport",
                CastSFX = "sfx_teleport",
                Animation = "emote_point",
                Tags = { "sorcerer", "magic", "mobility" }
            });
        }

        #endregion

        #region Guardian Abilities

        private static void RegisterGuardianAbilities(AbilityRegistry registry)
        {
            // === HEALING TOUCH ===
            // Heal a single target
            registry.Register(new AbilityDefinition("HealingTouch")
            {
                DisplayName = "$ability_healingtouch",
                Description = "$ability_healingtouch_desc",
                BaseCooldown = 8f,
                Cost = new ResourceCost("Eitr", 25f),
                TargetType = AbilityTargetType.Friendly,
                Range = 15f,
                BaseDamage = -50f, // Negative = healing
                DamageType = DamageType.Spirit,
                ScalingStat = "Intelligence",
                ScalingFactor = 2.0f,
                Category = AbilityCategory.Active,
                CastVFX = "spark_heal",
                CastSFX = "sfx_heal",
                Animation = "attack_magic",
                Tags = { "guardian", "healing", "support" }
            });

            // === HEALING AURA ===
            // AoE heal over time
            registry.Register(new AbilityDefinition("HealingAura")
            {
                DisplayName = "$ability_healingaura",
                Description = "$ability_healingaura_desc",
                BaseCooldown = 30f,
                Cost = new ResourceCost("Eitr", 50f),
                TargetType = AbilityTargetType.AroundSelf,
                Radius = 10f,
                Category = AbilityCategory.Active,
                CastVFX = "spark_healing_aura",
                CastSFX = "sfx_aura_up",
                Animation = "attack_magic_aoe",
                SelfEffects = new()
                {
                    new AbilityEffect("HealthRegen", ModifierType.Percent, 100f, 10f) // Double regen
                },
                Tags = { "guardian", "healing", "support", "aoe" }
            });

            // === DIVINE SHIELD ===
            // Protect target from damage
            registry.Register(new AbilityDefinition("DivineShield")
            {
                DisplayName = "$ability_divineshield",
                Description = "$ability_divineshield_desc",
                BaseCooldown = 45f,
                Cost = new ResourceCost("Eitr", 40f),
                TargetType = AbilityTargetType.Friendly,
                Range = 20f,
                Category = AbilityCategory.Active,
                CastVFX = "spark_divine_shield",
                CastSFX = "sfx_shield_divine",
                TargetEffects = new()
                {
                    new AbilityEffect("Armor", ModifierType.Flat, 50f, 8f),
                    new AbilityEffect("PhysicalResist", ModifierType.Flat, 0.3f, 8f)
                },
                Tags = { "guardian", "defensive", "support", "shield" }
            });

            // === TAUNT ===
            // Force enemies to attack you
            registry.Register(new AbilityDefinition("Taunt")
            {
                DisplayName = "$ability_taunt",
                Description = "$ability_taunt_desc",
                BaseCooldown = 20f,
                Cost = new ResourceCost("Stamina", 25f),
                TargetType = AbilityTargetType.AroundSelf,
                Radius = 12f,
                Category = AbilityCategory.Active,
                CastVFX = "spark_taunt",
                CastSFX = "sfx_taunt",
                Animation = "emote_challenge",
                SelfEffects = new()
                {
                    new AbilityEffect("Armor", ModifierType.Percent, 20f, 6f),
                    new AbilityEffect("BlockPower", ModifierType.Percent, 30f, 6f)
                },
                Tags = { "guardian", "tank", "control" }
            });

            // === CONSECRATION ===
            // Holy ground that damages undead and heals allies
            registry.Register(new AbilityDefinition("Consecration")
            {
                DisplayName = "$ability_consecration",
                Description = "$ability_consecration_desc",
                BaseCooldown = 25f,
                Cost = new ResourceCost("Eitr", 45f),
                TargetType = AbilityTargetType.AroundSelf,
                Radius = 8f,
                BaseDamage = 25f,
                DamageType = DamageType.Spirit,
                ScalingStat = "Intelligence",
                ScalingFactor = 1.5f,
                Category = AbilityCategory.Active,
                CastVFX = "spark_consecration",
                CastSFX = "sfx_holy_ground",
                Animation = "attack_magic_aoe",
                Tags = { "guardian", "magic", "spirit", "aoe", "zone" }
            });

            // === PALADIN ===
            // Keystone - spirit damage + healing on hit
            registry.Register(new AbilityDefinition("Paladin")
            {
                DisplayName = "$ability_paladin",
                Description = "$ability_paladin_desc",
                BaseCooldown = 0f, // Passive
                TargetType = AbilityTargetType.Self,
                Category = AbilityCategory.Passive,
                SelfEffects = new()
                {
                    new AbilityEffect("SpiritDamage", ModifierType.Percent, 30f, -1f),
                    new AbilityEffect("HitHeal", ModifierType.Percent, 3f, -1f),
                    new AbilityEffect("PhysicalResist", ModifierType.Flat, 0.2f, -1f)
                },
                Tags = { "guardian", "passive", "keystone", "spirit", "sustain" }
            });

            // === MASS HEAL ===
            // Keystone ability - powerful AoE heal
            registry.Register(new AbilityDefinition("MassHeal")
            {
                DisplayName = "$ability_massheal",
                Description = "$ability_massheal_desc",
                BaseCooldown = 60f,
                Cost = new ResourceCost("Eitr", 70f),
                TargetType = AbilityTargetType.AroundSelf,
                Radius = 15f,
                BaseDamage = -100f, // Negative = healing
                DamageType = DamageType.Spirit,
                ScalingStat = "Intelligence",
                ScalingFactor = 3.0f,
                Category = AbilityCategory.Active,
                CastVFX = "spark_mass_heal",
                CastSFX = "sfx_mass_heal",
                Animation = "attack_magic_aoe",
                Tags = { "guardian", "keystone", "healing", "aoe", "support" }
            });

            // === BASTION ===
            // Keystone - extreme defense with aura protection
            registry.Register(new AbilityDefinition("Bastion")
            {
                DisplayName = "$ability_bastion",
                Description = "$ability_bastion_desc",
                BaseCooldown = 0f, // Passive
                TargetType = AbilityTargetType.Self,
                Category = AbilityCategory.Passive,
                SelfEffects = new()
                {
                    new AbilityEffect("Armor", ModifierType.Percent, 50f, -1f),
                    new AbilityEffect("BlockPower", ModifierType.Percent, 50f, -1f),
                    new AbilityEffect("MoveSpeed", ModifierType.Percent, -30f, -1f)
                },
                CustomData = { ["allyDamageReduction"] = 15f, ["auraRadius"] = 10f },
                Tags = { "guardian", "passive", "keystone", "tank", "aura" }
            });

            // === RESURRECTION ===
            // Revive a fallen player (if implemented)
            registry.Register(new AbilityDefinition("Resurrection")
            {
                DisplayName = "$ability_resurrection",
                Description = "$ability_resurrection_desc",
                BaseCooldown = 300f, // 5 minutes
                Cost = new ResourceCost("Eitr", 100f),
                TargetType = AbilityTargetType.Friendly,
                Range = 10f,
                CastTime = 5f,
                Interruptible = true,
                CanMoveWhileCasting = false,
                Category = AbilityCategory.Ultimate,
                CastVFX = "spark_resurrection",
                CastSFX = "sfx_resurrection",
                Animation = "attack_magic_channel",
                Tags = { "guardian", "healing", "support", "ultimate" }
            });
        }

        #endregion

        #region Creature Abilities (for Denizen)

        private static void RegisterCreatureAbilities(AbilityRegistry registry)
        {
            // === FROST BREATH ===
            // Cone frost attack for frost creatures
            registry.Register(new AbilityDefinition("FrostBreath")
            {
                DisplayName = "$ability_frostbreath",
                Description = "$ability_frostbreath_desc",
                BaseCooldown = 10f,
                TargetType = AbilityTargetType.Cone,
                Range = 10f,
                Radius = 45f, // Cone angle
                BaseDamage = 30f,
                DamageType = DamageType.Frost,
                Category = AbilityCategory.Active,
                CastVFX = "spark_frost_breath",
                CastSFX = "sfx_frost_breath",
                TargetEffects = new()
                {
                    new AbilityEffect("MoveSpeed", ModifierType.Percent, -30f, 3f)
                },
                Tags = { "creature", "frost", "cone", "breath" }
            });

            // === FIRE BREATH ===
            // Cone fire attack for dragons/drakes
            registry.Register(new AbilityDefinition("FireBreath")
            {
                DisplayName = "$ability_firebreath",
                Description = "$ability_firebreath_desc",
                BaseCooldown = 12f,
                TargetType = AbilityTargetType.Cone,
                Range = 12f,
                Radius = 40f,
                BaseDamage = 40f,
                DamageType = DamageType.Fire,
                Category = AbilityCategory.Active,
                CastVFX = "spark_fire_breath",
                CastSFX = "sfx_fire_breath",
                Tags = { "creature", "fire", "cone", "breath" }
            });

            // === ENRAGE ===
            // Boost own damage when low health
            registry.Register(new AbilityDefinition("Enrage")
            {
                DisplayName = "$ability_enrage",
                Description = "$ability_enrage_desc",
                BaseCooldown = 60f,
                TargetType = AbilityTargetType.Self,
                Category = AbilityCategory.Active,
                CastVFX = "spark_enrage",
                CastSFX = "sfx_enrage",
                SelfEffects = new()
                {
                    new AbilityEffect("PhysicalDamage", ModifierType.Percent, 50f, 15f),
                    new AbilityEffect("AttackSpeed", ModifierType.Percent, 30f, 15f),
                    new AbilityEffect("MoveSpeed", ModifierType.Percent, 20f, 15f)
                },
                Tags = { "creature", "buff", "enrage" }
            });

            // === SUMMON MINIONS ===
            // Spawn adds (boss ability)
            registry.Register(new AbilityDefinition("SummonMinions")
            {
                DisplayName = "$ability_summonminions",
                Description = "$ability_summonminions_desc",
                BaseCooldown = 45f,
                TargetType = AbilityTargetType.AroundSelf,
                Radius = 8f,
                Category = AbilityCategory.Active,
                CastVFX = "spark_summon",
                CastSFX = "sfx_summon",
                CastTime = 2f,
                CustomData = { ["summonCount"] = 3, ["summonPrefab"] = "Skeleton" },
                Tags = { "creature", "boss", "summon" }
            });

            // === POISON SPIT ===
            // Ranged poison attack
            registry.Register(new AbilityDefinition("PoisonSpit")
            {
                DisplayName = "$ability_poisonspit",
                Description = "$ability_poisonspit_desc",
                BaseCooldown = 8f,
                TargetType = AbilityTargetType.Projectile,
                Range = 15f,
                Radius = 2f,
                BaseDamage = 20f,
                DamageType = DamageType.Poison,
                ProjectileSpeed = 20f,
                Category = AbilityCategory.Active,
                CastVFX = "spark_poison_spit",
                HitVFX = "spark_poison_cloud",
                CastSFX = "sfx_spit",
                Tags = { "creature", "poison", "ranged" }
            });

            // === ICE SPIKES ===
            // Ground-targeted frost AoE
            registry.Register(new AbilityDefinition("IceSpikes")
            {
                DisplayName = "$ability_icespikes",
                Description = "$ability_icespikes_desc",
                BaseCooldown = 12f,
                TargetType = AbilityTargetType.AreaOfEffect,
                Range = 20f,
                Radius = 4f,
                BaseDamage = 35f,
                DamageType = DamageType.Frost,
                Category = AbilityCategory.Active,
                CastVFX = "spark_ice_spikes",
                CastSFX = "sfx_ice_spikes",
                TargetEffects = new()
                {
                    new AbilityEffect("MoveSpeed", ModifierType.Percent, -50f, 2f)
                },
                Tags = { "creature", "frost", "aoe" }
            });

            // === GROUND POUND ===
            // Boss slam attack
            registry.Register(new AbilityDefinition("GroundPound")
            {
                DisplayName = "$ability_groundpound",
                Description = "$ability_groundpound_desc",
                BaseCooldown = 15f,
                TargetType = AbilityTargetType.AroundSelf,
                Radius = 10f,
                BaseDamage = 60f,
                DamageType = DamageType.Blunt,
                Category = AbilityCategory.Active,
                CastVFX = "spark_ground_pound",
                CastSFX = "sfx_ground_pound",
                Tags = { "creature", "boss", "aoe", "knockback" }
            });

            // === ROAR ===
            // Intimidate players, reduce their damage
            registry.Register(new AbilityDefinition("Roar")
            {
                DisplayName = "$ability_roar",
                Description = "$ability_roar_desc",
                BaseCooldown = 30f,
                TargetType = AbilityTargetType.AroundSelf,
                Radius = 15f,
                Category = AbilityCategory.Active,
                CastVFX = "spark_roar",
                CastSFX = "sfx_roar",
                TargetEffects = new()
                {
                    new AbilityEffect("PhysicalDamage", ModifierType.Percent, -20f, 8f),
                    new AbilityEffect("CritChance", ModifierType.Flat, -0.1f, 8f)
                },
                Tags = { "creature", "boss", "debuff", "fear" }
            });

            // === LIGHTNING STRIKE ===
            // Instant targeted lightning
            registry.Register(new AbilityDefinition("LightningStrike")
            {
                DisplayName = "$ability_lightningstrike",
                Description = "$ability_lightningstrike_desc",
                BaseCooldown = 10f,
                TargetType = AbilityTargetType.Enemy,
                Range = 25f,
                BaseDamage = 45f,
                DamageType = DamageType.Lightning,
                Category = AbilityCategory.Active,
                CastVFX = "spark_lightning_strike",
                CastSFX = "sfx_lightning_strike",
                Tags = { "creature", "lightning" }
            });

            // === SPIRIT DRAIN ===
            // Drain health from target
            registry.Register(new AbilityDefinition("SpiritDrain")
            {
                DisplayName = "$ability_spiritdrain",
                Description = "$ability_spiritdrain_desc",
                BaseCooldown = 15f,
                TargetType = AbilityTargetType.Enemy,
                Range = 10f,
                BaseDamage = 30f,
                DamageType = DamageType.Spirit,
                Category = AbilityCategory.Active,
                CastVFX = "spark_spirit_drain",
                CastSFX = "sfx_spirit_drain",
                SelfEffects = new()
                {
                    new AbilityEffect("HealthRegen", ModifierType.Flat, 20f, 5f) // Heal over time
                },
                Tags = { "creature", "spirit", "drain", "healing" }
            });
        }

        #endregion

        #region Consumable/Buff Abilities (for Tome)

        private static void RegisterConsumableAbilities(AbilityRegistry registry)
        {
            // === STRENGTH POTION ===
            registry.Register(new AbilityDefinition("StrengthPotion")
            {
                DisplayName = "$ability_strengthpotion",
                Description = "$ability_strengthpotion_desc",
                BaseCooldown = 0f, // No cooldown - consumable handles it
                TargetType = AbilityTargetType.Self,
                Category = AbilityCategory.Item,
                CastVFX = "spark_potion_drink",
                CastSFX = "sfx_drink",
                SelfEffects = new()
                {
                    new AbilityEffect("Strength", ModifierType.Flat, 15f, 300f),
                    new AbilityEffect("PhysicalDamage", ModifierType.Percent, 10f, 300f)
                },
                Tags = { "consumable", "potion", "buff" }
            });

            // === SWIFTNESS ELIXIR ===
            registry.Register(new AbilityDefinition("SwiftnessElixir")
            {
                DisplayName = "$ability_swiftnesselixir",
                Description = "$ability_swiftnesselixir_desc",
                BaseCooldown = 0f,
                TargetType = AbilityTargetType.Self,
                Category = AbilityCategory.Item,
                CastVFX = "spark_potion_drink",
                CastSFX = "sfx_drink",
                SelfEffects = new()
                {
                    new AbilityEffect("MoveSpeed", ModifierType.Percent, 25f, 300f),
                    new AbilityEffect("AttackSpeed", ModifierType.Percent, 10f, 300f)
                },
                Tags = { "consumable", "potion", "buff" }
            });

            // === IRON SKIN POTION ===
            registry.Register(new AbilityDefinition("IronSkinPotion")
            {
                DisplayName = "$ability_ironskinpotion",
                Description = "$ability_ironskinpotion_desc",
                BaseCooldown = 0f,
                TargetType = AbilityTargetType.Self,
                Category = AbilityCategory.Item,
                CastVFX = "spark_potion_drink",
                CastSFX = "sfx_drink",
                SelfEffects = new()
                {
                    new AbilityEffect("Armor", ModifierType.Flat, 30f, 300f),
                    new AbilityEffect("PhysicalResist", ModifierType.Flat, 0.15f, 300f)
                },
                Tags = { "consumable", "potion", "buff", "defensive" }
            });

            // === FIRE RESISTANCE POTION ===
            registry.Register(new AbilityDefinition("FireResistPotion")
            {
                DisplayName = "$ability_fireresistpotion",
                Description = "$ability_fireresistpotion_desc",
                BaseCooldown = 0f,
                TargetType = AbilityTargetType.Self,
                Category = AbilityCategory.Item,
                CastVFX = "spark_potion_drink",
                CastSFX = "sfx_drink",
                SelfEffects = new()
                {
                    new AbilityEffect("FireResist", ModifierType.Flat, 0.5f, 600f)
                },
                Tags = { "consumable", "potion", "buff", "resistance", "fire" }
            });

            // === FROST RESISTANCE POTION ===
            registry.Register(new AbilityDefinition("FrostResistPotion")
            {
                DisplayName = "$ability_frostresistpotion",
                Description = "$ability_frostresistpotion_desc",
                BaseCooldown = 0f,
                TargetType = AbilityTargetType.Self,
                Category = AbilityCategory.Item,
                CastVFX = "spark_potion_drink",
                CastSFX = "sfx_drink",
                SelfEffects = new()
                {
                    new AbilityEffect("FrostResist", ModifierType.Flat, 0.5f, 600f)
                },
                Tags = { "consumable", "potion", "buff", "resistance", "frost" }
            });

            // === MANA POTION ===
            registry.Register(new AbilityDefinition("ManaPotion")
            {
                DisplayName = "$ability_manapotion",
                Description = "$ability_manapotion_desc",
                BaseCooldown = 0f,
                TargetType = AbilityTargetType.Self,
                Category = AbilityCategory.Item,
                CastVFX = "spark_potion_drink",
                CastSFX = "sfx_drink",
                SelfEffects = new()
                {
                    new AbilityEffect("EitrRegen", ModifierType.Percent, 200f, 30f) // Triple regen for 30s
                },
                Tags = { "consumable", "potion", "mana" }
            });

            // === BERSERKER MUSHROOM ===
            registry.Register(new AbilityDefinition("BerserkerMushroom")
            {
                DisplayName = "$ability_berserkermushroom",
                Description = "$ability_berserkermushroom_desc",
                BaseCooldown = 0f,
                TargetType = AbilityTargetType.Self,
                Category = AbilityCategory.Item,
                CastVFX = "spark_mushroom_eat",
                CastSFX = "sfx_eat",
                SelfEffects = new()
                {
                    new AbilityEffect("PhysicalDamage", ModifierType.Percent, 30f, 120f),
                    new AbilityEffect("AttackSpeed", ModifierType.Percent, 15f, 120f),
                    new AbilityEffect("Armor", ModifierType.Percent, -15f, 120f) // Drawback
                },
                Tags = { "consumable", "food", "buff" }
            });

            // === CRITICAL SCROLL ===
            registry.Register(new AbilityDefinition("CriticalScroll")
            {
                DisplayName = "$ability_criticalscroll",
                Description = "$ability_criticalscroll_desc",
                BaseCooldown = 0f,
                TargetType = AbilityTargetType.Self,
                Category = AbilityCategory.Item,
                CastVFX = "spark_scroll_use",
                CastSFX = "sfx_scroll",
                SelfEffects = new()
                {
                    new AbilityEffect("CritChance", ModifierType.Flat, 0.2f, 180f), // +20% crit
                    new AbilityEffect("CritDamage", ModifierType.Flat, 0.5f, 180f) // +50% crit damage
                },
                Tags = { "consumable", "scroll", "buff", "crit" }
            });
        }

        #endregion

        #region Utility Abilities

        private static void RegisterUtilityAbilities(AbilityRegistry registry)
        {
            // === SPRINT ===
            // Burst of speed
            registry.Register(new AbilityDefinition("Sprint")
            {
                DisplayName = "$ability_sprint",
                Description = "$ability_sprint_desc",
                BaseCooldown = 30f,
                Cost = new ResourceCost("Stamina", 40f),
                TargetType = AbilityTargetType.Self,
                Category = AbilityCategory.Active,
                CastVFX = "spark_sprint",
                CastSFX = "sfx_dash",
                SelfEffects = new()
                {
                    new AbilityEffect("MoveSpeed", ModifierType.Percent, 50f, 5f)
                },
                Tags = { "utility", "mobility" }
            });

            // === SECOND WIND ===
            // Recover stamina
            registry.Register(new AbilityDefinition("SecondWind")
            {
                DisplayName = "$ability_secondwind",
                Description = "$ability_secondwind_desc",
                BaseCooldown = 60f,
                TargetType = AbilityTargetType.Self,
                Category = AbilityCategory.Active,
                CastVFX = "spark_second_wind",
                CastSFX = "sfx_breath",
                SelfEffects = new()
                {
                    new AbilityEffect("StaminaRegen", ModifierType.Percent, 300f, 8f) // 4x regen
                },
                Tags = { "utility", "recovery" }
            });

            // === BATTLE FOCUS ===
            // Increase crit chance temporarily
            registry.Register(new AbilityDefinition("BattleFocus")
            {
                DisplayName = "$ability_battlefocus",
                Description = "$ability_battlefocus_desc",
                BaseCooldown = 45f,
                Cost = new ResourceCost("Stamina", 25f),
                TargetType = AbilityTargetType.Self,
                Category = AbilityCategory.Active,
                CastVFX = "spark_focus",
                CastSFX = "sfx_focus",
                SelfEffects = new()
                {
                    new AbilityEffect("CritChance", ModifierType.Flat, 0.25f, 10f),
                    new AbilityEffect("CritDamage", ModifierType.Flat, 0.25f, 10f)
                },
                Tags = { "utility", "buff", "crit" }
            });

            // === FORTIFY ===
            // Defensive stance
            registry.Register(new AbilityDefinition("Fortify")
            {
                DisplayName = "$ability_fortify",
                Description = "$ability_fortify_desc",
                BaseCooldown = 40f,
                Cost = new ResourceCost("Stamina", 30f),
                TargetType = AbilityTargetType.Self,
                Category = AbilityCategory.Active,
                CastVFX = "spark_fortify",
                CastSFX = "sfx_fortify",
                SelfEffects = new()
                {
                    new AbilityEffect("Armor", ModifierType.Percent, 30f, 8f),
                    new AbilityEffect("BlockPower", ModifierType.Percent, 40f, 8f),
                    new AbilityEffect("MoveSpeed", ModifierType.Percent, -20f, 8f) // Slower while fortified
                },
                Tags = { "utility", "defensive" }
            });

            // === LIFE TAP ===
            // Convert health to eitr
            registry.Register(new AbilityDefinition("LifeTap")
            {
                DisplayName = "$ability_lifetap",
                Description = "$ability_lifetap_desc",
                BaseCooldown = 10f,
                Cost = new ResourceCost("Health", 20f),
                TargetType = AbilityTargetType.Self,
                Category = AbilityCategory.Active,
                CastVFX = "spark_life_tap",
                CastSFX = "sfx_life_tap",
                SelfEffects = new()
                {
                    new AbilityEffect("EitrRegen", ModifierType.Flat, 50f, 5f) // Instant eitr boost
                },
                Tags = { "utility", "magic", "conversion" }
            });

            // === BLOODLUST ===
            // Lifesteal buff
            registry.Register(new AbilityDefinition("Bloodlust")
            {
                DisplayName = "$ability_bloodlust",
                Description = "$ability_bloodlust_desc",
                BaseCooldown = 60f,
                Cost = new ResourceCost("Stamina", 35f),
                TargetType = AbilityTargetType.Self,
                Category = AbilityCategory.Active,
                CastVFX = "spark_bloodlust",
                CastSFX = "sfx_bloodlust",
                SelfEffects = new()
                {
                    new AbilityEffect("LifeSteal", ModifierType.Flat, 0.2f, 12f) // 20% lifesteal
                },
                Tags = { "utility", "buff", "lifesteal" }
            });
        }

        #endregion
    }
}
