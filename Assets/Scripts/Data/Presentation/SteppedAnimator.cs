using UnityEngine;

namespace RPG.Presentation
{
    [RequireComponent(typeof(Animator))]
    public class SteppedAnimator : MonoBehaviour
    {
        [Min(1f)] public float framesPerSecond = 12f;
        public bool randomizePhase = true;

        Animator animator;
        float accumulated;

        void Awake()
        {
            animator = GetComponent<Animator>();
        }

        void OnEnable()
        {
            animator.enabled = false;
            accumulated = randomizePhase ? Random.Range(0f, 1f / framesPerSecond) : 0f;
            animator.Update(0f);
        }

        void OnDisable()
        {
            if (animator != null) animator.enabled = true;
        }

        void Update()
        {
            accumulated += Time.deltaTime;

            if (accumulated < 1f / framesPerSecond) return;

            animator.Update(accumulated);
            accumulated = 0f;
        }
    }
}
