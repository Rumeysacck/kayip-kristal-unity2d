using TMPro;
using UnityEngine;

public static class TurkishTextStyle
{
    private static TMP_FontAsset _font;

    public static void Apply(TMP_Text text)
    {
        if (text == null) return;

        TMP_FontAsset font = ResolveFont();
        if (font != null)
        {
            text.font = font;
            text.fontSharedMaterial = font.material;
        }

        text.characterSpacing = 0f;
    }

    private static TMP_FontAsset ResolveFont()
    {
        if (_font != null) return _font;

        _font = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
        return _font;
    }
}
