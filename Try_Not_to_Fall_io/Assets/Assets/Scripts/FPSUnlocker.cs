using UnityEngine;

public class FPSUnlocker : MonoBehaviour
{
    // =====================================================================
    // FIELDS & PROPERTIES
    // =====================================================================

    [Header("Framerate Settings")]
    [Tooltip("The target framerate. Set to 120 or higher. Android will automatically cap this to the device's maximum physical screen refresh rate.")]
    public int targetFPS = 120;


    // =====================================================================
    // INITIALIZATION
    // =====================================================================

    void Awake()
    {
        UnlockFramerate();
    }


    // =====================================================================
    // FRAMERATE LOGIC
    // =====================================================================

    private void UnlockFramerate()
    {
        // 1. Disable Unity's built-in VSync. 
        // CRITICAL: If this is not set to 0, Application.targetFrameRate is completely ignored on mobile!
        QualitySettings.vSyncCount = 0;

        // 2. Set the target framerate.
        // Requesting a high frame rate forces the device to run as fast as its hardware screen allows.
        Application.targetFrameRate = targetFPS;

        Debug.Log($"[FPSUnlocker] Target framerate unlocked and set to: {targetFPS}");
    }
}