using UnityEngine;
using TMPro; // Only if using TextMeshPro
using UnityEngine.UI;
using System.Collections;

public class DialogueManager : MonoBehaviour
{
    [Header("UI")]
    public TextMeshProUGUI dialogueText; // Drag your TMP text here
    [Header("Settings")]
    public float displayDuration = 3f;   // Can edit duration in Inspector

    private Coroutine dialogueRoutine;

    public void ShowDialogue(string message)
    {
        if (dialogueRoutine != null)
            StopCoroutine(dialogueRoutine);

        dialogueRoutine = StartCoroutine(ShowDialogueRoutine(message));
    }

    private IEnumerator ShowDialogueRoutine(string message)
    {
        dialogueText.text = message;
        dialogueText.gameObject.SetActive(true);

        yield return new WaitForSeconds(displayDuration);

        dialogueText.gameObject.SetActive(false);
    }
}
