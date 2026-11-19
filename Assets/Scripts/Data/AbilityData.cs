using UnityEngine;

namespace RPG.Data
{
    [CreateAssetMenu(fileName = "NewAbility", menuName = "RPG/Ability")]
    public class AbilityData : ScriptableObject
    {
        public string displayName = "New Ability";
        [TextArea] public string description;

        public AbilityEffect effect = AbilityEffect.Damage;
        public DamageType damageType = DamageType.Physical;
        public TargetType targetType = TargetType.SingleEnemy;

        [Min(0f)] public float power = 1f;
        [Min(0f)] public float atbCost = 0f;
        [Min(0)] public int mpCost = 0;
    }
}
