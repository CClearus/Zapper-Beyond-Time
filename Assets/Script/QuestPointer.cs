using UnityEngine;

public class QuestPointer : MonoBehaviour
{
    public Transform questItem;   // Assign quest object
    public Transform playerCam;   // Assign Main Camera

    void Update()
    {
        if (questItem == null || playerCam == null) return;

        // Keep this pointer above quest item
        transform.position = questItem.position + Vector3.up * 2f;

        // Make pointer always face camera
        transform.LookAt(playerCam);
    }
}
