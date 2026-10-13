using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class ScreenFadeIn : MonoBehaviour
{
    [Header("Fade Settings")]
    public Image fadeImage;        // The UI Image covering the screen (black)
    public float fadeDuration = 1f; // How long it takes to fade from black to clear

    void Start()
    {
        if (fadeImage != null)
        {
            // Start fully black
            fadeImage.color = Color.black;
            StartCoroutine(FadeFromBlack());
        }
        else
        {
            Debug.LogWarning("Fade image not assigned in " + gameObject.name);
        }
    }

    IEnumerator FadeFromBlack()
    {
        float elapsed = 0f;
        Color color = fadeImage.color;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            color.a = Mathf.Lerp(1f, 0f, elapsed / fadeDuration);
            fadeImage.color = color;
            yield return null;
        }

        // Make sure it's fully clear at the end
        color.a = 0f;
        fadeImage.color = color;
    }
}
