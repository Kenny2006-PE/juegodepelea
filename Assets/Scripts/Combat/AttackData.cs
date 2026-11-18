using System;
using UnityEngine;

namespace FightingGame
{
    public enum AttackType
    {
        Jab = 0,
        Kick = 1
    }

    [Flags]
    public enum HitboxLimb
    {
        None = 0,
        RightHand = 1 << 0,
        LeftHand = 1 << 1,
        RightFoot = 1 << 2,
        LeftFoot = 1 << 3
    }

    /// <summary>
    /// Defines damage, knockback, hitstun, knockdown behavior, active limbs, and timing
    /// for the two combat attacks: Jab Cross (light) and Roundhouse Kick (heavy knockdown).
    /// </summary>
    [Serializable]
    public struct AttackData
    {
        public AttackType attackType;
        public string animatorTrigger;
        public string animatorStateName;
        public float damage;
        public float knockbackForce;
        public float hitstunDuration;
        public bool causesKnockdown;
        public HitboxLimb activeLimbs;

        [Header("Synchronized Backup Window (Normalized 0..1)")]
        [Range(0f, 1f)] public float activeStartNormalized;
        [Range(0f, 1f)] public float activeEndNormalized;
        public float fallbackDuration;

        public static AttackData CreateJab()
        {
            return new AttackData
            {
                attackType = AttackType.Jab,
                animatorTrigger = "Jab",
                animatorStateName = "Jab Cross",
                damage = 8f,
                knockbackForce = 1.8f,
                hitstunDuration = 0.32f,
                causesKnockdown = false,
                activeLimbs = HitboxLimb.RightHand | HitboxLimb.LeftHand,
                activeStartNormalized = 0.22f,
                activeEndNormalized = 0.65f,
                fallbackDuration = 0.65f
            };
        }

        public static AttackData CreateKick()
        {
            return new AttackData
            {
                attackType = AttackType.Kick,
                animatorTrigger = "Kick",
                animatorStateName = "Roundhouse Kick",
                damage = 18f,
                knockbackForce = 6.8f,
                hitstunDuration = 0.50f,
                causesKnockdown = true,
                activeLimbs = HitboxLimb.RightFoot | HitboxLimb.LeftFoot,
                activeStartNormalized = 0.26f,
                activeEndNormalized = 0.64f,
                fallbackDuration = 0.88f
            };
        }
    }
}
