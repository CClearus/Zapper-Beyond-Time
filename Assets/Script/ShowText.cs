using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class ShowText : MonoBehaviour
{
    public GameObject[] textObjects;
    public float displayTime = 3f;
    public float startDelay = 2f;

    void Start()
    {
       
        foreach (GameObject textObj in textObjects)
        {
            textObj.SetActive(false);
        }

       
        StartCoroutine(ShowTextsInSequence());
    }

    IEnumerator ShowTextsInSequence()
    {
        yield return new WaitForSeconds(startDelay);

        foreach (GameObject textObj in textObjects)
        {
            textObj.SetActive(true);
            yield return new WaitForSeconds(displayTime);
            textObj.SetActive(false);
        }
    }
}
