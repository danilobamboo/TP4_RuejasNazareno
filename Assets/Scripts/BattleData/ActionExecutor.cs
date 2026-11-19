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
            Stats userStats = action.User.Stats;

            foreach (Combatant target in action.Targets)
            {
                if (!target.IsAlive) continue;

                switch (action.Ability.effect)
                {
                    case AbilityEffect.Heal:
                    {
                        int amount = DamageCalculator.CalculateHeal(userStats, action.Ability, variance);
                        results.Add(new ActionResult(target, target.Heal(amount), ActionResultType.Heal, false));
                        break;
                    }
                    case AbilityEffect.RestoreMP:
                    {
                        int amount = DamageCalculator.CalculateMPRestore(userStats, action.Ability, variance);
                        results.Add(new ActionResult(target, target.RestoreMP(amount), ActionResultType.RestoreMP, false));
                        break;
                    }
                    default:
                    {
                        int amount = DamageCalculator.CalculateDamage(userStats, target.Stats, action.Ability, variance);
                        int dealt = target.TakeDamage(amount);
                        results.Add(new ActionResult(target, dealt, ActionResultType.Damage, !target.IsAlive));
                        break;
                    }
                }
            }

            return results;
        }
    }
}
