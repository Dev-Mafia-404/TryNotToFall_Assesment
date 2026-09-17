using UnityEngine;
using System.Collections;

public class HexTile : MonoBehaviour
{
    // =====================================================================
    // FIELDS & PROPERTIES
    // =====================================================================

    [Header("Interaction & Animation")]
    [Tooltip("How far the tile drops when stepped on.")]
    public float springDropDistance = 0.2f;
    [Tooltip("Total time for the tile to go down and come back up.")]
    public float springDuration = 0.1f;

    [Header("Audio")]
    [Tooltip("Audio clip to play when the tile is stepped on.")]
    public AudioClip popSound;

    [Header("Standard Destruction (!useFall)")]
    public Color warningColor = Color.white;
    [Tooltip("How fast the tile turns to the warning color before disappearing.")]
    public float colorLerpDuration = 0.1f;

    [Header("Fall Mechanic Variant (useFall)")]
    public bool useFall = false;
    [Tooltip("How many times the tile flashes white before falling.")]
    public int blinkCount = 3;
    public float blinkSpeed = 0.1f;
    public float fallSpeed = 5f;
    [Tooltip("How long the tile falls before completely destroying/disabling.")]
    public float destroyDelay = 2f;

    private bool isCrumbling = false;
    private MeshRenderer meshRenderer;
    private Color originalColor;
    private Vector3 originalPosition;
    private AudioSource audioSource;


    // =====================================================================
    // INITIALIZATION
    // =====================================================================

    void Start()
    {
        meshRenderer = GetComponent<MeshRenderer>();

        if (meshRenderer != null)
        {
            originalColor = meshRenderer.material.color;
        }

        originalPosition = transform.localPosition;

        // Get or create the AudioSource component
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
    }


    // =====================================================================
    // EXTERNAL TRIGGERS
    // =====================================================================

    public void StartCrumble()
    {
        if (!isCrumbling)
        {
            isCrumbling = true;
            StartCoroutine(CrumbleSequence());
        }
    }


    // =====================================================================
    // CRUMBLE LOGIC & ANIMATION
    // =====================================================================

    private IEnumerator CrumbleSequence()
    {
        // 1. Play the pop sound effect
        if (popSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(popSound,0.25f);
        }

        // 2. Perform the spring motion (down, then back up)
        Vector3 targetDownPos = originalPosition + (Vector3.down * springDropDistance);
        float halfSpringTime = springDuration / 2f;
        float elapsedTime = 0f;

        // Move down
        while (elapsedTime < halfSpringTime)
        {
            transform.localPosition = Vector3.Lerp(originalPosition, targetDownPos, elapsedTime / halfSpringTime);
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        // Move back up
        elapsedTime = 0f;
        while (elapsedTime < halfSpringTime)
        {
            transform.localPosition = Vector3.Lerp(targetDownPos, originalPosition, elapsedTime / halfSpringTime);
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        // Ensure it snaps perfectly back to the original position
        transform.localPosition = originalPosition;

        // 3. Handle the destruction visuals based on the useFall toggle
        if (!useFall)
        {
            // Rapid Lerp to warning color, then instantly disappear
            elapsedTime = 0f;
            while (elapsedTime < colorLerpDuration)
            {
                if (meshRenderer != null)
                {
                    meshRenderer.material.color = Color.Lerp(originalColor, warningColor, elapsedTime / colorLerpDuration);
                }
                elapsedTime += Time.deltaTime;
                yield return null;
            }

            gameObject.SetActive(false);
        }
        else
        {
            // Blink cycle, fall, then disappear
            for (int i = 0; i < blinkCount; i++)
            {
                if (meshRenderer != null) meshRenderer.material.color = warningColor;
                yield return new WaitForSeconds(blinkSpeed);
                if (meshRenderer != null) meshRenderer.material.color = originalColor;
                yield return new WaitForSeconds(blinkSpeed);
            }

            // Fall dynamically over time
            float fallTimer = 0f;
            while (fallTimer < destroyDelay)
            {
                transform.position += Vector3.down * fallSpeed * Time.deltaTime;
                fallTimer += Time.deltaTime;
                yield return null;
            }

            gameObject.SetActive(false);
        }
    }
}