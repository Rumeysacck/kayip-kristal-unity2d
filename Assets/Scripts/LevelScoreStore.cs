using UnityEngine;

public static class LevelScoreStore
{
    public static readonly (string sceneName, string title)[] Levels =
    {
        ("SampleLevel", "1. Bölüm: Yeşil Koru"),
        ("Level2_GreenRuins", "2. Bölüm: Mavi Harabeler"),
        ("Level3_CrimsonKeep", "3. Bölüm: Kızıl Kale")
    };

    public static int GetHighScore(string sceneName)
    {
        return PlayerPrefs.GetInt(GetKey(sceneName), 0);
    }

    public static bool SaveHighScore(string sceneName, int score)
    {
        int currentBest = GetHighScore(sceneName);
        if (score <= currentBest) return false;

        PlayerPrefs.SetInt(GetKey(sceneName), score);
        PlayerPrefs.Save();
        return true;
    }

    private static string GetKey(string sceneName)
    {
        return $"KayipKristal.HighScore.{sceneName}";
    }
}
