using System.Collections.Generic;
using UnityEngine;
using RPG.Battle;

namespace RPG.Presentation
{
    [RequireComponent(typeof(AudioSource))]
    public class TurnSoundPlayer : MonoBehaviour
    {
        public BattleManager battle;
        public bool playForEnemies = true;
        [Range(0f, 1f)] public float volume = 1f;
        [Range(0f, 0.5f)] public float pitchVariance = 0.05f;

        public AudioClip fallbackSound;
        public AudioClip fallbackActionSound;
        public AudioClip fallbackHurtSound;
        public AudioClip fallbackDeathSound;

        [Range(0f, 1f)] public float turnVolume = 1f;
        [Range(0f, 1f)] public float actionVolume = 1f;
        [Range(0f, 1f)] public float hurtVolume = 1f;
        [Range(0f, 1f)] public float deathVolume = 1f;

        readonly List<Combatant> bound = new List<Combatant>();
        AudioSource source;

        void Awake()
        {
            source = GetComponent<AudioSource>();
            source.playOnAwake = false;
            if (battle == null) battle = FindFirstObjectByType<BattleManager>();
        }

        void OnEnable()
        {
            if (battle == null) return;

            battle.BattleStarted += BindCombatants;
            battle.TurnStarted += OnTurnStarted;
            battle.ActionStarted += OnActionStarted;
        }

        void OnDisable()
        {
            if (battle != null)
            {
                battle.BattleStarted -= BindCombatants;
                battle.TurnStarted -= OnTurnStarted;
                battle.ActionStarted -= OnActionStarted;
            }

            UnbindCombatants();
        }

        void Start()
        {
            if (bound.Count == 0 && battle != null && battle.Combatants.Count > 0) BindCombatants();
        }

        void BindCombatants()
        {
            UnbindCombatants();

            foreach (Combatant combatant in battle.Combatants)
            {
                combatant.HPChanged += OnHPChanged;
                combatant.Died += OnDied;
                bound.Add(combatant);
            }
        }

        void UnbindCombatants()
        {
            foreach (Combatant combatant in bound)
            {
                combatant.HPChanged -= OnHPChanged;
                combatant.Died -= OnDied;
            }
            bound.Clear();
        }

        void OnTurnStarted(Combatant combatant)
        {
            Play(combatant, combatant.Data.turnSound, fallbackSound, turnVolume);
        }

        void OnActionStarted(BattleAction action)
        {
            AudioClip clip = action.UseSound != null ? action.UseSound : action.User.Data.actionSound;
            Play(action.User, clip, fallbackActionSound, actionVolume);
        }

        void OnHPChanged(Combatant combatant, int delta)
        {
            if (delta >= 0 || !combatant.IsAlive) return;
            Play(combatant, combatant.Data.hurtSound, fallbackHurtSound, hurtVolume);
        }

        void OnDied(Combatant combatant)
        {
            Play(combatant, combatant.Data.deathSound, fallbackDeathSound, deathVolume);
        }

        void Play(Combatant combatant, AudioClip clip, AudioClip fallback, float clipVolume)
        {
            if (!playForEnemies && combatant.Team == Team.Enemy) return;

            AudioClip chosen = clip != null ? clip : fallback;
            if (chosen == null) return;

            source.pitch = 1f + Random.Range(-pitchVariance, pitchVariance);
            source.PlayOneShot(chosen, volume * clipVolume);
        }
    }
}
