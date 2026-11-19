using System.Collections.Generic;
using UnityEngine;

namespace RPG.Data
{
    [CreateAssetMenu(fileName = "NewCombatant", menuName = "RPG/Combatant")]
    public class CombatantData : ScriptableObject
    {
        public string displayName = "New Combatant";
        public Sprite portrait;

        public Stats stats = new Stats
        {
            maxHP = 100,
            attack = 10,
            defense = 5,
            magic = 10,
            resistance = 5,
            speed = 10
        };

        public AbilityData basicAttack;
        public List<AbilityData> abilities = new List<AbilityData>();
    }
}
