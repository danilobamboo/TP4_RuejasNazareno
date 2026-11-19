using System;
using System.Collections.Generic;
using UnityEngine;

namespace RPG.Data
{
    public enum AICondition
    {
        Always,
        SelfHPBelow,
        AllyHPBelow,
        EnemyHPBelow,
        AlliesAliveAtMost,
        EnemiesAliveAtLeast,
        EveryNTurns
    }

    public enum AITargetStrategy
    {
        Random,
        LowestHP,
        LowestHPPercent,
        HighestHP,
        Strongest,
        Fastest
    }

    [Serializable]
    public class AIRule
    {
        public AbilityData ability;
        public AICondition condition = AICondition.Always;
        [Range(0f, 1f)] public float hpThreshold = 0.5f;
        [Min(1)] public int count = 1;
        [Range(0f, 100f)] public float chance = 100f;
        public AITargetStrategy targetStrategy = AITargetStrategy.Random;
    }

    [CreateAssetMenu(fileName = "NewEnemyAI", menuName = "RPG/Enemy AI Profile")]
    public class EnemyAIProfile : ScriptableObject
    {
        public List<AIRule> rules = new List<AIRule>();
        public AITargetStrategy fallbackTargetStrategy = AITargetStrategy.Random;
    }
}
