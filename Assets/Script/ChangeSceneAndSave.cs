using UnityEngine;
using UnityEngine.SceneManagement;

public class ChangeSceneAndSave : MonoBehaviour
{
    [SerializeField] private string sceneToLoad;
    [SerializeField] private Transform player; // Drag your player here

    public void ChangeScene()
    {
        // Save player position before changing the scene
        PlayerPrefs.SetFloat("PlayerX", player.position.x);
        PlayerPrefs.SetFloat("PlayerY", player.position.y);
        PlayerPrefs.SetFloat("PlayerZ", player.position.z);

        // Optionally save the current scene name (for returning later)
        PlayerPrefs.SetString("LastScene", SceneManager.GetActiveScene().name);

        PlayerPrefs.Save();

        // Load new scene
        SceneManager.LoadScene(sceneToLoad);
    }
}
