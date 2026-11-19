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
        public Image atbFill;
        public Image highlight;
        public CanvasGroup canvasGroup;

        public Color atbFillingColor = new Color(0.3f, 0.7f, 1f);
        public Color atbReadyColor = new Color(1f, 0.85f, 0.2f);
        public Color atbPenaltyColor = new Color(1f, 0.35f, 0.35f);
        public Color activeHighlightColor = new Color(1f, 0.85f, 0.2f, 0.25f);
        public Color targetHighlightColor = new Color(1f, 0.3f, 0.3f, 0.3f);
        [Range(0f, 1f)] public float defeatedAlpha = 0.4f;

        Combatant combatant;
        float maxGauge = 100f;

        public Combatant Combatant => combatant;

        public void Bind(Combatant target, float gaugeMax)
        {
            combatant = target;
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

        void Update()
        {
            if (combatant != null) Refresh();
        }

        void Refresh()
        {
            SetFill(hpFill, combatant.HPPercent);

            if (hpLabel != null) hpLabel.text = $"{combatant.CurrentHP}/{combatant.MaxHP}";

            float gauge = combatant.Gauge / maxGauge;
            SetFill(atbFill, Mathf.Abs(gauge));

            if (atbFill != null)
                atbFill.color = gauge < 0f ? atbPenaltyColor : gauge >= 1f ? atbReadyColor : atbFillingColor;

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
