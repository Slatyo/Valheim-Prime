using System.Collections.Generic;
using HarmonyLib;
using Prime.Core;
using Prime.Modifiers;
using UnityEngine;

namespace Prime.Patches
{
    /// <summary>
    /// Harmony patches that apply Prime stats to Valheim's character systems.
    ///
    /// APPROACH: Use Postfix patches on getter methods to add Prime bonuses.
    /// This intercepts ALL vanilla checks (UI, clamping, regen, etc.) cleanly.
    /// No field manipulation = no compounding issues.
    /// </summary>
    [HarmonyPatch]
    public static class StatPatches
    {
        // === MAX RESOURCE PATCHES ===
        // Postfix on Get methods to add Prime bonuses to the return value.
        // This is the cleanest approach - vanilla uses these methods everywhere.

        /// <summary>
        /// Apply MaxHealth modifier to Character.GetMaxHealth().
        /// Works for both players and creatures.
        /// </summary>
        [HarmonyPatch(typeof(Character), nameof(Character.GetMaxHealth))]
        [HarmonyPostfix]
        public static void Character_GetMaxHealth_Postfix(Character __instance, ref float __result)
        {
            var container = EntityManager.Instance.Get(__instance);
            if (container == null) return;

            float bonus = CalculateBonus(container, "MaxHealth", __result);
            if (bonus != __result)
            {
                __result = bonus;
            }
        }

        /// <summary>
        /// Apply MaxStamina modifier to Player.GetMaxStamina().
        /// </summary>
        [HarmonyPatch(typeof(Player), nameof(Player.GetMaxStamina))]
        [HarmonyPostfix]
        public static void Player_GetMaxStamina_Postfix(Player __instance, ref float __result)
        {
            var container = EntityManager.Instance.Get(__instance);
            if (container == null) return;

            float bonus = CalculateBonus(container, "MaxStamina", __result);
            if (bonus != __result)
            {
                __result = bonus;
            }
        }

        /// <summary>
        /// Apply MaxEitr modifier to Player.GetMaxEitr().
        /// </summary>
        [HarmonyPatch(typeof(Player), nameof(Player.GetMaxEitr))]
        [HarmonyPostfix]
        public static void Player_GetMaxEitr_Postfix(Player __instance, ref float __result)
        {
            var container = EntityManager.Instance.Get(__instance);
            if (container == null) return;

            float bonus = CalculateBonus(container, "MaxEitr", __result);
            if (bonus != __result)
            {
                __result = bonus;
            }
        }

        /// <summary>
        /// Calculate final value with Prime modifiers applied.
        /// Formula: (baseValue + flat) * (1 + percent/100) * multiply
        /// </summary>
        private static float CalculateBonus(Stats.StatContainer container, string statId, float baseValue)
        {
            var modifiers = container.GetModifiers(statId);
            if (modifiers == null || modifiers.Count == 0) return baseValue;

            float flatBonus = 0f;
            float percentBonus = 1f;
            float multiplyBonus = 1f;

            foreach (var mod in modifiers)
            {
                switch (mod.Type)
                {
                    case ModifierType.Flat:
                        flatBonus += mod.Value;
                        break;
                    case ModifierType.Percent:
                        percentBonus += mod.Value / 100f;
                        break;
                    case ModifierType.Multiply:
                        multiplyBonus *= mod.Value;
                        break;
                }
            }

            return (baseValue + flatBonus) * percentBonus * multiplyBonus;
        }

        // === CLAMPING PREVENTION ===
        // Vanilla may pass pre-clamped values to SetHealth. We need to allow
        // health to reach Prime's max, not vanilla's max.

