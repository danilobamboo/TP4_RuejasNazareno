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
            EnemyAIProfile profile = user.Data.aiProfile;
            if (profile == null) return ChooseRandomAction(user, combatants);

            foreach (AIRule rule in profile.rules)
            {
                if (rule == null) continue;
                if (!IsConditionMet(rule, user, combatants)) continue;
                if (rule.chance < 100f && Random.Range(0f, 100f) >= rule.chance) continue;

                AbilityData ability = rule.ability != null ? rule.ability : user.BasicAttack;
                BattleAction action = BuildAction(user, ability, rule.targetStrategy, combatants, rule);
                if (action != null) return action;
            }

            return BuildAction(user, user.BasicAttack, profile.fallbackTargetStrategy, combatants, null)
                ?? ChooseRandomAction(user, combatants);
        }

        static bool IsConditionMet(AIRule rule, Combatant user, IReadOnlyList<Combatant> combatants)
        {
            switch (rule.condition)
            {
                case AICondition.Always:
                    return true;
                case AICondition.SelfHPBelow:
                    return user.HPPercent < rule.hpThreshold;
                case AICondition.AllyHPBelow:
                    return Allies(user, combatants).Any(c => c.HPPercent < rule.hpThreshold);
                case AICondition.EnemyHPBelow:
                    return Opponents(user, combatants).Any(c => c.HPPercent < rule.hpThreshold);
                case AICondition.AlliesAliveAtMost:
                    return Allies(user, combatants).Count() <= rule.count;
                case AICondition.EnemiesAliveAtLeast:
                    return Opponents(user, combatants).Count() >= rule.count;
                case AICondition.EveryNTurns:
                    return (user.TurnsTaken + 1) % rule.count == 0;
                default:
                    return false;
            }
        }

        static BattleAction BuildAction(Combatant user, AbilityData ability, AITargetStrategy strategy, IReadOnlyList<Combatant> combatants, AIRule rule)
        {
            List<Combatant> candidates = TargetSelector.GetCandidates(user, ability, combatants);
            if (candidates.Count == 0) return null;

            Combatant chosen = null;

            if (TargetSelector.IsSingleTarget(ability.targetType))
            {
                if (rule != null && (rule.condition == AICondition.AllyHPBelow || rule.condition == AICondition.EnemyHPBelow))
                {
                    List<Combatant> belowThreshold = candidates.Where(c => c.HPPercent < rule.hpThreshold).ToList();
                    if (belowThreshold.Count > 0) candidates = belowThreshold;
                }

                chosen = PickTarget(strategy, candidates);
            }

            List<Combatant> targets = TargetSelector.ResolveTargets(user, ability, combatants, chosen);
            return targets.Count > 0 ? new BattleAction(user, ability, targets) : null;
        }

        static Combatant PickTarget(AITargetStrategy strategy, List<Combatant> candidates)
        {
            switch (strategy)
            {
                case AITargetStrategy.LowestHP:
                    return candidates.OrderBy(c => c.CurrentHP).First();
                case AITargetStrategy.LowestHPPercent:
                    return candidates.OrderBy(c => c.HPPercent).First();
                case AITargetStrategy.HighestHP:
                    return candidates.OrderByDescending(c => c.CurrentHP).First();
                case AITargetStrategy.Strongest:
                    return candidates.OrderByDescending(c => Mathf.Max(c.Stats.attack, c.Stats.magic)).First();
                case AITargetStrategy.Fastest:
                    return candidates.OrderByDescending(c => c.Stats.speed).First();
                default:
                    return candidates[Random.Range(0, candidates.Count)];
            }
        }

        static IEnumerable<Combatant> Allies(Combatant user, IReadOnlyList<Combatant> combatants)
        {
            return combatants.Where(c => c.IsAlive && c.Team == user.Team);
        }

        static IEnumerable<Combatant> Opponents(Combatant user, IReadOnlyList<Combatant> combatants)
        {
            return combatants.Where(c => c.IsAlive && c.Team != user.Team);
        }

        static BattleAction ChooseRandomAction(Combatant user, IReadOnlyList<Combatant> combatants)
        {
            List<AbilityData> options = new List<AbilityData> { user.BasicAttack };
            options.AddRange(user.Abilities);
            options = options.Where(a => TargetSelector.GetCandidates(user, a, combatants).Count > 0).ToList();

            if (options.Count == 0) return null;

            AbilityData ability = options[Random.Range(0, options.Count)];
            AITargetStrategy strategy = ability.effect == AbilityEffect.Heal ? AITargetStrategy.LowestHPPercent : AITargetStrategy.Random;

            return BuildAction(user, ability, strategy, combatants, null);
        }
    }
}
