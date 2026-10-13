using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using TMPro; // For TextMeshPro

public class MainMenuFade : MonoBehaviour
{
    public CanvasGroup fadeScreen;
    public float fadeDuration = 1f;
    public float delayBetweenTexts = 2f; // delay before showing next text
    public string sceneToLoad = "GameScene";

    public TextMeshProUGUI[] fadeTexts; // assign all texts in order

    private bool isFading = false;

    public void OnPlayButtonPressed()
    {
        if (!isFading)
            StartCoroutine(FadeAndLoadScene());
    }

    private IEnumerator FadeAndLoadScene()
    {
        isFading = true;

        fadeScreen.gameObject.SetActive(true);
        fadeScreen.alpha = 0;

        // Hide all texts at the start
        foreach (var txt in fadeTexts)
            if (txt != null) txt.gameObject.SetActive(false);

        // Fade to black
        float t = 0f;
        while (t < fadeDuration)
        {
            t += Time.deltaTime;
            fadeScreen.alpha = Mathf.Lerp(0f, 1f, t / fadeDuration);
            yield return null;
        }

        fadeScreen.alpha = 1f;

        // Show texts one by one, keeping the previous ones visible
        for (int i = 0; i < fadeTexts.Length; i++)
        {
            if (fadeTexts[i] != null)
                fadeTexts[i].gameObject.SetActive(true);

            yield return new WaitForSeconds(delayBetweenTexts);
        }

        // Wait a little before changing scene
        yield return new WaitForSeconds(2f);

        SceneManager.LoadScene(sceneToLoad);
    }
}
