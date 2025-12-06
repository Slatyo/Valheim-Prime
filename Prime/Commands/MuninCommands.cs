using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Munin;
using Prime.Core;
using Prime.Stats;
using Prime.Modifiers;
using Prime.Abilities;
using Prime.Effects;
using Prime.Combat;
using UnityEngine;

namespace Prime.Commands
{
    /// <summary>
    /// Munin command registration for Prime.
    /// All commands are registered under: munin prime [command]
    /// </summary>
    public static class MuninCommands
    {
        private const string MOD_NAME = "prime";

        /// <summary>
        /// Registers all Prime commands with Munin.
        /// </summary>
        public static void Register()
        {
            // Check if Munin is loaded
            if (!BepInEx.Bootstrap.Chainloader.PluginInfos.ContainsKey("com.slatyo.munin"))
            {
                Plugin.Log.LogWarning("Munin not found - commands will not be available");
                return;
            }

            RegisterStatCommands();
            RegisterAbilityCommands();
            RegisterEffectCommands();
            RegisterCombatCommands();

            Plugin.Log.LogInfo("Registered Prime commands with Munin");
        }

        #region Stat Commands

        private static void RegisterStatCommands()
        {
            // munin prime stats - Show all stats
            Command.Register(MOD_NAME, new CommandConfig
            {
                Name = "stats",
                Description = "Show all stats for local player",
                Permission = PermissionLevel.Anyone,
                Handler = (args) =>
                {
                    var player = args.Player ?? Player.m_localPlayer;
                    if (player == null)
                        return CommandResult.Error("No player found");

                    var sb = new StringBuilder();
                    sb.AppendLine($"<color=#{ChatColor.Gold}>=== Prime Stats ===</color>");

                    foreach (var category in Enum.GetValues(typeof(StatCategory)).Cast<StatCategory>())
                    {
                        var stats = StatRegistry.Instance.GetByCategory(category).ToList();
                        if (stats.Count == 0) continue;

                        sb.AppendLine($"\n<color=#{ChatColor.Info}>[{category}]</color>");
                        foreach (var stat in stats)
                        {
                            float value = PrimeAPI.Get(player, stat.Id);
                            float baseValue = PrimeAPI.GetBase(player, stat.Id);

                            string valueStr = FormatStatValue(stat, value);
                            string baseStr = Math.Abs(value - baseValue) > 0.001f
                                ? $" (base: {FormatStatValue(stat, baseValue)})"
                                : "";

                            sb.AppendLine($"  {stat.Id}: {valueStr}{baseStr}");
                        }
                    }

                    return CommandResult.Info(sb.ToString());
                }
            });

            // munin prime get <stat> - Get specific stat
            Command.Register(MOD_NAME, new CommandConfig
            {
                Name = "get",
                Description = "Get a specific stat value",
                Usage = "<stat>",
                Examples = new[] { "Strength", "CritChance", "MoveSpeed" },
                Permission = PermissionLevel.Anyone,
                Handler = (args) =>
                {
                    var player = args.Player ?? Player.m_localPlayer;
                    if (player == null)
                        return CommandResult.Error("No player found");

                    var statId = args.Get(0);
                    if (string.IsNullOrEmpty(statId))
                        return CommandResult.Error("Usage: munin prime get <stat>");

                    var stat = StatRegistry.Instance.Get(statId);
                    if (stat == null)
                        return CommandResult.NotFound($"Unknown stat: {statId}");

                    float value = PrimeAPI.Get(player, statId);
                    float baseValue = PrimeAPI.GetBase(player, statId);

                    return CommandResult.Success($"{statId}: {FormatStatValue(stat, value)} (base: {FormatStatValue(stat, baseValue)})");
                }
            });

            // munin prime set <stat> <value> - Set stat base value
            Command.Register(MOD_NAME, new CommandConfig
            {
                Name = "set",
                Description = "Set base value of a stat",
                Usage = "<stat> <value>",
                Examples = new[] { "Strength 25", "CritChance 0.5", "MoveSpeed 1.5" },
                Permission = PermissionLevel.Admin,
                Handler = (args) =>
                {
                    var player = args.Player ?? Player.m_localPlayer;
                    if (player == null)
                        return CommandResult.Error("No player found");

                    var statId = args.Get(0);
                    var value = args.Get<float>(1, float.NaN);

                    if (string.IsNullOrEmpty(statId) || float.IsNaN(value))
                        return CommandResult.Error("Usage: munin prime set <stat> <value>");

                    if (!StatRegistry.Instance.IsRegistered(statId))
                        return CommandResult.NotFound($"Unknown stat: {statId}");

                    PrimeAPI.SetBase(player, statId, value);
                    return CommandResult.Success($"Set {statId} base to {value}");
                }
            });

            // munin prime mod <add|remove|list|clear>
            Command.Register(MOD_NAME, new CommandConfig
            {
                Name = "mod",
                Description = "Manage stat modifiers",
                Usage = "<add|remove|list|clear> [args...]",
                Examples = new[] { "add Strength percent 50 60", "list", "clear" },
                Permission = PermissionLevel.Admin,
                Handler = HandleModifierCommand
            });

            // munin prime breakdown <stat>
            Command.Register(MOD_NAME, new CommandConfig
            {
                Name = "breakdown",
                Description = "Show detailed stat calculation breakdown",
                Usage = "<stat>",
                Examples = new[] { "Strength", "CritDamage" },
                Permission = PermissionLevel.Anyone,
                Handler = (args) =>
                {
                    var player = args.Player ?? Player.m_localPlayer;
                    if (player == null)
                        return CommandResult.Error("No player found");

                    var statId = args.Get(0);
                    if (string.IsNullOrEmpty(statId))
                        return CommandResult.Error("Usage: munin prime breakdown <stat>");

                    if (!StatRegistry.Instance.IsRegistered(statId))
                        return CommandResult.NotFound($"Unknown stat: {statId}");

                    var breakdown = PrimeAPI.GetBreakdown(player, statId);
                    return CommandResult.Info(breakdown.ToString());
                }
            });

            // munin prime registered - List all registered stats
            Command.Register(MOD_NAME, new CommandConfig
            {
                Name = "registered",
                Description = "List all registered stat definitions",
                Permission = PermissionLevel.Anyone,
                Handler = (args) =>
                {
                    var stats = StatRegistry.Instance.GetAll().OrderBy(s => s.Category).ThenBy(s => s.Id);

                    var sb = new StringBuilder();
                    sb.AppendLine($"<color=#{ChatColor.Gold}>=== Registered Stats ({StatRegistry.Instance.Count}) ===</color>");

                    StatCategory? lastCategory = null;
                    foreach (var stat in stats)
                    {
                        if (stat.Category != lastCategory)
                        {
                            sb.AppendLine($"\n<color=#{ChatColor.Info}>[{stat.Category}]</color>");
                            lastCategory = stat.Category;
                        }

                        string bounds = "";
                        if (stat.MinValue.HasValue || stat.MaxValue.HasValue)
                        {
                            bounds = $" [{stat.MinValue?.ToString() ?? "-inf"} to {stat.MaxValue?.ToString() ?? "inf"}]";
                        }

                        sb.AppendLine($"  {stat.Id}: base={stat.BaseValue}{bounds}");
                    }

                    return CommandResult.Info(sb.ToString());
                }
            });
        }

