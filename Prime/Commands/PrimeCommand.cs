using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Jotunn.Entities;
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
    /// Console commands for Prime stat system debugging and testing.
    /// </summary>
    public class PrimeCommand : ConsoleCommand
    {
        public override string Name => "prime";

        public override string Help => @"Prime combat and stats system commands.

Usage:
  prime stats                 - Show all stats for local player
  prime get <stat>            - Get a specific stat value
  prime set <stat> <value>    - Set base value of a stat
  prime mod add <stat> <type> <value> [duration] - Add a modifier
  prime mod remove <id>       - Remove a modifier by ID
  prime mod list              - List all modifiers
  prime mod clear             - Clear all modifiers
  prime breakdown <stat>      - Show detailed stat breakdown
  prime registered            - List all registered stat definitions

  prime ability list          - List all registered abilities
  prime ability grant <id>    - Grant an ability to yourself
  prime ability revoke <id>   - Revoke an ability
  prime ability use <id>      - Use an ability
  prime ability test          - Register test abilities (Fireball, Heal)

  prime effect list           - List active effects on yourself
  prime effect apply <id>     - Apply a test effect
  prime effect remove <id>    - Remove an effect
  prime effect test           - Register test effects

  prime damage <amount> [type] - Deal damage to yourself (for testing)

Modifier types: flat, percent, multiply
Damage types: physical, fire, frost, lightning, poison

Examples:
  prime stats
  prime set Strength 25
  prime mod add Strength percent 50 60
  prime ability test
  prime ability grant Fireball
  prime ability use Fireball";

        public override void Run(string[] args)
        {
            if (args.Length == 0)
            {
                Console.instance.Print(Help);
                return;
            }

            var player = Player.m_localPlayer;
            if (player == null)
            {
                Console.instance.Print("No local player found");
                return;
            }

            string subCommand = args[0].ToLowerInvariant();

            switch (subCommand)
            {
                case "stats":
                    ShowStats(player);
                    break;

                case "get":
                    if (args.Length < 2)
                    {
                        Console.instance.Print("Usage: prime get <stat>");
                        return;
                    }
                    GetStat(player, args[1]);
                    break;

                case "set":
                    if (args.Length < 3 || !float.TryParse(args[2], out float setValue))
                    {
                        Console.instance.Print("Usage: prime set <stat> <value>");
                        return;
                    }
                    SetStat(player, args[1], setValue);
                    break;

                case "mod":
                    if (args.Length < 2)
                    {
                        Console.instance.Print("Usage: prime mod <add|remove|list|clear> ...");
                        return;
                    }
                    HandleModifier(player, args.Skip(1).ToArray());
                    break;

                case "breakdown":
                    if (args.Length < 2)
                    {
                        Console.instance.Print("Usage: prime breakdown <stat>");
                        return;
                    }
                    ShowBreakdown(player, args[1]);
                    break;

                case "registered":
                    ShowRegisteredStats();
                    break;

                case "ability":
                    if (args.Length < 2)
                    {
                        Console.instance.Print("Usage: prime ability <list|grant|revoke|use|test> ...");
                        return;
                    }
                    HandleAbility(player, args.Skip(1).ToArray());
                    break;

                case "effect":
                    if (args.Length < 2)
                    {
                        Console.instance.Print("Usage: prime effect <list|apply|remove|test> ...");
                        return;
                    }
                    HandleEffect(player, args.Skip(1).ToArray());
                    break;

                case "damage":
                    if (args.Length < 2 || !float.TryParse(args[1], out float dmgAmount))
                    {
                        Console.instance.Print("Usage: prime damage <amount> [type]");
                        return;
                    }
                    DealTestDamage(player, dmgAmount, args.Length > 2 ? args[2] : "physical");
                    break;

                default:
                    Console.instance.Print($"Unknown subcommand: {subCommand}");
                    Console.instance.Print("Use 'prime' for help");
                    break;
            }
        }

        private void ShowStats(Player player)
        {
            var sb = new StringBuilder();
            sb.AppendLine("=== Prime Stats ===");

            foreach (var category in Enum.GetValues(typeof(StatCategory)).Cast<StatCategory>())
            {
                var stats = StatRegistry.Instance.GetByCategory(category).ToList();
                if (stats.Count == 0) continue;

                sb.AppendLine($"\n[{category}]");
                foreach (var stat in stats)
                {
                    float value = PrimeAPI.Get(player, stat.Id);
                    float baseValue = PrimeAPI.GetBase(player, stat.Id);

                    string valueStr = FormatStatValue(stat, value);
                    string baseStr = Math.Abs(value - baseValue) > 0.001f ? $" (base: {FormatStatValue(stat, baseValue)})" : "";

                    sb.AppendLine($"  {stat.Id}: {valueStr}{baseStr}");
                }
            }

            Console.instance.Print(sb.ToString());
        }

        private void GetStat(Player player, string statId)
        {
            var stat = StatRegistry.Instance.Get(statId);
            if (stat == null)
            {
                Console.instance.Print($"Unknown stat: {statId}");
                return;
            }

            float value = PrimeAPI.Get(player, statId);
            float baseValue = PrimeAPI.GetBase(player, statId);

            Console.instance.Print($"{statId}: {FormatStatValue(stat, value)} (base: {FormatStatValue(stat, baseValue)})");
        }

        private void SetStat(Player player, string statId, float value)
        {
            if (!StatRegistry.Instance.IsRegistered(statId))
            {
                Console.instance.Print($"Unknown stat: {statId}");
                return;
            }

            PrimeAPI.SetBase(player, statId, value);
            Console.instance.Print($"Set {statId} base to {value}");
        }

        private void HandleModifier(Player player, string[] args)
        {
            if (args.Length == 0) return;

            string action = args[0].ToLowerInvariant();

            switch (action)
            {
                case "add":
                    if (args.Length < 4)
                    {
                        Console.instance.Print("Usage: prime mod add <stat> <type> <value> [duration]");
                        return;
                    }
                    AddModifier(player, args[1], args[2], args[3], args.Length > 4 ? args[4] : null);
                    break;

                case "remove":
                    if (args.Length < 2)
                    {
                        Console.instance.Print("Usage: prime mod remove <id>");
                        return;
                    }
                    RemoveModifier(player, args[1]);
                    break;

                case "list":
                    ListModifiers(player);
                    break;

                case "clear":
                    PrimeAPI.ClearAllModifiers(player);
                    Console.instance.Print("Cleared all modifiers");
                    break;

                default:
                    Console.instance.Print($"Unknown modifier action: {action}");
                    break;
            }
        }

        private void AddModifier(Player player, string statId, string typeStr, string valueStr, string durationStr)
        {
            if (!StatRegistry.Instance.IsRegistered(statId))
            {
                Console.instance.Print($"Unknown stat: {statId}");
                return;
            }

            if (!float.TryParse(valueStr, out float value))
            {
                Console.instance.Print($"Invalid value: {valueStr}");
                return;
            }

            ModifierType type;
            switch (typeStr.ToLowerInvariant())
            {
                case "flat": type = ModifierType.Flat; break;
                case "percent": type = ModifierType.Percent; break;
                case "multiply": type = ModifierType.Multiply; break;
                default:
                    Console.instance.Print($"Unknown modifier type: {typeStr}. Use: flat, percent, multiply");
                    return;
            }

            float? duration = null;
            if (!string.IsNullOrEmpty(durationStr) && float.TryParse(durationStr, out float d))
            {
                duration = d;
            }

            string id = $"console_{statId}_{Guid.NewGuid():N}";
            var modifier = new Modifier(id, statId, type, value)
            {
                Duration = duration,
                Source = "Console"
            };

            PrimeAPI.AddModifier(player, modifier);

            string durationInfo = duration.HasValue ? $" for {duration}s" : " (permanent)";
            Console.instance.Print($"Added {type} {value} to {statId}{durationInfo}");
            Console.instance.Print($"Modifier ID: {id}");
        }

        private void RemoveModifier(Player player, string modifierId)
        {
            if (PrimeAPI.RemoveModifier(player, modifierId))
            {
                Console.instance.Print($"Removed modifier: {modifierId}");
            }
            else
            {
                Console.instance.Print($"Modifier not found: {modifierId}");
            }
        }

        private void ListModifiers(Player player)
        {
            var modifiers = PrimeAPI.GetAllModifiers(player).ToList();

            if (modifiers.Count == 0)
            {
                Console.instance.Print("No active modifiers");
                return;
            }

            var sb = new StringBuilder();
            sb.AppendLine($"=== Active Modifiers ({modifiers.Count}) ===");

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
                    var remaining = mod.GetRemainingDuration(UnityEngine.Time.time);
                    durationStr = $" ({remaining:F1}s left)";
                }

                string stackStr = mod.Stacks > 1 ? $" x{mod.Stacks}" : "";

                sb.AppendLine($"  [{mod.StatId}] {valueStr}{stackStr}{durationStr}");
                sb.AppendLine($"    ID: {mod.Id}");
            }

            Console.instance.Print(sb.ToString());
        }

        private void ShowBreakdown(Player player, string statId)
        {
            if (!StatRegistry.Instance.IsRegistered(statId))
            {
                Console.instance.Print($"Unknown stat: {statId}");
                return;
            }

            var breakdown = PrimeAPI.GetBreakdown(player, statId);
            Console.instance.Print(breakdown.ToString());
        }

        private void ShowRegisteredStats()
        {
            var stats = StatRegistry.Instance.GetAll().OrderBy(s => s.Category).ThenBy(s => s.Id);

            var sb = new StringBuilder();
            sb.AppendLine($"=== Registered Stats ({StatRegistry.Instance.Count}) ===");

            StatCategory? lastCategory = null;
            foreach (var stat in stats)
            {
                if (stat.Category != lastCategory)
                {
                    sb.AppendLine($"\n[{stat.Category}]");
                    lastCategory = stat.Category;
                }

                string bounds = "";
                if (stat.MinValue.HasValue || stat.MaxValue.HasValue)
                {
                    bounds = $" [{stat.MinValue?.ToString() ?? "-∞"} to {stat.MaxValue?.ToString() ?? "∞"}]";
                }

                sb.AppendLine($"  {stat.Id}: base={stat.BaseValue}{bounds}");
            }

            Console.instance.Print(sb.ToString());
        }

        private string FormatStatValue(StatDefinition stat, float value)
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

        // ==================== ABILITY COMMANDS ====================

        private void HandleAbility(Player player, string[] args)
        {
            string action = args[0].ToLowerInvariant();

            switch (action)
            {
                case "list":
                    ListAbilities();
                    break;

                case "grant":
                    if (args.Length < 2)
                    {
                        Console.instance.Print("Usage: prime ability grant <id>");
                        return;
                    }
                    GrantAbility(player, args[1]);
                    break;

                case "revoke":
                    if (args.Length < 2)
                    {
                        Console.instance.Print("Usage: prime ability revoke <id>");
                        return;
                    }
                    RevokeAbility(player, args[1]);
                    break;

                case "use":
                    if (args.Length < 2)
                    {
                        Console.instance.Print("Usage: prime ability use <id>");
                        return;
                    }
                    UseAbility(player, args[1]);
                    break;

                case "test":
                    RegisterTestAbilities();
                    break;

                case "granted":
                    ListGrantedAbilities(player);
                    break;

                default:
                    Console.instance.Print($"Unknown ability action: {action}");
                    break;
            }
        }

        private void ListAbilities()
        {
            var abilities = AbilityRegistry.Instance.GetAll().ToList();

            if (abilities.Count == 0)
            {
                Console.instance.Print("No abilities registered. Use 'prime ability test' to register test abilities.");
                return;
            }

            var sb = new StringBuilder();
            sb.AppendLine($"=== Registered Abilities ({abilities.Count}) ===");

            foreach (var ability in abilities.OrderBy(a => a.Category).ThenBy(a => a.Id))
            {
                string dmg = ability.BaseDamage > 0 ? $" {ability.BaseDamage} {ability.DamageType}" : "";
                string cd = ability.BaseCooldown > 0 ? $" ({ability.BaseCooldown}s CD)" : "";
                sb.AppendLine($"  [{ability.Category}] {ability.Id}{dmg}{cd}");
            }

            Console.instance.Print(sb.ToString());
        }

        private void ListGrantedAbilities(Player player)
        {
            var abilities = PrimeAPI.GetGrantedAbilities(player).ToList();

            if (abilities.Count == 0)
            {
                Console.instance.Print("No abilities granted. Use 'prime ability grant <id>' to grant abilities.");
                return;
            }

            var sb = new StringBuilder();
            sb.AppendLine($"=== Granted Abilities ({abilities.Count}) ===");

            foreach (var instance in abilities)
            {
                string state = instance.State.ToString();
                string cd = instance.State == AbilityState.OnCooldown
                    ? $" ({instance.GetRemainingCooldown():F1}s)"
                    : "";
                sb.AppendLine($"  {instance.Definition.Id}: {state}{cd}");
            }

            Console.instance.Print(sb.ToString());
        }

        private void GrantAbility(Player player, string abilityId)
        {
            if (!AbilityRegistry.Instance.IsRegistered(abilityId))
            {
                Console.instance.Print($"Unknown ability: {abilityId}");
                Console.instance.Print("Use 'prime ability list' to see available abilities.");
                return;
            }

            if (PrimeAPI.GrantAbility(player, abilityId))
            {
                Console.instance.Print($"Granted ability: {abilityId}");
            }
            else
            {
                Console.instance.Print($"Already have ability: {abilityId}");
            }
        }

        private void RevokeAbility(Player player, string abilityId)
        {
            if (PrimeAPI.RevokeAbility(player, abilityId))
            {
                Console.instance.Print($"Revoked ability: {abilityId}");
            }
            else
            {
                Console.instance.Print($"Don't have ability: {abilityId}");
            }
        }

        private void UseAbility(Player player, string abilityId)
        {
            if (!PrimeAPI.HasAbility(player, abilityId))
            {
                Console.instance.Print($"Don't have ability: {abilityId}. Use 'prime ability grant {abilityId}' first.");
                return;
            }

            if (PrimeAPI.UseAbility(player, abilityId))
            {
                Console.instance.Print($"Used ability: {abilityId}");
            }
            else
            {
                Console.instance.Print($"Cannot use ability: {abilityId} (on cooldown or missing resources)");
            }
        }

        private void RegisterTestAbilities()
        {
            // Fireball - damage ability
            var fireball = new AbilityDefinition("Fireball")
            {
                DisplayName = "Fireball",
                Description = "Hurls a ball of fire dealing fire damage.",
                BaseCooldown = 3f,
                BaseDamage = 50f,
                DamageType = DamageType.Fire,
                ScalingStat = "Intelligence",
                ScalingFactor = 2f,
                Cost = new ResourceCost("Eitr", 15f),
                TargetType = AbilityTargetType.Projectile,
                Category = AbilityCategory.Active
            };
            fireball.Tags.Add("fire");
            fireball.Tags.Add("damage");
            PrimeAPI.RegisterAbility(fireball);

            // Heal - self buff
            var heal = new AbilityDefinition("Heal")
            {
                DisplayName = "Heal",
                Description = "Restores health over time.",
                BaseCooldown = 10f,
                TargetType = AbilityTargetType.Self,
                Category = AbilityCategory.Active,
                Cost = new ResourceCost("Eitr", 25f)
            };
            heal.SelfEffects.Add(new AbilityEffect("MaxHealth", ModifierType.Flat, 50f, 10f));
            heal.Tags.Add("heal");
            PrimeAPI.RegisterAbility(heal);

            // WarCry - buff ability
            var warcry = new AbilityDefinition("WarCry")
            {
                DisplayName = "War Cry",
                Description = "Increases Strength and Armor for 10 seconds.",
                BaseCooldown = 30f,
                TargetType = AbilityTargetType.Self,
                Category = AbilityCategory.Active,
                Cost = new ResourceCost("Stamina", 30f)
            };
            warcry.SelfEffects.Add(new AbilityEffect("Strength", ModifierType.Percent, 25f, 10f));
            warcry.SelfEffects.Add(new AbilityEffect("Armor", ModifierType.Percent, 20f, 10f));
            warcry.Tags.Add("buff");
            PrimeAPI.RegisterAbility(warcry);

            Console.instance.Print("Registered test abilities: Fireball, Heal, WarCry");
            Console.instance.Print("Use 'prime ability grant <id>' to get them.");
        }

        // ==================== EFFECT COMMANDS ====================

        private void HandleEffect(Player player, string[] args)
        {
            string action = args[0].ToLowerInvariant();

            switch (action)
            {
                case "list":
                    ListEffects(player);
                    break;

                case "apply":
                    if (args.Length < 2)
                    {
                        Console.instance.Print("Usage: prime effect apply <id>");
                        return;
                    }
                    ApplyTestEffect(player, args[1]);
                    break;

                case "remove":
                    if (args.Length < 2)
                    {
                        Console.instance.Print("Usage: prime effect remove <id>");
                        return;
                    }
                    RemoveTestEffect(player, args[1]);
                    break;

                case "test":
                    RegisterTestEffects(player);
                    break;

                case "clear":
                    EffectManager.RemoveAllEffects(player);
                    Console.instance.Print("Cleared all effects");
                    break;

                default:
                    Console.instance.Print($"Unknown effect action: {action}");
                    break;
            }
        }

        private void ListEffects(Player player)
        {
            var effects = PrimeAPI.GetEffects(player).ToList();

            if (effects.Count == 0)
            {
                Console.instance.Print("No active effects.");
                return;
            }

            var sb = new StringBuilder();
            sb.AppendLine($"=== Active Effects ({effects.Count}) ===");

            foreach (var effect in effects)
            {
                string duration = effect.Definition.Duration > 0
                    ? $" ({effect.GetRemainingDuration():F1}s left)"
                    : " (permanent)";
                string stacks = effect.Stacks > 1 ? $" x{effect.Stacks}" : "";
                sb.AppendLine($"  {effect.Definition.Id}{stacks}{duration}");
            }

            Console.instance.Print(sb.ToString());
        }

        private void ApplyTestEffect(Player player, string effectId)
        {
            // Create simple effect based on ID
            var effect = new EffectDefinition(effectId)
            {
                Duration = 30f,
                IsBuff = true
            };

            // Add a simple stat boost based on effect name
            if (effectId.ToLowerInvariant().Contains("str"))
            {
                effect.OnApply = (owner) => PrimeAPI.ApplyTimedPercent(owner, "Strength", 25f, 30f, effectId);
            }
            else if (effectId.ToLowerInvariant().Contains("speed"))
            {
                effect.OnApply = (owner) => PrimeAPI.ApplyTimedPercent(owner, "MoveSpeed", 30f, 30f, effectId);
            }

            var instance = PrimeAPI.ApplyEffect(player, effect);
            if (instance != null)
            {
                Console.instance.Print($"Applied effect: {effectId}");
            }
            else
            {
                Console.instance.Print($"Failed to apply effect: {effectId}");
            }
        }

        private void RemoveTestEffect(Player player, string effectId)
        {
            if (PrimeAPI.RemoveEffect(player, effectId))
            {
                Console.instance.Print($"Removed effect: {effectId}");
            }
            else
            {
                Console.instance.Print($"Effect not found: {effectId}");
            }
        }

        private void RegisterTestEffects(Player player)
        {
            // Burning - DoT effect
            var burning = new EffectDefinition("Burning")
            {
                Duration = 6f,
                TickInterval = 1f,
                IsBuff = false,
                OnTick = (owner) =>
                {
                    Console.instance.Print($"[Burning] Tick! (would deal 5 fire damage)");
                    // In real use: PrimeAPI.DealDamage(null, owner, DamageType.Fire, 5f, false);
                }
            };

            // Thorns - on damage taken proc
            var thorns = new EffectDefinition("Thorns")
            {
                Duration = 60f,
                Trigger = EffectTrigger.OnDamageTaken,
                ProcChance = 1f,
                OnProc = (owner, attacker, damageInfo) =>
                {
                    if (attacker != null)
                    {
                        Console.instance.Print($"[Thorns] Reflected damage to attacker!");
                        // In real use: PrimeAPI.DealDamage(owner, attacker, DamageType.Physical, 10f, false);
                    }
                }
            };

            // Lifesteal - on hit proc
            var lifesteal = new EffectDefinition("Lifesteal")
            {
                Duration = 30f,
                Trigger = EffectTrigger.OnHit,
                ProcChance = 0.25f,
                OnProc = (owner, target, damageInfo) =>
                {
                    Console.instance.Print($"[Lifesteal] Healed for {damageInfo?.FinalDamage * 0.1f:F1}!");
                    // In real use: heal the owner
                }
            };

            PrimeAPI.ApplyEffect(player, burning);
            PrimeAPI.ApplyEffect(player, thorns);
            PrimeAPI.ApplyEffect(player, lifesteal);

            Console.instance.Print("Applied test effects: Burning (DoT), Thorns (on damage taken), Lifesteal (on hit)");
        }

        // ==================== DAMAGE COMMAND ====================

        private void DealTestDamage(Player player, float amount, string typeStr)
        {
            DamageType type = typeStr.ToLowerInvariant() switch
            {
                "fire" => DamageType.Fire,
                "frost" => DamageType.Frost,
                "lightning" => DamageType.Lightning,
                "poison" => DamageType.Poison,
                "true" => DamageType.True,
                _ => DamageType.Physical
            };

            float finalDamage = PrimeAPI.DealDamage(null, player, type, amount, false);
            Console.instance.Print($"Dealt {finalDamage:F1} {type} damage to yourself (input: {amount})");
        }
    }
}
