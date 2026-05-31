using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MissionHUD : MonoBehaviour
{
    private enum HudIconKind
    {
        Crystal,
        Score,
        Health,
        Objective
    }

    [Header("Text")]
    public TMP_Text CrystalText;
    public TMP_Text ScoreText;
    public TMP_Text HealthText;
    public TMP_Text ObjectiveText;
    public TMP_Text ExitHintText;
    public TMP_Text ToastText;

    [Header("Optional Bar")]
    public Slider CrystalProgress;
    private Actor _playerActor;
    private float _toastUntil;
    private Image _crystalIcon;
    private Image _scoreIcon;
    private Image _healthIcon;
    private Image _objectiveIcon;

    private void OnEnable()
    {
        if (MissionState.Instance == null) return;

        ApplyTextStyle();
        EnsureHudIcons();
        _playerActor = PlayerLocator.FindMainActor();
        MissionState.Instance.Changed += UpdateView;
        MissionState.Instance.Completed += ShowCompleted;
        UpdateView(MissionState.Instance.Crystals, MissionState.Instance.RequiredCrystals, MissionState.Instance.Score);
    }

    private void Update()
    {
        if (HealthText != null && _playerActor != null)
            HealthText.text = $"Can: {_playerActor.Health}";

        if (ToastText != null && ToastText.gameObject.activeSelf && Time.unscaledTime > _toastUntil)
            ToastText.gameObject.SetActive(false);
    }

    private void OnDisable()
    {
        if (MissionState.Instance == null) return;

        MissionState.Instance.Changed -= UpdateView;
        MissionState.Instance.Completed -= ShowCompleted;
    }

    private void UpdateView(int crystals, int requiredCrystals, int score)
    {
        if (CrystalText != null)
            CrystalText.text = $"Kristaller: {crystals}/{requiredCrystals}";
        if (ScoreText != null)
            ScoreText.text = $"Puan: {score}";
        if (HealthText != null && _playerActor != null)
            HealthText.text = $"Can: {_playerActor.Health}";
        if (ObjectiveText != null)
            ObjectiveText.text = crystals >= requiredCrystals
                ? "Kale kapısı açıldı. Devam etmek için içeri gir."
                : ResolveObjectiveText(requiredCrystals);
        if (ExitHintText != null)
        {
            ExitHintText.text = crystals >= requiredCrystals ? "KAPI AÇIK ->" : "";
            ExitHintText.gameObject.SetActive(crystals >= requiredCrystals);
        }
        if (CrystalProgress != null)
        {
            CrystalProgress.maxValue = requiredCrystals;
            CrystalProgress.value = crystals;
        }
    }

    private void ShowCompleted()
    {
        if (ObjectiveText != null)
            ObjectiveText.text = "Tüm kristaller toplandı. Kale kapısından içeri gir.";
        if (ExitHintText != null)
        {
            ExitHintText.text = "KAPI AÇIK ->";
            ExitHintText.gameObject.SetActive(true);
        }
        ShowToast("Kale kapısı açıldı. İçeri gir.");
    }

    private string ResolveObjectiveText(int requiredCrystals)
    {
        string sceneName = SceneManager.GetActiveScene().name;
        if (sceneName.Contains("Level2"))
            return $"MAVİ HARABELER: {requiredCrystals} safir topla, sonra kale kapısından gir.";
        if (sceneName.Contains("Level3"))
            return $"KIZIL KALE: {requiredCrystals} yakut topla, sonra kale kapısından gir.";
        return $"YEŞİL KORU: {requiredCrystals} zümrüt kristali topla, tehlikelerden kaç ve kale kapısına ulaş.";
    }

    public void ShowToast(string message, float seconds = 2.5f)
    {
        if (ToastText == null) return;

        ToastText.text = message;
        ToastText.gameObject.SetActive(true);
        _toastUntil = Time.unscaledTime + seconds;
    }

    private void ApplyTextStyle()
    {
        TurkishTextStyle.Apply(CrystalText);
        TurkishTextStyle.Apply(ScoreText);
        TurkishTextStyle.Apply(HealthText);
        TurkishTextStyle.Apply(ObjectiveText);
        TurkishTextStyle.Apply(ExitHintText);
        TurkishTextStyle.Apply(ToastText);
    }

    private void EnsureHudIcons()
    {
        EnsureIcon(ref _crystalIcon, CrystalText, HudIconKind.Crystal, new Color(0.22f, 0.96f, 1f, 1f));
        EnsureIcon(ref _scoreIcon, ScoreText, HudIconKind.Score, new Color(1f, 0.82f, 0.24f, 1f));
        EnsureIcon(ref _healthIcon, HealthText, HudIconKind.Health, new Color(1f, 0.27f, 0.33f, 1f));
        EnsureIcon(ref _objectiveIcon, ObjectiveText, HudIconKind.Objective, new Color(0.58f, 1f, 0.38f, 1f));
    }

    private void EnsureIcon(ref Image icon, TMP_Text text, HudIconKind kind, Color color)
    {
        if (icon != null || text == null || text.rectTransform == null) return;

        RectTransform textRect = text.rectTransform;
        RectTransform root = textRect.parent as RectTransform;
        if (root == null) return;

        string iconName = $"{text.name}_Icon";
        Transform existing = root.Find(iconName);
        if (existing != null)
        {
            icon = existing.GetComponent<Image>();
            return;
        }

        Vector2 originalPosition = textRect.anchoredPosition;
        textRect.anchoredPosition += new Vector2(20f, 0f);

        GameObject iconGo = new GameObject(iconName, typeof(Image));
        iconGo.transform.SetParent(root, false);

        RectTransform iconRect = iconGo.GetComponent<RectTransform>();
        iconRect.anchorMin = textRect.anchorMin;
        iconRect.anchorMax = textRect.anchorMax;
        iconRect.pivot = textRect.pivot;
        iconRect.sizeDelta = new Vector2(15f, 15f);
        iconRect.anchoredPosition = originalPosition + new Vector2(6f, 0f);

        icon = iconGo.GetComponent<Image>();
        icon.sprite = CreateIconSprite(kind, color);
        icon.color = Color.white;
        icon.raycastTarget = false;
    }

    private static Sprite CreateIconSprite(HudIconKind kind, Color color)
    {
        const int size = 24;
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        Color edge = new Color(color.r * 0.32f, color.g * 0.32f, color.b * 0.32f, 1f);

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                bool fill = IsIconPixel(kind, x, y, size, false);
                bool outer = IsIconPixel(kind, x, y, size, true);
                texture.SetPixel(x, y, outer && !fill ? edge : fill ? color : Color.clear);
            }
        }

        texture.filterMode = FilterMode.Point;
        texture.Apply();
        return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
    }

    private static bool IsIconPixel(HudIconKind kind, int x, int y, int size, bool outline)
    {
        float px = x - size * 0.5f;
        float py = y - size * 0.5f;
        float inflate = outline ? 1.6f : 0f;

        switch (kind)
        {
            case HudIconKind.Crystal:
                return Mathf.Abs(px) * 0.75f + Mathf.Abs(py) < 8.5f + inflate;
            case HudIconKind.Score:
            {
                float angle = Mathf.Atan2(py, px);
                float radius = outline ? 9.7f : 8.2f;
                float starRadius = radius * (0.72f + 0.28f * Mathf.Cos(angle * 5f));
                return new Vector2(px, py).magnitude < starRadius;
            }
            case HudIconKind.Health:
            {
                Vector2 left = new Vector2(px + 4f, py - 3f);
                Vector2 right = new Vector2(px - 4f, py - 3f);
                bool lobes = left.sqrMagnitude < (5.5f + inflate) * (5.5f + inflate)
                    || right.sqrMagnitude < (5.5f + inflate) * (5.5f + inflate);
                bool point = py < 4f + inflate && py > -8f - inflate && Mathf.Abs(px) < 9f + py + inflate;
                return lobes || point;
            }
            default:
                bool tower = (x >= 4 - inflate && x <= 8 + inflate || x >= 16 - inflate && x <= 20 + inflate) && y >= 6 - inflate && y <= 18 + inflate;
                bool wall = x >= 7 - inflate && x <= 17 + inflate && y >= 5 - inflate && y <= 14 + inflate;
                bool battlement = y >= 16 - inflate && y <= 19 + inflate && (x < 7 || x > 10 && x < 14 || x > 17);
                return tower || wall || battlement;
        }
    }
}
