using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using RPG.Data;

namespace RPG.Battle
{
    public enum BattleState
    {
        NotStarted,
        Running,
        WaitingForCommand,
        Executing,
        Finished
    }

    public class BattleManager : MonoBehaviour
    {
        public BattleSettings settings;
        public List<CombatantData> party = new List<CombatantData>();
        public List<CombatantData> enemies = new List<CombatantData>();
        [Min(0f)] public float actionPause = 0.6f;
        public bool startOnStart = true;
        public bool logToConsole = true;

        readonly List<Combatant> combatants = new List<Combatant>();
        readonly List<Combatant> readyQueue = new List<Combatant>();

        public IReadOnlyList<Combatant> Combatants => combatants;
        public IReadOnlyList<Combatant> ReadyQueue => readyQueue;
        public BattleState State { get; private set; } = BattleState.NotStarted;
        public Combatant ActiveCommander { get; private set; }
        public Team? Winner { get; private set; }
        public float MaxGauge => settings != null ? settings.maxGauge : 100f;

        public event Action BattleStarted;
        public event Action<Combatant> TurnStarted;
        public event Action<Combatant> CommandRequested;
        public event Action<BattleAction, IReadOnlyList<ActionResult>> ActionExecuted;
        public event Action<Team> BattleEnded;

        void Start()
        {
            if (startOnStart) StartBattle();
        }

        public void StartBattle()
        {
            if (settings == null)
            {
                Debug.LogError("[BattleManager] Falta asignar BattleSettings.");
                return;
            }

            StopAllCoroutines();
            combatants.Clear();
            readyQueue.Clear();
            ActiveCommander = null;
            Winner = null;

            foreach (CombatantData data in party)
                if (data != null) combatants.Add(new Combatant(data, Team.Party));

            foreach (CombatantData data in enemies)
                if (data != null) combatants.Add(new Combatant(data, Team.Enemy));

            if (!HasAlive(Team.Party) || !HasAlive(Team.Enemy))
            {
                Debug.LogWarning("[BattleManager] Hace falta al menos un combatiente en party y otro en enemies.");
                State = BattleState.NotStarted;
                return;
            }

            if (settings.atbMode == AtbMode.Active)
                Debug.LogWarning("[BattleManager] El modo Active todavía no está implementado. Se usa Wait.");

            foreach (Combatant combatant in combatants)
            {
                combatant.Gauge = UnityEngine.Random.Range(0f, settings.startingGaugeVariance) * MaxGauge;
                combatant.Died += OnCombatantDied;
            }

            State = BattleState.Running;
            Log("=== Comienza la batalla ===");
            BattleStarted?.Invoke();
        }

        void Update()
        {
            if (State != BattleState.Running) return;

            if (readyQueue.Count == 0) FillGauges(Time.deltaTime);
            if (readyQueue.Count > 0) BeginNextTurn();
        }

        void FillGauges(float deltaTime)
        {
            List<Combatant> filled = new List<Combatant>();

            foreach (Combatant combatant in combatants)
            {
                if (!combatant.IsAlive) continue;

                float fill = combatant.Stats.speed * settings.fillRateMultiplier * deltaTime;
                combatant.Gauge = Mathf.Min(MaxGauge, combatant.Gauge + fill);

                if (combatant.Gauge >= MaxGauge) filled.Add(combatant);
            }

            readyQueue.AddRange(filled.OrderByDescending(c => c.Stats.speed));
        }

        void BeginNextTurn()
        {
            Combatant next = readyQueue[0];

            if (next.Team == Team.Enemy)
            {
                readyQueue.RemoveAt(0);
                BattleAction action = EnemyAI.ChooseAction(next, combatants);

                if (action == null)
                {
                    next.Gauge = 0f;
                    return;
                }

                TurnStarted?.Invoke(next);
                StartCoroutine(ExecuteAction(action));
            }
            else
            {
                ActiveCommander = next;
                State = BattleState.WaitingForCommand;
                Log($"Turno de <color=cyan>{next.Name}</color>. Esperando comando...");
                TurnStarted?.Invoke(next);
                CommandRequested?.Invoke(next);
            }
        }

        public bool SubmitCommand(AbilityData ability, Combatant target = null)
        {
            if (State != BattleState.WaitingForCommand || ActiveCommander == null || ability == null) return false;
            if (!ActiveCommander.CanAfford(ability)) return false;

            List<Combatant> targets = TargetSelector.ResolveTargets(ActiveCommander, ability, combatants, target);
            if (targets.Count == 0) return false;

            Combatant user = ActiveCommander;
            readyQueue.Remove(user);
            ActiveCommander = null;

            StartCoroutine(ExecuteAction(new BattleAction(user, ability, targets)));
            return true;
        }

        IEnumerator ExecuteAction(BattleAction action)
        {
            State = BattleState.Executing;

            action.User.SpendMP(action.Ability.mpCost);
            List<ActionResult> results = ActionExecutor.Execute(action, settings);
            action.User.Gauge = -action.Ability.atbCost;
            action.User.TurnsTaken++;

            LogAction(action, results);
            ActionExecuted?.Invoke(action, results);

            if (actionPause > 0f) yield return new WaitForSeconds(actionPause);

            if (CheckBattleEnd()) yield break;

            State = BattleState.Running;
        }

        void OnCombatantDied(Combatant combatant)
        {
            readyQueue.Remove(combatant);
            combatant.Gauge = 0f;
            Log($"<color=red>{combatant.Name} fue derrotado.</color>");
        }

        bool CheckBattleEnd()
        {
            if (!HasAlive(Team.Enemy)) Finish(Team.Party);
            else if (!HasAlive(Team.Party)) Finish(Team.Enemy);

            return State == BattleState.Finished;
        }

        void Finish(Team winner)
        {
            State = BattleState.Finished;
            Winner = winner;
            Log(winner == Team.Party ? "<color=green>=== ¡Victoria! ===</color>" : "<color=red>=== Derrota ===</color>");
            BattleEnded?.Invoke(winner);
        }

        bool HasAlive(Team team)
        {
            return combatants.Any(c => c.Team == team && c.IsAlive);
        }

        void LogAction(BattleAction action, List<ActionResult> results)
        {
            if (!logToConsole) return;

            string color = action.User.Team == Team.Party ? "cyan" : "orange";
            Debug.Log($"<color={color}>{action.User.Name}</color> usa <b>{action.Ability.displayName}</b> (ATB {action.Ability.atbCost}, MP {action.Ability.mpCost}) · MP {action.User.CurrentMP}/{action.User.MaxMP}");

            foreach (ActionResult result in results)
            {
                switch (result.Type)
                {
                    case ActionResultType.Heal:
                        Debug.Log($"    {result.Target.Name} recupera {result.Amount} HP ({result.Target.CurrentHP}/{result.Target.MaxHP})");
                        break;
                    case ActionResultType.RestoreMP:
                        Debug.Log($"    {result.Target.Name} recupera {result.Amount} MP ({result.Target.CurrentMP}/{result.Target.MaxMP})");
                        break;
                    default:
                        Debug.Log($"    {result.Target.Name} recibe {result.Amount} de daño ({result.Target.CurrentHP}/{result.Target.MaxHP})");
                        break;
                }
            }
        }

        void Log(string message)
        {
            if (logToConsole) Debug.Log(message);
        }
    }
}
