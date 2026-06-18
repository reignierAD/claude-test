using UnityEngine;

public class PlayerController : MonoBehaviour
{
    public float speed = 5f;
    public GameObject bulletPrefab;
    public Transform firePoint;
    public int lives = 3;

    private float shootCooldown = 0.3f;
    private float shootTimer = 0f;
    private float invincibilityDuration = 1.5f;
    private float invincibilityTimer = 0f;
    private bool isInvincible = false;
    private Renderer rend;
    private Camera mainCamera;
    private float flickerInterval = 0.1f;
    private float flickerTimer = 0f;

    void Start()
    {
        rend = GetComponent<Renderer>();
        mainCamera = Camera.main;
    }

    void Update()
    {
        HandleMovement();
        HandleShooting();
        HandleInvincibility();
    }

    void HandleMovement()
    {
        float h = Input.GetAxis("Horizontal");
        float v = Input.GetAxis("Vertical");
        Vector3 move = new Vector3(h, v, 0f) * speed * Time.deltaTime;
        transform.position += move;

        float camHeight = mainCamera.orthographicSize;
        float camWidth = mainCamera.orthographicSize * mainCamera.aspect;
        float x = Mathf.Clamp(transform.position.x, -camWidth + 0.5f, camWidth - 0.5f);
        float y = Mathf.Clamp(transform.position.y, -camHeight + 0.5f, camHeight - 0.5f);
        transform.position = new Vector3(x, y, transform.position.z);
    }

    void HandleShooting()
    {
        shootTimer -= Time.deltaTime;
        if (Input.GetKey(KeyCode.Space) && shootTimer <= 0f)
        {
            shootTimer = shootCooldown;
            Transform spawnPoint = firePoint != null ? firePoint : transform;
            Instantiate(bulletPrefab, spawnPoint.position, Quaternion.identity);
        }
    }

    void HandleInvincibility()
    {
        if (isInvincible)
        {
            invincibilityTimer -= Time.deltaTime;
            flickerTimer -= Time.deltaTime;
            if (flickerTimer <= 0f)
            {
                flickerTimer = flickerInterval;
                rend.enabled = !rend.enabled;
            }
            if (invincibilityTimer <= 0f)
            {
                isInvincible = false;
                rend.enabled = true;
            }
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("EnemyBullet"))
        {
            TakeDamage();
            Destroy(other.gameObject);
        }
    }

    public void TakeDamage()
    {
        if (isInvincible) return;
        lives--;
        UIManager.Instance.UpdateLives(lives);
        if (lives <= 0)
        {
            GameManager.Instance.GameOver();
            gameObject.SetActive(false);
        }
        else
        {
            isInvincible = true;
            invincibilityTimer = invincibilityDuration;
            flickerTimer = flickerInterval;
        }
    }
}
