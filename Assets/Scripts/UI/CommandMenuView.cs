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
            Add("Defender", "Próximamente", false, null);

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

        void SelectAbility(AbilityData ability, Action back)
        {
            if (TargetSelector.IsSingleTarget(ability.targetType)) ShowTargetMenu(ability, back);
            else Submit(ability, null);
        }

        void ShowTargetMenu(AbilityData ability, Action back)
        {
            Clear();
            titleLabel.text = $"{ability.displayName}: elegí un objetivo";

            foreach (Combatant target in TargetSelector.GetCandidates(user, ability, battle.Combatants))
            {
                string detail = ability.effect == AbilityEffect.RestoreMP
                    ? $"MP {target.CurrentMP}/{target.MaxMP}"
                    : $"HP {target.CurrentHP}/{target.MaxHP}";
                ActionButtonView button = Add(target.Name, detail, true, () => Submit(ability, target));
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

        void Submit(AbilityData ability, Combatant target)
        {
            if (battle.SubmitCommand(ability, target)) Hide();
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
