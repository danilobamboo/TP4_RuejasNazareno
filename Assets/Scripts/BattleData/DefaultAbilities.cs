using UnityEngine;
using RPG.Data;

namespace RPG.Battle
{
    public static class DefaultAbilities
    {
        static AbilityData basicAttack;

        public static AbilityData BasicAttack
        {
            get
            {
                if (basicAttack == null)
                {
                    basicAttack = ScriptableObject.CreateInstance<AbilityData>();
                    basicAttack.name = "DefaultBasicAttack";
                    basicAttack.displayName = "Atacar";
                    basicAttack.hideFlags = HideFlags.DontSave;
                }
                return basicAttack;
            }
        }
    }
}
