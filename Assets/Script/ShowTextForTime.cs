using UnityEngine;
using TMPro; // only if using TextMeshPro

public class ShowTextForTime : MonoBehaviour
{
    public TMP_Text messageText; // assign in inspector
    public float displayTime = 5f; // show text for 5 seconds

    void Start()
    {
        if (messageText != null)
        {
            messageText.gameObject.SetActive(true);
            Invoke("HideText", displayTime);
        }
    }

    void HideText()
    {
        if (messageText != null)
            messageText.gameObject.SetActive(false);
    }
}
