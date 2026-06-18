using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    public GameObject enemyPrefab;
    public float startY = 4f;
    public float spacingX = 1.5f;
    public float spacingY = 1.2f;
    public int enemiesPerRow = 5;

    public int SpawnWave(int waveNumber)
    {
        if (enemyPrefab == null)
        {
            Debug.LogWarning("EnemySpawner: enemyPrefab is not assigned!");
            return 0;
        }

        int totalEnemies = 5 + waveNumber * 2;
        int rows = Mathf.CeilToInt((float)totalEnemies / enemiesPerRow);
        int spawned = 0;

        for (int row = 0; row < rows; row++)
        {
            int enemiesInRow = Mathf.Min(enemiesPerRow, totalEnemies - spawned);
            float rowWidth = (enemiesInRow - 1) * spacingX;
            float startX = -rowWidth / 2f;

            for (int col = 0; col < enemiesInRow; col++)
            {
                float x = startX + col * spacingX;
                float y = startY + row * spacingY;
                Vector3 pos = new Vector3(x, y, 0f);
                Instantiate(enemyPrefab, pos, Quaternion.identity);
                spawned++;
            }
        }

        return spawned;
    }
}
