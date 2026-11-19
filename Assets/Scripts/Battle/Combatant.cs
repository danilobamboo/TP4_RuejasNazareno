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
        public float Gauge { get; set; }

        public string Name => Data.displayName;
        public Stats Stats => Data.stats;
        public int MaxHP => Data.stats.maxHP;
        public bool IsAlive => CurrentHP > 0;
        public float HPPercent => (float)CurrentHP / MaxHP;
        public IEnumerable<AbilityData> Abilities => Data.abilities.Where(a => a != null);

        public event Action<Combatant, int> HPChanged;
        public event Action<Combatant> Died;

        public Combatant(CombatantData data, Team team)
        {
            Data = data;
            Team = team;
            CurrentHP = data.stats.maxHP;
            BasicAttack = data.basicAttack != null ? data.basicAttack : DefaultAbilities.BasicAttack;
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
