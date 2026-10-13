using UnityEngine;
using System.Collections;

public class LoadingScreen : MonoBehaviour
{
    public GameObject loadingScreen; // Assign your loading panel here

    public void ShowLoadingScreen()
    {
        StartCoroutine(LoadingRoutine());
    }

    IEnumerator LoadingRoutine()
    {
        loadingScreen.SetActive(true);     // Show UI
        yield return new WaitForSeconds(5); // Wait 5 seconds
        loadingScreen.SetActive(false);    // Hide UI
    }
}
