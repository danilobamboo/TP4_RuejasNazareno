using System;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace RPG.UI
{
    [RequireComponent(typeof(Button))]
    public class ActionButtonView : MonoBehaviour, ISelectHandler, IPointerEnterHandler
    {
        public Button button;
        public TMP_Text label;
        public TMP_Text detail;
        [Range(0f, 1f)] public float disabledTextAlpha = 0.4f;

        public event Action Highlighted;

        void Reset()
        {
            button = GetComponent<Button>();
            label = GetComponentInChildren<TMP_Text>();
        }

        public void Setup(string text, string detailText, bool interactable, UnityAction onClick)
        {
            label.text = text;

            if (detail != null)
            {
                detail.text = detailText ?? string.Empty;
                detail.gameObject.SetActive(!string.IsNullOrEmpty(detailText));
            }

            button.interactable = interactable;
            button.onClick.RemoveAllListeners();
            if (onClick != null) button.onClick.AddListener(onClick);

            float alpha = interactable ? 1f : disabledTextAlpha;
            SetAlpha(label, alpha);
            SetAlpha(detail, alpha);

            Highlighted = null;
        }

        public void OnSelect(BaseEventData eventData)
        {
            Highlighted?.Invoke();
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (button.interactable) Highlighted?.Invoke();
        }

        static void SetAlpha(TMP_Text text, float alpha)
        {
            if (text == null) return;
            Color color = text.color;
            color.a = alpha;
            text.color = color;
        }
    }
}
