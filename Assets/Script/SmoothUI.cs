using UnityEngine;
using System.Collections;

public class SmoothUI : MonoBehaviour
{
    public CanvasGroup uiElement;
    public float fadeDuration = 0.5f;

    // Call this to show UI
    public void ShowUI()
    {
        StartCoroutine(FadeUI(uiElement, uiElement.alpha, 1));
    }

    // Call this to hide UI
    public void HideUI()
    {
        StartCoroutine(FadeUI(uiElement, uiElement.alpha, 0));
    }

    private IEnumerator FadeUI(CanvasGroup cg, float start, float end)
    {
        float elapsed = 0f;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            cg.alpha = Mathf.Lerp(start, end, elapsed / fadeDuration);
            yield return null;
        }

        cg.alpha = end;

        // Optionally disable interaction when hidden
        cg.interactable = (end == 1);
        cg.blocksRaycasts = (end == 1);
    }
}
