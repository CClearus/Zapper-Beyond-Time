using UnityEngine;

public class CameraManager : MonoBehaviour
{
    public Camera currentCamera;

    public void SwitchToCamera(Camera newCamera)
    {
        if (currentCamera != null)
        {
            currentCamera.enabled = false;
        }

        newCamera.enabled = true;
        currentCamera = newCamera;
    }
}
