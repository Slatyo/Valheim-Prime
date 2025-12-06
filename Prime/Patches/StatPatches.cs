using System.Reflection;
using HarmonyLib;
using Prime.Core;
using UnityEngine;

namespace Prime.Patches
{
    /// <summary>
    /// Harmony patches that apply Prime stats to Valheim's character systems.
    /// </summary>
    [HarmonyPatch]
    public static class StatPatches
    {
        // Track the Prime bonus for health to prevent vanilla from resetting it
        private static float _lastPrimeHealthBonus = 0f;

        // Cached reflection field for direct health manipulation
        private static FieldInfo _healthField;
        private static FieldInfo _staminaField;

        /// <summary>
        /// Apply MaxHealth modifier to character health.
        /// Prime modifiers ADD to the vanilla value.
        /// </summary>
        [HarmonyPatch(typeof(Character), nameof(Character.GetMaxHealth))]
        [HarmonyPostfix]
        public static void Character_GetMaxHealth_Postfix(Character __instance, ref float __result)
        {
            var container = EntityManager.Instance.Get(__instance);
            if (container == null) return;

            // Get any flat bonuses from Prime modifiers
            float flatBonus = 0f;
            float percentBonus = 1f;

            var modifiers = container.GetModifiers("MaxHealth");
            foreach (var mod in modifiers)
            {
                if (mod.Type == Modifiers.ModifierType.Flat)
                    flatBonus += mod.Value;
                else if (mod.Type == Modifiers.ModifierType.Percent)
                    percentBonus += mod.Value / 100f;
                else if (mod.Type == Modifiers.ModifierType.Multiply)
                    percentBonus *= mod.Value;
            }

            // Track the bonus for SetHealth patch
            if (__instance is Player)
            {
                _lastPrimeHealthBonus = flatBonus + (__result * (percentBonus - 1f));
            }

            // Apply bonuses on top of vanilla result
            __result = (__result + flatBonus) * percentBonus;
        }

        /// <summary>
        /// Prevent vanilla from resetting health below what Prime allows.
        /// Vanilla food system can try to clamp health to its calculated max,
        /// but we want to allow health up to our Prime-modified max.
        /// </summary>
        [HarmonyPatch(typeof(Character), nameof(Character.SetHealth))]
        [HarmonyPrefix]
        public static void Character_SetHealth_Prefix(Character __instance, ref float health)
        {
            if (__instance == null) return;

            // Get current and max health
            float currentHealth = __instance.GetHealth();
            float maxHealth = __instance.GetMaxHealth(); // This includes our Prime bonuses

            // If vanilla is trying to SET health lower than current, but we have Prime bonuses,
            // don't let it reduce below current health (unless taking damage)
            // This prevents the food system from resetting health to vanilla max
            var container = EntityManager.Instance.Get(__instance);
            if (container != null)
            {
                var modifiers = container.GetModifiers("MaxHealth");
                bool hasPrimeBonus = false;
                foreach (var mod in modifiers)
                {
                    if (mod.Value != 0)
                    {
                        hasPrimeBonus = true;
                        break;
                    }
                }

                if (hasPrimeBonus)
                {
                    // If trying to set health lower than current, keep current
                    // (damage is handled separately via Damage() method)
                    if (health < currentHealth && health > 0)
                    {
                        health = currentHealth;
                    }

                    // Clamp to our Prime max, not vanilla max
                    if (health > maxHealth)
                    {
                        health = maxHealth;
                    }
                }
            }
        }

        /// <summary>
        /// Apply MaxStamina modifier to player stamina.
        /// </summary>
        [HarmonyPatch(typeof(Player), nameof(Player.GetMaxStamina))]
        [HarmonyPostfix]
        public static void Player_GetMaxStamina_Postfix(Player __instance, ref float __result)
        {
            var container = EntityManager.Instance.Get(__instance);
            if (container == null) return;

            float flatBonus = 0f;
            float percentBonus = 1f;

            var modifiers = container.GetModifiers("MaxStamina");
            foreach (var mod in modifiers)
            {
                if (mod.Type == Modifiers.ModifierType.Flat)
                    flatBonus += mod.Value;
                else if (mod.Type == Modifiers.ModifierType.Percent)
                    percentBonus += mod.Value / 100f;
                else if (mod.Type == Modifiers.ModifierType.Multiply)
                    percentBonus *= mod.Value;
            }

            __result = (__result + flatBonus) * percentBonus;
        }

        /// <summary>
        /// Apply MaxEitr modifier to player eitr.
        /// </summary>
        [HarmonyPatch(typeof(Player), nameof(Player.GetMaxEitr))]
        [HarmonyPostfix]
        public static void Player_GetMaxEitr_Postfix(Player __instance, ref float __result)
        {
            var container = EntityManager.Instance.Get(__instance);
            if (container == null) return;

            float flatBonus = 0f;
            float percentBonus = 1f;

            var modifiers = container.GetModifiers("MaxEitr");
            foreach (var mod in modifiers)
            {
                if (mod.Type == Modifiers.ModifierType.Flat)
                    flatBonus += mod.Value;
                else if (mod.Type == Modifiers.ModifierType.Percent)
                    percentBonus += mod.Value / 100f;
                else if (mod.Type == Modifiers.ModifierType.Multiply)
                    percentBonus *= mod.Value;
            }

            __result = (__result + flatBonus) * percentBonus;
        }