        private static CommandResult HandleModifierCommand(CommandArgs args)
        {
            var player = args.Player ?? Player.m_localPlayer;
            if (player == null)
                return CommandResult.Error("No player found");

            var action = args.Get(0)?.ToLowerInvariant();
            if (string.IsNullOrEmpty(action))
                return CommandResult.Error("Usage: munin prime mod <add|remove|list|clear>");

            switch (action)
            {
                case "add":
                    return AddModifier(player, args);
                case "remove":
                    return RemoveModifier(player, args);
                case "list":
                    return ListModifiers(player);
                case "clear":
                    PrimeAPI.ClearAllModifiers(player);
                    return CommandResult.Success("Cleared all modifiers");
                default:
                    return CommandResult.Error($"Unknown action: {action}. Use add, remove, list, or clear");
            }
        }

        private static CommandResult AddModifier(Player player, CommandArgs args)
        {
            var statId = args.Get(1);
            var typeStr = args.Get(2);
            var value = args.Get<float>(3, float.NaN);
            var duration = args.Get<float>(4, -1f);

            if (string.IsNullOrEmpty(statId) || string.IsNullOrEmpty(typeStr) || float.IsNaN(value))
                return CommandResult.Error("Usage: munin prime mod add <stat> <flat|percent|multiply> <value> [duration]");

            if (!StatRegistry.Instance.IsRegistered(statId))
                return CommandResult.NotFound($"Unknown stat: {statId}");

            ModifierType type;
            switch (typeStr.ToLowerInvariant())
            {
                case "flat": type = ModifierType.Flat; break;
                case "percent": type = ModifierType.Percent; break;
                case "multiply": type = ModifierType.Multiply; break;
                default:
                    return CommandResult.Error($"Unknown modifier type: {typeStr}. Use: flat, percent, multiply");
            }

            float? dur = duration > 0 ? (float?)duration : null;
            string id = $"console_{statId}_{Guid.NewGuid():N}";

            var modifier = new Modifier(id, statId, type, value)
            {
                Duration = dur,
                Source = "Console"
            };

            PrimeAPI.AddModifier(player, modifier);

            string durationInfo = dur.HasValue ? $" for {dur}s" : " (permanent)";
            return CommandResult.Success($"Added {type} {value} to {statId}{durationInfo}\nID: {id}");
        }

