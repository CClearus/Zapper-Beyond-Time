using UnityEngine;
using UnityEngine.UI;

public class SceneFadeIn : MonoBehaviour
{
    private Image fadeImage;
    private float timer = 0f;
    private float fadeDuration = 2f; // fixed to 2 seconds

    void Awake()
    {
        fadeImage = GetComponent<Image>();
        // Start fully black
        fadeImage.color = new Color(0f, 0f, 0f, 1f);
    }

    void Update()
    {
        if (timer < fadeDuration)
        {
            timer += Time.deltaTime;
            float alpha = Mathf.Lerp(1f, 0f, timer / fadeDuration);
            fadeImage.color = new Color(0f, 0f, 0f, alpha);
        }
    }
}
