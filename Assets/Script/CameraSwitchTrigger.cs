using UnityEngine;

public class CameraSwitchTrigger : MonoBehaviour
{
    public Camera mainCamera;
    public Camera thisRoomCamera;

    private bool isInRoom = false;  // Track which camera is currently active

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            // Toggle camera state
            isInRoom = !isInRoom;

            mainCamera.enabled = !isInRoom;
            thisRoomCamera.enabled = isInRoom;
        }
    }
}
