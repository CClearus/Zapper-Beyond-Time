using UnityEngine;

public class LoopSound : MonoBehaviour
{
    public AudioSource audioSource;

    void Start()
    {
        // Play loop when the scene starts
        audioSource.loop = true;
        audioSource.Play();
    }

    // Example: Stop looping sound
    void StopLoop()
    {
        audioSource.Stop();
    }
}
