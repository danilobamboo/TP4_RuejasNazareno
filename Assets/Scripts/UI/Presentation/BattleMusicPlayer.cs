using System.Collections;
using UnityEngine;
using RPG.Battle;

namespace RPG.Presentation
{
    public class BattleMusicPlayer : MonoBehaviour
    {
        public BattleManager battle;

        public AudioClip music;
        [Range(0f, 1f)] public float musicVolume = 0.6f;
        [Min(0f)] public float musicFadeIn = 1f;

        public AudioClip victorySound;
        public AudioClip defeatSound;
        [Range(0f, 1f)] public float resultVolume = 1f;

        [Min(0f)] public float musicFadeOut = 0.3f;
        public bool stopOtherSounds = true;

        AudioSource musicSource;
        AudioSource resultSource;
        Coroutine fadeRoutine;

        void Awake()
        {
            if (battle == null) battle = FindFirstObjectByType<BattleManager>();

            musicSource = CreateSource(true);
            resultSource = CreateSource(false);
        }

        void OnEnable()
        {
            if (battle == null) return;

            battle.BattleStarted += OnBattleStarted;
            battle.BattleEnded += OnBattleEnded;
        }

        void OnDisable()
        {
            if (battle == null) return;

            battle.BattleStarted -= OnBattleStarted;
            battle.BattleEnded -= OnBattleEnded;
        }

        void Start()
        {
            if (battle != null && battle.State != BattleState.NotStarted && battle.State != BattleState.Finished && !musicSource.isPlaying)
                OnBattleStarted();
        }

        void OnBattleStarted()
        {
            StopFade();
            resultSource.Stop();

            if (music == null) return;

            musicSource.clip = music;
            musicSource.volume = musicFadeIn > 0f ? 0f : musicVolume;
            musicSource.Play();

            if (musicFadeIn > 0f) fadeRoutine = StartCoroutine(FadeMusic(musicVolume, musicFadeIn));
        }

        void OnBattleEnded(Team winner)
        {
            StopFade();
            if (stopOtherSounds) StopOtherSources();

            AudioClip clip = winner == Team.Party ? victorySound : defeatSound;
            fadeRoutine = StartCoroutine(EndSequence(clip));
        }

        IEnumerator EndSequence(AudioClip clip)
        {
            if (musicSource.isPlaying && musicFadeOut > 0f) yield return FadeMusic(0f, musicFadeOut);
            musicSource.Stop();

            if (clip != null)
            {
                resultSource.clip = clip;
                resultSource.volume = resultVolume;
                resultSource.Play();
            }

            fadeRoutine = null;
        }

        IEnumerator FadeMusic(float target, float duration)
        {
            float start = musicSource.volume;

            for (float t = 0f; t < 1f; t += Time.unscaledDeltaTime / duration)
            {
                musicSource.volume = Mathf.Lerp(start, target, t);
                yield return null;
            }

            musicSource.volume = target;
        }

        void StopFade()
        {
            if (fadeRoutine != null) StopCoroutine(fadeRoutine);
            fadeRoutine = null;
        }

        void StopOtherSources()
        {
            foreach (AudioSource source in FindObjectsByType<AudioSource>(FindObjectsSortMode.None))
            {
                if (source == musicSource || source == resultSource) continue;
                source.Stop();
            }
        }

        AudioSource CreateSource(bool loop)
        {
            AudioSource source = gameObject.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.loop = loop;
            source.spatialBlend = 0f;
            return source;
        }
    }
}
