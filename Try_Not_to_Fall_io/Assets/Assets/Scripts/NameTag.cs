using UnityEngine;
using TMPro;

[RequireComponent(typeof(TextMeshPro))]
public class NameTag : MonoBehaviour
{
    // =====================================================================
    // FIELDS & PROPERTIES
    // =====================================================================

    [Header("Tag Settings")]
    [Tooltip("If true, the text will simply say 'You'. If false, it will generate a random bot name.")]
    public bool isPlayer = false;

    [Header("References")]
    private TextMeshPro nameText;
    private Transform mainCamera;

    // .io Style Name Generation Banks
    private readonly string[] adjectives = { "Sneaky", "Pro", "Epic", "Salty", "Swift", "Crazy", "Happy", "Fierce", "Lazy", "Neon" };
    private readonly string[] nouns = { "Ninja", "Gamer", "Panda", "Fox", "King", "Wolf", "Ghost", "Wizard", "Titan", "Potato" };


    // =====================================================================
    // INITIALIZATION
    // =====================================================================

    void Start()
    {
        nameText = GetComponent<TextMeshPro>();

        if (Camera.main != null)
        {
            mainCamera = Camera.main.transform;
        }

        SetupName();
    }


    // =====================================================================
    // NAME GENERATION
    // =====================================================================

    private void SetupName()
    {
        if (isPlayer)
        {
            nameText.text = "You";

      
            nameText.color = Color.green;
        }
        else
        {
            nameText.text = GenerateRandomName();
        }
    }

    private string GenerateRandomName()
    {
        int namingStyle = Random.Range(0, 3);
        string generatedName = "";

        switch (namingStyle)
        {
            case 0:
                // Style 1: Adjective + Noun (e.g., SneakyFox)
                generatedName = adjectives[Random.Range(0, adjectives.Length)] + nouns[Random.Range(0, nouns.Length)];
                break;

            case 1:
                // Style 2: Noun + Number (e.g., Ninja88)
                generatedName = nouns[Random.Range(0, nouns.Length)] + Random.Range(10, 999).ToString();
                break;

            case 2:
                // Style 3: Classic "Player" + Number (e.g., Player405)
                generatedName = "Player" + Random.Range(100, 9999).ToString();
                break;
        }

        return generatedName;
    }




    // =====================================================================
    // BILLBOARD ALIGNMENT (CAMERA FACING)
    // =====================================================================

    void LateUpdate()
    {
        if (mainCamera != null)
        {
            // Get the forward direction of the camera
            Vector3 camForward = mainCamera.forward;

            // Flatten the Y axis so the text stands perfectly straight up (no tilting)
            camForward.y = 0f;

            // Apply the rotation, locking the X and Z axes
            if (camForward != Vector3.zero)
            {
                transform.rotation = Quaternion.LookRotation(camForward);
            }
        }
    }
}