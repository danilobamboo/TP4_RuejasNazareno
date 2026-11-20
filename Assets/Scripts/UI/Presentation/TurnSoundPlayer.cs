using UnityEngine;
using RPG.Battle;

namespace RPG.Presentation
{
    [RequireComponent(typeof(AudioSource))]
    public class TurnSoundPlayer : MonoBehaviour
    {
        public BattleManager battle;
        public AudioClip fallbackSound;
        public bool playForEnemies = true;
        [Range(0f, 1f)] public float volume = 1f;
        [Range(0f, 0.5f)] public float pitchVariance = 0.05f;

        AudioSource source;

        void Awake()
        {
            source = GetComponent<AudioSource>();
            source.playOnAwake = false;
            if (battle == null) battle = FindFirstObjectByType<BattleManager>();
        }

        void OnEnable()
        {
            if (battle != null) battle.TurnStarted += OnTurnStarted;
        }

        void OnDisable()
        {
            if (battle != null) battle.TurnStarted -= OnTurnStarted;
        }

        void OnTurnStarted(Combatant combatant)
        {
            if (!playForEnemies && combatant.Team == Team.Enemy) return;

            AudioClip clip = combatant.Data.turnSound != null ? combatant.Data.turnSound : fallbackSound;
            if (clip == null) return;

            source.pitch = 1f + Random.Range(-pitchVariance, pitchVariance);
            source.PlayOneShot(clip, volume);
        }
    }
}
