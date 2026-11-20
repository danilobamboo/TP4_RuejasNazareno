using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using RPG.Battle;
using RPG.Data;

namespace RPG.UI
{
    public class CommandMenuView : MonoBehaviour
    {
        public TMP_Text titleLabel;
        public RectTransform buttonContainer;
        public ActionButtonView buttonPrefab;

        readonly List<ActionButtonView> spawned = new List<ActionButtonView>();

        BattleManager battle;
        Combatant user;

        public event Action<Combatant> TargetHighlighted;

        public void Show(BattleManager battleManager, Combatant commander)
        {
            battle = battleManager;
            user = commander;
            gameObject.SetActive(true);
            ShowMainMenu();
        }

        public void Hide()
        {
            user = null;
            Clear();
            TargetHighlighted?.Invoke(null);
            gameObject.SetActive(false);
        }

        void ShowMainMenu()
        {
            Clear();
            titleLabel.text = $"Turno de {user.Name}";

            AbilityData attack = user.BasicAttack;
            string attackDetail = attack.mpCost > 0 ? $"MP {attack.mpCost}" : null;
            Add("Atacar", attackDetail, user.CanAfford(attack), () => SelectAbility(attack, ShowMainMenu));
            Add("Habilidades", null, user.Abilities.Any(), ShowAbilityMenu);
            Add("Items", null, user.HasUsableItems, ShowItemMenu);

            SelectFirst();
        }

        void ShowAbilityMenu()
        {
            Clear();
            titleLabel.text = $"{user.Name} · Habilidades";

            foreach (AbilityData ability in user.Abilities)
            {
                bool usable = user.CanAfford(ability) && TargetSelector.GetCandidates(user, ability, battle.Combatants).Count > 0;
                string detail = ability.mpCost > 0 ? $"MP {ability.mpCost} · ATB {ability.atbCost:0}" : $"ATB {ability.atbCost:0}";
                Add(ability.displayName, detail, usable, () => SelectAbility(ability, ShowAbilityMenu));
            }

            Add("Volver", null, true, ShowMainMenu);
            SelectFirst();
        }

        void ShowItemMenu()
        {
            Clear();
            titleLabel.text = $"{user.Name} · Items";

            foreach (InventorySlot slot in user.Inventory)
            {
                if (slot.Quantity <= 0) continue;

                ItemData item = slot.Item;
                bool usable = TargetSelector.GetCandidates(user, item.targetType, battle.Combatants).Count > 0;
                Add(item.displayName, $"x{slot.Quantity}", usable, () => SelectItem(item));
            }

            Add("Volver", null, true, ShowMainMenu);
            SelectFirst();
        }

        void SelectAbility(AbilityData ability, Action back)
        {
            if (!TargetSelector.IsSingleTarget(ability.targetType))
            {
                SubmitAbility(ability, null);
                return;
            }

            Func<Combatant, string> detail = ability.effect == AbilityEffect.RestoreMP
                ? (Func<Combatant, string>)(t => $"MP {t.CurrentMP}/{t.MaxMP}")
                : t => $"HP {t.CurrentHP}/{t.MaxHP}";

            ShowTargetMenu(ability.displayName, ability.targetType, detail, t => SubmitAbility(ability, t), back);
        }

        void SelectItem(ItemData item)
        {
            if (!TargetSelector.IsSingleTarget(item.targetType))
            {
                SubmitItem(item, null);
                return;
            }

            ShowTargetMenu(item.displayName, item.targetType, DescribeForItem(item), t => SubmitItem(item, t), ShowItemMenu);
        }

        static Func<Combatant, string> DescribeForItem(ItemData item)
        {
            return target =>
            {
                List<string> parts = new List<string>();
                if (item.hpRestore > 0 || (item.mpRestore <= 0 && item.atbRestore <= 0f)) parts.Add($"HP {target.CurrentHP}/{target.MaxHP}");
                if (item.mpRestore > 0) parts.Add($"MP {target.CurrentMP}/{target.MaxMP}");
                return string.Join(" · ", parts);
            };
        }

        void ShowTargetMenu(string actionName, TargetType targetType, Func<Combatant, string> describe, Action<Combatant> submit, Action back)
        {
            Clear();
            titleLabel.text = $"{actionName}: elegí un objetivo";

            foreach (Combatant target in TargetSelector.GetCandidates(user, targetType, battle.Combatants))
            {
                ActionButtonView button = Add(target.Name, describe(target), true, () => submit(target));
                button.Highlighted += () => TargetHighlighted?.Invoke(target);
            }

            ActionButtonView backButton = Add("Volver", null, true, () =>
            {
                TargetHighlighted?.Invoke(null);
                back();
            });
            backButton.Highlighted += () => TargetHighlighted?.Invoke(null);

            SelectFirst();
        }

        void SubmitAbility(AbilityData ability, Combatant target)
        {
            if (battle.SubmitCommand(ability, target)) Hide();
        }

        void SubmitItem(ItemData item, Combatant target)
        {
            if (battle.SubmitItem(item, target)) Hide();
        }

        ActionButtonView Add(string text, string detail, bool interactable, Action onClick)
        {
            ActionButtonView button = Instantiate(buttonPrefab, buttonContainer);
            button.Setup(text, detail, interactable, onClick != null ? () => onClick() : null);
            spawned.Add(button);
            return button;
        }

        void Clear()
        {
            foreach (ActionButtonView button in spawned)
            {
                if (button == null) continue;
                button.gameObject.SetActive(false);
                Destroy(button.gameObject);
            }
            spawned.Clear();
        }

        void SelectFirst()
        {
            if (EventSystem.current == null) return;

            ActionButtonView first = spawned.FirstOrDefault(b => b.button.interactable);
            EventSystem.current.SetSelectedGameObject(first != null ? first.gameObject : null);
        }
    }
}
