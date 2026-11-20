using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using RPG.Battle;

namespace RPG.Presentation
{
    [RequireComponent(typeof(BattleActor))]
    public class BattleActorReactions : MonoBehaviour
    {
        static readonly string[] ColorProperties = { "_DiffuseTint", "_BaseColor", "_Color" };

        public BattleManager battle;
        public Animator animator;

        public string defaultActionTrigger = "Attack";
        public string hitTrigger = "Hit";
        public string deathTrigger = "Die";

        public Color flashColor = new Color(1f, 0.15f, 0.15f);
        [Range(0f, 1f)] public float flashStrength = 0.85f;
        [Min(0f)] public float hitFlashDuration = 0.6f;

        [Min(0f)] public float deathRedDelay = 1.5f;
        [Min(0f)] public float disappearDuration = 0.5f;

        class TintSlot
        {
            public Renderer renderer;
            public int materialIndex;
            public int propertyId;
            public Color original;
        }

        readonly List<TintSlot> slots = new List<TintSlot>();
        readonly HashSet<string> animatorParameters = new HashSet<string>();

        BattleActor actor;
        Combatant combatant;
        MaterialPropertyBlock block;
        Coroutine flashRoutine;
        Coroutine deathRoutine;
        Vector3 originalScale;
        Vector3 originalPosition;
        Quaternion originalRotation;
        Transform animatorTransform;
        Vector3 animatorLocalPosition;
        Quaternion animatorLocalRotation;
        bool hidden;
        Renderer[] renderers;

        void Awake()
        {
            actor = GetComponent<BattleActor>();
            if (battle == null) battle = FindFirstObjectByType<BattleManager>();
            if (animator == null) animator = GetComponentInChildren<Animator>();

            block = new MaterialPropertyBlock();
            originalScale = transform.localScale;
            originalPosition = transform.localPosition;
            originalRotation = transform.localRotation;

            if (animator != null && animator.transform != transform)
            {
                animatorTransform = animator.transform;
                animatorLocalPosition = animatorTransform.localPosition;
                animatorLocalRotation = animatorTransform.localRotation;
            }

            renderers = GetComponentsInChildren<Renderer>(true).Where(r => !(r is ParticleSystemRenderer)).ToArray();
            CacheTintSlots();
            CacheAnimatorParameters();
        }

        void OnEnable()
        {
            if (battle == null) return;

            battle.BattleStarted += OnBattleStarted;
            battle.ActionStarted += OnActionStarted;
        }

        void OnDisable()
        {
            if (battle != null)
            {
                battle.BattleStarted -= OnBattleStarted;
                battle.ActionStarted -= OnActionStarted;
            }

            Unbind();
        }

        void Start()
        {
            if (combatant == null && battle != null && battle.Combatants.Count > 0) Bind();
        }

        void OnBattleStarted()
        {
            StopAllCoroutines();
            flashRoutine = null;
            deathRoutine = null;

            transform.localScale = originalScale;
            transform.localPosition = originalPosition;
            transform.localRotation = originalRotation;

            if (animatorTransform != null)
            {
                animatorTransform.localPosition = animatorLocalPosition;
                animatorTransform.localRotation = animatorLocalRotation;
            }

            if (hidden) SetRenderersVisible(true);
            ApplyTint(0f);

            if (animator != null)
            {
                bool wasEnabled = animator.enabled;
                animator.enabled = true;

                foreach (string trigger in animatorParameters) animator.ResetTrigger(trigger);
                animator.Rebind();
                animator.Update(0f);

                animator.enabled = wasEnabled;
            }

            Bind();
        }

        void Bind()
        {
            Unbind();

            combatant = battle.Combatants.FirstOrDefault(c => c.Data == actor.data && c.Team == actor.team)
                ?? battle.Combatants.FirstOrDefault(c => c.Data == actor.data);

            if (combatant == null)
            {
                Debug.LogWarning($"[BattleActorReactions] '{name}' no corresponde a ningún combatiente de la batalla.");
                return;
            }

            combatant.HPChanged += OnHPChanged;
            combatant.Died += OnDied;
        }

