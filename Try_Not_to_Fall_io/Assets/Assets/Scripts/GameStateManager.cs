using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public class GameStateManager : MonoBehaviour
{
    public static GameStateManager Instance;
    public static bool IsGameActive = false;

    // =====================================================================
    // FIELDS
    // =====================================================================

    [Header("Start Sequence Settings")]
    [Tooltip("Put EXACTLY 9 bots in here. (9 bots + 1 player = 10 total contestants)")]
    public GameObject[] botsToEnable;
    public GameObject[] spawnPlatforms;
    public float botSpawnDelay = 0.3f;

    [Header("Camera FOV Settings")]
    public Camera mainCamera;
    public float startFOV = 40f;
    public float playFOV = 60f;
    public float fovLerpSpeed = 2f;

    [Header("UI - HUD")]
    public TextMeshProUGUI aliveCountText;

    [Header("UI - Start Sequence")]
    public CanvasGroup waitingCanvasGroup;
    public TextMeshProUGUI statusText;
    public float blinkSpeed = 2f;

    [Header("UI - Game Over Panel")]
    public GameObject gameOverPanel;
    public TextMeshProUGUI matchResultHeading;
    public Transform leaderboardVerticalLayout;
    public GameObject leaderboardTextPrefab;

    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip platformDropSound;

    // Internal State
    private bool waitingForTap = false;
    private bool shiftingFOV = false;
    private int totalContestants;
    private List<string> deathOrder = new List<string>();
    private List<GameObject> aliveBots = new List<GameObject>();

    private bool matchFinished = false;
    private int botsSpawned = 0; // NEW: Tracks bots as they are turned on

    void Awake()
    {
        Instance = this;
        IsGameActive = false;
        matchFinished = false;
        botsSpawned = 0;

        Time.timeScale = 1f;

        // This math requires exactly 9 bots in the array to equal a 10-player game
        totalContestants = botsToEnable.Length + 1;
    }

    void Start()
    {
        if (mainCamera == null) mainCamera = Camera.main;
        mainCamera.fieldOfView = startFOV;

        gameOverPanel.SetActive(false);
        if (aliveCountText != null) aliveCountText.gameObject.SetActive(true);

        foreach (GameObject bot in botsToEnable)
        {
            bot.SetActive(false);
            aliveBots.Add(bot);
        }

        // Will display "Alive: 1" right at the start because botsSpawned is 0
        UpdateAliveCountText();
        StartCoroutine(StartSequence());
    }

    void Update()
    {
        if (waitingCanvasGroup.gameObject.activeSelf && !waitingForTap)
        {
            waitingCanvasGroup.alpha = Mathf.PingPong(Time.time * blinkSpeed, 1f);
        }

        if (waitingForTap && Input.GetMouseButtonDown(0))
        {
            waitingForTap = false;
            waitingCanvasGroup.alpha = 1f;
            StartCoroutine(CountdownSequence());
        }

        if (shiftingFOV)
        {
            mainCamera.fieldOfView = Mathf.Lerp(mainCamera.fieldOfView, playFOV, Time.deltaTime * fovLerpSpeed);
        }
    }

    // =====================================================================
    // CORE SEQUENCES
    // =====================================================================

    private IEnumerator StartSequence()
    {
        statusText.text = "Waiting for players...";

        foreach (GameObject bot in botsToEnable)
        {
            yield return new WaitForSeconds(botSpawnDelay);
            bot.SetActive(true);

            // NEW: Increment the spawned count and update the UI so it ticks up
            botsSpawned++;
            UpdateAliveCountText();
        }

        statusText.text = "Drag Anywhere to Move";
        waitingCanvasGroup.alpha = 1f;
        waitingForTap = true;
    }

    private IEnumerator CountdownSequence()
    {
        shiftingFOV = true;

        statusText.text = "3";
        yield return new WaitForSeconds(1f);
        statusText.text = "2";
        yield return new WaitForSeconds(1f);
        statusText.text = "1";
        yield return new WaitForSeconds(1f);
        statusText.text = "GO!";

        if (audioSource != null && platformDropSound != null)
        {
            audioSource.PlayOneShot(platformDropSound);
        }

        foreach (GameObject platform in spawnPlatforms)
        {
            platform.SetActive(false);
        }

        IsGameActive = true;

        yield return new WaitForSeconds(1f);
        waitingCanvasGroup.gameObject.SetActive(false);
    }

    // =====================================================================
    // DEATH & LEADERBOARD LOGIC
    // =====================================================================

    public void RegisterDeath(string characterName, bool isPlayer, GameObject characterObject)
    {
        if (matchFinished) return;
        if (deathOrder.Contains(characterName)) return;

        deathOrder.Add(characterName);
        UpdateAliveCountText();

        if (!isPlayer && aliveBots.Contains(characterObject))
        {
            aliveBots.Remove(characterObject);
        }

        if (isPlayer)
        {
            matchFinished = true;
            IsGameActive = false;

            if (aliveCountText != null) aliveCountText.gameObject.SetActive(false);

            ShowLeaderboard(false);
            Time.timeScale = 0f;
        }
        else if (deathOrder.Count == totalContestants - 1)
        {
            matchFinished = true;
            IsGameActive = false;

            deathOrder.Add("You");

            if (aliveCountText != null) aliveCountText.gameObject.SetActive(false);

            ShowLeaderboard(true);
            Time.timeScale = 0f;
        }
    }

    // =====================================================================
    // HUD & UI UPDATES
    // =====================================================================

    private void UpdateAliveCountText()
    {
        if (aliveCountText != null)
        {
            // Calculates based on 1 (You) + the bots currently spawned, minus anyone who fell
            int currentlyAlive = 1 + botsSpawned - deathOrder.Count;
            aliveCountText.text = $"Alive: {Mathf.Max(0, currentlyAlive)}";
        }
    }

    private void ShowLeaderboard(bool playerWon)
    {
        gameOverPanel.SetActive(true);
        matchResultHeading.text = playerWon ? "VICTORY" : "GAME OVER";
        matchResultHeading.color = playerWon ? Color.yellow : Color.red;

        for (int i = deathOrder.Count - 1; i >= 0; i--)
        {
            GameObject entry = Instantiate(leaderboardTextPrefab, leaderboardVerticalLayout);
            TextMeshProUGUI entryText = entry.GetComponent<TextMeshProUGUI>();

            int actualPlace = totalContestants - i;

            entryText.text = $"{deathOrder[i]} -- {GetOrdinal(actualPlace)} place";

            if (deathOrder[i] == "You")
            {
                entryText.color = Color.green;
            }
        }
    }

    private string GetOrdinal(int num)
    {
        if (num <= 0) return num.ToString();

        switch (num % 100)
        {
            case 11:
            case 12:
            case 13:
                return num + "th";
        }

        switch (num % 10)
        {
            case 1: return num + "st";
            case 2: return num + "nd";
            case 3: return num + "rd";
            default: return num + "th";
        }
    }

    public void RestartGame()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void QuitGame()
    {
        Application.Quit();
    }   
}