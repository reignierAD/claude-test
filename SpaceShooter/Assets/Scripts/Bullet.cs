using UnityEngine;

public class Bullet : MonoBehaviour
{
    public float speed = 10f;

    void Start()
    {
        Destroy(gameObject, 3f);
    }

    void Update()
    {
        transform.position += Vector3.up * speed * Time.deltaTime;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Enemy"))
        {
            Enemy enemy = other.GetComponent<Enemy>();
            if (enemy != null)
            {
                enemy.TakeDamage();
            }
            Destroy(gameObject);
        }
    }
}
