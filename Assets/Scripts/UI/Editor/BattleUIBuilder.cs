using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;
using RPG.Battle;
using RPG.UI;

namespace RPG.EditorTools
{
    public static class BattleUIBuilder
    {
        const string PrefabFolder = "Assets/Assets/UI/Prefabs";
        const string ButtonPrefabPath = PrefabFolder + "/ActionButton.prefab";
        const string StatusPrefabPath = PrefabFolder + "/CombatantStatusRow.prefab";
        const string UIFontName = "runescape_uf SDF";

        static readonly Color PanelColor = new Color(0.06f, 0.07f, 0.1f, 0.85f);
        static readonly Color BarBackgroundColor = new Color(0f, 0f, 0f, 0.6f);
        static readonly Color HPColor = new Color(0.3f, 0.85f, 0.4f);
        static readonly Color ATBColor = new Color(0.3f, 0.7f, 1f);
        static readonly Color MPColor = new Color(0.45f, 0.4f, 0.95f);

        [MenuItem("Tools/RPG/Crear UI de batalla")]
        public static void CreateBattleUI()
        {
            if (Resources.Load<TMP_Settings>("TMP Settings") == null)
            {
                EditorUtility.DisplayDialog(
                    "Faltan los recursos de TextMeshPro",
                    "Antes de crear la UI hay que importar los recursos básicos de TextMeshPro.\n\nAndá a Window > TextMeshPro > Import TMP Essential Resources, importalos y volvé a ejecutar esta herramienta.",
                    "Entendido");
                return;
            }

            BattleHUD existing = Object.FindFirstObjectByType<BattleHUD>();
            if (existing != null)
            {
                bool replace = EditorUtility.DisplayDialog(
                    "Ya existe una UI de batalla",
                    $"La escena ya tiene una UI de batalla ('{existing.gameObject.name}'). ¿Querés reemplazarla?",
                    "Reemplazar",
                    "Cancelar");

                if (!replace) return;
                Undo.DestroyObjectImmediate(existing.gameObject);
            }

            EnsureFolder(PrefabFolder);

            ActionButtonView buttonPrefab = CreateButtonPrefab();
            CombatantStatusView statusPrefab = CreateStatusPrefab();

            EnsureEventSystem();
            GameObject canvas = CreateCanvas();

            RectTransform partyPanel = CreatePanel("PartyPanel", canvas.transform, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(40f, 40f), new Vector2(960f, 0f));
            AddVerticalLayout(partyPanel.gameObject, 12, 6, true);

            RectTransform enemyPanel = CreatePanel("EnemyPanel", canvas.transform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-40f, -40f), new Vector2(960f, 0f));
            AddVerticalLayout(enemyPanel.gameObject, 12, 6, true);

            RectTransform logPanel = CreatePanel("LogPanel", canvas.transform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(40f, -40f), new Vector2(820f, 170f));
            TextMeshProUGUI logLabel = CreateText("LogText", logPanel, "", 22, TextAlignmentOptions.TopLeft);
            Stretch(logLabel.rectTransform, 16f);
            logLabel.textWrappingMode = TextWrappingModes.Normal;

            RectTransform commandPanel = CreatePanel("CommandPanel", canvas.transform, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-40f, 40f), new Vector2(460f, 0f));
            AddVerticalLayout(commandPanel.gameObject, 16, 10, true);

            TextMeshProUGUI commandTitle = CreateText("Title", commandPanel, "Turno de ...", 28, TextAlignmentOptions.Left);
            commandTitle.fontStyle = FontStyles.Bold;
            AddLayoutElement(commandTitle.gameObject, -1f, 40f);

            RectTransform buttonContainer = CreateRect("Buttons", commandPanel);
            AddVerticalLayout(buttonContainer.gameObject, 0, 8, false);

            CommandMenuView commandMenu = commandPanel.gameObject.AddComponent<CommandMenuView>();
            commandMenu.titleLabel = commandTitle;
            commandMenu.buttonContainer = buttonContainer;
            commandMenu.buttonPrefab = buttonPrefab;

            RectTransform resultPanel = CreatePanel("ResultPanel", canvas.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(600f, 0f));
            AddVerticalLayout(resultPanel.gameObject, 32, 24, true);

            TextMeshProUGUI resultLabel = CreateText("ResultText", resultPanel, "¡Victoria!", 64, TextAlignmentOptions.Center);
            resultLabel.fontStyle = FontStyles.Bold;
            AddLayoutElement(resultLabel.gameObject, -1f, 100f);

