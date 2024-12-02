using UnityEngine;

public class Enemy_Sideways : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float horizontalDistance; // Yatay hareket mesafesi
    [SerializeField] private float verticalDistance;   // Dikey hareket mesafesi
    [SerializeField] private float speed;             // Hareket hýzý

    [Header("Damage Settings")]
    [SerializeField] private float damage;

    private bool movingLeft;
    private bool movingUp;
    private float leftEdge;
    private float rightEdge;
    private float bottomEdge;
    private float topEdge;

    private void Awake()
    {
        // Yatay ve dikey sýnýrlarý ayarla
        leftEdge = transform.position.x - horizontalDistance;
        rightEdge = transform.position.x + horizontalDistance;
        bottomEdge = transform.position.y - verticalDistance;
        topEdge = transform.position.y + verticalDistance;
    }

    private void Update()
    {
        // X ekseninde hareket
        if (movingLeft)
        {
            if (transform.position.x > leftEdge)
                transform.position += Vector3.left * speed * Time.deltaTime;
            else
                movingLeft = false;
        }
        else
        {
            if (transform.position.x < rightEdge)
                transform.position += Vector3.right * speed * Time.deltaTime;
            else
                movingLeft = true;
        }

        // Y ekseninde hareket
        if (movingUp)
        {
            if (transform.position.y < topEdge)
                transform.position += Vector3.up * speed * Time.deltaTime;
            else
                movingUp = false;
        }
        else
        {
            if (transform.position.y > bottomEdge)
                transform.position += Vector3.down * speed * Time.deltaTime;
            else
                movingUp = true;
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.tag == "Player")
        {
            collision.GetComponent<Health>()?.TakeDamage(damage);
        }
    }
}
