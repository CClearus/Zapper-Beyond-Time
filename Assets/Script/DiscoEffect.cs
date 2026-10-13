using UnityEngine;
using UnityEngine.UI;

public class DiscoEffect : MonoBehaviour
{
    public RawImage rawImage;
    public float colorChangeSpeed = 5f;

    void Start()
    {
        if (rawImage == null)
            rawImage = GetComponent<RawImage>();
    }

    void Update()
    {
        // Disco rainbow color flashing
        float r = Mathf.Sin(Time.time * colorChangeSpeed + 0f) * 0.5f + 0.5f;
        float g = Mathf.Sin(Time.time * colorChangeSpeed + 2f) * 0.5f + 0.5f;
        float b = Mathf.Sin(Time.time * colorChangeSpeed + 4f) * 0.5f + 0.5f;
        rawImage.color = new Color(r, g, b, 1f);
    }
}
