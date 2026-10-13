using UnityEngine;
using TMPro;
using System.Collections;

public class ShowTextSequence : MonoBehaviour
{
    public TMP_Text[] messages;   // Assign your texts in the inspector (order matters)
    public float displayTime = 5f; // Each text stays for 5 seconds

    void Start()
    {
        StartCoroutine(PlayMessages());
    }

    private IEnumerator PlayMessages()
    {
        // Hide all at start
        foreach (var msg in messages)
            msg.gameObject.SetActive(false);

        // Show each one in order
        foreach (var msg in messages)
        {
            msg.gameObject.SetActive(true);
            yield return new WaitForSeconds(displayTime);
            msg.gameObject.SetActive(false);
        }
    }
}
