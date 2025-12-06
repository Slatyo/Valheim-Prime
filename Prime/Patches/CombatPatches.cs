using HarmonyLib;
using Prime.Combat;
using Prime.Effects;
using UnityEngine;

namespace Prime.Patches
{
    /// <summary>
    /// Harmony patches that hook Prime into Valheim's combat system.
    /// </summary>
    [HarmonyPatch]
    public static class CombatPatches
    {
        /// <summary>
        /// Intercepts damage before it is applied, runs it through Prime's pipeline.
        /// </summary>
        [HarmonyPatch(typeof(Character), nameof(Character.Damage))]
        [HarmonyPrefix]
        public static void Character_Damage_Prefix(Character __instance, ref HitData hit)
        {
            if (hit == null || __instance == null)
                return;

            // Get attacker
            Character attacker = hit.GetAttacker();

            // Log original damage for debugging
            float originalTotal = hit.GetTotalDamage();

            // Convert HitData to DamageInfo
            var damageInfo = DamageInfo.FromHitData(hit, attacker, __instance);

            // Process through Prime's pipeline
            float finalDamage = CombatManager.ProcessDamage(damageInfo);

            // If cancelled, zero out damage
            if (damageInfo.Cancelled)
            {
                hit.m_damage.m_damage = 0;
                hit.m_damage.m_blunt = 0;
                hit.m_damage.m_slash = 0;
                hit.m_damage.m_pierce = 0;
                hit.m_damage.m_fire = 0;
                hit.m_damage.m_frost = 0;
                hit.m_damage.m_lightning = 0;
                hit.m_damage.m_poison = 0;
                hit.m_damage.m_spirit = 0;
                Plugin.Log?.LogDebug($"[Prime] Damage cancelled to {__instance.m_name}");
                return;
            }

            // Apply modified damage back to HitData
            damageInfo.ApplyToHitData(hit);

            // Log result
            float newTotal = hit.GetTotalDamage();
            Plugin.Log?.LogDebug($"[Prime] Damage to {__instance.m_name}: {originalTotal:F1} -> {newTotal:F1} (Final: {finalDamage:F1})");
        }

        /// <summary>
        /// Track when entities die for kill events.
        /// </summary>
        [HarmonyPatch(typeof(Character), nameof(Character.OnDeath))]
        [HarmonyPrefix]
        public static void Character_OnDeath_Prefix(Character __instance)
        {
            // Clean up effects on death
            EffectManager.RemoveAllEffects(__instance);

            // Remove from entity manager
            PrimeAPI.RemoveEntity(__instance);
        }

        /// <summary>
        /// Hook into blocking for block events.
        /// </summary>
        [HarmonyPatch(typeof(Humanoid), nameof(Humanoid.BlockAttack))]
        [HarmonyPostfix]
        public static void Humanoid_BlockAttack_Postfix(Humanoid __instance, HitData hit, bool __result)
        {
            if (!__result || hit == null || __instance == null)
                return;

            Character attacker = hit.GetAttacker();
            var damageInfo = DamageInfo.FromHitData(hit, attacker, __instance);
            damageInfo.IsBlocked = true;

            // Raise block event
            Events.PrimeEvents.RaiseOnBlock(__instance, attacker, damageInfo);

            // Trigger on-block effects
            EffectManager.TriggerOnBlockEffects(__instance, attacker, damageInfo);
        }

        /// <summary>
        /// Hook into stagger for stagger events.
        /// </summary>
        [HarmonyPatch(typeof(Character), nameof(Character.Stagger))]
        [HarmonyPostfix]
        public static void Character_Stagger_Postfix(Character __instance, Vector3 forceDirection)
        {
            // Try to find who caused the stagger (approximation)
            // In practice, you would track this from the damage event
            Events.PrimeEvents.RaiseOnStagger(__instance, null);
        }
    }

