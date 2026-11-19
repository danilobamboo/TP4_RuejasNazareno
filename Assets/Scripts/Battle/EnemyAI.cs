using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using RPG.Data;

namespace RPG.Battle
{
    public static class EnemyAI
    {
        public static BattleAction ChooseAction(Combatant user, IReadOnlyList<Combatant> combatants)
        {
            List<AbilityData> options = new List<AbilityData> { user.BasicAttack };
            options.AddRange(user.Abilities);
            options = options.Where(a => TargetSelector.GetCandidates(user, a, combatants).Count > 0).ToList();

            if (options.Count == 0) return null;

            AbilityData ability = options[Random.Range(0, options.Count)];
            List<Combatant> candidates = TargetSelector.GetCandidates(user, ability, combatants);

            Combatant chosen = null;
            if (TargetSelector.IsSingleTarget(ability.targetType))
            {
                chosen = ability.effect == AbilityEffect.Heal
                    ? candidates.OrderBy(c => c.HPPercent).First()
                    : candidates[Random.Range(0, candidates.Count)];
            }

            List<Combatant> targets = TargetSelector.ResolveTargets(user, ability, combatants, chosen);
            return new BattleAction(user, ability, targets);
        }
    }
}
