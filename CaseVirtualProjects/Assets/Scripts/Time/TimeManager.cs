using UnityEngine;

public class TimeManager : MonoBehaviour
{
    [Header("Speed Presets")]
    public float[] speeds = new float[] { 0.5f, 1f, 1.5f, 2f, 3f };
    public int currentIndex = 1;

    [Header("Input Key")]
    public KeyCode switchKey = KeyCode.R;

    private void Update()
    {
        if (Input.GetKeyDown(switchKey))
        {
            CycleSpeed();
        }
    }

    private void CycleSpeed()
    {
        currentIndex++;
        if (currentIndex >= speeds.Length)
            currentIndex = 0;

        Time.timeScale = speeds[currentIndex];
        Time.fixedDeltaTime = 0.02f * Time.timeScale;

        Debug.Log($"[TimeController] Speed set to {speeds[currentIndex]}x");
    }
}
