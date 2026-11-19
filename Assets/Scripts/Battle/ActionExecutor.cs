using System.Collections.Generic;
using RPG.Data;

namespace RPG.Battle
{
    public static class ActionExecutor
    {
        public static List<ActionResult> Execute(BattleAction action, BattleSettings settings)
        {
            List<ActionResult> results = new List<ActionResult>();
            float variance = settings != null ? settings.damageVariance : 0.1f;

            foreach (Combatant target in action.Targets)
            {
                if (!target.IsAlive) continue;

                if (action.Ability.effect == AbilityEffect.Heal)
                {
                    int amount = DamageCalculator.CalculateHeal(action.User.Stats, action.Ability, variance);
                    int healed = target.Heal(amount);
                    results.Add(new ActionResult(target, healed, true, false));
                }
                else
                {
                    int amount = DamageCalculator.CalculateDamage(action.User.Stats, target.Stats, action.Ability, variance);
                    int dealt = target.TakeDamage(amount);
                    results.Add(new ActionResult(target, dealt, false, !target.IsAlive));
                }
            }

            return results;
        }
    }
}
