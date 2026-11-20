using UnityEngine;

namespace RPG.Data
{
    public enum StatusEffectType
    {
        DamageOverTime,
        StatModifier
    }

    public enum StatType
    {
        Attack,
        Defense,
        Magic,
        Resistance,
        Speed
    }

    [CreateAssetMenu(fileName = "NewStatusEffect", menuName = "RPG/Status Effect")]
    public class StatusEffectData : ScriptableObject
    {
        public string displayName = "New Status";
        public string shortName = "EST";
        public Color color = Color.white;
        public Sprite icon;

        public StatusEffectType type = StatusEffectType.DamageOverTime;
        [Min(1)] public int durationTurns = 3;

        [Min(0)] public int damagePerTurn = 5;
        [Range(0f, 1f)] public float damagePercentOfMaxHP = 0f;

        public StatType stat = StatType.Resistance;
        [Min(0f)] public float statMultiplier = 1.5f;
    }
}
