using UnityEngine;
using UnityEngine.SceneManagement;

public static class RuntimeMenuCleaner
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void RegisterSceneCleanup()
    {
        SceneManager.sceneLoaded -= CleanupTitleMenuCanvases;
        SceneManager.sceneLoaded += CleanupTitleMenuCanvases;
    }

    private static void CleanupTitleMenuCanvases(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == "TitleScreen") return;

        HideAndDestroy("KayipKristal_HomeCanvas");
        HideAndDestroy("KayipKristal_LevelSelectCanvas");
    }

    private static void HideAndDestroy(string objectName)
    {
        GameObject canvas = GameObject.Find(objectName);
        if (canvas == null) return;

        canvas.SetActive(false);
        Object.Destroy(canvas);
    }
}
