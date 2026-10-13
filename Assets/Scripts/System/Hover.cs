using UnityEngine;
using UnityEngine.EventSystems;

public class HoverAnimatedCursor : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Hover Cursor Animation")]
    public Texture2D[] hoverFrames;
    public float hoverFrameRate = 10f;
    public Vector2 hotspot = Vector2.zero;
    public CursorMode cursorMode = CursorMode.Auto;

    private int currentFrame = 0;
    private float timer = 0f;
    private bool isHovering = false;

    void Update()
    {
        if (!isHovering || hoverFrames == null || hoverFrames.Length == 0) return;

        timer += Time.deltaTime;
        if (timer >= 1f / hoverFrameRate)
        {
            timer = 0f;
            currentFrame = (currentFrame + 1) % hoverFrames.Length;

            if (DefaultAnimatedCursor.Instance != null)
            {
                DefaultAnimatedCursor.Instance.SetOverride(
                    hoverFrames[currentFrame], hotspot, cursorMode
                );
            }
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        isHovering = true;
        currentFrame = 0;
        timer = 0f;

        if (DefaultAnimatedCursor.Instance != null && hoverFrames.Length > 0)
        {
            DefaultAnimatedCursor.Instance.SetOverride(
                hoverFrames[currentFrame], hotspot, cursorMode
            );
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        isHovering = false;
        if (DefaultAnimatedCursor.Instance != null)
        {
            DefaultAnimatedCursor.Instance.ReleaseOverride();
        }
    }
}