            GameObject restartObject = (GameObject)PrefabUtility.InstantiatePrefab(buttonPrefab.gameObject, resultPanel);
            restartObject.name = "RestartButton";
            ActionButtonView restartButton = restartObject.GetComponent<ActionButtonView>();
            restartButton.label.text = "Reiniciar batalla";
            restartButton.label.alignment = TextAlignmentOptions.Center;

            BattleHUD hud = canvas.AddComponent<BattleHUD>();
            hud.battle = Object.FindFirstObjectByType<BattleManager>();
            hud.partyContainer = partyPanel;
            hud.enemyContainer = enemyPanel;
            hud.statusPrefab = statusPrefab;
            hud.commandMenu = commandMenu;
            hud.logLabel = logLabel;
            hud.resultPanel = resultPanel.gameObject;
            hud.resultLabel = resultLabel;
            hud.restartButton = restartButton.button;

            Undo.RegisterCreatedObjectUndo(canvas, "Crear UI de batalla");
            AddTurnFeedback(hud);
            EditorSceneManager.MarkSceneDirty(canvas.scene);
            Selection.activeGameObject = canvas;

            if (hud.battle == null)
                Debug.LogWarning("[BattleUIBuilder] UI creada, pero no hay un BattleManager en la escena. Asignalo a mano en BattleHUD.");
            else
                Debug.Log("[BattleUIBuilder] UI de batalla creada. Prefabs en " + PrefabFolder);
        }

        [MenuItem("Tools/RPG/Agregar retrato y sonido de turno")]
        public static void AddTurnFeedbackMenu()
        {
            BattleHUD hud = Object.FindFirstObjectByType<BattleHUD>();
            if (hud == null)
            {
                EditorUtility.DisplayDialog("Falta la UI de batalla", "Primero creá la UI con Tools > RPG > Crear UI de batalla.", "Entendido");
                return;
            }

            TurnPortraitView existing = Object.FindFirstObjectByType<TurnPortraitView>();
            if (existing != null)
            {
                bool replace = EditorUtility.DisplayDialog(
                    "Ya existe un retrato de turno",
                    "La escena ya tiene el retrato de turno. ¿Querés reemplazarlo?",
                    "Reemplazar",
                    "Cancelar");

                if (!replace) return;
                Undo.DestroyObjectImmediate(existing.gameObject);
            }

            AddTurnFeedback(hud);
            EditorSceneManager.MarkSceneDirty(hud.gameObject.scene);
            Debug.Log("[BattleUIBuilder] Retrato y sonido de turno agregados.");
        }

        static void AddTurnFeedback(BattleHUD hud)
        {
            RectTransform root = CreateRect("TurnPortrait", hud.transform);
            Stretch(root, 0f);
            if (hud.partyContainer != null) root.SetSiblingIndex(hud.partyContainer.GetSiblingIndex());

            RectTransform hidden = CreateRect("PortraitHidden", root);
            hidden.anchorMin = hidden.anchorMax = Vector2.zero;
            hidden.anchoredPosition = new Vector2(220f, 90f);
            hidden.sizeDelta = new Vector2(20f, 20f);

            RectTransform shown = CreateRect("PortraitShown", root);
            shown.anchorMin = shown.anchorMax = new Vector2(0.5f, 0.5f);
            shown.anchoredPosition = new Vector2(0f, 60f);
            shown.sizeDelta = new Vector2(20f, 20f);

            RectTransform frame = CreatePanel("PortraitFrame", root, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(340f, 340f));
            frame.GetComponent<Image>().raycastTarget = false;

            RectTransform imageRect = CreateRect("Portrait", frame);
            Stretch(imageRect, 10f);
            RawImage display = imageRect.gameObject.AddComponent<RawImage>();
            display.raycastTarget = false;

            GameObject cameraObject = GameObject.Find("PortraitCamera");
            if (cameraObject == null)
            {
                cameraObject = new GameObject("PortraitCamera");
                Undo.RegisterCreatedObjectUndo(cameraObject, "Crear cámara de retrato");
            }

            Camera portraitCamera = cameraObject.GetComponent<Camera>();
            if (portraitCamera == null) portraitCamera = cameraObject.AddComponent<Camera>();
            portraitCamera.fieldOfView = 30f;
            portraitCamera.nearClipPlane = 0.05f;
            portraitCamera.enabled = false;

            TurnPortraitView view = root.gameObject.AddComponent<TurnPortraitView>();
            view.battle = hud.battle;
            view.portraitCamera = portraitCamera;
            view.display = display;
            view.panel = frame;
            view.hiddenAnchor = hidden;
            view.shownAnchor = shown;

            if (Object.FindFirstObjectByType<RPG.Presentation.TurnSoundPlayer>() == null)
            {
                GameObject soundObject = new GameObject("TurnSounds", typeof(AudioSource));
                RPG.Presentation.TurnSoundPlayer player = soundObject.AddComponent<RPG.Presentation.TurnSoundPlayer>();
                player.battle = hud.battle;
                Undo.RegisterCreatedObjectUndo(soundObject, "Crear sonidos de turno");
            }
        }

        static ActionButtonView CreateButtonPrefab()
        {
            GameObject root = new GameObject("ActionButton", typeof(RectTransform));

            Image background = root.AddComponent<Image>();
            background.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            background.type = Image.Type.Sliced;
            background.color = Color.white;

            Button button = root.AddComponent<Button>();
            ColorBlock colors = button.colors;
            colors.normalColor = new Color(0.18f, 0.2f, 0.26f);
            colors.highlightedColor = new Color(0.3f, 0.42f, 0.62f);
            colors.selectedColor = new Color(0.3f, 0.42f, 0.62f);
            colors.pressedColor = new Color(0.2f, 0.3f, 0.48f);
            colors.disabledColor = new Color(0.12f, 0.12f, 0.15f, 0.7f);
            colors.colorMultiplier = 1f;
            button.colors = colors;

            AddLayoutElement(root, -1f, 56f);

            TextMeshProUGUI label = CreateText("Label", root.transform, "Acción", 26, TextAlignmentOptions.Left);
            Stretch(label.rectTransform, 0f);
            label.rectTransform.offsetMin = new Vector2(18f, 0f);
            label.rectTransform.offsetMax = new Vector2(-18f, 0f);

            TextMeshProUGUI detail = CreateText("Detail", root.transform, "", 20, TextAlignmentOptions.Right);
            Stretch(detail.rectTransform, 0f);
            detail.rectTransform.offsetMin = new Vector2(18f, 0f);
            detail.rectTransform.offsetMax = new Vector2(-18f, 0f);
            detail.color = new Color(0.75f, 0.78f, 0.85f);

            ActionButtonView view = root.AddComponent<ActionButtonView>();
            view.button = button;
            view.label = label;
            view.detail = detail;

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, ButtonPrefabPath);
            Object.DestroyImmediate(root);
            return prefab.GetComponent<ActionButtonView>();
        }

        static CombatantStatusView CreateStatusPrefab()
        {
            GameObject root = new GameObject("CombatantStatusRow", typeof(RectTransform));
            AddLayoutElement(root, -1f, 48f);
            CanvasGroup group = root.AddComponent<CanvasGroup>();

            HorizontalLayoutGroup layout = root.AddComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(10, 10, 4, 4);
            layout.spacing = 14f;
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;

            RectTransform highlightRect = CreateRect("Highlight", root.transform);
            Stretch(highlightRect, 0f);
            Image highlight = highlightRect.gameObject.AddComponent<Image>();
            highlight.raycastTarget = false;
            highlight.enabled = false;
            highlightRect.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;

            TextMeshProUGUI nameLabel = CreateText("Name", root.transform, "Nombre", 26, TextAlignmentOptions.Left);
            AddLayoutElement(nameLabel.gameObject, 200f, 40f);

            Image hpFill = CreateBar("HPBar", root.transform, 260f, 28f, HPColor, out RectTransform hpBar);
            TextMeshProUGUI hpLabel = CreateText("HPText", hpBar, "100/100", 20, TextAlignmentOptions.Center);
            Stretch(hpLabel.rectTransform, 0f);

            Image mpFill = CreateBar("MPBar", root.transform, 170f, 28f, MPColor, out RectTransform mpBar);
            TextMeshProUGUI mpLabel = CreateText("MPText", mpBar, "30/30", 20, TextAlignmentOptions.Center);
            Stretch(mpLabel.rectTransform, 0f);

            Image atbFill = CreateBar("ATBBar", root.transform, 200f, 14f, ATBColor, out _);

            CombatantStatusView view = root.AddComponent<CombatantStatusView>();
            view.nameLabel = nameLabel;
            view.hpLabel = hpLabel;
            view.hpFill = hpFill;
            view.mpLabel = mpLabel;
            view.mpFill = mpFill;
            view.atbFill = atbFill;
            view.highlight = highlight;
            view.canvasGroup = group;

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, StatusPrefabPath);
            Object.DestroyImmediate(root);
            return prefab.GetComponent<CombatantStatusView>();
        }

        static Image CreateBar(string name, Transform parent, float width, float height, Color fillColor, out RectTransform barRect)
        {
            barRect = CreateRect(name, parent);
            Image background = barRect.gameObject.AddComponent<Image>();
            background.color = BarBackgroundColor;
            background.raycastTarget = false;
            AddLayoutElement(barRect.gameObject, width, height);

            RectTransform fillRect = CreateRect("Fill", barRect);
            Stretch(fillRect, 0f);
            Image fill = fillRect.gameObject.AddComponent<Image>();
            fill.color = fillColor;
            fill.raycastTarget = false;

            return fill;
        }

        static GameObject CreateCanvas()
        {
            GameObject canvasObject = new GameObject("BattleUI", typeof(RectTransform));

            Canvas canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            canvasObject.AddComponent<GraphicRaycaster>();
            return canvasObject;
        }

        static void EnsureEventSystem()
        {
            EventSystem eventSystem = Object.FindFirstObjectByType<EventSystem>();

            if (eventSystem == null)
            {
                GameObject go = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
                Undo.RegisterCreatedObjectUndo(go, "Crear EventSystem");
                return;
            }

            StandaloneInputModule legacy = eventSystem.GetComponent<StandaloneInputModule>();
            if (legacy != null) Undo.DestroyObjectImmediate(legacy);

            if (eventSystem.GetComponent<InputSystemUIInputModule>() == null)
                Undo.AddComponent<InputSystemUIInputModule>(eventSystem.gameObject);
        }

        static RectTransform CreatePanel(string name, Transform parent, Vector2 anchor, Vector2 pivot, Vector2 position, Vector2 size)
        {
            RectTransform rect = CreateRect(name, parent);
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = pivot;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;

            Image image = rect.gameObject.AddComponent<Image>();
            image.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Background.psd");
            image.type = Image.Type.Sliced;
            image.color = PanelColor;

            return rect;
        }

        static void AddVerticalLayout(GameObject target, int padding, float spacing, bool fitHeight)
        {
            VerticalLayoutGroup layout = target.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(padding, padding, padding, padding);
            layout.spacing = spacing;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            if (fitHeight)
            {
                ContentSizeFitter fitter = target.AddComponent<ContentSizeFitter>();
                fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            }
        }

        static void AddLayoutElement(GameObject target, float width, float height)
        {
            LayoutElement element = target.AddComponent<LayoutElement>();
            if (width >= 0f)
            {
                element.preferredWidth = width;
                element.minWidth = width;
            }
            if (height >= 0f)
            {
                element.preferredHeight = height;
                element.minHeight = height;
            }
        }

        static TextMeshProUGUI CreateText(string name, Transform parent, string text, float size, TextAlignmentOptions alignment)
        {
            RectTransform rect = CreateRect(name, parent);
            TextMeshProUGUI label = rect.gameObject.AddComponent<TextMeshProUGUI>();

            TMP_FontAsset font = FindUIFont();
            if (font != null)
            {
                label.font = font;
                label.fontSharedMaterial = font.material;
            }

            label.text = text;
            label.fontSize = size;
            label.alignment = alignment;
            label.color = Color.white;
            label.raycastTarget = false;
            label.textWrappingMode = TextWrappingModes.NoWrap;
            label.overflowMode = TextOverflowModes.Ellipsis;
            return label;
        }

        static TMP_FontAsset FindUIFont()
        {
            foreach (string guid in AssetDatabase.FindAssets(UIFontName + " t:TMP_FontAsset"))
            {
                TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(AssetDatabase.GUIDToAssetPath(guid));
                if (font != null && font.name == UIFontName) return font;
            }
            return null;
        }

        static RectTransform CreateRect(string name, Transform parent)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return (RectTransform)go.transform;
        }

        static void Stretch(RectTransform rect, float inset)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = new Vector2(inset, inset);
            rect.offsetMax = new Vector2(-inset, -inset);
        }

        static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;

            string parent = Path.GetDirectoryName(path).Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        }
    }
}
