using System;
using System.Text;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem.UI;
#endif

/// <summary>Builds and updates the prototype UI without using any external assets.</summary>
public sealed class GameUI : MonoBehaviour
{
    static readonly Color PanelColor = new Color(0.055f, 0.065f, 0.09f, 0.94f);
    static readonly Color NormalButtonColor = new Color(0.2f, 0.22f, 0.28f, 1f);
    static readonly Color DisabledButtonColor = new Color(0.12f, 0.13f, 0.16f, 1f);

    GameManager gameManager;
    PaintTool paintTool;
    IceMeltSystem iceMeltSystem;
    Font font;

    Text deathText;
    Text paintText;
    Text hintText;
    Text messageText;
    Text overlayText;
    Button[] levelButtons;
    Button[] colorButtons;
    Button clueButton;
    GameObject overlay;

    int clueIndex;
    bool connected;

    // The UI creates itself when the scene starts.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void CreateRuntimeUI()
    {
        if (FindFirstObjectByType<GameUI>() == null)
            new GameObject("GameUI").AddComponent<GameUI>();
    }

    void Start()
    {
        font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        CreateEventSystem();
        BuildCanvas();
    }

    void Update()
    {
        if (!connected)
            TryConnectSystems();
    }

    void TryConnectSystems()
    {
        gameManager = FindFirstObjectByType<GameManager>();
        paintTool = FindFirstObjectByType<PaintTool>();
        iceMeltSystem = FindFirstObjectByType<IceMeltSystem>();
        if (gameManager == null || paintTool == null || iceMeltSystem == null)
            return;

        connected = true;
        gameManager.StateChanged += OnStateChanged;
        gameManager.LevelChanged += OnLevelChanged;
        gameManager.DeathCountChanged += OnDeathCountChanged;
        gameManager.LevelReset += OnLevelReset;
        paintTool.StateChanged += RefreshPaintUI;
        iceMeltSystem.IceMelted += OnIceMelted;
        PlayerController.Died += OnPlayerDied;

        RefreshAll();
    }

    void BuildCanvas()
    {
        GameObject canvasObject = new GameObject("Game Canvas");
        canvasObject.transform.SetParent(transform, false);

        Canvas canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;

        CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;
        canvasObject.AddComponent<GraphicRaycaster>();

        CreateTopBar(canvasObject.transform);
        CreateBottomBar(canvasObject.transform);
        CreateOverlay(canvasObject.transform);
    }

    void CreateTopBar(Transform parent)
    {
        RectTransform bar = CreatePanel("Top Bar", parent, PanelColor);
        SetAnchors(bar, new Vector2(0f, 1f), new Vector2(1f, 1f));
        bar.pivot = new Vector2(0.5f, 1f);
        bar.anchoredPosition = Vector2.zero;
        bar.sizeDelta = new Vector2(0f, 94f);

        levelButtons = new Button[5];
        for (int i = 0; i < levelButtons.Length; i++)
        {
            int levelNumber = i + 1;
            levelButtons[i] = CreateButton(
                $"Level {levelNumber} Button",
                bar,
                levelNumber.ToString(),
                new Vector2(20f + i * 62f, -17f),
                new Vector2(52f, 52f),
                () => gameManager?.StartLevel(levelNumber));
        }

        deathText = CreateText(
            "Death Count",
            bar,
            "Deaths: 0",
            27,
            TextAnchor.MiddleLeft,
            new Vector2(350f, -17f),
            new Vector2(220f, 52f));

        Text colorLabel = CreateText(
            "Color Label",
            bar,
            "Colors",
            23,
            TextAnchor.MiddleLeft,
            new Vector2(610f, -17f),
            new Vector2(100f, 52f));
        colorLabel.color = new Color(0.8f, 0.82f, 0.88f);

        colorButtons = new Button[3];
        colorButtons[0] = CreateColorButton(bar, "1  BLUE", PaintColor.Blue, 720f);
        colorButtons[1] = CreateColorButton(bar, "2  GREEN", PaintColor.Green, 870f);
        colorButtons[2] = CreateColorButton(bar, "3  RED", PaintColor.Red, 1030f);

        paintText = CreateText(
            "Paint Left",
            bar,
            "Paint: --",
            27,
            TextAnchor.MiddleLeft,
            new Vector2(1230f, -17f),
            new Vector2(500f, 52f));
    }

    Button CreateColorButton(Transform parent, string label, PaintColor color, float x)
    {
        Button button = CreateButton(
            $"{color} Button",
            parent,
            label,
            new Vector2(x, -17f),
            new Vector2(140f, 52f),
            () => paintTool?.TrySelectColor(color));

        button.image.color = ColorForPaint(color);
        return button;
    }

