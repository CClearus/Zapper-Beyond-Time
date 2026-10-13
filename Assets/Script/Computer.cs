using UnityEngine;

public class Computer : MonoBehaviour, IInteractable
{
    public string GetDescription()
    {
        return "Press E to open the Computer.";
    }

    public void Interact()
    {
        Debug.Log("The Computer opens!");

    }
}
