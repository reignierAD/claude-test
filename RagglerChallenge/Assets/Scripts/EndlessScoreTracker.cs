using UnityEngine;
using TMPro;

/// <summary>
/// Tracks score in Endless mode (1 point per matched triple cleared).
/// </summary>
public class EndlessScoreTracker : MonoBehaviour
{
    public static EndlessScoreTracker Instance { get; private set; }

    public TextMeshProUGUI scoreText;
    public TextMeshProUGUI highScoreText;

    private int score = 0;
    private int highScore = 0;

    void Awake()
    {
        Instance = this;
        highScore = PlayerPrefs.GetInt("endless_highscore", 0);
    }

    public void AddMatch()
    {
        score++;
        if (score > highScore)
        {
            highScore = score;
            PlayerPrefs.SetInt("endless_highscore", highScore);
        }
        Refresh();
    }

    public void Reset()
    {
        score = 0;
        Refresh();
    }

    void Refresh()
    {
        if (scoreText)     scoreText.text     = $"Score: {score}";
        if (highScoreText) highScoreText.text = $"Best: {highScore}";
    }
}
