using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class HyperCasualController : MonoBehaviour
{
    // =====================================================================
    // FIELDS & PROPERTIES
    // =====================================================================

    [Header("Movement Settings")]
    public float moveSpeed = 8f;
    public float jumpForce = 8f;
    public float gravity = -10f;
    public float turnSmoothTime = 0.1f;
    public float dragDeadzone = 0.1f;
    public float dragSensitivity = 100f;

    [Header("Ground Check Settings")]
    [Tooltip("The layer your floor objects are on.")]
    public LayerMask groundLayer;
    [Tooltip("How far down the raycast checks for ground.")]
    public float groundCheckDistance = 0.5f;
    [Tooltip("Radius of the sphere check for breaking tiles.")]
    public float tileDetectionRadius = 1f;
    private bool isGroundedCustom;

    [Header("Auto-Jump (Coyote Time)")]
    public float autoJumpDelay = 0.1f;
    private float timeInAir = 0f;
    private bool hasAutoJumped = false;

    [Header("Audio Settings")]
    public AudioSource audioSource;
    public AudioClip jumpSound;

    private bool hasPlayedFallSound = false;

    [Header("References")]
    public Animator animator;
    public Transform mainCamera;

    private CharacterController controller;
    private Vector3 velocity;
    private float turnSmoothVelocity;

    // Mouse Input Tracking
    private Vector2 touchStartPos;
    private Vector2 currentInput;


    // =====================================================================
    // INITIALIZATION
    // =====================================================================

    void Start()
    {
        controller = GetComponent<CharacterController>();

        if (mainCamera == null)
        {
            mainCamera = Camera.main.transform;
        }

        hasAutoJumped = true;
    }


    // =====================================================================
    // INPUT HANDLING & MAIN LOOP
    // =====================================================================

    void Update()
    {
        PerformGroundCheck();
        HandleMouseInput();
        HandleMovementProcessing();
        HandleGravityAndJumping();
    }


    // =====================================================================
    // CUSTOM GROUND CHECK
    // =====================================================================

    private void PerformGroundCheck()
    {
        Vector3 checkCenter = transform.position + (Vector3.up * 0.2f);

        // 1. Precise Raycast for Jumping
        isGroundedCustom = Physics.Raycast(checkCenter, Vector3.down, groundCheckDistance + 0.2f, groundLayer);
        Debug.DrawRay(checkCenter, Vector3.down * (groundCheckDistance + 0.2f), isGroundedCustom ? Color.green : Color.red);

        // 2. OverlapSphere to catch multiple tiles on seams
        Collider[] hitColliders = Physics.OverlapSphere(checkCenter, tileDetectionRadius, groundLayer);

        foreach (Collider hit in hitColliders)
        {
            if (hit.transform.position.y < transform.position.y)
            {
                HexTile hex = hit.GetComponent<HexTile>();
                if (hex != null)
                {
                    hex.StartCrumble();
                }
            }
        }
    }


    // =====================================================================
    // MOUSE DRAG INPUT
    // =====================================================================

    private void HandleMouseInput()
    {
        if (Input.GetMouseButtonDown(0))
        {
            touchStartPos = Input.mousePosition;
            currentInput = Vector2.zero;
        }
        else if (Input.GetMouseButton(0))
        {
            Vector2 currentPos = Input.mousePosition;
            Vector2 delta = currentPos - touchStartPos;
            currentInput = delta / dragSensitivity;
        }
        else
        {
            currentInput = Vector2.zero;
        }
    }


    // =====================================================================
    // MOVEMENT & ROTATION PROCESSING
    // =====================================================================

    private void HandleMovementProcessing()
    {
        // Prevent movement before the GO signal
        if (!GameStateManager.IsGameActive)
        {
            animator.SetBool("IsRunning", false);
            return;
        }

        Vector3 rawDirection = new Vector3(currentInput.x, 0f, currentInput.y);

        if (rawDirection.magnitude >= dragDeadzone)
        {
            Vector3 moveDirection = rawDirection.normalized;

            float targetAngle = Mathf.Atan2(moveDirection.x, moveDirection.z) * Mathf.Rad2Deg + mainCamera.eulerAngles.y;
            float angle = Mathf.SmoothDampAngle(transform.eulerAngles.y, targetAngle, ref turnSmoothVelocity, turnSmoothTime);
            transform.rotation = Quaternion.Euler(0f, angle, 0f);

            Vector3 moveDir = Quaternion.Euler(0f, targetAngle, 0f) * Vector3.forward;
            controller.Move(moveDir.normalized * moveSpeed * Time.deltaTime);

            animator.SetBool("IsRunning", true);
        }
        else
        {
            animator.SetBool("IsRunning", false);
        }
    }


    // =====================================================================
    // GRAVITY, JUMPING & FALLING
    // =====================================================================

    private void HandleGravityAndJumping()
    {
        if (isGroundedCustom)
        {
            if (velocity.y < 0)
            {
                velocity.y = -2f;
            }

            // Reset jump, fall, and audio states
            timeInAir = 0f;
            hasAutoJumped = false;
            hasPlayedFallSound = false;
            animator.SetBool("IsJumping", false);
            animator.SetBool("IsFalling", false);
        }
        else
        {
            timeInAir += Time.deltaTime;

            if (velocity.y < 0)
            {
                animator.SetBool("IsJumping", false);
                animator.SetBool("IsFalling", true);

              

                // Auto-Jump Logic (Coyote Time)
                if (timeInAir >= autoJumpDelay && !hasAutoJumped)
                {
                    ExecuteJump();
                    hasAutoJumped = true;
                }
            }
        }

        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);
    }


    // =====================================================================
    // ACTION EXECUTION
    // =====================================================================

    private void ExecuteJump()
    {
        velocity.y = jumpForce;
        animator.SetBool("IsJumping", true);
        animator.SetBool("IsFalling", false);

        // Ensure the fall sound resets so it can play again when coming down from the jump
        hasPlayedFallSound = false;

        if (audioSource != null && jumpSound != null)
        {
            audioSource.PlayOneShot(jumpSound, 0.7f);
        }
    }
}