using UnityEngine;
using TMPro;

public class GameEnd : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        // Check if the object falling in is a player or bot
        if (other.GetComponent<CharacterController>() != null)
        {
            // Find their NameTag to get their identity
            NameTag tag = other.GetComponentInChildren<NameTag>();

            if (tag != null)
            {
                string characterName = tag.GetComponent<TextMeshPro>().text;
                bool isPlayer = tag.isPlayer;

                // Sends the death to the manager, which handles the Win/Loss logic automatically
                GameStateManager.Instance.RegisterDeath(characterName, isPlayer, other.gameObject);

                // Disable the character so they stop falling and disappear
                other.gameObject.SetActive(false);
            }
        }
    }
}