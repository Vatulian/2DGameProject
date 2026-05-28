using UnityEngine;

public class Enemy_Sideways : MonoBehaviour
{
    private static readonly Color HorizontalGizmoColor = new Color(1f, 0.85f, 0.1f, 1f);
    private static readonly Color VerticalGizmoColor = new Color(0.1f, 0.8f, 1f, 1f);
    private static readonly Color BoundsGizmoColor = new Color(1f, 1f, 1f, 0.5f);

    [Header("Movement Settings")]
    [SerializeField] private float horizontalDistance; // Horizontal movement distance
    [SerializeField] private float verticalDistance;   // Vertical movement distance
    [SerializeField] private float speed;              // Movement speed

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
        // Horizontal and vertical edge limits.
        leftEdge = transform.position.x - horizontalDistance;
        rightEdge = transform.position.x + horizontalDistance;
        bottomEdge = transform.position.y - verticalDistance;
        topEdge = transform.position.y + verticalDistance;
    }

    private void Update()
    {
        // X-axis movement
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

        // Y-axis movement
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

    private void OnDrawGizmosSelected()
    {
        Vector3 center = GetGizmoCenter();
        float horizontalRange = Mathf.Abs(horizontalDistance);
        float verticalRange = Mathf.Abs(verticalDistance);

        Vector3 leftPoint = center + Vector3.left * horizontalRange;
        Vector3 rightPoint = center + Vector3.right * horizontalRange;
        Vector3 bottomPoint = center + Vector3.down * verticalRange;
        Vector3 topPoint = center + Vector3.up * verticalRange;

        if (horizontalRange > 0f)
        {
            Gizmos.color = HorizontalGizmoColor;
            Gizmos.DrawLine(leftPoint, rightPoint);
            Gizmos.DrawWireSphere(leftPoint, 0.12f);
            Gizmos.DrawWireSphere(rightPoint, 0.12f);
        }

        if (verticalRange > 0f)
        {
            Gizmos.color = VerticalGizmoColor;
            Gizmos.DrawLine(bottomPoint, topPoint);
            Gizmos.DrawWireSphere(bottomPoint, 0.12f);
            Gizmos.DrawWireSphere(topPoint, 0.12f);
        }

        if (horizontalRange > 0f && verticalRange > 0f)
        {
            Gizmos.color = BoundsGizmoColor;
            Gizmos.DrawWireCube(center, new Vector3(horizontalRange * 2f, verticalRange * 2f, 0f));
        }
    }

    private Vector3 GetGizmoCenter()
    {
        if (!Application.isPlaying)
        {
            return transform.position;
        }

        return new Vector3(
            (leftEdge + rightEdge) * 0.5f,
            (bottomEdge + topEdge) * 0.5f,
            transform.position.z);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.tag == "Player")
        {
            collision.GetComponent<Health>()?.TakeDamage(damage);
        }
    }
}