    /// <summary>
    /// Patches for player-specific combat mechanics.
    /// </summary>
    [HarmonyPatch]
    public static class PlayerCombatPatches
    {
        /// <summary>
        /// Apply movement speed modifier.
        /// </summary>
        [HarmonyPatch(typeof(Character), nameof(Character.GetRunSpeedFactor))]
        [HarmonyPostfix]
        public static void Character_GetRunSpeedFactor_Postfix(Character __instance, ref float __result)
        {
            // Get MoveSpeed stat
            float moveSpeed = PrimeAPI.Get(__instance, "MoveSpeed");

            // MoveSpeed is a multiplier (1.0 is normal)
            if (moveSpeed > 0 && moveSpeed != 1f)
            {
                __result *= moveSpeed;
            }
        }

        /// <summary>
        /// Apply max carry weight modification.
        /// </summary>
        [HarmonyPatch(typeof(Player), nameof(Player.GetMaxCarryWeight))]
        [HarmonyPostfix]
        public static void Player_GetMaxCarryWeight_Postfix(Player __instance, ref float __result)
        {
            // Get CarryWeight stat (this replaces the base value)
            float carryWeight = PrimeAPI.Get(__instance, "CarryWeight");

            // If Prime has a value set, use it
            if (carryWeight > 0)
            {
                __result = carryWeight;
            }
        }
    }

    /// <summary>
    /// Patches to suppress vanilla damage text (let Veneer handle it via events).
    /// </summary>
    [HarmonyPatch]
    public static class DamageTextPatches
    {
        /// <summary>
        /// Suppress vanilla damage text overload (TextType, Vector3, float, bool).
        /// </summary>
        [HarmonyPatch(typeof(DamageText), nameof(DamageText.ShowText),
            new[] { typeof(DamageText.TextType), typeof(Vector3), typeof(float), typeof(bool) })]
        [HarmonyPrefix]
        public static bool DamageText_ShowText_Prefix_4Args()
        {
            return false;
        }

        /// <summary>
        /// Suppress AddInworldText - this is the internal method that actually creates the text.
        /// Patching this ensures all damage text is suppressed regardless of how it's called.
        /// </summary>
        [HarmonyPatch(typeof(DamageText), "AddInworldText")]
        [HarmonyPrefix]
        public static bool DamageText_AddInworldText_Prefix()
        {
            return false;
        }
    }

    /// <summary>
    /// Patches for initializing Prime stats on entities.
    /// </summary>
    [HarmonyPatch]
    public static class EntityInitPatches
    {
        /// <summary>
        /// Initialize Prime stats when a player spawns.
        /// </summary>
        [HarmonyPatch(typeof(Player), nameof(Player.OnSpawned))]
        [HarmonyPostfix]
        public static void Player_OnSpawned_Postfix(Player __instance)
        {
            // Initialize stats container for player
            var container = PrimeAPI.InitializeEntity(__instance);

            // Sync some base values from Valheim
            // Note: These could be overwritten by mods that want different base values
            if (container != null)
            {
                container.SetBase("MaxHealth", __instance.GetMaxHealth());
                container.SetBase("MaxStamina", __instance.GetMaxStamina());
            }

            Plugin.Log?.LogDebug($"[Prime] Initialized stats for player: {__instance.GetPlayerName()}");
        }

        /// <summary>
        /// Initialize Prime stats when a creature spawns.
        /// </summary>
        [HarmonyPatch(typeof(Character), nameof(Character.Awake))]
        [HarmonyPostfix]
        public static void Character_Awake_Postfix(Character __instance)
        {
            // Do not double-initialize players (handled separately)
            if (__instance is Player)
                return;

            // Initialize stats container
            // Note: Creatures get default stats, mods like Denizen would customize
            PrimeAPI.InitializeEntity(__instance);
        }

        /// <summary>
        /// Clean up when character is destroyed.
        /// </summary>
        [HarmonyPatch(typeof(Character), nameof(Character.OnDestroy))]
        [HarmonyPrefix]
        public static void Character_OnDestroy_Prefix(Character __instance)
        {
            // Clean up effects
            EffectManager.RemoveAllEffects(__instance);

            // Remove from entity tracking
            PrimeAPI.RemoveEntity(__instance);
        }
    }
}
