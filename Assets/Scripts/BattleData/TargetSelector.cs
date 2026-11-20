using System.Collections.Generic;
using System.Linq;
using RPG.Data;

namespace RPG.Battle
{
    public static class TargetSelector
    {
        public static bool IsSingleTarget(TargetType targetType)
        {
            return targetType == TargetType.SingleEnemy || targetType == TargetType.SingleAlly;
        }

        public static List<Combatant> GetCandidates(Combatant user, AbilityData ability, IEnumerable<Combatant> combatants)
        {
            return GetCandidates(user, ability.targetType, combatants);
        }

        public static List<Combatant> GetCandidates(Combatant user, TargetType targetType, IEnumerable<Combatant> combatants)
        {
            switch (targetType)
            {
                case TargetType.Self:
                    return user.IsAlive ? new List<Combatant> { user } : new List<Combatant>();

                case TargetType.SingleEnemy:
                case TargetType.AllEnemies:
                    return combatants.Where(c => c.IsAlive && c.Team != user.Team).ToList();

                default:
                    return combatants.Where(c => c.IsAlive && c.Team == user.Team).ToList();
            }
        }

        public static List<Combatant> ResolveTargets(Combatant user, AbilityData ability, IEnumerable<Combatant> combatants, Combatant chosenTarget)
        {
            return ResolveTargets(user, ability.targetType, combatants, chosenTarget);
        }

        public static List<Combatant> ResolveTargets(Combatant user, TargetType targetType, IEnumerable<Combatant> combatants, Combatant chosenTarget)
        {
            List<Combatant> candidates = GetCandidates(user, targetType, combatants);

            if (!IsSingleTarget(targetType)) return candidates;

            if (chosenTarget != null && candidates.Contains(chosenTarget))
                return new List<Combatant> { chosenTarget };

            return candidates.Count > 0 ? new List<Combatant> { candidates[0] } : new List<Combatant>();
        }
    }
}