        private static CommandResult RemoveModifier(Player player, CommandArgs args)
        {
            var modifierId = args.Get(1);
            if (string.IsNullOrEmpty(modifierId))
                return CommandResult.Error("Usage: munin prime mod remove <id>");

            if (PrimeAPI.RemoveModifier(player, modifierId))
                return CommandResult.Success($"Removed modifier: {modifierId}");
            else
                return CommandResult.NotFound($"Modifier not found: {modifierId}");
        }

        private static CommandResult ListModifiers(Player player)
        {
            var modifiers = PrimeAPI.GetAllModifiers(player).ToList();

            if (modifiers.Count == 0)
                return CommandResult.Info("No active modifiers");

            var sb = new StringBuilder();
            sb.AppendLine($"<color=#{ChatColor.Gold}>=== Active Modifiers ({modifiers.Count}) ===</color>");

            foreach (var mod in modifiers.OrderBy(m => m.StatId))
            {
                string typeSymbol = mod.Type switch
                {
                    ModifierType.Flat => "+",
                    ModifierType.Percent => "%",
                    ModifierType.Multiply => "x",
                    _ => "?"
                };

                string valueStr = mod.Type == ModifierType.Percent
                    ? $"+{mod.GetEffectiveValue()}%"
                    : $"{typeSymbol}{mod.GetEffectiveValue()}";

                string durationStr = "";
                if (mod.Duration.HasValue)
                {
                    var remaining = mod.GetRemainingDuration(Time.time);
                    durationStr = $" ({remaining:F1}s left)";
                }

                string stackStr = mod.Stacks > 1 ? $" x{mod.Stacks}" : "";
                sb.AppendLine($"  [{mod.StatId}] {valueStr}{stackStr}{durationStr}");
                sb.AppendLine($"    ID: {mod.Id}");
            }

            return CommandResult.Info(sb.ToString());
        }

        #endregion

        #region Ability Commands

