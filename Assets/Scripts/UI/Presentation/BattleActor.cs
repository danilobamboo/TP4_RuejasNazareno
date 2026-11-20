using System.Collections.Generic;
using UnityEngine;
using RPG.Battle;
using RPG.Data;

namespace RPG.Presentation
{
    public class BattleActor : MonoBehaviour
    {
        static readonly List<BattleActor> actors = new List<BattleActor>();

        public CombatantData data;
        public Team team = Team.Party;
        public Transform faceTarget;
        public Vector3 portraitOffset = new Vector3(0f, 0.1f, 1.2f);
        [Range(5f, 90f)] public float portraitFieldOfView = 30f;

        public Transform FaceTarget => faceTarget != null ? faceTarget : transform;

        public Vector3 PortraitCameraPosition => FaceTarget.position + transform.rotation * portraitOffset;

        void OnEnable()
        {
            if (!actors.Contains(this)) actors.Add(this);
        }

        void OnDisable()
        {
            actors.Remove(this);
        }

        public static BattleActor Find(Combatant combatant)
        {
            if (combatant == null) return null;

            BattleActor fallback = null;

            foreach (BattleActor actor in actors)
            {
                if (actor.data != combatant.Data) continue;
                if (actor.team == combatant.Team) return actor;
                if (fallback == null) fallback = actor;
            }

            return fallback;
        }

        void OnDrawGizmosSelected()
        {
            Vector3 cameraPosition = PortraitCameraPosition;
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(cameraPosition, 0.08f);
            Gizmos.DrawLine(cameraPosition, FaceTarget.position);
        }
    }
}
