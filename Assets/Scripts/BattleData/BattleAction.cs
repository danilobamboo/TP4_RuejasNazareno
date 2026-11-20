using System.Collections.Generic;
using UnityEngine;
using RPG.Data;

namespace RPG.Battle
{
    public class BattleAction
    {
        public Combatant User { get; }
        public AbilityData Ability { get; }
        public ItemData Item { get; }
        public IReadOnlyList<Combatant> Targets { get; }

        public bool IsItem => Item != null;
        public string DisplayName => IsItem ? Item.displayName : Ability.displayName;
        public float AtbCost => IsItem ? Item.atbCost : Ability.atbCost;
        public int MpCost => IsItem ? 0 : Ability.mpCost;
        public float ImpactDelay => IsItem ? Item.impactDelay : Ability.impactDelay;
        public string AnimationTrigger => IsItem ? Item.animationTrigger : Ability.animationTrigger;
        public AudioClip UseSound => IsItem ? Item.useSound : Ability.useSound;
        public TargetType TargetType => IsItem ? Item.targetType : Ability.targetType;

        public BattleAction(Combatant user, AbilityData ability, IReadOnlyList<Combatant> targets)
        {
            User = user;
            Ability = ability;
            Targets = targets;
        }

        public BattleAction(Combatant user, ItemData item, IReadOnlyList<Combatant> targets)
        {
            User = user;
            Item = item;
            Targets = targets;
        }
    }

    public enum ActionResultType
    {
        Damage,
        Heal,
        RestoreMP,
        RestoreATB,
        ReduceATB,
        StatusApplied
    }

    public readonly struct ActionResult
    {
        public Combatant Target { get; }
        public int Amount { get; }
        public ActionResultType Type { get; }
        public bool Defeated { get; }
        public StatusEffectData Status { get; }

        public bool IsHeal => Type == ActionResultType.Heal;

        public ActionResult(Combatant target, int amount, ActionResultType type, bool defeated, StatusEffectData status = null)
        {
            Target = target;
            Amount = amount;
            Type = type;
            Defeated = defeated;
            Status = status;
        }
    }
}
