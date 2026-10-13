using UnityEngine;

public class LightController : MonoBehaviour
{
    public GameObject lightItem; // Assign your Light object here

    // This will be called when the button is pressed
    public void ActivateLight()
    {
        if (lightItem != null)
        {
            lightItem.SetActive(true);
        }
    }
}