        private static void RegisterAbilityCommands()
        {
            // munin prime ability list - List all registered abilities
            Command.Register(MOD_NAME, new CommandConfig
            {
                Name = "ability",
                Description = "Manage abilities (list, grant, revoke, use, granted)",
                Usage = "<list|grant|revoke|use|granted> [ability_id]",
                Examples = new[] { "list", "grant Fireball", "use Fireball", "granted" },
                Permission = PermissionLevel.Admin,
                Handler = HandleAbilityCommand
            });

            // Shortcut for quick ability use
            Command.Register(MOD_NAME, new CommandConfig
            {
                Name = "cast",
                Description = "Grant and immediately use an ability",
                Usage = "<ability_id>",
                Examples = new[] { "Fireball", "FrostNova", "ChainLightning" },
                Permission = PermissionLevel.Admin,
                Handler = (args) =>
                {
                    var player = args.Player ?? Player.m_localPlayer;
                    if (player == null)
                        return CommandResult.Error("No player found");

                    var abilityId = args.Get(0);
                    if (string.IsNullOrEmpty(abilityId))
                        return CommandResult.Error("Usage: munin prime cast <ability_id>");

                    if (!AbilityRegistry.Instance.IsRegistered(abilityId))
                        return CommandResult.NotFound($"Unknown ability: {abilityId}");

                    // Grant if not already granted
                    PrimeAPI.GrantAbility(player, abilityId);

                    // Use the ability
                    if (PrimeAPI.UseAbility(player, abilityId))
                        return CommandResult.Success($"Cast {abilityId}!");
                    else
                        return CommandResult.Error($"Cannot use {abilityId} (on cooldown or missing resources)");
                }
            });
        }

        private static CommandResult HandleAbilityCommand(CommandArgs args)
        {
            var player = args.Player ?? Player.m_localPlayer;
            if (player == null)
                return CommandResult.Error("No player found");

            var action = args.Get(0)?.ToLowerInvariant();
            if (string.IsNullOrEmpty(action))
                return CommandResult.Error("Usage: munin prime ability <list|grant|revoke|use|granted>");

            switch (action)
            {
                case "list":
                    return ListAbilities(args.Get(1));
                case "grant":
                    return GrantAbility(player, args.Get(1));
                case "revoke":
                    return RevokeAbility(player, args.Get(1));
                case "use":
                    return UseAbility(player, args.Get(1));
                case "granted":
                    return ListGrantedAbilities(player);
                default:
                    return CommandResult.Error($"Unknown action: {action}. Use list, grant, revoke, use, or granted");
            }
        }

        private static CommandResult ListAbilities(string filter)
        {
            var abilities = AbilityRegistry.Instance.GetAll().ToList();

            if (!string.IsNullOrEmpty(filter))
            {
                filter = filter.ToLowerInvariant();
                abilities = abilities.Where(a =>
                    a.Id.ToLowerInvariant().Contains(filter) ||
                    a.Category.ToString().ToLowerInvariant().Contains(filter) ||
                    a.Tags.Any(t => t.ToLowerInvariant().Contains(filter))
                ).ToList();
            }

            if (abilities.Count == 0)
            {
                return string.IsNullOrEmpty(filter)
                    ? CommandResult.Info("No abilities registered")
                    : CommandResult.NotFound($"No abilities matching '{filter}'");
            }

            var sb = new StringBuilder();
            sb.AppendLine($"<color=#{ChatColor.Gold}>=== Abilities ({abilities.Count}) ===</color>");

            var grouped = abilities.GroupBy(a => a.Category).OrderBy(g => g.Key);
            foreach (var group in grouped)
            {
                sb.AppendLine($"\n<color=#{ChatColor.Info}>[{group.Key}]</color>");
                foreach (var ability in group.OrderBy(a => a.Id))
                {
                    string dmg = ability.BaseDamage > 0 ? $" {ability.BaseDamage:F0} {ability.DamageType}" : "";
                    string cd = ability.BaseCooldown > 0 ? $" ({ability.BaseCooldown}s)" : "";
                    sb.AppendLine($"  {ability.Id}{dmg}{cd}");
                }
            }

            return CommandResult.Info(sb.ToString());
        }

