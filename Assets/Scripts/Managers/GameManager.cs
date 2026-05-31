using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameManager : Singleton<GameManager>
{
    [Header("Runtime")]
    public bool EscapeQuitsApplication = true;
    private Canvas _canvas;
    private GameObject _missionPanel;
    private GameObject _pausePanel;
    private EnemySpeedBoostDirector _speedBoostDirector;
    private bool _paused;
    private bool _missionPanelShown;

    internal override void Awake()
    {
        base.Awake();
    }

    private void Start()
    {
        if (PlayerLocator.FindMainPlayer() == null) return;

        DestroyTitleMenuCanvases();
        if (MissionState.Instance != null)
            MissionState.Instance.ConfigureForActiveScene();
        EnsureEventSystem();
        BuildRuntimeCanvas();
        EnsureEnemySpeedBoostDirector();
        ShowMissionPanel();
    }

    private void DestroyTitleMenuCanvases()
    {
        foreach (Canvas canvas in Resources.FindObjectsOfTypeAll<Canvas>())
        {
            if (canvas == null) continue;
            if (canvas.name != "KayipKristal_HomeCanvas" && canvas.name != "KayipKristal_LevelSelectCanvas") continue;

            canvas.gameObject.SetActive(false);
            Destroy(canvas.gameObject);
        }
    }

    private void Update()
    {
        if (PlayerLocator.FindMainPlayer() == null) return;

        if (_missionPanelShown && (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter) || Input.GetKeyDown(KeyCode.Space)))
            CloseMissionPanel();

        if (Input.GetKeyDown(KeyCode.Escape))
            TogglePause();

        if (!_paused && !_missionPanelShown && (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter)))
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void TogglePause()
    {
        if (_missionPanelShown)
        {
            CloseMissionPanel();
            return;
        }

        SetPaused(!_paused);
    }

    public void SetPaused(bool paused)
    {
        _paused = paused;
        Time.timeScale = paused ? 0f : 1f;
        AudioListener.pause = paused;
        if (_pausePanel != null)
            _pausePanel.SetActive(paused);
    }

    public void RestartLevel()
    {
        Time.timeScale = 1f;
        AudioListener.pause = false;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void ReturnToMainMenu()
    {
        Time.timeScale = 1f;
        AudioListener.pause = false;
        SceneManager.LoadScene(0);
    }

    public void QuitGame()
    {
        Application.Quit();
    }

    private void ShowMissionPanel()
    {
        _missionPanelShown = true;
        Time.timeScale = 0f;
        AudioListener.pause = true;
        if (_missionPanel != null)
            _missionPanel.SetActive(true);
    }

    private void CloseMissionPanel()
    {
        _missionPanelShown = false;
        Time.timeScale = 1f;
        AudioListener.pause = false;
        if (_missionPanel != null)
            _missionPanel.SetActive(false);
    }

    private void EnsureEventSystem()
    {
        if (FindObjectOfType<EventSystem>() != null) return;
        GameObject eventSystem = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
        eventSystem.hideFlags = HideFlags.DontSaveInBuild;
    }

    private void BuildRuntimeCanvas()
    {
        if (_canvas != null) return;

        GameObject canvasGo = new GameObject("KayipKristal_RuntimeMenuCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        _canvas = canvasGo.GetComponent<Canvas>();
        _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        _canvas.sortingOrder = 800;

        CanvasScaler scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280f, 720f);
        scaler.matchWidthOrHeight = 0.5f;

        RectTransform root = canvasGo.GetComponent<RectTransform>();
        string sceneName = SceneManager.GetActiveScene().name;
        int requiredCrystals = MissionState.Instance != null ? MissionState.Instance.RequiredCrystals : 5;
        string missionTitle = "BÖLÜM 1: YEŞİL KORU";
        string missionText = $"{requiredCrystals} zümrüt kristali topla, tehlikelerden uzak dur, sonra kale kapısından gir.";
        if (sceneName.Contains("Level2"))
        {
            missionTitle = "BÖLÜM 2: MAVİ HARABELER";
            missionText = $"{requiredCrystals} safir topla, mavi harabeleri geç, sonra kale kapısından gir.";
        }
        else if (sceneName.Contains("Level3"))
        {
            missionTitle = "BÖLÜM 3: KIZIL KALE";
            missionText = $"{requiredCrystals} yakut topla, kızıl tehlikelerden kaç, sonra son kale kapısından gir.";
        }

        CreateHudMenuButton(root);
        _missionPanel = CreateOverlayPanel(root, "MissionPanel", missionTitle, missionText, "Başla", CloseMissionPanel);
        _pausePanel = CreateOverlayPanel(root, "PausePanel", "DURAKLATILDI", "Devam et, bölümü yeniden başlat ya da ana menüye dön.", "Devam", () => SetPaused(false));
        CreateButton(_pausePanel.GetComponent<RectTransform>(), "Yeniden Başlat", new Vector2(0f, -96f), RestartLevel);
        CreateButton(_pausePanel.GetComponent<RectTransform>(), "Ana Menü", new Vector2(0f, -164f), ReturnToMainMenu);
        _pausePanel.SetActive(false);
    }

    private void EnsureEnemySpeedBoostDirector()
    {
        if (_speedBoostDirector != null) return;

        GameObject directorGo = new GameObject("KayipKristal_EnemySpeedBoostDirector");
        _speedBoostDirector = directorGo.AddComponent<EnemySpeedBoostDirector>();
        _speedBoostDirector.Initialize();
    }

    private GameObject CreateOverlayPanel(RectTransform root, string name, string title, string message, string primaryText, UnityEngine.Events.UnityAction primaryAction)
    {
        GameObject panel = new GameObject(name, typeof(RectTransform), typeof(Image));
        panel.transform.SetParent(root, false);
        RectTransform rect = panel.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        panel.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.72f);

        CreateText(rect, "Title", title, 58, new Vector2(0f, 116f), FontStyles.Bold, new Color(1f, 0.78f, 0.22f, 1f));
        CreateText(rect, "Message", message, 28, new Vector2(0f, 32f), FontStyles.Normal, new Color(0.94f, 0.9f, 0.82f, 1f));
        CreateButton(rect, primaryText, new Vector2(0f, -40f), primaryAction);
        return panel;
    }

    private TextMeshProUGUI CreateText(RectTransform root, string name, string text, int size, Vector2 anchoredPosition, FontStyles style, Color color)
    {
        GameObject go = new GameObject(name, typeof(TextMeshProUGUI));
        go.transform.SetParent(root, false);
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(920f, 96f);
        rect.anchoredPosition = anchoredPosition;

        TextMeshProUGUI label = go.GetComponent<TextMeshProUGUI>();
        label.text = text;
        label.fontSize = size;
        label.fontStyle = style;
        label.alignment = TextAlignmentOptions.Center;
        label.color = color;
        label.enableWordWrapping = true;
        TurkishTextStyle.Apply(label);
        return label;
    }

    private Button CreateButton(RectTransform root, string text, Vector2 anchoredPosition, UnityEngine.Events.UnityAction action)
    {
        GameObject go = new GameObject(text, typeof(Image), typeof(Button));
        go.transform.SetParent(root, false);
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(260f, 54f);
        rect.anchoredPosition = anchoredPosition;

        Image image = go.GetComponent<Image>();
        image.color = new Color(0.16f, 0.14f, 0.12f, 0.98f);

        Button button = go.GetComponent<Button>();
        button.onClick.AddListener(action);
        ColorBlock colors = button.colors;
        colors.normalColor = image.color;
        colors.highlightedColor = new Color(0.34f, 0.25f, 0.15f, 1f);
        colors.pressedColor = new Color(0.52f, 0.34f, 0.12f, 1f);
        button.colors = colors;

        TextMeshProUGUI label = CreateText(rect, "Label", text, 24, Vector2.zero, FontStyles.Bold, new Color(1f, 0.92f, 0.78f, 1f));
        label.rectTransform.sizeDelta = rect.sizeDelta;
        return button;
    }

    private Button CreateHudMenuButton(RectTransform root)
    {
        GameObject go = new GameObject("Hud_ReturnToMenu", typeof(Image), typeof(Button));
        go.transform.SetParent(root, false);
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(1f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(1f, 1f);
        rect.sizeDelta = new Vector2(150f, 42f);
        rect.anchoredPosition = new Vector2(-18f, -18f);

        Image image = go.GetComponent<Image>();
        image.color = new Color(0.04f, 0.08f, 0.07f, 0.9f);

        Button button = go.GetComponent<Button>();
        button.onClick.AddListener(ReturnToMainMenu);
        ColorBlock colors = button.colors;
        colors.normalColor = image.color;
        colors.highlightedColor = new Color(0.16f, 0.28f, 0.22f, 0.98f);
        colors.pressedColor = new Color(0.38f, 0.42f, 0.22f, 1f);
        button.colors = colors;

        TextMeshProUGUI label = CreateText(rect, "Label", "Ana Menü", 20, Vector2.zero, FontStyles.Bold, new Color(1f, 0.94f, 0.78f, 1f));
        label.rectTransform.sizeDelta = rect.sizeDelta;
        return button;
    }
}
