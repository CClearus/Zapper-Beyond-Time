using UnityEngine;
using UnityEngine.SceneManagement;  // Needed for scene loading

public class SceneChanger : MonoBehaviour
{
    // Call this function from your button's OnClick
    public void ChangeScene(string Game)
    {
        SceneManager.LoadScene("Game");
    }
}