        /// <summary>
        /// Ensure SetHealth respects Prime's max, not vanilla's internal max.
        /// Also prevents vanilla food system from clamping down when at full health.
        /// </summary>
        [HarmonyPatch(typeof(Character), nameof(Character.SetHealth))]
        [HarmonyPrefix]
        public static void Character_SetHealth_Prefix(Character __instance, ref float health)
        {
            if (__instance == null) return;
            if (__instance is not Player player) return;

            var container = EntityManager.Instance.Get(__instance);
            if (container == null) return;

            // Check if we have any MaxHealth modifiers
            var modifiers = container.GetModifiers("MaxHealth");
            if (modifiers == null || modifiers.Count == 0) return;

            bool hasBonus = false;
            foreach (var mod in modifiers)
            {
                if (mod.Value != 0)
                {
                    hasBonus = true;
                    break;
                }
            }
            if (!hasBonus) return;

            // Get Prime's max (includes our bonuses via the patched GetMaxHealth)
            float primeMax = __instance.GetMaxHealth();
            float currentHealth = __instance.GetHealth();

            // Calculate vanilla's max by reversing our bonus calculation
            float vanillaMax = GetVanillaMax(container, "MaxHealth", primeMax);

            // Detect food clamping: vanilla is trying to set health to its max
            // while we're above vanilla's max. This happens when food tries to
            // "update" health to match what it thinks is max.
            bool isFoodClamping = Mathf.Abs(health - vanillaMax) < 1f && currentHealth > vanillaMax;

            if (isFoodClamping)
            {
                // Don't let food system drag us down - keep current health
                health = currentHealth;
            }

            // Always enforce Prime's max as the ceiling
            health = Mathf.Min(health, primeMax);
        }

        /// <summary>
        /// Calculate what vanilla's max would be without Prime bonuses.
        /// Used to detect when vanilla is trying to clamp to its internal max.
        /// </summary>
        private static float GetVanillaMax(Stats.StatContainer container, string statId, float primeMax)
        {
            var modifiers = container.GetModifiers(statId);
            if (modifiers == null || modifiers.Count == 0) return primeMax;

            float flatBonus = 0f;
            float percentBonus = 1f;
            float multiplyBonus = 1f;

            foreach (var mod in modifiers)
            {
                switch (mod.Type)
                {
                    case ModifierType.Flat:
                        flatBonus += mod.Value;
                        break;
                    case ModifierType.Percent:
                        percentBonus += mod.Value / 100f;
                        break;
                    case ModifierType.Multiply:
                        multiplyBonus *= mod.Value;
                        break;
                }
            }

            // Reverse: primeMax = (vanillaMax + flat) * percent * multiply
            // vanillaMax = (primeMax / multiply / percent) - flat
            if (multiplyBonus == 0 || percentBonus == 0) return primeMax;
            return (primeMax / multiplyBonus / percentBonus) - flatBonus;
        }

        // === STAMINA CLAMPING FIX ===
        // Vanilla clamps stamina internally. We need to ensure the clamp
        // uses Prime's max, not vanilla's internal m_maxStamina field.

        /// <summary>
        /// After UpdateStats runs, ensure stamina respects Prime's max.
        /// Vanilla clamps m_stamina to m_maxStamina internally.
        /// </summary>
        [HarmonyPatch(typeof(Player), nameof(Player.UpdateStats), new[] { typeof(float) })]
        [HarmonyPostfix]
        public static void Player_UpdateStats_Postfix(Player __instance)
        {
            var container = EntityManager.Instance.Get(__instance);
            if (container == null) return;

            // Fix stamina clamping - vanilla uses m_maxStamina field for clamping
            // but GetMaxStamina() returns our enhanced value
            float primeMaxStamina = __instance.GetMaxStamina();
            float currentStamina = __instance.GetStamina();

            // If stamina was clamped down by vanilla, restore it
            // (only if we have stamina bonuses)
            var staminaMods = container.GetModifiers("MaxStamina");
            if (staminaMods != null && staminaMods.Count > 0)
            {
                float vanillaMaxStamina = GetVanillaMax(container, "MaxStamina", primeMaxStamina);

                // If current equals vanilla max but should be higher, player was at full
                if (Mathf.Abs(currentStamina - vanillaMaxStamina) < 1f && primeMaxStamina > vanillaMaxStamina)
                {
                    // Player was at "full" vanilla stamina - set to Prime full
                    __instance.m_stamina = primeMaxStamina;
                }
            }

            // Same for eitr
            float primeMaxEitr = __instance.GetMaxEitr();
            if (primeMaxEitr > 0)
            {
                var eitrMods = container.GetModifiers("MaxEitr");
                if (eitrMods != null && eitrMods.Count > 0)
                {
                    float currentEitr = __instance.GetEitr();
                    float vanillaMaxEitr = GetVanillaMax(container, "MaxEitr", primeMaxEitr);

                    if (Mathf.Abs(currentEitr - vanillaMaxEitr) < 1f && primeMaxEitr > vanillaMaxEitr)
                    {
                        __instance.m_eitr = primeMaxEitr;
                    }
                }
            }
        }

        // === REGEN MULTIPLIER PATCHES ===
        // These multiply vanilla's regen rates by Prime's regen stats.

