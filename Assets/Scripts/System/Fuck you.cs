using UnityEngine;

public class DefaultAnimatedCursor : MonoBehaviour
{
    public static DefaultAnimatedCursor Instance; // Singleton access

    [Header("Default Cursor Animation")]
    public Texture2D[] frames;
    public float frameRate = 10f;
    public Vector2 hotspot = Vector2.zero;
    public CursorMode cursorMode = CursorMode.Auto;

    private int currentFrame = 0;
    private float timer = 0f;

    private bool overrideActive = false;

    private void Awake()
    {
        Instance = this; // Store global reference
    }

    private void Update()
    {
        if (overrideActive) return; // Don't animate if hover is overriding

        if (frames == null || frames.Length == 0) return;

        timer += Time.deltaTime;
        if (timer >= 1f / frameRate)
        {
            timer = 0f;
            currentFrame = (currentFrame + 1) % frames.Length;
            Cursor.SetCursor(frames[currentFrame], hotspot, cursorMode);
        }
    }

    // Called by hover script to take control
    public void SetOverride(Texture2D frame, Vector2 hotspot, CursorMode mode)
    {
        overrideActive = true;
        Cursor.SetCursor(frame, hotspot, mode);
    }

    // Called by hover script to release control
    public void ReleaseOverride()
    {
        overrideActive = false;
        timer = 0f; // resume cleanly
    }
}
