using System;
using System.Collections.Generic;
using Prime.Abilities;
using UnityEngine;

namespace Prime.Combat
{
    /// <summary>
    /// Contains all information about a damage instance.
    /// This is passed through the damage pipeline and can be modified by handlers.
    /// </summary>
    public class DamageInfo
    {
        /// <summary>
        /// Unique ID for this damage instance.
        /// </summary>
        public string Id { get; } = Guid.NewGuid().ToString("N");

        /// <summary>
        /// The character dealing damage.
        /// </summary>
        public Character Attacker { get; set; }

        /// <summary>
        /// The character receiving damage.
        /// </summary>
        public Character Target { get; set; }

        /// <summary>
        /// Damage amounts by type.
        /// </summary>
        public Dictionary<DamageType, float> Damages { get; } = new Dictionary<DamageType, float>();

        /// <summary>
        /// Total damage before mitigation.
        /// </summary>
        public float TotalDamage => GetTotalDamage();

        /// <summary>
        /// Final damage after all calculations.
        /// </summary>
        public float FinalDamage { get; set; }

        /// <summary>
        /// Is this a critical hit?
        /// </summary>
        public bool IsCritical { get; set; }

        /// <summary>
        /// Critical damage multiplier applied.
        /// </summary>
        public float CritMultiplier { get; set; } = 1f;

        /// <summary>
        /// Was this damage blocked?
        /// </summary>
        public bool IsBlocked { get; set; }

        /// <summary>
        /// Amount of damage that was blocked.
        /// </summary>
        public float BlockedAmount { get; set; }

        /// <summary>
        /// Source of this damage.
        /// </summary>
        public DamageSource Source { get; set; } = DamageSource.Attack;

        /// <summary>
        /// Ability that caused this damage (if applicable).
        /// </summary>
        public AbilityDefinition Ability { get; set; }

        /// <summary>
        /// Weapon/item that caused this damage (if applicable).
        /// </summary>
        public ItemDrop.ItemData Weapon { get; set; }

        /// <summary>
        /// Hit point in world space.
        /// </summary>
        public Vector3 HitPoint { get; set; }

        /// <summary>
        /// Hit direction.
        /// </summary>
        public Vector3 HitDirection { get; set; }

        /// <summary>
        /// Knockback force applied.
        /// </summary>
        public float Knockback { get; set; }

        /// <summary>
        /// Stagger damage dealt.
        /// </summary>
        public float Stagger { get; set; }

        /// <summary>
        /// Should this damage be cancelled?
        /// </summary>
        public bool Cancelled { get; set; }

        /// <summary>
        /// Reason for cancellation (for debugging).
        /// </summary>
        public string CancelReason { get; set; }

        /// <summary>
        /// Should this hit proc on-hit effects?
        /// </summary>
        public bool CanProcEffects { get; set; } = true;

        /// <summary>
        /// Is this a backstab?
        /// </summary>
        public bool IsBackstab { get; set; }

        /// <summary>
        /// Backstab multiplier applied.
        /// </summary>
        public float BackstabMultiplier { get; set; } = 1f;

        /// <summary>
        /// Custom data for mod extensions.
        /// </summary>
        public Dictionary<string, object> CustomData { get; } = new Dictionary<string, object>();

        /// <summary>
        /// Tags for filtering in event handlers.
        /// </summary>
        public HashSet<string> Tags { get; } = new HashSet<string>();

        /// <summary>
        /// The original Valheim HitData (for compatibility).
        /// </summary>
        public HitData OriginalHitData { get; set; }

        /// <summary>
        /// Time when this damage was created.
        /// </summary>
        public float Timestamp { get; } = Time.time;

        public DamageInfo() { }

        public DamageInfo(Character attacker, Character target)
        {
            Attacker = attacker;
            Target = target;
        }

        /// <summary>
        /// Sets damage for a specific type.
        /// </summary>
        public void SetDamage(DamageType type, float amount)
        {
            if (amount > 0)
                Damages[type] = amount;
            else
                Damages.Remove(type);
        }

        /// <summary>
        /// Adds damage of a specific type.
        /// </summary>
        public void AddDamage(DamageType type, float amount)
        {
            if (Damages.ContainsKey(type))
                Damages[type] += amount;
            else if (amount > 0)
                Damages[type] = amount;
        }

        /// <summary>
        /// Gets damage for a specific type.
        /// </summary>
        public float GetDamage(DamageType type)
        {
            return Damages.TryGetValue(type, out float amount) ? amount : 0f;
        }

        /// <summary>
        /// Multiplies all damage by a factor.
        /// </summary>
        public void MultiplyAllDamage(float factor)
        {
            var keys = new List<DamageType>(Damages.Keys);
            foreach (var key in keys)
            {
                Damages[key] *= factor;
            }
        }

        /// <summary>
        /// Gets total damage across all types.
        /// </summary>
        public float GetTotalDamage()
        {
            float total = 0f;
            foreach (var kvp in Damages)
            {
                total += kvp.Value;
            }
            return total;
        }