        void Unbind()
        {
            if (combatant == null) return;

            combatant.HPChanged -= OnHPChanged;
            combatant.Died -= OnDied;
            combatant = null;
        }

        void OnActionStarted(BattleAction action)
        {
            if (action.User != combatant) return;

            string trigger = string.IsNullOrEmpty(action.AnimationTrigger)
                ? defaultActionTrigger
                : action.AnimationTrigger;

            SetTrigger(trigger);
        }

        void OnHPChanged(Combatant source, int delta)
        {
            if (delta >= 0 || !source.IsAlive) return;

            SetTrigger(hitTrigger);

            if (flashRoutine != null) StopCoroutine(flashRoutine);
            flashRoutine = StartCoroutine(HitFlash());
        }

        void OnDied(Combatant source)
        {
            if (flashRoutine != null) StopCoroutine(flashRoutine);
            flashRoutine = null;

            SetTrigger(deathTrigger);

            if (deathRoutine != null) StopCoroutine(deathRoutine);
            deathRoutine = StartCoroutine(DeathSequence());
        }

        IEnumerator HitFlash()
        {
            ApplyTint(flashStrength);

            for (float t = 0f; t < 1f; t += Time.deltaTime / Mathf.Max(0.01f, hitFlashDuration))
            {
                ApplyTint(Mathf.Lerp(flashStrength, 0f, t * t));
                yield return null;
            }

            ApplyTint(0f);
            flashRoutine = null;
        }

        IEnumerator DeathSequence()
        {
            ApplyTint(flashStrength);

            if (deathRedDelay > 0f) yield return new WaitForSeconds(deathRedDelay);

            for (float t = 0f; t < 1f; t += Time.deltaTime / Mathf.Max(0.01f, disappearDuration))
            {
                transform.localScale = Vector3.Lerp(originalScale, Vector3.zero, t * t);
                yield return null;
            }

            SetRenderersVisible(false);
            transform.localScale = originalScale;
            deathRoutine = null;
        }

        void SetTrigger(string trigger)
        {
            if (animator == null || string.IsNullOrEmpty(trigger)) return;
            if (!animatorParameters.Contains(trigger)) return;

            animator.SetTrigger(trigger);
        }

        void CacheAnimatorParameters()
        {
            animatorParameters.Clear();
            if (animator == null || animator.runtimeAnimatorController == null) return;

            foreach (AnimatorControllerParameter parameter in animator.parameters)
                if (parameter.type == AnimatorControllerParameterType.Trigger) animatorParameters.Add(parameter.name);
        }

        void CacheTintSlots()
        {
            slots.Clear();

            foreach (Renderer renderer in renderers)
            {
                Material[] materials = renderer.sharedMaterials;
                for (int i = 0; i < materials.Length; i++)
                {
                    Material material = materials[i];
                    if (material == null) continue;

                    foreach (string property in ColorProperties)
                    {
                        if (!material.HasProperty(property)) continue;

                        int id = Shader.PropertyToID(property);
                        slots.Add(new TintSlot
                        {
                            renderer = renderer,
                            materialIndex = i,
                            propertyId = id,
                            original = material.GetColor(id)
                        });
                        break;
                    }
                }
            }
        }

        void ApplyTint(float amount)
        {
            foreach (TintSlot slot in slots)
            {
                if (slot.renderer == null) continue;

                slot.renderer.GetPropertyBlock(block, slot.materialIndex);
                Color target = flashColor;
                target.a = slot.original.a;
                block.SetColor(slot.propertyId, Color.Lerp(slot.original, target, amount));
                slot.renderer.SetPropertyBlock(block, slot.materialIndex);
            }
        }

        void SetRenderersVisible(bool visible)
        {
            hidden = !visible;

            foreach (Renderer target in renderers)
                if (target != null) target.enabled = visible;
        }
    }
}
