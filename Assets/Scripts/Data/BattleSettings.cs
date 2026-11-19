using UnityEngine;

namespace RPG.Data
{
    public enum AtbMode
    {
        Wait,
        Active
    }

    [CreateAssetMenu(fileName = "BattleSettings", menuName = "RPG/Battle Settings")]
    public class BattleSettings : ScriptableObject
    {
        public AtbMode atbMode = AtbMode.Wait;
        [Min(1f)] public float maxGauge = 100f;
        [Min(0.01f)] public float fillRateMultiplier = 2.5f;
        [Range(0f, 1f)] public float startingGaugeVariance = 0.3f;
        [Range(0f, 0.5f)] public float damageVariance = 0.1f;
    }
}
