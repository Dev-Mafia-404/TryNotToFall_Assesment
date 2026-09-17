using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class HexBotController : MonoBehaviour
{
    // =====================================================================
    // FIELDS & PROPERTIES
    // =====================================================================

    [Header("Movement Settings (Matches Player)")]
    public float moveSpeed = 8f;
    public float jumpForce = 8f;
    public float gravity = -10f;
    public float turnSmoothTime = 0.1f;

    [Header("AI Settings - Navigation")]
    public float lookAheadDistance = 3.5f;
    public Vector2 directionChangeTimeRange = new Vector2(0.5f, 1.5f);

    [Header("AI Settings - Avoidance")]
    [Tooltip("How far the bot looks for others to avoid them.")]
    public float avoidanceRadius = 2.5f;
    [Tooltip("How strongly the bot tries to avoid others (0.1 to 1).")]
    [Range(0.1f, 1f)] public float avoidanceStrength = 0.5f;

    private Vector3 currentMoveDirection;
    private float directionTimer;
    private float panicCooldownTimer = 0f;

    [Header("Ground Check & Tile Breaking")]
    public LayerMask groundLayer;
    public float groundCheckDistance = 0.5f;
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
    private CharacterController controller;
    private Vector3 velocity;
    private float turnSmoothVelocity;


    // =====================================================================
    // INITIALIZATION
    // =====================================================================

    void Start()
    {
        controller = GetComponent<CharacterController>();
        hasAutoJumped = true;

        AssignRandomColor();
        PickRandomDirection();
    }


    // =====================================================================
    // RANDOM COLOR GENERATION
    // =====================================================================

    private void AssignRandomColor()
    {
        Renderer[] renderers = GetComponentsInChildren<Renderer>();
        Color botColor = Random.ColorHSV(0f, 1f, 0.6f, 1f, 0.7f, 1f);

        foreach (Renderer rend in renderers)
        {
            Material mat = new Material(rend.sharedMaterial);
            mat.color = botColor;
            rend.material = mat;
        }
    }


    // =====================================================================
    // MAIN LOOP
    // =====================================================================

    void Update()
    {
        PerformGroundCheck();
        HandleAILogic();
        HandleMovementProcessing();
        HandleGravityAndJumping();
    }


    // =====================================================================
    // AI LOGIC & NAVIGATION (WHISKERS & AVOIDANCE)
    // =====================================================================

    private void HandleAILogic()
    {
        directionTimer -= Time.deltaTime;
        panicCooldownTimer -= Time.deltaTime;

        Vector3 rayStart = transform.position + (Vector3.up * 0.2f);

        // Define Whisker Directions
        Vector3 forwardDir = transform.forward;
        Vector3 right45Dir = Quaternion.Euler(0, 45, 0) * transform.forward;
        Vector3 left45Dir = Quaternion.Euler(0, -45, 0) * transform.forward;

        // Cast rays 5 units down from the look-ahead position to ensure we find ground even if jumping
        bool forwardSafe = Physics.Raycast(rayStart + (forwardDir * lookAheadDistance), Vector3.down, 5f, groundLayer);
        bool rightSafe = Physics.Raycast(rayStart + (right45Dir * lookAheadDistance), Vector3.down, 5f, groundLayer);
        bool leftSafe = Physics.Raycast(rayStart + (left45Dir * lookAheadDistance), Vector3.down, 5f, groundLayer);

        // Debug Lines
        Debug.DrawRay(rayStart + (forwardDir * lookAheadDistance), Vector3.down * 5f, forwardSafe ? Color.cyan : Color.red);
        Debug.DrawRay(rayStart + (right45Dir * lookAheadDistance), Vector3.down * 5f, rightSafe ? Color.cyan : Color.red);
        Debug.DrawRay(rayStart + (left45Dir * lookAheadDistance), Vector3.down * 5f, leftSafe ? Color.cyan : Color.red);

        // 1. SURVIVAL: Steer away from edges using whiskers
        if (!forwardSafe && panicCooldownTimer <= 0f)
        {
            if (rightSafe && !leftSafe)
            {
                currentMoveDirection = right45Dir;
            }
            else if (leftSafe && !rightSafe)
            {
                currentMoveDirection = left45Dir;
            }
            else if (rightSafe && leftSafe)
            {
                currentMoveDirection = (Random.value > 0.5f) ? right45Dir : left45Dir;
            }
            else
            {
                // Dead end on all sides! Pull a 180-degree turn.
                currentMoveDirection = -transform.forward;
            }

            panicCooldownTimer = 0.5f;
            ResetDirectionTimer();
        }
        else if (forwardSafe && directionTimer <= 0f && panicCooldownTimer <= 0f)
        {
            PickRandomDirection();
        }

        // 2. SOCIAL DISTANCING: Try to avoid other players/bots if we aren't currently panicking
        if (panicCooldownTimer <= 0f)
        {
            Collider[] nearbyEntities = Physics.OverlapSphere(transform.position, avoidanceRadius);
            Vector3 avoidanceVector = Vector3.zero;

            foreach (Collider hit in nearbyEntities)
            {
                // If it has a CharacterController, it's a player or bot. Don't avoid ourselves!
                if (hit.gameObject != this.gameObject && hit.GetComponent<CharacterController>() != null)
                {
                    Vector3 awayFromThem = transform.position - hit.transform.position;
                    // Flatten the vector so they don't try to fly up or dig down
                    awayFromThem.y = 0;
                    avoidanceVector += awayFromThem.normalized;
                }
            }

            if (avoidanceVector != Vector3.zero)
            {
                // Softly blend the avoidance vector into the current movement
                currentMoveDirection = Vector3.Lerp(currentMoveDirection, (currentMoveDirection + avoidanceVector).normalized, avoidanceStrength).normalized;
            }
        }
    }

    private void PickRandomDirection()
    {
        float randomAngle = Random.Range(0f, 360f);
        currentMoveDirection = new Vector3(Mathf.Sin(randomAngle), 0f, Mathf.Cos(randomAngle)).normalized;
        ResetDirectionTimer();
    }

    private void ResetDirectionTimer()
    {
        directionTimer = Random.Range(directionChangeTimeRange.x, directionChangeTimeRange.y);
    }


    // =====================================================================
    // MOVEMENT & ROTATION
    // =====================================================================

    private void HandleMovementProcessing()
    {
        // Prevent bots from moving before the start
        if (!GameStateManager.IsGameActive)
        {
            animator.SetBool("IsRunning", false);
            return;
        }

        if (currentMoveDirection.magnitude >= 0.1f)
        {
            float targetAngle = Mathf.Atan2(currentMoveDirection.x, currentMoveDirection.z) * Mathf.Rad2Deg;
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
    // CUSTOM GROUND CHECK & TILE BREAKING
    // =====================================================================

    private void PerformGroundCheck()
    {
        Vector3 checkCenter = transform.position + (Vector3.up * 0.2f);
        isGroundedCustom = Physics.Raycast(checkCenter, Vector3.down, groundCheckDistance + 0.2f, groundLayer);

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
    // GRAVITY & JUMPING
    // =====================================================================

    private void HandleGravityAndJumping()
    {
        if (isGroundedCustom)
        {
            if (velocity.y < 0) velocity.y = -2f;

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

    private void ExecuteJump()
    {
        velocity.y = jumpForce;
        animator.SetBool("IsJumping", true);
        animator.SetBool("IsFalling", false);
        hasPlayedFallSound = false;

        if (audioSource != null && jumpSound != null) audioSource.PlayOneShot(jumpSound, 0.5f);
    }
}