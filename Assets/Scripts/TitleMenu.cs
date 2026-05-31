using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class TitleMenu : MonoBehaviour
{
    private enum ScreenState
    {
        Home,
        Roadmap
    }

    [Header("UI")]
    public TextMeshProUGUI StartGameText;
    [Header("Audio")]
    public AudioPlayer BackgroundMusic;
    public AudioPlayer AmbientSoundFX;
    public AudioPlayer VoiceSoundFX;
    public AudioPlayer CursorSoundFX;
    public AudioPlayer ConfirmSoundFX;

    private bool _pendingStartGame;
    private ScreenState _screenState = ScreenState.Home;
    private GameObject _homeCanvas;
    private GameObject _levelSelectCanvas;
    private void Awake()
    {
        BackgroundMusic?.Play();
        AmbientSoundFX?.Play();
        VoiceSoundFX?.Play();
    }

    private void Start()
    {
        ApplyTitleScreenText();

        if (StartGameText != null)
        {
            StartGameText.text = "MENÜ";
            TurkishTextStyle.Apply(StartGameText);
            StartGameText.enabled = false;
        }

        BuildHomeMenuButton();
    }

    void Update()
    {
        if (_pendingStartGame) return;

        if (_screenState == ScreenState.Home)
        {
            if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter) || Input.GetKeyDown(KeyCode.M))
                OpenRoadmap();
            return;
        }

        if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.Backspace))
            CloseRoadmap();
        if (Input.GetKeyDown(KeyCode.Alpha1) || Input.GetKeyDown(KeyCode.Keypad1))
            StartCoroutine(StartLevel(LevelScoreStore.Levels[0].sceneName));
        if (Input.GetKeyDown(KeyCode.Alpha2) || Input.GetKeyDown(KeyCode.Keypad2))
            StartCoroutine(StartLevel(LevelScoreStore.Levels[1].sceneName));
        if (Input.GetKeyDown(KeyCode.Alpha3) || Input.GetKeyDown(KeyCode.Keypad3))
            StartCoroutine(StartLevel(LevelScoreStore.Levels[2].sceneName));
    }

    private IEnumerator StartLevel(string sceneName)
    {
        if (_pendingStartGame) yield break;
        _pendingStartGame = true;
        if (StartGameText != null)
            StartGameText.enabled = false;
        BackgroundMusic?.Stop();
        AmbientSoundFX?.Stop();
        VoiceSoundFX?.Stop();
        ConfirmSoundFX?.Play();
        if (ConfirmSoundFX != null)
            yield return new WaitWhile(() => ConfirmSoundFX.IsPlaying);
        if (_homeCanvas != null)
        {
            _homeCanvas.SetActive(false);
            Destroy(_homeCanvas);
        }
        if (_levelSelectCanvas != null)
        {
            _levelSelectCanvas.SetActive(false);
            Destroy(_levelSelectCanvas);
        }
        DestroyRuntimeCanvasByName("KayipKristal_HomeCanvas");
        DestroyRuntimeCanvasByName("KayipKristal_LevelSelectCanvas");
        SceneManager.LoadScene(sceneName);
        _pendingStartGame = false;
    }

    private void ApplyTitleScreenText()
    {
        foreach (TMP_Text text in FindObjectsOfType<TMP_Text>(true))
        {
            if (text.text.Trim().Equals("Prototype", System.StringComparison.OrdinalIgnoreCase))
            {
                text.text = "KAYIP KRİSTAL";
                text.fontSize = 56f;
                text.enableAutoSizing = true;
                text.fontSizeMin = 36f;
                text.fontSizeMax = 56f;
                if (text.rectTransform != null)
                {
                    text.rectTransform.anchoredPosition = new Vector2(0f, 148f);
                    text.rectTransform.sizeDelta = new Vector2(760f, 86f);
                }
            }

            TurkishTextStyle.Apply(text);
        }
    }

    private void BuildHomeMenuButton()
    {
        EnsureEventSystem();

        if (_homeCanvas != null)
            Destroy(_homeCanvas);

        DestroyRuntimeCanvasByName("KayipKristal_HomeCanvas");
        GameObject canvasGo = new GameObject("KayipKristal_HomeCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        _homeCanvas = canvasGo;
        Canvas canvas = canvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 1000;

        CanvasScaler scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280f, 720f);
        scaler.matchWidthOrHeight = 0.5f;

        RectTransform root = canvasGo.GetComponent<RectTransform>();
        Button button = CreateButton(root, "MENÜ", new Vector2(286f, -164f), new Vector2(240f, 58f));
        button.onClick.AddListener(OpenRoadmap);
        CreateText(root, "EnterHint", "Enter veya M", 18, new Vector2(286f, -212f), new Vector2(240f, 42f), FontStyles.Normal, new Color(0.76f, 0.82f, 0.74f, 1f));
    }

    private void OpenRoadmap()
    {
        if (_pendingStartGame) return;

        _screenState = ScreenState.Roadmap;
        if (_homeCanvas != null)
            _homeCanvas.SetActive(false);
        BuildRoadmapMenu();
    }

    private void CloseRoadmap()
    {
        if (_pendingStartGame) return;

        _screenState = ScreenState.Home;
        if (_levelSelectCanvas != null)
            Destroy(_levelSelectCanvas);
        if (_homeCanvas != null)
            _homeCanvas.SetActive(true);
    }

    private void BuildRoadmapMenu()
    {
        EnsureEventSystem();

        if (_levelSelectCanvas != null)
            Destroy(_levelSelectCanvas);

        DestroyRuntimeCanvasByName("KayipKristal_LevelSelectCanvas");
        GameObject canvasGo = new GameObject("KayipKristal_LevelSelectCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        _levelSelectCanvas = canvasGo;
        Canvas canvas = canvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 1100;

        CanvasScaler scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280f, 720f);
        scaler.matchWidthOrHeight = 0.5f;

        RectTransform root = canvasGo.GetComponent<RectTransform>();
        HideHomeCanvases();
        CreateMenuScreen(root);
        CreateText(root, "Title", "BÖLÜM YOL HARİTASI", 46, new Vector2(0f, 248f), new Vector2(720f, 62f), FontStyles.Bold, new Color(1f, 0.82f, 0.26f, 1f));
        CreateText(root, "Subtitle", "Demo sürümünde tüm bölümler açık. En yüksek skorunu yükseltmek için bölümleri tekrar oynayabilirsin.", 21, new Vector2(0f, 200f), new Vector2(820f, 52f), FontStyles.Normal, new Color(0.94f, 0.9f, 0.82f, 1f));

        Vector2[] nodePositions =
        {
            new(-330f, -68f),
            new(0f, 60f),
            new(330f, -68f)
        };

        CreateRoute(root, nodePositions[0], nodePositions[1]);
        CreateRoute(root, nodePositions[1], nodePositions[2]);

        for (int i = 0; i < LevelScoreStore.Levels.Length; i++)
        {
            int levelIndex = i;
            Button button = CreateWaypoint(root, levelIndex + 1, LevelScoreStore.Levels[i].title, LevelScoreStore.Levels[i].sceneName, nodePositions[i]);
            button.onClick.AddListener(() => StartCoroutine(StartLevel(LevelScoreStore.Levels[levelIndex].sceneName)));
        }

        Button backButton = CreateButton(root, "GERİ", new Vector2(-360f, -250f), new Vector2(160f, 50f));
        backButton.onClick.AddListener(CloseRoadmap);
        CreateText(root, "ShortcutHint", "Kısayollar: 1, 2, 3    Esc: geri", 18, new Vector2(120f, -250f), new Vector2(460f, 48f), FontStyles.Normal, new Color(0.76f, 0.82f, 0.74f, 1f));
    }

    private void HideHomeCanvases()
    {
        if (_homeCanvas != null)
            _homeCanvas.SetActive(false);

        foreach (Canvas homeCanvas in Resources.FindObjectsOfTypeAll<Canvas>())
        {
            if (homeCanvas == null) continue;
            if (homeCanvas != null && homeCanvas.name == "KayipKristal_HomeCanvas")
                homeCanvas.gameObject.SetActive(false);
        }
    }

    private void DestroyRuntimeCanvasByName(string canvasName)
    {
        foreach (Canvas canvas in Resources.FindObjectsOfTypeAll<Canvas>())
        {
            if (canvas == null || canvas.name != canvasName) continue;

            canvas.gameObject.SetActive(false);
            if (Application.isPlaying)
                Destroy(canvas.gameObject);
            else
                DestroyImmediate(canvas.gameObject);
        }
    }

    private void EnsureEventSystem()
    {
        if (FindObjectOfType<EventSystem>() != null) return;
        GameObject eventSystem = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
        eventSystem.hideFlags = HideFlags.DontSaveInBuild;
    }

    private void CreateMenuScreen(RectTransform root)
    {
        GameObject background = new GameObject("LevelSelectScreen", typeof(Image));
        background.transform.SetParent(root, false);
        RectTransform bgRect = background.GetComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.offsetMin = Vector2.zero;
        bgRect.offsetMax = Vector2.zero;
        background.GetComponent<Image>().color = new Color(0.015f, 0.055f, 0.05f, 1f);

        GameObject routeBand = new GameObject("RouteBand", typeof(Image));
        routeBand.transform.SetParent(root, false);
        RectTransform bandRect = routeBand.GetComponent<RectTransform>();
        bandRect.anchorMin = new Vector2(0.5f, 0.5f);
        bandRect.anchorMax = new Vector2(0.5f, 0.5f);
        bandRect.pivot = new Vector2(0.5f, 0.5f);
        bandRect.sizeDelta = new Vector2(1060f, 360f);
        bandRect.anchoredPosition = new Vector2(0f, -24f);
        routeBand.GetComponent<Image>().color = new Color(0.02f, 0.11f, 0.09f, 0.96f);
    }

    private TextMeshProUGUI CreateText(RectTransform root, string name, string text, int size, Vector2 anchoredPosition, FontStyles style, Color color)
    {
        return CreateText(root, name, text, size, anchoredPosition, new Vector2(600f, 70f), style, color);
    }

    private TextMeshProUGUI CreateText(RectTransform root, string name, string text, int size, Vector2 anchoredPosition, Vector2 sizeDelta, FontStyles style, Color color)
    {
        GameObject textGo = new GameObject(name, typeof(TextMeshProUGUI));
        textGo.transform.SetParent(root, false);
        RectTransform rect = textGo.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = sizeDelta;
        rect.anchoredPosition = anchoredPosition;

        TextMeshProUGUI label = textGo.GetComponent<TextMeshProUGUI>();
        label.text = text;
        label.fontSize = size;
        label.fontStyle = style;
        label.alignment = TextAlignmentOptions.Center;
        label.color = color;
        label.enableWordWrapping = true;
        TurkishTextStyle.Apply(label);
        return label;
    }

    private Button CreateButton(RectTransform root, string text, Vector2 anchoredPosition)
    {
        return CreateButton(root, text, anchoredPosition, new Vector2(460f, 58f));
    }

    private Button CreateButton(RectTransform root, string text, Vector2 anchoredPosition, Vector2 size)
    {
        GameObject buttonGo = new GameObject(text, typeof(Image), typeof(Button));
        buttonGo.transform.SetParent(root, false);
        RectTransform rect = buttonGo.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = size;
        rect.anchoredPosition = anchoredPosition;

        Image image = buttonGo.GetComponent<Image>();
        image.color = new Color(0.12f, 0.18f, 0.14f, 0.98f);

        Button button = buttonGo.GetComponent<Button>();
        ColorBlock colors = button.colors;
        colors.normalColor = image.color;
        colors.highlightedColor = new Color(0.22f, 0.34f, 0.22f, 1f);
        colors.pressedColor = new Color(0.38f, 0.52f, 0.25f, 1f);
        button.colors = colors;

        TextMeshProUGUI label = CreateText(rect, "Label", text, 25, Vector2.zero, FontStyles.Bold, new Color(1f, 0.94f, 0.78f, 1f));
        label.rectTransform.sizeDelta = rect.sizeDelta;
        return button;
    }

    private void CreateRoute(RectTransform root, Vector2 from, Vector2 to)
    {
        Vector2 delta = to - from;
        GameObject routeGo = new GameObject("RoadRoute", typeof(Image));
        routeGo.transform.SetParent(root, false);
        RectTransform rect = routeGo.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(delta.magnitude, 12f);
        rect.anchoredPosition = (from + to) * 0.5f;
        rect.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg);
        routeGo.GetComponent<Image>().color = new Color(0.7f, 0.54f, 0.24f, 0.92f);
    }

    private Button CreateWaypoint(RectTransform root, int number, string title, string sceneName, Vector2 anchoredPosition)
    {
        GameObject nodeGo = new GameObject($"Waypoint_{number}", typeof(Image), typeof(Button));
        nodeGo.transform.SetParent(root, false);
        RectTransform rect = nodeGo.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(96f, 96f);
        rect.anchoredPosition = anchoredPosition;

        Image image = nodeGo.GetComponent<Image>();
        image.color = new Color(0.12f, 0.18f, 0.14f, 1f);

        Button button = nodeGo.GetComponent<Button>();
        ColorBlock colors = button.colors;
        colors.normalColor = image.color;
        colors.highlightedColor = new Color(0.23f, 0.38f, 0.22f, 1f);
        colors.pressedColor = new Color(0.44f, 0.58f, 0.24f, 1f);
        button.colors = colors;

        CreateText(rect, "Number", number.ToString(), 42, new Vector2(0f, 10f), new Vector2(84f, 52f), FontStyles.Bold, new Color(1f, 0.86f, 0.34f, 1f));
        string levelLabel = title.Replace(": ", "\n");
        int highScore = LevelScoreStore.GetHighScore(sceneName);
        CreateText(root, $"WaypointLabel_{number}", $"{levelLabel}\nEn yüksek skor: {highScore}", 19, anchoredPosition + new Vector2(0f, -102f), new Vector2(240f, 96f), FontStyles.Bold, new Color(1f, 0.94f, 0.78f, 1f));
        return button;
    }
}
