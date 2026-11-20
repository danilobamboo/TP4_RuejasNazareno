using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using RPG.Data;

namespace RPG.Battle
{
    public class InventorySlot
    {
        public ItemData Item { get; }
        public int Quantity { get; internal set; }

        public InventorySlot(ItemData item, int quantity)
        {
            Item = item;
            Quantity = quantity;
        }
    }

    public class StatusInstance
    {
        public StatusEffectData Effect { get; }
        public Combatant Source { get; }
        public int RemainingTurns { get; internal set; }

        public StatusInstance(StatusEffectData effect, Combatant source)
        {
            Effect = effect;
            Source = source;
            RemainingTurns = effect.durationTurns;
        }
    }

    public readonly struct StatusTick
    {
        public StatusInstance Status { get; }
        public int Damage { get; }
        public bool Expired { get; }

        public StatusTick(StatusInstance status, int damage, bool expired)
        {
            Status = status;
            Damage = damage;
            Expired = expired;
        }
    }

    public class Combatant
    {
        readonly List<InventorySlot> inventory = new List<InventorySlot>();
        readonly List<StatusInstance> statuses = new List<StatusInstance>();

        public CombatantData Data { get; }
        public Team Team { get; }
        public AbilityData BasicAttack { get; }
        public int CurrentHP { get; private set; }
        public int CurrentMP { get; private set; }
        public float Gauge { get; set; }
        public int TurnsTaken { get; set; }

        public string Name => Data.displayName;
        public Stats BaseStats => Data.stats;
        public Stats Stats => GetEffectiveStats();
        public IReadOnlyList<StatusInstance> Statuses => statuses;
        public int MaxHP => Data.stats.maxHP;
        public int MaxMP => Data.stats.maxMP;
        public bool IsAlive => CurrentHP > 0;
        public float HPPercent => (float)CurrentHP / MaxHP;
        public float MPPercent => MaxMP > 0 ? (float)CurrentMP / MaxMP : 1f;
        public IEnumerable<AbilityData> Abilities => Data.abilities.Where(a => a != null);
        public IReadOnlyList<InventorySlot> Inventory => inventory;
        public bool HasUsableItems => inventory.Any(s => s.Quantity > 0);

        public event Action<Combatant, int> HPChanged;
        public event Action<Combatant, int> MPChanged;
        public event Action<Combatant> Died;
        public event Action<Combatant, ItemData> ItemConsumed;
        public event Action<Combatant> StatusesChanged;

        public Combatant(CombatantData data, Team team)
        {
            Data = data;
            Team = team;
            CurrentHP = data.stats.maxHP;
            CurrentMP = data.stats.maxMP;
            BasicAttack = data.basicAttack != null ? data.basicAttack : DefaultAbilities.BasicAttack;

            if (data.startingItems == null) return;

            foreach (ItemEntry entry in data.startingItems)
            {
                if (entry == null || entry.item == null || entry.quantity <= 0) continue;

                InventorySlot slot = inventory.FirstOrDefault(s => s.Item == entry.item);
                if (slot != null) slot.Quantity += entry.quantity;
                else inventory.Add(new InventorySlot(entry.item, entry.quantity));
            }
        }

        public int GetItemCount(ItemData item)
        {
            InventorySlot slot = inventory.FirstOrDefault(s => s.Item == item);
            return slot != null ? slot.Quantity : 0;
        }

        public bool ConsumeItem(ItemData item)
        {
            InventorySlot slot = inventory.FirstOrDefault(s => s.Item == item);
            if (slot == null || slot.Quantity <= 0) return false;

            slot.Quantity--;
            ItemConsumed?.Invoke(this, item);
            return true;
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

            if (CurrentHP == 0)
            {
                ClearStatuses();
                Died?.Invoke(this);
            }

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

        public void ApplyStatus(StatusEffectData effect, Combatant source)
        {
            if (effect == null || !IsAlive) return;

            StatusInstance existing = statuses.FirstOrDefault(s => s.Effect == effect);
            if (existing != null) existing.RemainingTurns = Mathf.Max(existing.RemainingTurns, effect.durationTurns);
            else statuses.Add(new StatusInstance(effect, source));

            StatusesChanged?.Invoke(this);
        }

        public List<StatusTick> TickStatuses()
        {
            List<StatusTick> ticks = new List<StatusTick>();
            if (statuses.Count == 0) return ticks;

            foreach (StatusInstance status in statuses.ToList())
            {
                if (!IsAlive) break;

                int damage = 0;
                if (status.Effect.type == StatusEffectType.DamageOverTime)
                {
                    int amount = status.Effect.damagePerTurn + Mathf.RoundToInt(status.Effect.damagePercentOfMaxHP * MaxHP);
                    damage = TakeDamage(Mathf.Max(1, amount));
                }

                if (!IsAlive)
                {
                    ticks.Add(new StatusTick(status, damage, true));
                    break;
                }

                status.RemainingTurns--;
                bool expired = status.RemainingTurns <= 0;
                if (expired) statuses.Remove(status);

                ticks.Add(new StatusTick(status, damage, expired));
            }

            StatusesChanged?.Invoke(this);
            return ticks;
        }

        void ClearStatuses()
        {
            if (statuses.Count == 0) return;

            statuses.Clear();
            StatusesChanged?.Invoke(this);
        }

        Stats GetEffectiveStats()
        {
            Stats stats = Data.stats;
            if (statuses.Count == 0) return stats;

            foreach (StatusInstance status in statuses)
            {
                if (status.Effect.type != StatusEffectType.StatModifier) continue;

                float multiplier = status.Effect.statMultiplier;
                switch (status.Effect.stat)
                {
                    case StatType.Attack: stats.attack = Mathf.RoundToInt(stats.attack * multiplier); break;
                    case StatType.Defense: stats.defense = Mathf.RoundToInt(stats.defense * multiplier); break;
                    case StatType.Magic: stats.magic = Mathf.RoundToInt(stats.magic * multiplier); break;
                    case StatType.Resistance: stats.resistance = Mathf.RoundToInt(stats.resistance * multiplier); break;
                    case StatType.Speed: stats.speed = Mathf.Max(1, Mathf.RoundToInt(stats.speed * multiplier)); break;
                }
            }

            return stats;
        }
    }
}
