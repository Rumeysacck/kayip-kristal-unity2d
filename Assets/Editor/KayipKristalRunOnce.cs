using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;

public static class KayipKristalRunOnce
{
    private const string FlagPath = "/private/tmp/kayip_kristal_run_once.flag";

    [InitializeOnLoadMethod]
    private static void RunIfRequested()
    {
        if (!File.Exists(FlagPath)) return;

        File.Delete(FlagPath);
        EditorApplication.delayCall += () =>
        {
            EditorSceneManager.OpenScene("Assets/Scenes/TitleScreen.unity", OpenSceneMode.Single);
            EditorApplication.isPlaying = true;
        };
    }
}