        /// <summary>
        /// Apply armor modifier.
        /// </summary>
        [HarmonyPatch(typeof(Player), nameof(Player.GetBodyArmor))]
        [HarmonyPostfix]
        public static void Player_GetBodyArmor_Postfix(Player __instance, ref float __result)
        {
            var container = EntityManager.Instance.Get(__instance);
            if (container == null) return;

            float flatBonus = 0f;
            float percentBonus = 1f;

            var modifiers = container.GetModifiers("Armor");
            foreach (var mod in modifiers)
            {
                if (mod.Type == Modifiers.ModifierType.Flat)
                    flatBonus += mod.Value;
                else if (mod.Type == Modifiers.ModifierType.Percent)
                    percentBonus += mod.Value / 100f;
                else if (mod.Type == Modifiers.ModifierType.Multiply)
                    percentBonus *= mod.Value;
            }

            __result = (__result + flatBonus) * percentBonus;
        }

        /// <summary>
        /// Ensure health can reach our modified max by directly manipulating the field.
        /// This runs after Player.UpdateStats to undo vanilla's clamping.
        /// </summary>
        [HarmonyPatch(typeof(Player), nameof(Player.UpdateStats), new[] { typeof(float) })]
        [HarmonyPostfix]
        public static void Player_UpdateStats_Postfix(Player __instance)
        {
            var container = EntityManager.Instance.Get(__instance);
            if (container == null) return;

            // Cache reflection fields on first use
            if (_healthField == null)
            {
                _healthField = typeof(Character).GetField("m_health", BindingFlags.NonPublic | BindingFlags.Instance);
            }
            if (_staminaField == null)
            {
                _staminaField = typeof(Player).GetField("m_stamina", BindingFlags.NonPublic | BindingFlags.Instance);
            }

            // Get our modified max health
            float primeMaxHealth = __instance.GetMaxHealth();
            float currentHealth = __instance.GetHealth();

            // Check if we have any health modifiers
            var healthMods = container.GetModifiers("MaxHealth");
            float totalHealthBonus = 0f;
            foreach (var mod in healthMods)
            {
                if (mod.Value != 0)
                {
                    totalHealthBonus += mod.Value;
                }
            }

            // If we have Prime health bonuses and vanilla just clamped our health
            if (totalHealthBonus > 0 && _healthField != null)
            {
                // Calculate vanilla's base max (what it would be without Prime)
                // This is approximate - base health is 25 without food
                float vanillaBaseHealth = 25f;
                float foodBonus = primeMaxHealth - totalHealthBonus - vanillaBaseHealth;
                if (foodBonus < 0) foodBonus = 0;
                float vanillaMax = vanillaBaseHealth + foodBonus;

                // If vanilla clamped us to its max (or close to it),
                // we need to scale up proportionally
                if (currentHealth <= vanillaMax + 1f && currentHealth < primeMaxHealth)
                {
                    // Calculate what health SHOULD be based on vanilla's percentage
                    // If vanilla thinks we're at 100% (25/25), we should be at 100% (178/178)
                    float healthPercent = vanillaMax > 0 ? currentHealth / vanillaMax : 1f;
                    float targetHealth = primeMaxHealth * healthPercent;

                    // Only adjust upward (don't reduce health)
                    if (targetHealth > currentHealth)
                    {
                        _healthField.SetValue(__instance, targetHealth);
                    }
                }
            }

            // Same for stamina
            float primeMaxStamina = __instance.GetMaxStamina();
            float currentStamina = __instance.GetStamina();

            var staminaMods = container.GetModifiers("MaxStamina");
            float totalStaminaBonus = 0f;
            foreach (var mod in staminaMods)
            {
                if (mod.Value != 0)
                {
                    totalStaminaBonus += mod.Value;
                }
            }

            if (totalStaminaBonus > 0 && _staminaField != null)
            {
                float vanillaBaseStamina = 75f; // Base stamina without food
                float staminaFoodBonus = primeMaxStamina - totalStaminaBonus - vanillaBaseStamina;
                if (staminaFoodBonus < 0) staminaFoodBonus = 0;
                float vanillaMaxStamina = vanillaBaseStamina + staminaFoodBonus;

                if (currentStamina <= vanillaMaxStamina + 1f && currentStamina < primeMaxStamina)
                {
                    float staminaPercent = vanillaMaxStamina > 0 ? currentStamina / vanillaMaxStamina : 1f;
                    float targetStamina = primeMaxStamina * staminaPercent;

                    if (targetStamina > currentStamina)
                    {
                        _staminaField.SetValue(__instance, targetStamina);
                    }
                }
            }
        }

        /// <summary>
        /// When player spawns, set health to our modified max after a short delay.
        /// </summary>
        [HarmonyPatch(typeof(Player), nameof(Player.OnSpawned))]
        [HarmonyPostfix]
        public static void Player_OnSpawned_Postfix(Player __instance)
        {
            // Use coroutine-style delayed action
            if (__instance != null)
            {
                // Set health immediately to max
                SetPlayerToMax(__instance);
            }
        }

        /// <summary>
        /// Sets player health and stamina to their Prime-modified max.
        /// </summary>
        private static void SetPlayerToMax(Player player)
        {
            if (player == null) return;

            float maxHealth = player.GetMaxHealth();
            float currentHealth = player.GetHealth();

            // Set health to max
            if (currentHealth < maxHealth)
            {
                player.SetHealth(maxHealth);
                Plugin.Log?.LogDebug($"[Prime] Set player health to max: {maxHealth}");
            }

            float maxStamina = player.GetMaxStamina();
            float currentStamina = player.GetStamina();

            if (currentStamina < maxStamina)
            {
                player.AddStamina(maxStamina - currentStamina);
            }
        }
    }
}
