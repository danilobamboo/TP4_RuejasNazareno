using System;
using UnityEngine;

namespace RPG.Data
{
    [CreateAssetMenu(fileName = "NewItem", menuName = "RPG/Item")]
    public class ItemData : ScriptableObject
    {
        public string displayName = "New Item";
        [TextArea] public string description;
        public Sprite icon;

        public TargetType targetType = TargetType.SingleAlly;

        [Min(0)] public int hpRestore = 0;
        [Min(0)] public int mpRestore = 0;
        [Min(0f)] public float atbRestore = 0f;

        [Min(0f)] public float atbCost = 0f;
        public string animationTrigger = "Item";
        [Min(0f)] public float impactDelay = 0f;
        public AudioClip useSound;
    }

    [Serializable]
    public class ItemEntry
    {
        public ItemData item;
        [Min(0)] public int quantity = 1;
    }
}
