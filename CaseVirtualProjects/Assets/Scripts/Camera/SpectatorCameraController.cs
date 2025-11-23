using UnityEngine;

public class SpectatorCameraController : MonoBehaviour
{
    [Header("Hareket")]
    public float moveSpeed = 10f;
    public float fastMultiplier = 2f;

    [Header("Mouse Look")]
    public float mouseSensitivity = 2f;
    public float minPitch = -80f;
    public float maxPitch = 80f;

    [Header("Zoom (FOV)")]
    public float zoomSpeed = 10f;
    public float minFov = 25f;
    public float maxFov = 80f;

    private float yaw;
    private float pitch;
    private Camera cam;

    private void Start()
    {
        cam = GetComponent<Camera>();

        Vector3 angles = transform.eulerAngles;
        yaw = angles.y;
        pitch = angles.x;
    }

    private void Update()
    {
        HandleMouseLook();
        HandleMovement();
        HandleZoom();
    }

    private void HandleMouseLook()
    {
        if (Input.GetMouseButton(1))
        {
            float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
            float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

            yaw += mouseX;
            pitch -= mouseY;
            pitch = Mathf.Clamp(pitch, minPitch, maxPitch);

            transform.rotation = Quaternion.Euler(pitch, yaw, 0f);
        }
    }

    private void HandleMovement()
    {
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");

        Vector3 move = (transform.forward * v + transform.right * h).normalized;

        float speed = moveSpeed;
        if (Input.GetKey(KeyCode.LeftShift))
            speed *= fastMultiplier;

        transform.position += move * speed * Time.deltaTime;

        float up = 0f;
        if (Input.GetKey(KeyCode.E)) up += 1f;
        if (Input.GetKey(KeyCode.Q)) up -= 1f;

        if (up != 0f)
        {
            transform.position += Vector3.up * up * speed * 0.7f * Time.deltaTime;
        }
    }

    private void HandleZoom()
    {
        if (cam == null) return;

        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (Mathf.Abs(scroll) > 0.0001f)
        {
            float fov = cam.fieldOfView;
            fov -= scroll * zoomSpeed;
            fov = Mathf.Clamp(fov, minFov, maxFov);
            cam.fieldOfView = fov;
        }
    }
}
