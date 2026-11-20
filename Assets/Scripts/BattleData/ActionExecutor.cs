using System.Collections.Generic;
using UnityEngine;
using RPG.Data;

namespace RPG.Battle
{
    public static class ActionExecutor
    {
        public static List<ActionResult> Execute(BattleAction action, BattleSettings settings)
        {
            return action.IsItem ? ExecuteItem(action, settings) : ExecuteAbility(action, settings);
        }

        static List<ActionResult> ExecuteAbility(BattleAction action, BattleSettings settings)
        {
            List<ActionResult> results = new List<ActionResult>();
            float variance = settings != null ? settings.damageVariance : 0.1f;
            float maxGauge = settings != null ? settings.maxGauge : 100f;
            Stats userStats = action.User.Stats;
            AbilityData ability = action.Ability;

            foreach (Combatant target in action.Targets)
            {
                if (!target.IsAlive) continue;

                switch (ability.effect)
                {
                    case AbilityEffect.None:
                        break;
                    case AbilityEffect.Heal:
                    {
                        int amount = DamageCalculator.CalculateHeal(userStats, ability, variance);
                        results.Add(new ActionResult(target, target.Heal(amount), ActionResultType.Heal, false));
                        break;
                    }
                    case AbilityEffect.RestoreMP:
                    {
                        int amount = DamageCalculator.CalculateMPRestore(userStats, ability, variance);
                        results.Add(new ActionResult(target, target.RestoreMP(amount), ActionResultType.RestoreMP, false));
                        break;
                    }
                    default:
                    {
                        int amount = DamageCalculator.CalculateDamage(userStats, target.Stats, ability, variance);
                        int dealt = target.TakeDamage(amount);
                        results.Add(new ActionResult(target, dealt, ActionResultType.Damage, !target.IsAlive));
                        break;
                    }
                }

                if (!target.IsAlive) continue;

                if (ability.atbReduction > 0f)
                {
                    float before = target.Gauge;
                    target.Gauge = Mathf.Max(-maxGauge, target.Gauge - ability.atbReduction);
                    results.Add(new ActionResult(target, Mathf.RoundToInt(before - target.Gauge), ActionResultType.ReduceATB, false));
                }

                if (ability.statusEffect != null && (ability.statusChance >= 100f || Random.Range(0f, 100f) < ability.statusChance))
                {
                    target.ApplyStatus(ability.statusEffect, action.User);
                    results.Add(new ActionResult(target, 0, ActionResultType.StatusApplied, false, ability.statusEffect));
                }
            }

            return results;
        }

        static List<ActionResult> ExecuteItem(BattleAction action, BattleSettings settings)
        {
            List<ActionResult> results = new List<ActionResult>();
            ItemData item = action.Item;
            float maxGauge = settings != null ? settings.maxGauge : 100f;

            foreach (Combatant target in action.Targets)
            {
                if (!target.IsAlive) continue;

                if (item.hpRestore > 0)
                    results.Add(new ActionResult(target, target.Heal(item.hpRestore), ActionResultType.Heal, false));

                if (item.mpRestore > 0)
                    results.Add(new ActionResult(target, target.RestoreMP(item.mpRestore), ActionResultType.RestoreMP, false));

                if (item.atbRestore > 0f)
                {
                    float before = target.Gauge;
                    target.Gauge = Mathf.Min(maxGauge, target.Gauge + item.atbRestore);
                    results.Add(new ActionResult(target, Mathf.RoundToInt(target.Gauge - before), ActionResultType.RestoreATB, false));
                }
            }

            return results;
        }
    }
}
