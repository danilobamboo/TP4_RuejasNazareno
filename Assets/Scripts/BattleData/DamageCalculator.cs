using UnityEngine;
using RPG.Data;

namespace RPG.Battle
{
    public static class DamageCalculator
    {
        public static int CalculateDamage(Stats user, Stats target, AbilityData ability, float variance)
        {
            int offense = ability.damageType == DamageType.Physical ? user.attack : user.magic;
            int mitigation = ability.damageType == DamageType.Physical ? target.defense : target.resistance;

            float raw = offense * ability.power * 2f - mitigation;
            float randomFactor = Random.Range(1f - variance, 1f + variance);

            return Mathf.Max(1, Mathf.RoundToInt(raw * randomFactor));
        }

        public static int CalculateHeal(Stats user, AbilityData ability, float variance)
        {
            float raw = user.magic * ability.power * 2f;
            float randomFactor = Random.Range(1f - variance, 1f + variance);

            return Mathf.Max(1, Mathf.RoundToInt(raw * randomFactor));
        }

        public static int CalculateMPRestore(Stats user, AbilityData ability, float variance)
        {
            float raw = user.magic * ability.power;
            float randomFactor = Random.Range(1f - variance, 1f + variance);

            return Mathf.Max(1, Mathf.RoundToInt(raw * randomFactor));
        }
    }
}
