using System.Collections.Generic;
using RPG.Data;

namespace RPG.Battle
{
    public class BattleAction
    {
        public Combatant User { get; }
        public AbilityData Ability { get; }
        public IReadOnlyList<Combatant> Targets { get; }

        public BattleAction(Combatant user, AbilityData ability, IReadOnlyList<Combatant> targets)
        {
            User = user;
            Ability = ability;
            Targets = targets;
        }
    }

    public enum ActionResultType
    {
        Damage,
        Heal,
        RestoreMP
    }

    public readonly struct ActionResult
    {
        public Combatant Target { get; }
        public int Amount { get; }
        public ActionResultType Type { get; }
        public bool Defeated { get; }

        public bool IsHeal => Type == ActionResultType.Heal;

        public ActionResult(Combatant target, int amount, ActionResultType type, bool defeated)
        {
            Target = target;
            Amount = amount;
            Type = type;
            Defeated = defeated;
        }
    }
}