        // Cached vanilla regen values (set once per player)
        private static readonly Dictionary<long, float> _baseStaminaRegen = new();
        private static readonly Dictionary<long, float> _baseEitrRegen = new();

        /// <summary>
        /// Store base regen values when player awakens.
        /// </summary>
        [HarmonyPatch(typeof(Player), nameof(Player.Awake))]
        [HarmonyPostfix]
        public static void Player_Awake_Postfix(Player __instance)
        {
            long playerId = __instance.GetPlayerID();
            _baseStaminaRegen[playerId] = __instance.m_staminaRegen;
            _baseEitrRegen[playerId] = __instance.m_eiterRegen;
        }

        /// <summary>
        /// Apply HealthRegen multiplier to healing.
        /// </summary>
        [HarmonyPatch(typeof(Character), nameof(Character.Heal))]
        [HarmonyPrefix]
        public static void Character_Heal_Prefix(Character __instance, ref float hp, bool showText)
        {
            if (__instance is not Player) return;

            var container = EntityManager.Instance.Get(__instance);
            if (container == null) return;

            float regenMult = GetRegenMultiplier(container, "HealthRegen");
            if (regenMult != 1f)
            {
                hp *= regenMult;
            }
        }

        /// <summary>
        /// Apply StaminaRegen and EitrRegen multipliers.
        /// </summary>
        [HarmonyPatch(typeof(Player), nameof(Player.UpdateStats), new[] { typeof(float) })]
        [HarmonyPrefix]
        public static void Player_UpdateStats_Prefix(Player __instance)
        {
            var container = EntityManager.Instance.Get(__instance);
            if (container == null) return;

            long playerId = __instance.GetPlayerID();

            // Apply StaminaRegen multiplier
            if (_baseStaminaRegen.TryGetValue(playerId, out float baseStamRegen))
            {
                float staminaRegenMult = GetRegenMultiplier(container, "StaminaRegen");
                __instance.m_staminaRegen = baseStamRegen * staminaRegenMult;
            }

            // Apply EitrRegen multiplier
            if (_baseEitrRegen.TryGetValue(playerId, out float baseEitrRegen))
            {
                float eitrRegenMult = GetRegenMultiplier(container, "EitrRegen");
                __instance.m_eiterRegen = baseEitrRegen * eitrRegenMult;
            }
        }

        /// <summary>
        /// Get regen multiplier from Prime stat.
        /// Base is 1.0. Flat adds, Percent multiplies.
        /// </summary>
        private static float GetRegenMultiplier(Stats.StatContainer container, string statId)
        {
            var modifiers = container.GetModifiers(statId);
            float flatBonus = 0f;
            float percentBonus = 1f;
            float multiplyBonus = 1f;

            foreach (var mod in modifiers)
            {
                switch (mod.Type)
                {
                    case ModifierType.Flat:
                        flatBonus += mod.Value;
                        break;
                    case ModifierType.Percent:
                        percentBonus += mod.Value / 100f;
                        break;
                    case ModifierType.Multiply:
                        multiplyBonus *= mod.Value;
                        break;
                }
            }

            return (1f + flatBonus) * percentBonus * multiplyBonus;
        }

        // === ARMOR MODIFIER ===

        /// <summary>
        /// Apply armor modifier.
        /// </summary>
        [HarmonyPatch(typeof(Player), nameof(Player.GetBodyArmor))]
        [HarmonyPostfix]
        public static void Player_GetBodyArmor_Postfix(Player __instance, ref float __result)
        {
            var container = EntityManager.Instance.Get(__instance);
            if (container == null) return;

            __result = CalculateBonus(container, "Armor", __result);
        }

        // === SPAWN HANDLING ===

        /// <summary>
        /// When player spawns, ensure they start at Prime's max health.
        /// </summary>
        [HarmonyPatch(typeof(Player), nameof(Player.OnSpawned))]
        [HarmonyPostfix]
        public static void Player_OnSpawned_Postfix(Player __instance)
        {
            if (__instance == null) return;

            // Set to Prime's max
            float maxHealth = __instance.GetMaxHealth();
            float currentHealth = __instance.GetHealth();

            if (currentHealth < maxHealth)
            {
                __instance.SetHealth(maxHealth);
                Plugin.Log?.LogDebug($"[Prime] Set player health to Prime max: {maxHealth}");
            }

            float maxStamina = __instance.GetMaxStamina();
            float currentStamina = __instance.GetStamina();

            if (currentStamina < maxStamina)
            {
                __instance.m_stamina = maxStamina;
            }
        }
    }
}
