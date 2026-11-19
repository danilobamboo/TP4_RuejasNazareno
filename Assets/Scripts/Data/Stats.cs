using System;
using UnityEngine;

namespace RPG.Data
{
    [Serializable]
    public struct Stats
    {
        [Min(1)] public int maxHP;
        [Min(0)] public int attack;
        [Min(0)] public int defense;
        [Min(0)] public int magic;
        [Min(0)] public int resistance;
        [Min(1)] public int speed;
    }
}
