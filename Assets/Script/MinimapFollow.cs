using UnityEngine;

public class MinimapFollow : MonoBehaviour
{
    public Transform target; // Assign your player here

    void LateUpdate()
    {
        Vector3 newPos = target.position;
        newPos.y = transform.position.y; // Keep the original height
        transform.position = newPos;

        // Do NOT rotate — keep camera fixed looking straight down
        transform.rotation = Quaternion.Euler(90f, 0f, 0f);
    }
}
