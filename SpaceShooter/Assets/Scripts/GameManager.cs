using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public int Score { get; private set; }
    public int Lives { get; private set; } = 3;
    public int Wave { get; private set; } = 1;

    private int enemiesAlive = 0;
    private EnemySpawner enemySpawner;
    private bool gameOver = false;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    void Start()
    {
        enemySpawner = FindObjectOfType<EnemySpawner>();
        UIManager.Instance.UpdateScore(Score);
        UIManager.Instance.UpdateLives(Lives);
        UIManager.Instance.HideGameOver();
        StartWave();
    }

    void Update()
    {
        if (gameOver && Input.GetKeyDown(KeyCode.R))
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
    }

    void StartWave()
    {
        if (enemySpawner != null)
        {
            int count = enemySpawner.SpawnWave(Wave);
            enemiesAlive = count;
        }
    }

    public void AddScore(int points)
    {
        Score += points;
        UIManager.Instance.UpdateScore(Score);
    }

    public void PlayerHit()
    {
        Lives--;
        UIManager.Instance.UpdateLives(Lives);
        if (Lives <= 0)
        {
            GameOver();
        }
    }

    public void EnemyDestroyed()
    {
        enemiesAlive--;
        if (enemiesAlive <= 0 && !gameOver)
        {
            Wave++;
            StartWave();
        }
    }

    public void GameOver()
    {
        gameOver = true;
        UIManager.Instance.ShowGameOver();
    }
}