    void CreateBottomBar(Transform parent)
    {
        RectTransform bar = CreatePanel("Bottom Bar", parent, PanelColor);
        SetAnchors(bar, new Vector2(0f, 0f), new Vector2(1f, 0f));
        bar.pivot = new Vector2(0.5f, 0f);
        bar.anchoredPosition = Vector2.zero;
        bar.sizeDelta = new Vector2(0f, 154f);

        hintText = CreateText(
            "Level Hint",
            bar,
            "",
            25,
            TextAnchor.UpperLeft,
            new Vector2(24f, -16f),
            new Vector2(1450f, 62f));

        messageText = CreateText(
            "Message",
            bar,
            "",
            24,
            TextAnchor.MiddleLeft,
            new Vector2(24f, -74f),
            new Vector2(900f, 48f));
        messageText.color = new Color(1f, 0.75f, 0.3f);

        Text controls = CreateText(
            "Controls",
            bar,
            "A/D or Arrows: Move    Space/W/Up: Jump    1/2/3: Color    Mouse: Paint    R: Reset",
            21,
            TextAnchor.MiddleRight,
            new Vector2(790f, -81f),
            new Vector2(1080f, 42f));
        controls.color = new Color(0.72f, 0.74f, 0.8f);

        clueButton = CreateButton(
            "Clue Button",
            bar,
            "SHOW CLUE",
            new Vector2(1630f, -16f),
            new Vector2(240f, 52f),
            ShowNextClue);
        clueButton.gameObject.SetActive(false);
    }

