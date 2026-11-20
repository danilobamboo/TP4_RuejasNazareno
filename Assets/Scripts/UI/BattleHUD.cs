using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using RPG.Battle;

namespace RPG.UI
{
    public class BattleHUD : MonoBehaviour
    {
        public BattleManager battle;
        public RectTransform partyContainer;
        public RectTransform enemyContainer;
        public CombatantStatusView statusPrefab;
        public CommandMenuView commandMenu;
        public TMP_Text logLabel;
        public GameObject resultPanel;
        public TMP_Text resultLabel;
        public Button restartButton;
        [Min(1)] public int logLines = 4;
        [Min(0f)] public float restartDelay = 1.5f;

        readonly List<CombatantStatusView> views = new List<CombatantStatusView>();
        readonly Queue<string> logEntries = new Queue<string>();

        Coroutine restartRoutine;
        Combatant activeCommander;
        Combatant highlightedTarget;

        void Awake()
        {
            if (battle == null) battle = FindFirstObjectByType<BattleManager>();
            if (commandMenu != null) commandMenu.Hide();
            if (resultPanel != null) resultPanel.SetActive(false);
        }

        void OnEnable()
        {
            if (battle == null) return;

            battle.BattleStarted += OnBattleStarted;
            battle.CommandRequested += OnCommandRequested;
            battle.ActionExecuted += OnActionExecuted;
            battle.BattleEnded += OnBattleEnded;
            battle.StatusTicked += OnStatusTicked;

            if (commandMenu != null) commandMenu.TargetHighlighted += OnTargetHighlighted;
            if (restartButton != null) restartButton.onClick.AddListener(Restart);
        }

        void OnDisable()
        {
            if (battle == null) return;

            battle.BattleStarted -= OnBattleStarted;
            battle.CommandRequested -= OnCommandRequested;
            battle.ActionExecuted -= OnActionExecuted;
            battle.BattleEnded -= OnBattleEnded;
            battle.StatusTicked -= OnStatusTicked;

            if (commandMenu != null) commandMenu.TargetHighlighted -= OnTargetHighlighted;
            if (restartButton != null) restartButton.onClick.RemoveListener(Restart);
        }

        void Start()
        {
            if (battle == null)
            {
                Debug.LogError("[BattleHUD] No se encontró un BattleManager en la escena.");
                return;
            }

            if (views.Count == 0 && battle.Combatants.Count > 0) BuildViews();
        }

        void OnBattleStarted()
        {
            if (restartRoutine != null) StopCoroutine(restartRoutine);
            restartRoutine = null;

            BuildViews();
            logEntries.Clear();
            AddLog("¡Comienza la batalla!");
            if (resultPanel != null) resultPanel.SetActive(false);
            commandMenu.Hide();
        }

        void OnCommandRequested(Combatant commander)
        {
            activeCommander = commander;
            RefreshHighlights();
            commandMenu.Show(battle, commander);
        }

        void OnActionExecuted(BattleAction action, IReadOnlyList<ActionResult> results)
        {
            activeCommander = null;
            highlightedTarget = null;
            RefreshHighlights();

            string color = action.User.Team == Team.Party ? "#6EC6FF" : "#FFA94D";
            string line = $"<color={color}>{action.User.Name}</color> usa <b>{action.DisplayName}</b>";

            if (results.Count > 0)
            {
                IEnumerable<string> parts = results.Select(r => r.Type switch
                {
                    ActionResultType.Heal => $"{r.Target.Name} <color=#7CFC9A>+{r.Amount} HP</color>",
                    ActionResultType.RestoreMP => $"{r.Target.Name} <color=#7FB8FF>+{r.Amount} MP</color>",
                    ActionResultType.RestoreATB => $"{r.Target.Name} <color=#FFE066>+{r.Amount} ATB</color>",
                    ActionResultType.ReduceATB => $"{r.Target.Name} <color=#FFB347>-{r.Amount} ATB</color>",
                    ActionResultType.StatusApplied => $"{r.Target.Name} <color=#{ColorUtility.ToHtmlStringRGB(r.Status.color)}>{r.Status.displayName}</color>",
                    _ => $"{r.Target.Name} <color=#FF8A8A>-{r.Amount}</color>"
                });
                line += ": " + string.Join(", ", parts);
            }

            AddLog(line);

            foreach (ActionResult result in results)
                if (result.Defeated) AddLog($"<color=#FF6B6B>{result.Target.Name} fue derrotado.</color>");
        }

        void OnStatusTicked(Combatant combatant, StatusTick tick)
        {
            string color = ColorUtility.ToHtmlStringRGB(tick.Status.Effect.color);
            string status = $"<color=#{color}>{tick.Status.Effect.displayName}</color>";

            if (tick.Damage > 0) AddLog($"{combatant.Name} sufre <color=#FF8A8A>-{tick.Damage}</color> por {status}");
            if (!combatant.IsAlive) AddLog($"<color=#FF6B6B>{combatant.Name} fue derrotado.</color>");
            else if (tick.Expired) AddLog($"{status} de {combatant.Name} terminó");
        }

        void OnBattleEnded(Team winner)
        {
            commandMenu.Hide();
            activeCommander = null;
            highlightedTarget = null;
            RefreshHighlights();

            if (resultPanel == null) return;

            resultPanel.SetActive(true);
            if (resultLabel != null) resultLabel.text = winner == Team.Party ? "¡Victoria!" : "Derrota";
            if (restartButton != null)
            {
                if (restartRoutine != null) StopCoroutine(restartRoutine);
                restartRoutine = StartCoroutine(EnableRestartAfterDelay());
            }
        }

        System.Collections.IEnumerator EnableRestartAfterDelay()
        {
            restartButton.interactable = false;
            UnityEngine.EventSystems.EventSystem.current?.SetSelectedGameObject(null);

            if (restartDelay > 0f) yield return new WaitForSecondsRealtime(restartDelay);

            restartButton.interactable = true;
            UnityEngine.EventSystems.EventSystem.current?.SetSelectedGameObject(restartButton.gameObject);
            restartRoutine = null;
        }

        void OnTargetHighlighted(Combatant target)
        {
            highlightedTarget = target;
            RefreshHighlights();
        }

        void Restart()
        {
            battle.StartBattle();
        }

        void BuildViews()
        {
            foreach (CombatantStatusView view in views)
                if (view != null) Destroy(view.gameObject);
            views.Clear();

            foreach (Combatant combatant in battle.Combatants)
            {
                RectTransform container = combatant.Team == Team.Party ? partyContainer : enemyContainer;
                CombatantStatusView view = Instantiate(statusPrefab, container);
                view.Bind(combatant, battle.MaxGauge);
                views.Add(view);
            }

            activeCommander = null;
            highlightedTarget = null;
        }

        void RefreshHighlights()
        {
            foreach (CombatantStatusView view in views)
            {
                if (view.Combatant == highlightedTarget) view.SetHighlight(StatusHighlight.Target);
                else if (view.Combatant == activeCommander) view.SetHighlight(StatusHighlight.Active);
                else view.SetHighlight(StatusHighlight.None);
            }
        }

        void AddLog(string line)
        {
            logEntries.Enqueue(line);
            while (logEntries.Count > logLines) logEntries.Dequeue();
            if (logLabel != null) logLabel.text = string.Join("\n", logEntries);
        }
    }
}
