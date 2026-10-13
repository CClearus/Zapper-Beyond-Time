
using UnityEngine;         // Gives you access to Unity API like MonoBehaviour, Transform, etc.
using System.Collections;  // Gives you access to IEnumerator for Coroutines.
using System.Collections.Generic;

public class SlideUpDoor : MonoBehaviour
{
    public Transform door;           // The door to slide up
    public Vector3 openOffset = new Vector3(0, 3, 0); // How far up to slide
    public float openSpeed = 2f;     // How fast it opens
    public float stayOpenTime = 3f;  // How long to stay open

    private Vector3 closedPosition;
    private Vector3 openPosition;
    private bool isOpening = false;
    private bool isClosing = false;

    void Start()
    {
        closedPosition = door.position;
        openPosition = closedPosition + openOffset;
    }

    void Update()
    {
        if (isOpening)
        {
            door.position = Vector3.MoveTowards(door.position, openPosition, openSpeed * Time.deltaTime);

            if (Vector3.Distance(door.position, openPosition) < 0.01f)
            {
                isOpening = false;
                StartCoroutine(StayOpenThenClose());
            }
        }
        else if (isClosing)
        {
            door.position = Vector3.MoveTowards(door.position, closedPosition, openSpeed * Time.deltaTime);

            if (Vector3.Distance(door.position, closedPosition) < 0.01f)
            {
                isClosing = false;
            }
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isOpening = true;
            isClosing = false; // Stop closing if it was
            StopAllCoroutines(); // Stop any running coroutine to reset timer
        }
    }

    IEnumerator StayOpenThenClose()
    {
        yield return new WaitForSeconds(stayOpenTime);
        isClosing = true;
    }
}