        /// <summary>
        /// Converts from Valheim's HitData.
        /// </summary>
        public static DamageInfo FromHitData(HitData hitData, Character attacker, Character target)
        {
            var info = new DamageInfo(attacker, target)
            {
                OriginalHitData = hitData,
                HitPoint = hitData.m_point,
                HitDirection = hitData.m_dir,
                Knockback = hitData.m_pushForce,
                Stagger = hitData.m_staggerMultiplier,
                IsBackstab = hitData.m_backstabBonus > 1f,
                BackstabMultiplier = hitData.m_backstabBonus
            };

            // Map Valheim damage types
            if (hitData.m_damage.m_damage > 0)
                info.SetDamage(DamageType.Physical, hitData.m_damage.m_damage);
            if (hitData.m_damage.m_blunt > 0)
                info.SetDamage(DamageType.Blunt, hitData.m_damage.m_blunt);
            if (hitData.m_damage.m_slash > 0)
                info.SetDamage(DamageType.Slash, hitData.m_damage.m_slash);
            if (hitData.m_damage.m_pierce > 0)
                info.SetDamage(DamageType.Pierce, hitData.m_damage.m_pierce);
            if (hitData.m_damage.m_fire > 0)
                info.SetDamage(DamageType.Fire, hitData.m_damage.m_fire);
            if (hitData.m_damage.m_frost > 0)
                info.SetDamage(DamageType.Frost, hitData.m_damage.m_frost);
            if (hitData.m_damage.m_lightning > 0)
                info.SetDamage(DamageType.Lightning, hitData.m_damage.m_lightning);
            if (hitData.m_damage.m_poison > 0)
                info.SetDamage(DamageType.Poison, hitData.m_damage.m_poison);
            if (hitData.m_damage.m_spirit > 0)
                info.SetDamage(DamageType.Spirit, hitData.m_damage.m_spirit);
            if (hitData.m_damage.m_chop > 0)
                info.SetDamage(DamageType.Chop, hitData.m_damage.m_chop);
            if (hitData.m_damage.m_pickaxe > 0)
                info.SetDamage(DamageType.Pickaxe, hitData.m_damage.m_pickaxe);

            return info;
        }

        /// <summary>
        /// Applies calculated damage back to Valheim's HitData.
        /// </summary>
        public void ApplyToHitData(HitData hitData)
        {
            if (hitData == null) return;

            // Clear existing damage
            hitData.m_damage.m_damage = 0;
            hitData.m_damage.m_blunt = 0;
            hitData.m_damage.m_slash = 0;
            hitData.m_damage.m_pierce = 0;
            hitData.m_damage.m_fire = 0;
            hitData.m_damage.m_frost = 0;
            hitData.m_damage.m_lightning = 0;
            hitData.m_damage.m_poison = 0;
            hitData.m_damage.m_spirit = 0;
            hitData.m_damage.m_chop = 0;
            hitData.m_damage.m_pickaxe = 0;

            // Apply Prime damages
            foreach (var kvp in Damages)
            {
                switch (kvp.Key)
                {
                    case DamageType.Physical:
                        hitData.m_damage.m_damage = kvp.Value;
                        break;
                    case DamageType.Blunt:
                        hitData.m_damage.m_blunt = kvp.Value;
                        break;
                    case DamageType.Slash:
                        hitData.m_damage.m_slash = kvp.Value;
                        break;
                    case DamageType.Pierce:
                        hitData.m_damage.m_pierce = kvp.Value;
                        break;
                    case DamageType.Fire:
                        hitData.m_damage.m_fire = kvp.Value;
                        break;
                    case DamageType.Frost:
                        hitData.m_damage.m_frost = kvp.Value;
                        break;
                    case DamageType.Lightning:
                        hitData.m_damage.m_lightning = kvp.Value;
                        break;
                    case DamageType.Poison:
                        hitData.m_damage.m_poison = kvp.Value;
                        break;
                    case DamageType.Spirit:
                        hitData.m_damage.m_spirit = kvp.Value;
                        break;
                    case DamageType.Chop:
                        hitData.m_damage.m_chop = kvp.Value;
                        break;
                    case DamageType.Pickaxe:
                        hitData.m_damage.m_pickaxe = kvp.Value;
                        break;
                }
            }

            hitData.m_pushForce = Knockback;
            hitData.m_staggerMultiplier = Stagger;
            hitData.m_backstabBonus = BackstabMultiplier;
        }

        public override string ToString()
        {
            return $"DamageInfo({Attacker?.GetHoverName() ?? "?"} -> {Target?.GetHoverName() ?? "?"}: {TotalDamage:F1} dmg{(IsCritical ? " CRIT" : "")})";
        }
    }

    /// <summary>
    /// Source of damage for categorization.
    /// </summary>
    public enum DamageSource
    {
        /// <summary>Regular weapon attack.</summary>
        Attack,
        /// <summary>Ability/skill use.</summary>
        Ability,
        /// <summary>Damage over time effect.</summary>
        DoT,
        /// <summary>Environmental damage.</summary>
        Environment,
        /// <summary>Fall damage.</summary>
        Fall,
        /// <summary>Reflected damage.</summary>
        Reflect,
        /// <summary>Self-inflicted damage.</summary>
        Self,
        /// <summary>Unknown or unspecified.</summary>
        Unknown
    }
}
