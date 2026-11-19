using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using RPG.Data;

namespace RPG.Battle
{
    public class Combatant
    {
        public CombatantData Data { get; }
        public Team Team { get; }
        public AbilityData BasicAttack { get; }
        public int CurrentHP { get; private set; }
        public int CurrentMP { get; private set; }
        public float Gauge { get; set; }
        public int TurnsTaken { get; set; }

        public string Name => Data.displayName;
        public Stats Stats => Data.stats;
        public int MaxHP => Data.stats.maxHP;
        public int MaxMP => Data.stats.maxMP;
        public bool IsAlive => CurrentHP > 0;
        public float HPPercent => (float)CurrentHP / MaxHP;
        public float MPPercent => MaxMP > 0 ? (float)CurrentMP / MaxMP : 1f;
        public IEnumerable<AbilityData> Abilities => Data.abilities.Where(a => a != null);

        public event Action<Combatant, int> HPChanged;
        public event Action<Combatant, int> MPChanged;
        public event Action<Combatant> Died;

        public Combatant(CombatantData data, Team team)
        {
            Data = data;
            Team = team;
            CurrentHP = data.stats.maxHP;
            CurrentMP = data.stats.maxMP;
            BasicAttack = data.basicAttack != null ? data.basicAttack : DefaultAbilities.BasicAttack;
        }

        public bool CanAfford(AbilityData ability)
        {
            return ability != null && CurrentMP >= ability.mpCost;
        }

        public bool SpendMP(int amount)
        {
            if (amount <= 0) return true;
            if (CurrentMP < amount) return false;

            CurrentMP -= amount;
            MPChanged?.Invoke(this, -amount);
            return true;
        }

        public int RestoreMP(int amount)
        {
            if (!IsAlive || amount <= 0) return 0;

            int restored = Mathf.Min(amount, MaxMP - CurrentMP);
            CurrentMP += restored;
            if (restored > 0) MPChanged?.Invoke(this, restored);

            return restored;
        }

        public int TakeDamage(int amount)
        {
            if (!IsAlive || amount <= 0) return 0;

            int dealt = Mathf.Min(amount, CurrentHP);
            CurrentHP -= dealt;
            HPChanged?.Invoke(this, -dealt);

            if (CurrentHP == 0) Died?.Invoke(this);

            return dealt;
        }

        public int Heal(int amount)
        {
            if (!IsAlive || amount <= 0) return 0;

            int healed = Mathf.Min(amount, MaxHP - CurrentHP);
            CurrentHP += healed;
            HPChanged?.Invoke(this, healed);

            return healed;
        }
    }
}
