using UnityEngine;

public class Enemy : MonoBehaviour
{
    public float speed = 2f;
    public int health = 1;
    public GameObject enemyBulletPrefab;

    private float shootTimer;
    private float shootInterval;
    private bool canShoot = false;
    private Camera mainCamera;

    void Start()
    {
        mainCamera = Camera.main;
        canShoot = Random.value > 0.5f;
        shootInterval = Random.Range(2f, 4f);
        shootTimer = shootInterval;
    }

    void Update()
    {
        transform.position += Vector3.down * speed * Time.deltaTime;

        float camBottom = mainCamera != null ? -mainCamera.orthographicSize - 1f : -7f;
        if (transform.position.y < camBottom)
        {
            GameManager.Instance.PlayerHit();
            Destroy(gameObject);
            return;
        }

        if (canShoot && enemyBulletPrefab != null)
        {
            shootTimer -= Time.deltaTime;
            if (shootTimer <= 0f)
            {
                shootTimer = Random.Range(2f, 4f);
                Instantiate(enemyBulletPrefab, transform.position, Quaternion.identity);
            }
        }
    }

    public void TakeDamage()
    {
        health--;
        if (health <= 0)
        {
            Die();
        }
    }

    void Die()
    {
        GameManager.Instance.AddScore(10);
        GameManager.Instance.EnemyDestroyed();
        Destroy(gameObject);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("PlayerBullet"))
        {
            Bullet bullet = other.GetComponent<Bullet>();
            if (bullet != null)
            {
                Destroy(other.gameObject);
            }
            TakeDamage();
        }
    }
}
