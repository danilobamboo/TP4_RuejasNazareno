using UnityEngine;
using RPG.Data;

namespace RPG.Battle.Testing
{
    public class DebugCommandPanel : MonoBehaviour
    {
        public BattleManager battle;

        AbilityData selectedAbility;
        AbilityData queuedAbility;
        Combatant queuedTarget;
        bool hasQueuedCommand;
        bool restartRequested;

        void Reset()
        {
            battle = GetComponent<BattleManager>();
        }

        void Awake()
        {
            if (battle == null) battle = GetComponent<BattleManager>();
            if (battle == null) battle = FindFirstObjectByType<BattleManager>();
            Debug.Log($"[DebugCommandPanel] Awake en '{gameObject.name}'. BattleManager encontrado: {battle != null}. Activo: {isActiveAndEnabled}");
        }

        bool loggedGui;

        void Update()
        {
            if (battle == null) return;

            if (hasQueuedCommand)
            {
                battle.SubmitCommand(queuedAbility, queuedTarget);
                hasQueuedCommand = false;
                queuedAbility = null;
                queuedTarget = null;
            }

            if (restartRequested)
            {
                restartRequested = false;
                selectedAbility = null;
                battle.StartBattle();
            }
        }

        void OnGUI()
        {
            if (!loggedGui)
            {
                loggedGui = true;
                Debug.Log($"[DebugCommandPanel] OnGUI se está ejecutando. Pantalla: {Screen.width}x{Screen.height}. BattleManager: {battle != null}");
            }

            if (battle == null) return;

            GUILayout.BeginArea(new Rect(10, 10, 360, Screen.height - 20), GUI.skin.box);
            GUILayout.Label($"Estado: {battle.State}");
            GUILayout.Space(6);

            foreach (Combatant combatant in battle.Combatants)
                DrawCombatant(combatant);

            GUILayout.Space(10);

            if (battle.State == BattleState.WaitingForCommand && battle.ActiveCommander != null && !hasQueuedCommand)
                DrawCommands(battle.ActiveCommander);
            else
                selectedAbility = null;

            if (battle.State == BattleState.Finished)
            {
                GUILayout.Label(battle.Winner == Team.Party ? "¡Victoria!" : "Derrota");
                if (GUILayout.Button("Reiniciar batalla")) restartRequested = true;
            }

            GUILayout.EndArea();
        }

        void DrawCombatant(Combatant combatant)
        {
            GUILayout.BeginHorizontal();

            string tag = combatant.Team == Team.Party ? "[P]" : "[E]";
            GUILayout.Label($"{tag} {combatant.Name}", GUILayout.Width(120));
            GUILayout.Label(combatant.IsAlive ? $"{combatant.CurrentHP}/{combatant.MaxHP}" : "KO", GUILayout.Width(70));

            Rect bar = GUILayoutUtility.GetRect(140, 14, GUILayout.Width(140));
            DrawBar(bar, combatant);

            GUILayout.EndHorizontal();
        }

        void DrawBar(Rect rect, Combatant combatant)
        {
            Color previous = GUI.color;

            GUI.color = new Color(0f, 0f, 0f, 0.6f);
            GUI.DrawTexture(rect, Texture2D.whiteTexture);

            float percent = combatant.Gauge / battle.MaxGauge;

            if (percent >= 0f)
            {
                GUI.color = percent >= 1f ? Color.yellow : new Color(0.3f, 0.7f, 1f);
                GUI.DrawTexture(new Rect(rect.x, rect.y, rect.width * Mathf.Clamp01(percent), rect.height), Texture2D.whiteTexture);
            }
            else
            {
                GUI.color = new Color(1f, 0.3f, 0.3f);
                GUI.DrawTexture(new Rect(rect.x, rect.y, rect.width * Mathf.Clamp01(-percent), rect.height), Texture2D.whiteTexture);
            }

            GUI.color = previous;
        }

        void DrawCommands(Combatant user)
        {
            GUILayout.Label($"Turno de {user.Name}");

            if (selectedAbility == null)
            {
                if (GUILayout.Button("Atacar")) Choose(user, user.BasicAttack);

                foreach (AbilityData ability in user.Abilities)
                {
                    GUI.enabled = TargetSelector.GetCandidates(user, ability, battle.Combatants).Count > 0;
                    if (GUILayout.Button($"{ability.displayName}  (ATB -{ability.atbCost})")) Choose(user, ability);
                    GUI.enabled = true;
                }
            }
            else
            {
                GUILayout.Label($"{selectedAbility.displayName}: elegí un objetivo");

                foreach (Combatant target in TargetSelector.GetCandidates(user, selectedAbility, battle.Combatants))
                {
                    if (GUILayout.Button($"{target.Name}  ({target.CurrentHP}/{target.MaxHP})"))
                        Queue(selectedAbility, target);
                }

                if (GUILayout.Button("Volver")) selectedAbility = null;
            }
        }

        void Choose(Combatant user, AbilityData ability)
        {
            if (TargetSelector.IsSingleTarget(ability.targetType)) selectedAbility = ability;
            else Queue(ability, null);
        }

        void Queue(AbilityData ability, Combatant target)
        {
            queuedAbility = ability;
            queuedTarget = target;
            hasQueuedCommand = true;
            selectedAbility = null;
        }
    }
}