        private static CommandResult GrantAbility(Player player, string abilityId)
        {
            if (string.IsNullOrEmpty(abilityId))
                return CommandResult.Error("Usage: munin prime ability grant <ability_id>");

            // Support "all" to grant all abilities
            if (abilityId.ToLowerInvariant() == "all")
            {
                int count = 0;
                foreach (var ability in AbilityRegistry.Instance.GetAll())
                {
                    if (PrimeAPI.GrantAbility(player, ability.Id))
                        count++;
                }
                return CommandResult.Success($"Granted {count} abilities");
            }

            if (!AbilityRegistry.Instance.IsRegistered(abilityId))
                return CommandResult.NotFound($"Unknown ability: {abilityId}");

            if (PrimeAPI.GrantAbility(player, abilityId))
                return CommandResult.Success($"Granted ability: {abilityId}");
            else
                return CommandResult.Info($"Already have ability: {abilityId}");
        }

        private static CommandResult RevokeAbility(Player player, string abilityId)
        {
            if (string.IsNullOrEmpty(abilityId))
                return CommandResult.Error("Usage: munin prime ability revoke <ability_id>");

            // Support "all" to revoke all abilities
            if (abilityId.ToLowerInvariant() == "all")
            {
                var granted = PrimeAPI.GetGrantedAbilities(player).ToList();
                foreach (var instance in granted)
                {
                    PrimeAPI.RevokeAbility(player, instance.Definition.Id);
                }
                return CommandResult.Success($"Revoked {granted.Count} abilities");
            }

            if (PrimeAPI.RevokeAbility(player, abilityId))
                return CommandResult.Success($"Revoked ability: {abilityId}");
            else
                return CommandResult.NotFound($"Don't have ability: {abilityId}");
        }

        private static CommandResult UseAbility(Player player, string abilityId)
        {
            if (string.IsNullOrEmpty(abilityId))
                return CommandResult.Error("Usage: munin prime ability use <ability_id>");

            if (!PrimeAPI.HasAbility(player, abilityId))
            {
                // Auto-grant for testing convenience
                if (AbilityRegistry.Instance.IsRegistered(abilityId))
                {
                    PrimeAPI.GrantAbility(player, abilityId);
                }
                else
                {
                    return CommandResult.NotFound($"Unknown ability: {abilityId}");
                }
            }

            if (PrimeAPI.UseAbility(player, abilityId))
                return CommandResult.Success($"Used ability: {abilityId}");
            else
                return CommandResult.Error($"Cannot use {abilityId} (on cooldown or missing resources)");
        }

        private static CommandResult ListGrantedAbilities(Player player)
        {
            var abilities = PrimeAPI.GetGrantedAbilities(player).ToList();

            if (abilities.Count == 0)
                return CommandResult.Info("No abilities granted. Use 'munin prime ability grant <id>' or 'munin prime cast <id>'");

            var sb = new StringBuilder();
            sb.AppendLine($"<color=#{ChatColor.Gold}>=== Granted Abilities ({abilities.Count}) ===</color>");

            foreach (var instance in abilities.OrderBy(a => a.Definition.Id))
            {
                string state = instance.State.ToString();
                string cd = instance.State == AbilityState.OnCooldown
                    ? $" ({instance.GetRemainingCooldown():F1}s)"
                    : "";
                string stateColor = instance.State == AbilityState.Ready ? ChatColor.Success : ChatColor.Warning;
                sb.AppendLine($"  {instance.Definition.Id}: <color=#{stateColor}>{state}</color>{cd}");
            }

            return CommandResult.Info(sb.ToString());
        }

        #endregion

        #region Effect Commands

        private static void RegisterEffectCommands()
        {
            Command.Register(MOD_NAME, new CommandConfig
            {
                Name = "effect",
                Description = "Manage status effects",
                Usage = "<list|apply|remove|clear> [effect_id]",
                Examples = new[] { "list", "apply Burning", "clear" },
                Permission = PermissionLevel.Admin,
                Handler = HandleEffectCommand
            });
        }

        private static CommandResult HandleEffectCommand(CommandArgs args)
        {
            var player = args.Player ?? Player.m_localPlayer;
            if (player == null)
                return CommandResult.Error("No player found");

            var action = args.Get(0)?.ToLowerInvariant();
            if (string.IsNullOrEmpty(action))
                return CommandResult.Error("Usage: munin prime effect <list|apply|remove|clear>");

            switch (action)
            {
                case "list":
                    return ListEffects(player);
                case "apply":
                    return ApplyEffect(player, args);
                case "remove":
                    return RemoveEffect(player, args.Get(1));
                case "clear":
                    EffectManager.RemoveAllEffects(player);
                    return CommandResult.Success("Cleared all effects");
                default:
                    return CommandResult.Error($"Unknown action: {action}");
            }
        }

