using UnityEngine;

public class PlayerInteract : MonoBehaviour
{
    public GameObject interactionPanel;
    private bool isPanelActive = false;

    public FirstPersonCam playerMovement;

    private bool isInRange = false;

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.E) && isInRange)
        {
            if (!isPanelActive)
            {
                OpenPanel();
            }
            else
            {
                ClosePanel();
            }
        }   
    }

    void OpenPanel()
    {
        interactionPanel.SetActive(true);
        isPanelActive = true;

        playerMovement.enabled = false;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        Time.timeScale = 0f;
    }

    void ClosePanel()
    {
        interactionPanel.SetActive(false);
        isPanelActive = false;

        playerMovement.enabled = true;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        Time.timeScale = 1f;
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Interactable"))
        {
            isInRange = true;
            Debug.Log("Player is in range");
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Interactable"))
        {
            isInRange = false;
            Debug.Log("Player left range");
        }
    }

    
}
