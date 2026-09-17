using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    // =====================================================================
    // FIELDS & PROPERTIES
    // =====================================================================

    [Header("Targeting")]
    public Transform target;

    [Tooltip("How smoothly the camera catches up to the target")]
    public float smoothSpeed = 5f;

    // Internal variables to store your manual Editor placement
    private Vector3 initialOffset;
    private Quaternion fixedRotation;


    // =====================================================================
    // INITIALIZATION
    // =====================================================================

    void Start()
    {
        if (target != null)
        {
            // Automatically calculate the offset based on where you placed the camera in the Scene view
            initialOffset = transform.position - target.position;

            // Lock in the exact rotation you set in the Editor
            fixedRotation = transform.rotation;
        }
    }


    // =====================================================================
    // CAMERA TRACKING
    // =====================================================================

    void LateUpdate()
    {
        if (target == null)
            return;

        // Calculate target position based on player's position + the initial offset
        Vector3 desiredPosition = target.position + initialOffset;

        // Smoothly move the camera towards the target position
        transform.position = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed * Time.deltaTime);

        // Maintain the exact angle you set in the editor
        transform.rotation = fixedRotation;
    }
}