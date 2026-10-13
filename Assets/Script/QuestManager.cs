using UnityEngine;
using UnityEngine.UI;

public class QuestManager : MonoBehaviour
{
    public GameObject questPanel;     // Your quest UI panel
    public GameObject questPointer;   // The pointer object (disabled at start)

    public void OnAcceptQuest()
    {
        // Hide the quest UI panel
        questPanel.SetActive(false);

        // Enable the quest pointer
        questPointer.SetActive(true);

        // (Optional) Show text in console or UI
        Debug.Log("Quest Started! Pointer enabled.");
    }
}
