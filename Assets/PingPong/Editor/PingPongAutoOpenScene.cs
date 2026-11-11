using UnityEditor;
using UnityEditor.SceneManagement;

namespace PingPong.EditorTools
{
    /// <summary>
    /// When the project is opened on a new computer Unity starts with an empty "Untitled" scene
    /// (the last opened scene is stored in the Library folder, which isn't in Git).
    /// This opens the PingPong scene automatically in that case.
    /// </summary>
    [InitializeOnLoad]
    static class PingPongAutoOpenScene
    {
        const string ScenePath = "Assets/PingPong/Scenes/PingPong.unity";
        const string SessionKey = "PingPong.AutoOpenScene.Done";

        static PingPongAutoOpenScene()
        {
            if (SessionState.GetBool(SessionKey, false)) return;
            SessionState.SetBool(SessionKey, true);
            EditorApplication.delayCall += OpenIfEmpty;
        }

        static void OpenIfEmpty()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            var active = EditorSceneManager.GetActiveScene();
            if (!string.IsNullOrEmpty(active.path) || active.isDirty) return; // a real scene is already open
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) == null) return;
            EditorSceneManager.OpenScene(ScenePath);
        }
    }
}
