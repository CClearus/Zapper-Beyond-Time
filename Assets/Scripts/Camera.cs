using UnityEngine;

public class BlenderLikeCamera : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float fastMoveMultiplier = 3f;
    [SerializeField] private float acceleration = 10f;
    [SerializeField] private float deceleration = 10f;

    [Header("Orbit")]
    [SerializeField] private float orbitSpeed = 5f;
    [SerializeField] private Transform orbitTarget;

    [Header("Zoom & Pan")]
    [SerializeField] private float zoomSpeed = 10f;
    [SerializeField] private float panSpeed = 0.5f;

    [Header("Free Move (Fly Mode)")]
    [SerializeField] private KeyCode toggleFreeMoveKey = KeyCode.F; // press F to toggle free move
    [SerializeField] private float lookSensitivity = 2f;
    [SerializeField] private bool freeMoveActive = false;

    private Vector3 targetPosition;
    private Vector3 velocity;

    void Start()
    {
        targetPosition = transform.position;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    void Update()
    {
        if (Input.GetKeyDown(toggleFreeMoveKey))
        {
            freeMoveActive = !freeMoveActive;
            Cursor.lockState = freeMoveActive ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !freeMoveActive;
        }

        if (freeMoveActive)
        {
            HandleFreeMove();
        }
        else
        {
            HandleMovement();
            HandleOrbit();
            HandlePan();
            HandleZoom();
        }

        // Smooth position transition
        transform.position = Vector3.SmoothDamp(transform.position, targetPosition, ref velocity, 1f / acceleration);
    }

    private void HandleMovement()
    {
        float h = Input.GetAxis("Horizontal"); // A/D
        float v = Input.GetAxis("Vertical");   // W/S
        float upDown = 0;

        if (Input.GetKey(KeyCode.E)) upDown = 1;   // Move Up
        if (Input.GetKey(KeyCode.Q)) upDown = -1;  // Move Down

        Vector3 move = (transform.forward * v + transform.right * h + transform.up * upDown).normalized;

        float speed = moveSpeed * (Input.GetKey(KeyCode.LeftShift) ? fastMoveMultiplier : 1f);
        targetPosition += move * speed * Time.deltaTime;
    }

    private void HandleOrbit()
    {
        if (Input.GetMouseButton(2)) // MMB
        {
            float mouseX = Input.GetAxis("Mouse X") * orbitSpeed;
            float mouseY = -Input.GetAxis("Mouse Y") * orbitSpeed;

            if (orbitTarget != null)
            {
                transform.RotateAround(orbitTarget.position, Vector3.up, mouseX);
                transform.RotateAround(orbitTarget.position, transform.right, mouseY);
                targetPosition = transform.position;
            }
            else
            {
                transform.Rotate(Vector3.up, mouseX, Space.World);
                transform.Rotate(transform.right, mouseY, Space.World);
            }
        }
    }

    private void HandlePan()
    {
        if (Input.GetMouseButton(1)) // RMB
        {
            float panX = -Input.GetAxis("Mouse X") * panSpeed;
            float panY = -Input.GetAxis("Mouse Y") * panSpeed;

            Vector3 right = transform.right * panX;
            Vector3 up = transform.up * panY;

            targetPosition += right + up;
        }
    }

    private void HandleZoom()
    {
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (Mathf.Abs(scroll) > 0.01f)
        {
            targetPosition += transform.forward * scroll * zoomSpeed;
        }
    }

    private void HandleFreeMove()
    {
        // Look around with mouse
        float mouseX = Input.GetAxis("Mouse X") * lookSensitivity;
        float mouseY = -Input.GetAxis("Mouse Y") * lookSensitivity;

        transform.Rotate(Vector3.up, mouseX, Space.World);
        transform.Rotate(transform.right, mouseY, Space.Self);

        // WASD + QE to move
        float h = Input.GetAxis("Horizontal");
        float v = Input.GetAxis("Vertical");
        float upDown = 0;
        if (Input.GetKey(KeyCode.E)) upDown = 1;
        if (Input.GetKey(KeyCode.Q)) upDown = -1;

        Vector3 move = (transform.forward * v + transform.right * h + transform.up * upDown).normalized;
        float speed = moveSpeed * (Input.GetKey(KeyCode.LeftShift) ? fastMoveMultiplier : 1f);

        targetPosition += move * speed * Time.deltaTime;
    }
}