        private static CommandResult ListEffects(Player player)
        {
            var effects = PrimeAPI.GetEffects(player).ToList();

            if (effects.Count == 0)
                return CommandResult.Info("No active effects");

            var sb = new StringBuilder();
            sb.AppendLine($"<color=#{ChatColor.Gold}>=== Active Effects ({effects.Count}) ===</color>");

            foreach (var effect in effects)
            {
                string duration = effect.Definition.Duration > 0
                    ? $" ({effect.GetRemainingDuration():F1}s left)"
                    : " (permanent)";
                string stacks = effect.Stacks > 1 ? $" x{effect.Stacks}" : "";
                string buffColor = effect.Definition.IsBuff ? ChatColor.Success : ChatColor.Error;
                sb.AppendLine($"  <color=#{buffColor}>{effect.Definition.Id}</color>{stacks}{duration}");
            }

            return CommandResult.Info(sb.ToString());
        }

        private static CommandResult ApplyEffect(Player player, CommandArgs args)
        {
            var effectId = args.Get(1);
            var duration = args.Get<float>(2, 30f);

            if (string.IsNullOrEmpty(effectId))
                return CommandResult.Error("Usage: munin prime effect apply <effect_id> [duration]");

            // Create a test effect
            var effect = new EffectDefinition(effectId)
            {
                Duration = duration,
                IsBuff = true
            };

            // Add stat boost based on effect name patterns
            var effectLower = effectId.ToLowerInvariant();
            if (effectLower.Contains("str"))
                effect.OnApply = (owner) => PrimeAPI.ApplyTimedPercent(owner, "Strength", 25f, duration, effectId);
            else if (effectLower.Contains("speed"))
                effect.OnApply = (owner) => PrimeAPI.ApplyTimedPercent(owner, "MoveSpeed", 30f, duration, effectId);
            else if (effectLower.Contains("crit"))
                effect.OnApply = (owner) => PrimeAPI.ApplyTimedFlat(owner, "CritChance", 0.15f, duration, effectId);
            else if (effectLower.Contains("armor"))
                effect.OnApply = (owner) => PrimeAPI.ApplyTimedPercent(owner, "Armor", 50f, duration, effectId);

            var instance = PrimeAPI.ApplyEffect(player, effect);
            if (instance != null)
                return CommandResult.Success($"Applied effect: {effectId} for {duration}s");
            else
                return CommandResult.Error($"Failed to apply effect: {effectId}");
        }

        private static CommandResult RemoveEffect(Player player, string effectId)
        {
            if (string.IsNullOrEmpty(effectId))
                return CommandResult.Error("Usage: munin prime effect remove <effect_id>");

            if (PrimeAPI.RemoveEffect(player, effectId))
                return CommandResult.Success($"Removed effect: {effectId}");
            else
                return CommandResult.NotFound($"Effect not found: {effectId}");
        }

        #endregion

        #region Combat Commands

