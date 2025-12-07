using UnityEngine;

namespace Prime.Core
{
    /// <summary>
    /// Helper class to trigger VFX via Spark (when available).
    /// Falls back gracefully if Spark is not loaded.
    /// </summary>
    public static class VFXHelper
    {
        private static bool? _sparkAvailable;

        /// <summary>
        /// Checks if Spark is loaded.
        /// </summary>
        public static bool IsSparkAvailable
        {
            get
            {
                if (!_sparkAvailable.HasValue)
                {
                    _sparkAvailable = BepInEx.Bootstrap.Chainloader.PluginInfos.ContainsKey("com.slatyo.spark");
                }
                return _sparkAvailable.Value;
            }
        }

        /// <summary>
        /// Play a VFX at a position.
        /// </summary>
        public static void PlayAtPosition(string vfxId, Vector3 position, float scale = 1f)
        {
            if (string.IsNullOrEmpty(vfxId)) return;
            if (!IsSparkAvailable) return;

            try
            {
                SparkBridge.PlayAtPosition(vfxId, position, scale);
            }
            catch (System.Exception ex)
            {
                Plugin.Log?.LogDebug($"VFX playback failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Play a VFX on a character.
        /// </summary>
        public static void PlayOnCharacter(string vfxId, Character character, float scale = 1f)
        {
            if (string.IsNullOrEmpty(vfxId)) return;
            if (character == null) return;
            if (!IsSparkAvailable) return;

            try
            {
                SparkBridge.PlayOnCharacter(vfxId, character, scale);
            }
            catch (System.Exception ex)
            {
                Plugin.Log?.LogDebug($"VFX playback failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Play a directional VFX.
        /// </summary>
        public static void PlayDirectional(string vfxId, Vector3 origin, Vector3 direction, float scale = 1f)
        {
            if (string.IsNullOrEmpty(vfxId)) return;
            if (!IsSparkAvailable) return;

            try
            {
                SparkBridge.PlayDirectional(vfxId, origin, direction, scale);
            }
            catch (System.Exception ex)
            {
                Plugin.Log?.LogDebug($"VFX playback failed: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// Bridge to Spark API. Separated to avoid type loading issues when Spark isn't available.
    /// </summary>
    internal static class SparkBridge
    {
        public static void PlayAtPosition(string vfxId, Vector3 position, float scale)
        {
            Spark.API.SparkAbility.PlayAtPosition(vfxId, position, scale);
        }

        public static void PlayOnCharacter(string vfxId, Character character, float scale)
        {
            Spark.API.SparkAbility.PlayOnCharacter(vfxId, character, scale);
        }

        public static void PlayDirectional(string vfxId, Vector3 origin, Vector3 direction, float scale)
        {
            Spark.API.SparkAbility.PlayDirectional(vfxId, origin, direction, scale);
        }
    }
}
