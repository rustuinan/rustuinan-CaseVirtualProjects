using UnityEngine;
using System.Collections;

public class CameraManager : MonoBehaviour
{
    public Camera spectatorCamera;
    public Camera godCamera;

    public MonoBehaviour spectatorController;
    public MonoBehaviour godController;

    [Header("Input")]
    public KeyCode toggleKey = KeyCode.F;

    [Header("Transition")]
    public float transitionDuration = 0.6f;

    [Header("Game State")]
    public bool startInUIMode = true;

    private bool isGodMode = false;
    private bool isTransitioning = false;

    private void Start()
    {
        if (startInUIMode)
        {
            if (spectatorCamera != null)
                spectatorCamera.gameObject.SetActive(true);
            if (godCamera != null)
                godCamera.gameObject.SetActive(false);

            if (spectatorController != null)
                spectatorController.enabled = false;
            if (godController != null)
                godController.enabled = false;

            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        else
        {
            ForceSetMode(false);
        }
    }

    private void Update()
    {
        if (startInUIMode)
            return;

        if (Input.GetKeyDown(toggleKey) && !isTransitioning)
        {
            isGodMode = !isGodMode;
            StartCoroutine(SmoothSwitch(isGodMode));
        }
    }

    public void StartBattleWithSpectator()
    {
        startInUIMode = false;

        if (godCamera != null)
            godCamera.gameObject.SetActive(false);
        if (godController != null)
            godController.enabled = false;

        if (spectatorCamera != null)
            spectatorCamera.gameObject.SetActive(true);
        if (spectatorController != null)
            spectatorController.enabled = true;

        isGodMode = false;
        isTransitioning = false;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void ForceSetMode(bool godMode)
    {
        isGodMode = godMode;

        if (spectatorCamera != null)
            spectatorCamera.gameObject.SetActive(!godMode);
        if (godCamera != null)
            godCamera.gameObject.SetActive(godMode);

        if (spectatorController != null)
            spectatorController.enabled = !godMode;
        if (godController != null)
            godController.enabled = godMode;

        if (godMode)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        else
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    private IEnumerator SmoothSwitch(bool toGodMode)
    {
        isTransitioning = true;

        if (spectatorController != null)
            spectatorController.enabled = false;
        if (godController != null)
            godController.enabled = false;

        Camera fromCam = toGodMode ? spectatorCamera : godCamera;
        Camera toCam = toGodMode ? godCamera : spectatorCamera;

        if (fromCam == null || toCam == null)
        {
            Debug.LogWarning("[CameraManager] Kameralar eksik, smooth switch yapılamıyor.");
            ForceSetMode(toGodMode);
            isTransitioning = false;
            yield break;
        }

        spectatorCamera.gameObject.SetActive(fromCam == spectatorCamera);
        godCamera.gameObject.SetActive(fromCam == godCamera);

        Vector3 startPos = fromCam.transform.position;
        Quaternion startRot = fromCam.transform.rotation;

        Vector3 targetPos = toCam.transform.position;
        Quaternion targetRot = toCam.transform.rotation;

        float t = 0f;
        while (t < transitionDuration)
        {
            t += Time.deltaTime;
            float lerp = Mathf.Clamp01(t / transitionDuration);

            fromCam.transform.position = Vector3.Lerp(startPos, targetPos, lerp);
            fromCam.transform.rotation = Quaternion.Slerp(startRot, targetRot, lerp);

            yield return null;
        }

        toCam.transform.position = fromCam.transform.position;
        toCam.transform.rotation = fromCam.transform.rotation;

        spectatorCamera.gameObject.SetActive(toCam == spectatorCamera);
        godCamera.gameObject.SetActive(toCam == godCamera);

        if (spectatorController != null)
            spectatorController.enabled = !toGodMode;
        if (godController != null)
            godController.enabled = toGodMode;

        if (toGodMode)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        else
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        isTransitioning = false;
    }

    public void DisableBattleCameras()
    {
        if (spectatorCamera != null)
            spectatorCamera.gameObject.SetActive(false);
        if (godCamera != null)
            godCamera.gameObject.SetActive(false);

        if (spectatorController != null)
            spectatorController.enabled = false;
        if (godController != null)
            godController.enabled = false;
    }
}
