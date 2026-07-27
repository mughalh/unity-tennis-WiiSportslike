using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    [Header("Scores")]
    public int playerScore = 0;
    public int aiScore = 0;
    public int winScore = 11;

    [Header("UI")]
    public Text playerScoreText;
    public Text aiScoreText;
    public Text statusText;

    [Header("Spawn Points")]
    public Transform playerSpawn;
    public Transform aiSpawn;

    [Header("References")]
    public GameObject ball;
    public GameObject player;
    public GameObject ai;

    private BallController ballController;
    private bool isPlayerServing = true;

    void Start()
    {
        if (ball != null)
            ballController = ball.GetComponent<BallController>();

        UpdateUI();
        Invoke(nameof(Serve), 1f);
    }

    void Update()
    {
        // Space to serve for testing
        if (Input.GetKeyDown(KeyCode.Space))
        {
            Serve();
        }

        if (Input.GetKeyDown(KeyCode.R))
        {
            ResetGame();
        }

        // Win conditions
        if (playerScore >= winScore)
        {
            statusText.text = "🏆 YOU WIN!";
            Invoke(nameof(LoadWinScene), 2f);
        }
        else if (aiScore >= winScore)
        {
            statusText.text = "😢 YOU LOSE!";
            Invoke(nameof(LoadLoseScene), 2f);
        }
        if (Input.GetKeyDown(KeyCode.R))
    {
        ResetGame();
    }
    }

    public void Serve()
    {
        if (ballController == null || ball == null) return;

        ballController.ResetBall();

        // Position ball based on who serves
        if (isPlayerServing && player != null)
        {
            ball.transform.position = player.transform.position + new Vector3(0.5f, 0.5f, 0.5f);
            statusText.text = "🎾 Your Serve";
        }
        else if (!isPlayerServing && ai != null)
        {
            ball.transform.position = ai.transform.position + new Vector3(0.5f, 0.5f, -0.5f);
            statusText.text = "🎾 Opponent's Serve";
        }
        else
        {
            ball.transform.position = new Vector3(0f, 0.5f, 0f);
            statusText.text = "🎾 Serve";
        }

        // Launch ball
        Vector3 dir = isPlayerServing ? Vector3.forward : Vector3.back;
        string hitter = isPlayerServing ? "Player" : "AI";
        ballController.LaunchBall(dir * 8f + Vector3.up * 3f, hitter);

        isPlayerServing = !isPlayerServing;
        UpdateUI();
    }

    public void PlayerScored()
    {
        playerScore++;
        statusText.text = "🎯 YOU SCORED!";
        UpdateUI();
        Invoke(nameof(Serve), 1.5f);
    }

    public void AIScored()
    {
        aiScore++;
        statusText.text = "🎯 OPPONENT SCORED!";
        UpdateUI();
        Invoke(nameof(Serve), 1.5f);
    }
    public void ResetGame()
    {
        playerScore = 0;
        aiScore = 0;
        isPlayerServing = true;
        UpdateUI();
        if (ballController != null)
            ballController.ResetBall();
        Serve();
    }


    void UpdateUI()
    {
        if (playerScoreText != null)
            playerScoreText.text = $"{playerScore}";

        if (aiScoreText != null)
            aiScoreText.text = $"{aiScore}";
    }

    void LoadWinScene()
    {
        SceneManager.LoadScene("WinScene");
    }

    void LoadLoseScene()
    {
        SceneManager.LoadScene("LoseScene");
    }

    public bool IsGameActive()
    {
        return playerScore < winScore && aiScore < winScore;
    }
}
