using UnityEngine;

public class Optionui : MonoBehaviour
{
    public GameObject uiPanel;       // Assign your UI panel
    public string playerTag = "Player";
    private bool isPlayerNearby = false;
    private GameObject player;
    private Rigidbody playerRb;

    void Start()
    {
        uiPanel.SetActive(false);
    }

    void Update()
    {
        if (isPlayerNearby && Input.GetKeyDown(KeyCode.E))
        {
            bool isActive = !uiPanel.activeSelf;
            uiPanel.SetActive(isActive);

            if (playerRb != null)
            {
                if (isActive)
                {
                    // Freeze player movement
                    playerRb.constraints = RigidbodyConstraints.FreezeAll;
                }
                else
                {
                    // Unfreeze player movement
                    playerRb.constraints = RigidbodyConstraints.None;
                }
            }
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(playerTag))
        {
            isPlayerNearby = true;
            player = other.gameObject;
            playerRb = player.GetComponent<Rigidbody>();
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag(playerTag))
        {
            isPlayerNearby = false;
            uiPanel.SetActive(false);

            if (playerRb != null)
            {
                playerRb.constraints = RigidbodyConstraints.None; // Unfreeze
            }
        }
    }
}