        private static void RegisterCombatCommands()
        {
            // munin prime damage <amount> [type] - Deal damage to self (testing)
            Command.Register(MOD_NAME, new CommandConfig
            {
                Name = "damage",
                Description = "Deal damage to yourself (for testing)",
                Usage = "<amount> [type]",
                Examples = new[] { "50", "100 fire", "75 frost" },
                Permission = PermissionLevel.Admin,
                Handler = (args) =>
                {
                    var player = args.Player ?? Player.m_localPlayer;
                    if (player == null)
                        return CommandResult.Error("No player found");

                    var amount = args.Get<float>(0, 0f);
                    var typeStr = args.Get(1) ?? "physical";

                    if (amount <= 0)
                        return CommandResult.Error("Usage: munin prime damage <amount> [type]");

                    DamageType type = typeStr.ToLowerInvariant() switch
                    {
                        "fire" => DamageType.Fire,
                        "frost" => DamageType.Frost,
                        "lightning" => DamageType.Lightning,
                        "poison" => DamageType.Poison,
                        "spirit" => DamageType.Spirit,
                        "true" => DamageType.True,
                        _ => DamageType.Physical
                    };

                    float finalDamage = PrimeAPI.DealDamage(null, player, type, amount, false);
                    return CommandResult.Success($"Dealt {finalDamage:F1} {type} damage (input: {amount})");
                }
            });

            // munin prime heal <amount> - Heal self
            Command.Register(MOD_NAME, new CommandConfig
            {
                Name = "heal",
                Description = "Heal yourself",
                Usage = "<amount>",
                Examples = new[] { "50", "100" },
                Permission = PermissionLevel.Admin,
                Handler = (args) =>
                {
                    var player = args.Player ?? Player.m_localPlayer;
                    if (player == null)
                        return CommandResult.Error("No player found");

                    var amount = args.Get<float>(0, 0f);
                    if (amount <= 0)
                        return CommandResult.Error("Usage: munin prime heal <amount>");

                    player.Heal(amount, true);
                    return CommandResult.Success($"Healed for {amount}");
                }
            });

            // munin prime buff <stat> <percent> [duration] - Quick buff
            Command.Register(MOD_NAME, new CommandConfig
            {
                Name = "buff",
                Description = "Apply a quick percentage buff to a stat",
                Usage = "<stat> <percent> [duration]",
                Examples = new[] { "Strength 50 60", "MoveSpeed 100 30", "CritChance 25" },
                Permission = PermissionLevel.Admin,
                Handler = (args) =>
                {
                    var player = args.Player ?? Player.m_localPlayer;
                    if (player == null)
                        return CommandResult.Error("No player found");

                    var statId = args.Get(0);
                    var percent = args.Get<float>(1, 0f);
                    var duration = args.Get<float>(2, 60f);

                    if (string.IsNullOrEmpty(statId) || percent == 0)
                        return CommandResult.Error("Usage: munin prime buff <stat> <percent> [duration]");

                    if (!StatRegistry.Instance.IsRegistered(statId))
                        return CommandResult.NotFound($"Unknown stat: {statId}");

                    PrimeAPI.ApplyTimedPercent(player, statId, percent, duration, $"buff_{statId}");
                    return CommandResult.Success($"Applied +{percent}% {statId} for {duration}s");
                }
            });

            // munin prime reset - Reset player to base stats
            Command.Register(MOD_NAME, new CommandConfig
            {
                Name = "reset",
                Description = "Reset all stats and clear all modifiers/effects",
                Permission = PermissionLevel.Admin,
                Handler = (args) =>
                {
                    var player = args.Player ?? Player.m_localPlayer;
                    if (player == null)
                        return CommandResult.Error("No player found");

                    PrimeAPI.ClearAllModifiers(player);
                    EffectManager.RemoveAllEffects(player);

                    // Revoke all abilities
                    var granted = PrimeAPI.GetGrantedAbilities(player).ToList();
                    foreach (var instance in granted)
                    {
                        PrimeAPI.RevokeAbility(player, instance.Definition.Id);
                    }

                    return CommandResult.Success("Reset all stats, cleared modifiers, effects, and abilities");
                }
            });
        }

        #endregion

        #region Helpers

        private static string FormatStatValue(StatDefinition stat, float value)
        {
            string format = stat.DecimalPlaces > 0 ? $"F{stat.DecimalPlaces}" : "F0";

            return stat.DisplayType switch
            {
                StatDisplayType.Percent => $"{(value * 100).ToString(format)}%",
                StatDisplayType.Multiplier => $"x{value.ToString(format)}",
                StatDisplayType.Seconds => $"{value.ToString(format)}s",
                StatDisplayType.Boolean => value > 0.5f ? "Yes" : "No",
                _ => value.ToString(format)
            };
        }

        #endregion
    }
}
