using UnityEngine;

public class AddBoxCollidersToChildren : MonoBehaviour
{
    [ContextMenu("Remove Box Colliders From Children")]
    void RemoveColliders()
    {
        int count = 0;

        foreach (Transform child in transform)
        {
            BoxCollider box = child.GetComponent<BoxCollider>();
            if (box != null)
            {
                DestroyImmediate(box);
                count++;
            }
        }

        Debug.Log($"Removed {count} BoxColliders from children.");
    }
}
