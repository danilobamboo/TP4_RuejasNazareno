using UnityEngine;
using RPG.Battle;

namespace RPG.Presentation
{
    public class BattleCameraOrbit : MonoBehaviour
    {
        public Transform center;
        public Vector3 centerOffset = Vector3.zero;
        public float degreesPerSecond = 6f;
        public bool useStartingPosition = true;
        [Min(0.1f)] public float radius = 10f;
        public float height = 4f;

        public BattleManager battle;
        public bool pauseWhileChoosing = false;
        [Min(0f)] public float speedSmoothing = 2f;

        float angle;
        float currentSpeed;

        Vector3 Pivot => (center != null ? center.position : Vector3.zero) + centerOffset;

        void Start()
        {
            if (battle == null) battle = FindFirstObjectByType<BattleManager>();

            if (useStartingPosition)
            {
                Vector3 offset = transform.position - Pivot;
                height = offset.y;
                radius = Mathf.Max(0.1f, new Vector2(offset.x, offset.z).magnitude);
                angle = Mathf.Atan2(offset.z, offset.x) * Mathf.Rad2Deg;
            }

            currentSpeed = TargetSpeed();
            ApplyPosition();
        }

        void LateUpdate()
        {
            float target = TargetSpeed();

            currentSpeed = speedSmoothing > 0f
                ? Mathf.Lerp(currentSpeed, target, 1f - Mathf.Exp(-speedSmoothing * Time.deltaTime))
                : target;

            angle = Mathf.Repeat(angle + currentSpeed * Time.deltaTime, 360f);
            ApplyPosition();
        }

        float TargetSpeed()
        {
            bool choosing = battle != null && battle.State == BattleState.WaitingForCommand;
            return pauseWhileChoosing && choosing ? 0f : degreesPerSecond;
        }

        void ApplyPosition()
        {
            float radians = angle * Mathf.Deg2Rad;
            Vector3 orbitOffset = new Vector3(Mathf.Cos(radians) * radius, height, Mathf.Sin(radians) * radius);

            transform.position = Pivot + orbitOffset;
            transform.LookAt(Pivot);
        }

        void OnDrawGizmosSelected()
        {
            Vector3 pivot = Pivot;
            float drawRadius = radius;
            float drawHeight = height;

            if (useStartingPosition && !Application.isPlaying)
            {
                Vector3 offset = transform.position - pivot;
                drawHeight = offset.y;
                drawRadius = new Vector2(offset.x, offset.z).magnitude;
            }

            Gizmos.color = Color.cyan;
            Vector3 previous = pivot + new Vector3(drawRadius, drawHeight, 0f);

            for (int i = 1; i <= 64; i++)
            {
                float radians = i / 64f * Mathf.PI * 2f;
                Vector3 next = pivot + new Vector3(Mathf.Cos(radians) * drawRadius, drawHeight, Mathf.Sin(radians) * drawRadius);
                Gizmos.DrawLine(previous, next);
                previous = next;
            }

            Gizmos.DrawWireSphere(pivot, 0.3f);
        }
    }
}
