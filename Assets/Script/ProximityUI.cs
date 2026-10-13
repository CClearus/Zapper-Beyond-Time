using UnityEngine;
using System.Collections;

public class ProximityUI : MonoBehaviour
{
    public GameObject uiPanel;     // Assign your UI element here
    public string playerTag = "Player";
    public float fadeDuration = 0.5f; // seconds for fade

    private CanvasGroup canvasGroup;
    private Coroutine fadeCoroutine;

    void Start()
    {
        // Add CanvasGroup if missing
        canvasGroup = uiPanel.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = uiPanel.AddComponent<CanvasGroup>();
        }

        canvasGroup.alpha = 0;
        uiPanel.SetActive(false); 
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(playerTag))
        {
            if (fadeCoroutine != null) StopCoroutine(fadeCoroutine);
            uiPanel.SetActive(true);
            fadeCoroutine = StartCoroutine(FadeUI(1)); // fade in
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag(playerTag))
        {
            if (fadeCoroutine != null) StopCoroutine(fadeCoroutine);
            fadeCoroutine = StartCoroutine(FadeUI(0)); // fade out
        }
    }

    IEnumerator FadeUI(float targetAlpha)
    {
        float startAlpha = canvasGroup.alpha;
        float time = 0f;

        while (time < fadeDuration)
        {
            time += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, time / fadeDuration);
            yield return null;
        }

        canvasGroup.alpha = targetAlpha;

        // Only disable after fade out is complete
        if (targetAlpha == 0)
        {
            uiPanel.SetActive(false);
        }
    }
}
