using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneHotkeyLoader : MonoBehaviour
{
    [Header("Scene Settings")]
    public string sceneName = "NextScene"; // Name of the scene to load
    public KeyCode hotkey = KeyCode.Semicolon; // The hotkey (default: ;)

    void Update()
    {
        if (Input.GetKeyDown(hotkey))
        {
            LoadScene();
        }
    }

    void LoadScene()
    {
        if (Application.CanStreamedLevelBeLoaded(sceneName))
        {
            SceneManager.LoadScene(sceneName);
        }
        else
        {
            Debug.LogWarning($"Scene '{sceneName}' not found! Make sure it's added to Build Settings.");
        }
    }
}