    void CreateOverlay(Transform parent)
    {
        overlay = CreatePanel("Screen Overlay", parent, new Color(0.02f, 0.025f, 0.04f, 0.9f)).gameObject;
        RectTransform rect = overlay.GetComponent<RectTransform>();
        SetAnchors(rect, Vector2.zero, Vector2.one);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        overlayText = CreateText(
            "Overlay Text",
            rect,
            "PAINT PLATFORMER\n\nPress Enter to Start",
            48,
            TextAnchor.MiddleCenter,
            Vector2.zero,
            new Vector2(1000f, 360f));
        SetAnchors(overlayText.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
        overlayText.rectTransform.pivot = new Vector2(0.5f, 0.5f);
    }

    void RefreshAll()
    {
        if (!connected)
            return;

        OnDeathCountChanged(gameManager.DeathCount);
        RefreshLevelUI();
        RefreshPaintUI();
        OnStateChanged(gameManager.State);
    }

    void RefreshLevelUI()
    {
        LevelDefinition current = LevelData.Get(gameManager.CurrentLevelNumber);
        hintText.text = $"Level {current.Number}: {current.Name}  -  {current.Hint}";

        for (int i = 0; i < levelButtons.Length; i++)
            levelButtons[i].image.color = i + 1 == current.Number
                ? new Color(0.95f, 0.72f, 0.18f)
                : NormalButtonColor;

        clueButton.gameObject.SetActive(current.Number == 5);
    }

    void RefreshPaintUI()
    {
        if (!connected)
            return;

        LevelDefinition current = LevelData.Get(gameManager.CurrentLevelNumber);
        for (int i = 0; i < colorButtons.Length; i++)
        {
            PaintColor color = (PaintColor)(i + 1);
            bool allowed = current.Allows(color);
            colorButtons[i].interactable = allowed;
            colorButtons[i].image.color = allowed ? ColorForPaint(color) : DisabledButtonColor;

            Text label = colorButtons[i].GetComponentInChildren<Text>();
            label.color = allowed ? Color.white : new Color(0.45f, 0.46f, 0.5f);

            if (allowed && paintTool.SelectedColor == color)
                label.text = $">{i + 1} {color.ToString().ToUpperInvariant()}<";
            else
                label.text = $"{i + 1} {color.ToString().ToUpperInvariant()}";
        }

        StringBuilder dots = new StringBuilder("Paint: ");
        for (int i = 0; i < current.PaintBudget; i++)
            dots.Append(i < paintTool.PaintLeft ? "● " : "○ ");
        paintText.text = dots.ToString();
    }

    void OnStateChanged(GameState state)
    {
        if (overlay == null)
            return;

        overlay.SetActive(state != GameState.Playing);
        if (state == GameState.WaitingToStart)
            overlayText.text = "PAINT PLATFORMER\n\nPress Enter to Start";
        else if (state == GameState.LevelComplete)
            overlayText.text = $"LEVEL {gameManager.CurrentLevelNumber} COMPLETE\n\nDeaths: {gameManager.DeathCount}   Paint Left: {paintTool.PaintLeft}\n\nPress Enter to Continue";
        else if (state == GameState.GameComplete)
            overlayText.text = $"ALL LEVELS COMPLETE\n\nTotal Deaths: {gameManager.DeathCount}";
    }

    void OnLevelChanged(int levelNumber)
    {
        clueIndex = 0;
        messageText.text = "";
        RefreshLevelUI();
        RefreshPaintUI();
    }

    void OnDeathCountChanged(int count)
    {
        deathText.text = $"Deaths: {count}";
    }

    void OnLevelReset()
    {
        messageText.text = "Level reset.";
        RefreshPaintUI();
    }

    void OnIceMelted(int count)
    {
        messageText.text = count == 1 ? "Ice melted!" : $"Ice melted! {count} tiles cleared.";
    }

    void OnPlayerDied(string reason)
    {
        messageText.text = reason;
    }

    void ShowNextClue()
    {
        LevelDefinition current = LevelData.Get(gameManager.CurrentLevelNumber);
        if (current.Number != 5 || current.ProgressiveHints.Length == 0)
            return;

        int index = Mathf.Min(clueIndex, current.ProgressiveHints.Length - 1);
        messageText.text = $"Clue {index + 1}: {current.ProgressiveHints[index]}";
        clueIndex = Mathf.Min(clueIndex + 1, current.ProgressiveHints.Length);
    }

    Button CreateButton(
        string objectName,
        Transform parent,
        string label,
        Vector2 position,
        Vector2 size,
        UnityEngine.Events.UnityAction onClick)
    {
        GameObject buttonObject = new GameObject(objectName);
        buttonObject.transform.SetParent(parent, false);

        RectTransform rect = buttonObject.AddComponent<RectTransform>();
        SetAnchors(rect, new Vector2(0f, 1f), new Vector2(0f, 1f));
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;

        Image image = buttonObject.AddComponent<Image>();
        image.color = NormalButtonColor;

        Button button = buttonObject.AddComponent<Button>();
        button.targetGraphic = image;
        button.onClick.AddListener(onClick);

        Text text = CreateText(
            "Label",
            rect,
            label,
            22,
            TextAnchor.MiddleCenter,
            Vector2.zero,
            Vector2.zero);
        SetAnchors(text.rectTransform, Vector2.zero, Vector2.one);
        text.rectTransform.offsetMin = Vector2.zero;
        text.rectTransform.offsetMax = Vector2.zero;
        return button;
    }

    Text CreateText(
        string objectName,
        Transform parent,
        string value,
        int size,
        TextAnchor alignment,
        Vector2 position,
        Vector2 dimensions)
    {
        GameObject textObject = new GameObject(objectName);
        textObject.transform.SetParent(parent, false);

        RectTransform rect = textObject.AddComponent<RectTransform>();
        SetAnchors(rect, new Vector2(0f, 1f), new Vector2(0f, 1f));
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = position;
        rect.sizeDelta = dimensions;

        Text text = textObject.AddComponent<Text>();
        text.font = font;
        text.text = value;
        text.fontSize = size;
        text.alignment = alignment;
        text.color = Color.white;
        text.raycastTarget = false;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        return text;
    }

    static RectTransform CreatePanel(string objectName, Transform parent, Color color)
    {
        GameObject panelObject = new GameObject(objectName);
        panelObject.transform.SetParent(parent, false);
        RectTransform rect = panelObject.AddComponent<RectTransform>();
        Image image = panelObject.AddComponent<Image>();
        image.color = color;
        return rect;
    }

    static void SetAnchors(RectTransform rect, Vector2 minimum, Vector2 maximum)
    {
        rect.anchorMin = minimum;
        rect.anchorMax = maximum;
    }

    static Color ColorForPaint(PaintColor color)
    {
        switch (color)
        {
            case PaintColor.Blue: return new Color(0.12f, 0.4f, 0.95f);
            case PaintColor.Green: return new Color(0.12f, 0.68f, 0.3f);
            case PaintColor.Red: return new Color(0.86f, 0.12f, 0.12f);
            default: return NormalButtonColor;
        }
    }

    static void CreateEventSystem()
    {
        if (FindFirstObjectByType<EventSystem>() != null)
            return;

        GameObject eventObject = new GameObject("EventSystem");
        eventObject.AddComponent<EventSystem>();
#if ENABLE_INPUT_SYSTEM
        InputSystemUIInputModule inputModule = eventObject.AddComponent<InputSystemUIInputModule>();
        inputModule.AssignDefaultActions();
#else
        eventObject.AddComponent<StandaloneInputModule>();
#endif
    }

    void OnDestroy()
    {
        if (!connected)
            return;

        gameManager.StateChanged -= OnStateChanged;
        gameManager.LevelChanged -= OnLevelChanged;
        gameManager.DeathCountChanged -= OnDeathCountChanged;
        gameManager.LevelReset -= OnLevelReset;
        paintTool.StateChanged -= RefreshPaintUI;
        iceMeltSystem.IceMelted -= OnIceMelted;
        PlayerController.Died -= OnPlayerDied;
    }
}
