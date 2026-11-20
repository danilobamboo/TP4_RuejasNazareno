using TMPro;
using UnityEngine;
using UnityEngine.UI;
using RPG.Battle;

namespace RPG.UI
{
    public enum StatusHighlight
    {
        None,
        Active,
        Target
    }

    public class CombatantStatusView : MonoBehaviour
    {
        public TMP_Text nameLabel;
        public TMP_Text hpLabel;
        public Image hpFill;
        public TMP_Text mpLabel;
        public Image mpFill;
        public Image atbFill;
        public Image highlight;
        public TMP_Text statusLabel;
        public bool autoCreateStatusLabel = true;
        public CanvasGroup canvasGroup;

        public Color atbFillingColor = new Color(0.3f, 0.7f, 1f);
        public Color atbReadyColor = new Color(1f, 0.85f, 0.2f);
        public Color atbPenaltyColor = new Color(1f, 0.35f, 0.35f);
        public Color activeHighlightColor = new Color(1f, 0.85f, 0.2f, 0.25f);
        public Color targetHighlightColor = new Color(1f, 0.3f, 0.3f, 0.3f);
        [Range(0f, 1f)] public float defeatedAlpha = 0.4f;

        Combatant combatant;
        float maxGauge = 100f;
        bool statusDirty = true;

        public Combatant Combatant => combatant;

        public void Bind(Combatant target, float gaugeMax)
        {
            if (combatant != null) combatant.StatusesChanged -= OnStatusesChanged;

            combatant = target;
            combatant.StatusesChanged += OnStatusesChanged;
            statusDirty = true;
            if (statusLabel == null && autoCreateStatusLabel) CreateStatusLabel();
            maxGauge = Mathf.Max(1f, gaugeMax);
            nameLabel.text = target.Name;
            SetHighlight(StatusHighlight.None);
            Refresh();
        }

        public void SetHighlight(StatusHighlight mode)
        {
            if (highlight == null) return;

            highlight.enabled = mode != StatusHighlight.None;
            highlight.color = mode == StatusHighlight.Target ? targetHighlightColor : activeHighlightColor;
        }

        void OnDestroy()
        {
            if (combatant != null) combatant.StatusesChanged -= OnStatusesChanged;
        }

        void OnStatusesChanged(Combatant source)
        {
            statusDirty = true;
        }

        void Update()
        {
            if (combatant != null) Refresh();
        }

        void RefreshStatuses()
        {
            statusDirty = false;
            if (statusLabel == null) return;

            System.Text.StringBuilder builder = new System.Text.StringBuilder();
            foreach (StatusInstance status in combatant.Statuses)
            {
                if (builder.Length > 0) builder.Append("  ");
                builder.Append("<color=#").Append(ColorUtility.ToHtmlStringRGB(status.Effect.color)).Append('>')
                    .Append(status.Effect.shortName).Append(' ').Append(status.RemainingTurns).Append("</color>");
            }

            statusLabel.text = builder.ToString();
        }

        void CreateStatusLabel()
        {
            if (atbFill == null || atbFill.rectTransform.parent == null) return;

            GameObject go = new GameObject("StatusText", typeof(RectTransform));
            RectTransform rect = (RectTransform)go.transform;
            rect.SetParent(atbFill.rectTransform.parent, false);
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(0f, 0f);
            rect.anchoredPosition = new Vector2(0f, 1f);
            rect.sizeDelta = new Vector2(0f, 18f);

            TextMeshProUGUI label = go.AddComponent<TextMeshProUGUI>();
            if (nameLabel != null)
            {
                label.font = nameLabel.font;
                label.fontSharedMaterial = nameLabel.fontSharedMaterial;
            }
            label.fontSize = 16f;
            label.alignment = TextAlignmentOptions.BottomLeft;
            label.textWrappingMode = TextWrappingModes.NoWrap;
            label.overflowMode = TextOverflowModes.Ellipsis;
            label.raycastTarget = false;
            label.richText = true;

            statusLabel = label;
        }

        void Refresh()
        {
            SetFill(hpFill, combatant.HPPercent);

            if (hpLabel != null) hpLabel.text = $"{combatant.CurrentHP}/{combatant.MaxHP}";

            bool hasMP = combatant.MaxMP > 0;
            SetFill(mpFill, hasMP ? combatant.MPPercent : 0f);
            if (mpLabel != null) mpLabel.text = hasMP ? $"{combatant.CurrentMP}/{combatant.MaxMP}" : "-";

            float gauge = combatant.Gauge / maxGauge;
            SetFill(atbFill, Mathf.Abs(gauge));

            if (atbFill != null)
                atbFill.color = gauge < 0f ? atbPenaltyColor : gauge >= 1f ? atbReadyColor : atbFillingColor;

            if (statusDirty) RefreshStatuses();

            if (canvasGroup != null) canvasGroup.alpha = combatant.IsAlive ? 1f : defeatedAlpha;
        }

        static void SetFill(Image fill, float percent)
        {
            if (fill == null) return;

            RectTransform rect = fill.rectTransform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = new Vector2(Mathf.Clamp01(percent), 1f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }
    }
}
