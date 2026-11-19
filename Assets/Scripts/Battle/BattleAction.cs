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

    public readonly struct ActionResult
    {
        public Combatant Target { get; }
        public int Amount { get; }
        public bool IsHeal { get; }
        public bool Defeated { get; }

        public ActionResult(Combatant target, int amount, bool isHeal, bool defeated)
        {
            Target = target;
            Amount = amount;
            IsHeal = isHeal;
            Defeated = defeated;
        }
    }
}
