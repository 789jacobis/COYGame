using System.Collections.Generic;
using COYGame;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class CoyMvpSceneBuilder
{
    private const string Root = "Assets/Game";
    private const string CardDataPath = Root + "/Data/Cards";
    private const string PlayerDataPath = Root + "/Data/Players";
    private const string PrefabPath = Root + "/UI";
    private const string ScenePath = Root + "/Scenes/BattleScene.unity";

    [MenuItem("Tools/COY/Build MVP Scene")]
    public static void BuildMvpScene()
    {
        EnsureFolders();
        var cards = CreateCards();
        var players = CreatePlayers(cards);
        var cardPrefab = CreateCardPrefab();
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        CreateCamera();
        CreateEventSystem();
        var controller = CreateBattleSceneUi(cardPrefab, players);
        CoyFontUtility.RepairBattleUiReferences();

        EditorSceneManager.SaveScene(scene, ScenePath);
        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
        Selection.activeObject = controller.gameObject;
        Debug.Log("COY MVP scene built at " + ScenePath);
    }

    private static void EnsureFolders()
    {
        CreateFolder("Assets", "Game");
        CreateFolder(Root, "Data");
        CreateFolder(Root + "/Data", "Cards");
        CreateFolder(Root + "/Data", "Players");
        CreateFolder(Root, "UI");
        CreateFolder(Root, "Scenes");
    }

    private static Dictionary<string, CardData> CreateCards()
    {
        var cards = new Dictionary<string, CardData>();
        foreach (var guid in AssetDatabase.FindAssets("t:CardData", new[] { CardDataPath }))
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            var card = AssetDatabase.LoadAssetAtPath<CardData>(path);
            if (card != null)
            {
                cards[card.name] = card;
            }
        }

        return cards;
    }

    private static Dictionary<string, PlayerData> CreatePlayers(Dictionary<string, CardData> cards)
    {
        var players = new Dictionary<string, PlayerData>();
        foreach (var guid in AssetDatabase.FindAssets("t:PlayerData", new[] { PlayerDataPath }))
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            var player = AssetDatabase.LoadAssetAtPath<PlayerData>(path);
            if (player != null)
            {
                players[player.name] = player;
            }
        }

        return players;
    }

    private static CardData Card(string assetName, string displayName, CardType type, CardEffectType effect, int ap, float multiplier, string text, float percentage = 0f, int flat = 0)
    {
        var path = $"{CardDataPath}/{assetName}.asset";
        var card = AssetDatabase.LoadAssetAtPath<CardData>(path);
        if (card == null)
        {
            card = ScriptableObject.CreateInstance<CardData>();
            AssetDatabase.CreateAsset(card, path);
        }

        card.cardName = displayName;
        card.cardType = type;
        card.effectType = effect;
        card.apCost = ap;
        card.powerMultiplier = multiplier;
        card.rulesText = text;
        card.percentageValue = percentage;
        card.flatValue = flat;
        card.effects.Clear();
        card.effects.Add(new CardEffectData
        {
            effectType = effect,
            powerMultiplier = multiplier,
            percentageValue = percentage,
            flatValue = flat
        });
        EditorUtility.SetDirty(card);
        return card;
    }

    private static PlayerData Player(string name, int attack, int defense, Color color, Dictionary<string, CardData> cards, string[] uniqueAttack, string[] uniqueDefense)
    {
        var path = $"{PlayerDataPath}/{name}.asset";
        var player = AssetDatabase.LoadAssetAtPath<PlayerData>(path);
        if (player == null)
        {
            player = ScriptableObject.CreateInstance<PlayerData>();
            AssetDatabase.CreateAsset(player, path);
        }

        player.playerName = name;
        player.attack = attack;
        player.defense = defense;
        player.placeholderColor = color;
        player.attackCards.Clear();
        player.defenseCards.Clear();
        player.attackCards.Add(cards["Layup"]);
        player.attackCards.Add(cards["Layup"]);
        foreach (var key in uniqueAttack)
        {
            player.attackCards.Add(cards[key]);
        }

        player.defenseCards.Add(cards["SingleDefense"]);
        player.defenseCards.Add(cards["SingleDefense"]);
        foreach (var key in uniqueDefense)
        {
            player.defenseCards.Add(cards[key]);
        }

        EditorUtility.SetDirty(player);
        return player;
    }

    private static CardView CreateCardPrefab()
    {
        var path = $"{PrefabPath}/CardView.prefab";
        var existing = AssetDatabase.LoadAssetAtPath<CardView>(path);
        if (existing != null)
        {
            return existing;
        }

        var root = new GameObject("CardView", typeof(RectTransform), typeof(Image), typeof(CanvasGroup), typeof(CardView));
        var rect = (RectTransform)root.transform;
        rect.sizeDelta = new Vector2(160, 250);
        root.GetComponent<Image>().color = Color.white;
        var title = Text(root.transform, "Title", "Card", 22, TextAlignmentOptions.Center, new Vector2(0, 92), new Vector2(145, 44));
        var body = Text(root.transform, "Body", "Rules", 18, TextAlignmentOptions.TopLeft, new Vector2(0, -10), new Vector2(138, 135));
        var cost = Text(root.transform, "Cost", "1", 24, TextAlignmentOptions.Center, new Vector2(-70, 105), new Vector2(42, 34));
        var costRect = cost.rectTransform;
        costRect.anchorMin = costRect.anchorMax = new Vector2(0f, 1f);
        costRect.pivot = new Vector2(0f, 1f);
        costRect.anchoredPosition = new Vector2(8f, -8f);

        var so = new SerializedObject(root.GetComponent<CardView>());
        so.FindProperty("background").objectReferenceValue = root.GetComponent<Image>();
        so.FindProperty("titleText").objectReferenceValue = title;
        so.FindProperty("bodyText").objectReferenceValue = body;
        so.FindProperty("costText").objectReferenceValue = cost;
        so.FindProperty("canvasGroup").objectReferenceValue = root.GetComponent<CanvasGroup>();
        so.ApplyModifiedPropertiesWithoutUndo();

        var prefabGo = PrefabUtility.SaveAsPrefabAsset(root, path);
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path).GetComponent<CardView>();
        Object.DestroyImmediate(root);
        return prefab;
    }

    private static BattleController CreateBattleSceneUi(CardView cardPrefab, Dictionary<string, PlayerData> players)
    {
        var canvasGo = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        var canvas = canvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        var controllerGo = new GameObject("BattleController", typeof(BattleController));
        var controller = controllerGo.GetComponent<BattleController>();
        var uiGo = new GameObject("BattleUI", typeof(BattleUI));
        uiGo.transform.SetParent(canvasGo.transform, false);
        var ui = uiGo.GetComponent<BattleUI>();

        var court = Panel(canvasGo.transform, "Court", new Color(0.83f, 0.48f, 0.25f), Vector2.zero, new Vector2(1920, 1080));
        court.anchorMin = Vector2.zero;
        court.anchorMax = Vector2.one;
        court.sizeDelta = Vector2.zero;
        Panel(court.transform, "PaintLeft", new Color(0.75f, 0.25f, 0.2f), new Vector2(-470, 0), new Vector2(210, 430));
        Panel(court.transform, "PaintRight", new Color(0.75f, 0.25f, 0.2f), new Vector2(470, 0), new Vector2(210, 430));
        Panel(court.transform, "MidCourt", new Color(0.95f, 0.76f, 0.32f), Vector2.zero, new Vector2(10, 620));
        var drop = court.gameObject.AddComponent<DropZone>();
        drop.Bind(controller);

        var playerHoop = Hoop(canvasGo.transform, "PlayerHoop", new Vector2(-500, 235));
        var enemyHoop = Hoop(canvasGo.transform, "EnemyHoop", new Vector2(500, 235));
        var playerBars = Bars(canvasGo.transform, "PlayerBars", new Vector2(-455, 360));
        var enemyBars = Bars(canvasGo.transform, "EnemyBars", new Vector2(455, 360));

        var phase = Text(canvasGo.transform, "Phase", "Player Turn", 38, TextAlignmentOptions.Center, new Vector2(-455, 445), new Vector2(300, 58));
        var score = Text(canvasGo.transform, "Score", "0   0", 64, TextAlignmentOptions.Center, new Vector2(0, 450), new Vector2(330, 95));
        var round = Text(canvasGo.transform, "Round", "Round\n1", 32, TextAlignmentOptions.Center, new Vector2(0, 445), new Vector2(130, 90));
        var log = Text(canvasGo.transform, "Log", "COY", 28, TextAlignmentOptions.Center, new Vector2(0, 275), new Vector2(780, 60));

        var playerHoopText = Text(canvasGo.transform, "PlayerHoopText", "150/150\n盾 0", 26, TextAlignmentOptions.Center, new Vector2(-460, 360), new Vector2(220, 65));
        var enemyHoopText = Text(canvasGo.transform, "EnemyHoopText", "150/150\n盾 0", 26, TextAlignmentOptions.Center, new Vector2(460, 360), new Vector2(220, 65));

        var hand = Panel(canvasGo.transform, "Hand", new Color(0, 0, 0, 0), new Vector2(0, -405), new Vector2(980, 300));

        var dragLayer = Panel(canvasGo.transform, "DragLayer", new Color(0, 0, 0, 0), Vector2.zero, new Vector2(1920, 1080));
        dragLayer.GetComponent<Image>().raycastTarget = false;

        var ap = Text(canvasGo.transform, "AP", "AP 4/4", 38, TextAlignmentOptions.Center, new Vector2(0, -510), new Vector2(230, 64));
        var deckBox = Panel(canvasGo.transform, "DeckBox", new Color(1f, 1f, 1f, 0.92f), new Vector2(-760, -330), new Vector2(110, 90));
        var deck = Text(deckBox.transform, "Deck", "Deck", 26, TextAlignmentOptions.Center, Vector2.zero, new Vector2(110, 90));
        var discardBox = Panel(canvasGo.transform, "DiscardBox", new Color(1f, 1f, 1f, 0.92f), new Vector2(760, -330), new Vector2(120, 90));
        var discard = Text(discardBox.transform, "Discard", "Discard", 24, TextAlignmentOptions.Center, Vector2.zero, new Vector2(120, 90));
        var confirm = Button(canvasGo.transform, "ConfirmButton", "OK", 34, new Vector2(820, -450), new Vector2(120, 76));

        var strategy = Panel(canvasGo.transform, "StrategyPanel", new Color(0, 0, 0, 0.55f), new Vector2(0, 75), new Vector2(410, 170));
        Text(strategy.transform, "StrategyTitle", "Choose Attack", 28, TextAlignmentOptions.Center, new Vector2(0, 48), new Vector2(360, 40));
        var two = Button(strategy.transform, "TwoPoint", "2 PT", 30, new Vector2(-95, -32), new Vector2(150, 65));
        var three = Button(strategy.transform, "ThreePoint", "3 PT", 30, new Vector2(95, -32), new Vector2(150, 65));
        strategy.gameObject.SetActive(false);

        var preview = Panel(canvasGo.transform, "CardPreview", new Color(1f, 1f, 0.95f, 0.96f), new Vector2(0, -40), new Vector2(300, 430));
        foreach (var graphic in preview.GetComponentsInChildren<Graphic>(true))
        {
            graphic.raycastTarget = false;
        }
        var previewTitle = Text(preview.transform, "PreviewTitle", "Card", 34, TextAlignmentOptions.Center, new Vector2(0, 160), new Vector2(260, 70));
        var previewBody = Text(preview.transform, "PreviewBody", "Rules", 24, TextAlignmentOptions.TopLeft, new Vector2(0, 0), new Vector2(250, 230));
        var previewCost = Text(preview.transform, "PreviewCost", "1 AP", 26, TextAlignmentOptions.Center, new Vector2(0, -170), new Vector2(250, 50));
        preview.gameObject.SetActive(false);

        var modal = Panel(canvasGo.transform, "Modal", new Color(0, 0, 0, 0.65f), Vector2.zero, new Vector2(1920, 1080));
        var modalBox = Panel(modal.transform, "ModalBox", new Color(1f, 1f, 0.95f, 1f), Vector2.zero, new Vector2(520, 260));
        var modalText = Text(modalBox.transform, "ModalText", "Result", 34, TextAlignmentOptions.Center, new Vector2(0, 45), new Vector2(460, 120));
        var modalButton = Button(modalBox.transform, "ModalButton", "OK", 30, new Vector2(0, -75), new Vector2(170, 65));
        modal.gameObject.SetActive(false);

        AddPlaceholderPlayers(canvasGo.transform, players);

        var uiSo = new SerializedObject(ui);
        uiSo.FindProperty("phaseText").objectReferenceValue = phase;
        uiSo.FindProperty("roundText").objectReferenceValue = round;
        uiSo.FindProperty("scoreText").objectReferenceValue = score;
        uiSo.FindProperty("logText").objectReferenceValue = log;
        uiSo.FindProperty("playerHoopText").objectReferenceValue = playerHoopText;
        uiSo.FindProperty("enemyHoopText").objectReferenceValue = enemyHoopText;
        uiSo.FindProperty("playerHpFill").objectReferenceValue = playerBars.hp;
        uiSo.FindProperty("playerShieldFill").objectReferenceValue = playerBars.shield;
        uiSo.FindProperty("enemyHpFill").objectReferenceValue = enemyBars.hp;
        uiSo.FindProperty("enemyShieldFill").objectReferenceValue = enemyBars.shield;
        uiSo.FindProperty("apText").objectReferenceValue = ap;
        uiSo.FindProperty("deckText").objectReferenceValue = deck;
        uiSo.FindProperty("discardText").objectReferenceValue = discard;
        uiSo.FindProperty("handRoot").objectReferenceValue = hand;
        uiSo.FindProperty("dragLayer").objectReferenceValue = dragLayer;
        uiSo.FindProperty("playArea").objectReferenceValue = court;
        uiSo.FindProperty("cardPreview").objectReferenceValue = preview;
        uiSo.FindProperty("previewTitle").objectReferenceValue = previewTitle;
        uiSo.FindProperty("previewBody").objectReferenceValue = previewBody;
        uiSo.FindProperty("previewCost").objectReferenceValue = previewCost;
        uiSo.FindProperty("cardViewPrefab").objectReferenceValue = cardPrefab;
        uiSo.FindProperty("controller").objectReferenceValue = controller;
        uiSo.FindProperty("confirmButton").objectReferenceValue = confirm;
        uiSo.FindProperty("twoPointButton").objectReferenceValue = two;
        uiSo.FindProperty("threePointButton").objectReferenceValue = three;
        uiSo.FindProperty("strategyPanel").objectReferenceValue = strategy.gameObject;
        uiSo.FindProperty("modalPanel").objectReferenceValue = modal.gameObject;
        uiSo.FindProperty("modalText").objectReferenceValue = modalText;
        uiSo.FindProperty("modalButton").objectReferenceValue = modalButton;
        uiSo.ApplyModifiedPropertiesWithoutUndo();

        var controllerSo = new SerializedObject(controller);
        controllerSo.FindProperty("ui").objectReferenceValue = ui;
        FillRoster(controllerSo.FindProperty("playerRoster"), players["Curry"], players["Green"], players["Klay"]);
        FillRoster(controllerSo.FindProperty("enemyRoster"), players["LeBron"], players["Doncic"], players["Reaves"]);
        controllerSo.ApplyModifiedPropertiesWithoutUndo();

        return controller;
    }

    private static void FillRoster(SerializedProperty roster, params PlayerData[] players)
    {
        roster.arraySize = players.Length;
        for (var i = 0; i < players.Length; i++)
        {
            roster.GetArrayElementAtIndex(i).objectReferenceValue = players[i];
        }
    }

    private static void AddPlaceholderPlayers(Transform parent, Dictionary<string, PlayerData> players)
    {
        var names = new[] { "Curry", "Green", "Klay", "LeBron", "Doncic", "Reaves" };
        var positions = new[]
        {
            new Vector2(-260, -105), new Vector2(-125, -95), new Vector2(10, -105),
            new Vector2(185, -105), new Vector2(315, -95), new Vector2(445, -105)
        };

        for (var i = 0; i < names.Length; i++)
        {
            var p = Panel(parent, names[i], players[names[i]].placeholderColor, positions[i], new Vector2(72, 72));
            Text(p.transform, "Name", names[i], 17, TextAlignmentOptions.Center, new Vector2(0, -52), new Vector2(100, 28));
        }
    }

    private static RectTransform Hoop(Transform parent, string name, Vector2 position)
    {
        var root = Panel(parent, name, new Color(1f, 0.92f, 0.55f), position, new Vector2(88, 150));
        Panel(root.transform, "Rim", new Color(1f, 0.35f, 0.2f), new Vector2(0, 40), new Vector2(95, 18));
        Panel(root.transform, "Net", new Color(1f, 1f, 1f), new Vector2(0, -8), new Vector2(52, 95));
        return root;
    }

    private static (Image hp, Image shield) Bars(Transform parent, string name, Vector2 position)
    {
        var root = Panel(parent, name, new Color(0, 0, 0, 0.1f), position, new Vector2(250, 28));
        var hp = Panel(root.transform, "HP", Color.red, Vector2.zero, new Vector2(250, 28)).GetComponent<Image>();
        hp.type = Image.Type.Filled;
        hp.fillMethod = Image.FillMethod.Horizontal;
        var shield = Panel(root.transform, "Shield", new Color(0f, 0.75f, 0.2f, 0.55f), new Vector2(0, 18), new Vector2(250, 18)).GetComponent<Image>();
        shield.type = Image.Type.Filled;
        shield.fillMethod = Image.FillMethod.Horizontal;
        return (hp, shield);
    }

    private static Button Button(Transform parent, string name, string label, int size, Vector2 position, Vector2 dimensions)
    {
        var root = Panel(parent, name, new Color(1f, 1f, 0.95f), position, dimensions);
        var button = root.gameObject.AddComponent<Button>();
        var colors = button.colors;
        colors.highlightedColor = new Color(0.95f, 0.9f, 0.7f);
        colors.pressedColor = new Color(0.8f, 0.75f, 0.55f);
        button.colors = colors;
        Text(root.transform, "Label", label, size, TextAlignmentOptions.Center, Vector2.zero, dimensions);
        return button;
    }

    private static RectTransform Panel(Transform parent, string name, Color color, Vector2 position, Vector2 dimensions)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        var rect = (RectTransform)go.transform;
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = dimensions;
        go.GetComponent<Image>().color = color;
        return rect;
    }

    private static TMP_Text Text(Transform parent, string name, string value, int size, TextAlignmentOptions alignment, Vector2 position, Vector2 dimensions)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);
        var rect = (RectTransform)go.transform;
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = dimensions;
        var text = go.GetComponent<TMP_Text>();
        text.text = value;
        text.fontSize = size;
        text.color = Color.black;
        text.alignment = alignment;
        text.textWrappingMode = TextWrappingModes.Normal;
        return text;
    }

    private static void CreateCamera()
    {
        var cameraGo = new GameObject("Main Camera", typeof(Camera));
        cameraGo.tag = "MainCamera";
        var camera = cameraGo.GetComponent<Camera>();
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0.12f, 0.2f, 0.24f);
        camera.orthographic = true;
    }

    private static void CreateEventSystem()
    {
        var eventSystem = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
        Selection.activeGameObject = eventSystem;
    }

    private static void CreateFolder(string parent, string child)
    {
        if (!AssetDatabase.IsValidFolder(parent + "/" + child))
        {
            AssetDatabase.CreateFolder(parent, child);
        }
    }
}
