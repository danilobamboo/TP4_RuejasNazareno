using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using RPG.Battle;
using RPG.Presentation;

namespace RPG.UI
{
    public class TurnPortraitView : MonoBehaviour
    {
        public BattleManager battle;
        public Camera portraitCamera;
        public RawImage display;
        public RectTransform panel;
        public RectTransform hiddenAnchor;
        public RectTransform shownAnchor;
        [Min(64)] public int textureSize = 512;
        [Min(0.01f)] public float showDuration = 0.4f;
        [Min(0.01f)] public float hideDuration = 0.3f;
        [Range(0.1f, 1f)] public float hiddenScale = 0.6f;
        public bool showForEnemies = false;

        RenderTexture renderTexture;
        BattleActor currentActor;
        Combatant currentCombatant;
        Coroutine activeRoutine;

        void Awake()
        {
            if (battle == null) battle = FindFirstObjectByType<BattleManager>();
            if (portraitCamera != null) portraitCamera.enabled = false;
            SnapToHidden();
        }

        void OnEnable()
        {
            if (battle == null) return;

            battle.TurnStarted += OnTurnStarted;
            battle.ActionExecuted += OnActionExecuted;
            battle.BattleStarted += OnBattleReset;
            battle.BattleEnded += OnBattleEnded;
        }

        void OnDisable()
        {
            if (battle == null) return;

            battle.TurnStarted -= OnTurnStarted;
            battle.ActionExecuted -= OnActionExecuted;
            battle.BattleStarted -= OnBattleReset;
            battle.BattleEnded -= OnBattleEnded;
        }

        void OnDestroy()
        {
            if (renderTexture == null) return;

            if (portraitCamera != null) portraitCamera.targetTexture = null;
            renderTexture.Release();
            Destroy(renderTexture);
        }

        void LateUpdate()
        {
            if (currentActor == null || portraitCamera == null || !portraitCamera.enabled) return;

            portraitCamera.transform.position = currentActor.PortraitCameraPosition;
            portraitCamera.transform.LookAt(currentActor.FaceTarget.position);
            portraitCamera.fieldOfView = currentActor.portraitFieldOfView;
        }

        void OnTurnStarted(Combatant combatant)
        {
            if (!showForEnemies && combatant.Team == Team.Enemy) return;

            BattleActor actor = BattleActor.Find(combatant);
            if (actor == null)
            {
                Debug.LogWarning($"[TurnPortraitView] No hay un BattleActor en la escena para '{combatant.Name}'.");
                return;
            }

            currentActor = actor;
            currentCombatant = combatant;

            EnsureRenderTexture();
            portraitCamera.enabled = true;
            LateUpdate();

            Play(Show());
        }

        void OnActionExecuted(BattleAction action, IReadOnlyList<ActionResult> results)
        {
            if (action.User == currentCombatant) HidePortrait();
        }

        void OnBattleEnded(Team winner)
        {
            HidePortrait();
        }

        void OnBattleReset()
        {
            if (activeRoutine != null) StopCoroutine(activeRoutine);
            activeRoutine = null;
            currentActor = null;
            currentCombatant = null;
            if (portraitCamera != null) portraitCamera.enabled = false;
            SnapToHidden();
        }

        void HidePortrait()
        {
            if (currentCombatant == null) return;

            currentCombatant = null;
            Play(Hide());
        }

        void Play(IEnumerator routine)
        {
            if (activeRoutine != null) StopCoroutine(activeRoutine);
            activeRoutine = StartCoroutine(routine);
        }

        IEnumerator Show()
        {
            panel.gameObject.SetActive(true);
            Vector3 startPosition = panel.position;
            Vector3 startScale = panel.localScale;

            for (float t = 0f; t < 1f; t += Time.unscaledDeltaTime / showDuration)
            {
                float eased = EaseOutBack(t);
                panel.position = Vector3.LerpUnclamped(startPosition, shownAnchor.position, eased);
                panel.localScale = Vector3.LerpUnclamped(startScale, Vector3.one, eased);
                yield return null;
            }

            panel.position = shownAnchor.position;
            panel.localScale = Vector3.one;
            activeRoutine = null;
        }

        IEnumerator Hide()
        {
            Vector3 startPosition = panel.position;
            Vector3 startScale = panel.localScale;
            Vector3 endScale = Vector3.one * hiddenScale;

            for (float t = 0f; t < 1f; t += Time.unscaledDeltaTime / hideDuration)
            {
                float eased = t * t;
                panel.position = Vector3.Lerp(startPosition, hiddenAnchor.position, eased);
                panel.localScale = Vector3.Lerp(startScale, endScale, eased);
                yield return null;
            }

            SnapToHidden();
            currentActor = null;
            if (portraitCamera != null) portraitCamera.enabled = false;
            activeRoutine = null;
        }

        void SnapToHidden()
        {
            if (panel == null || hiddenAnchor == null) return;

            panel.position = hiddenAnchor.position;
            panel.localScale = Vector3.one * hiddenScale;
            panel.gameObject.SetActive(false);
        }

        void EnsureRenderTexture()
        {
            if (renderTexture == null)
            {
                renderTexture = new RenderTexture(textureSize, textureSize, 24)
                {
                    name = "TurnPortraitTexture"
                };
                renderTexture.Create();
            }

            portraitCamera.targetTexture = renderTexture;
            display.texture = renderTexture;
        }

        static float EaseOutBack(float t)
        {
            const float overshoot = 1.4f;
            float p = t - 1f;
            return 1f + p * p * ((overshoot + 1f) * p + overshoot);
        }
    }
}
