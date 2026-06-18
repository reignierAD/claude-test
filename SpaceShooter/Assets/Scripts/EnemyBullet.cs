using UnityEngine;

public class EnemyBullet : MonoBehaviour
{
    public float speed = 7f;

    void Start()
    {
        Destroy(gameObject, 5f);
    }

    void Update()
    {
        transform.position += Vector3.down * speed * Time.deltaTime;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            PlayerController player = other.GetComponent<PlayerController>();
            if (player != null)
            {
                player.TakeDamage();
            }
            Destroy(gameObject);
        }
    }
}
