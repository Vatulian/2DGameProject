using UnityEngine;

public class LandMovement : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float horizontalDistance;
    [SerializeField] private float verticalDistance;
    [SerializeField] private float speed;

    private bool movingLeft;
    private bool movingUp;
    private float leftEdge;
    private float rightEdge;
    private float bottomEdge;
    private float topEdge;

    public Vector3 PlatformSpeed { get; private set; } // We just made it readable.

    private void Awake()
    {
        leftEdge = transform.position.x - horizontalDistance;
        rightEdge = transform.position.x + horizontalDistance;
        bottomEdge = transform.position.y - verticalDistance;
        topEdge = transform.position.y + verticalDistance;
    }

    private void FixedUpdate()
    {
        PlatformSpeed = Vector3.zero; // Reset every frame.

        // X-axis movement
        if (movingLeft)
        {
            if (transform.position.x > leftEdge)
            {
                PlatformSpeed = Vector3.left * speed * Time.fixedDeltaTime;
                transform.position += PlatformSpeed;
            }
            else
            {
                movingLeft = false;
            }
        }
        else
        {
            if (transform.position.x < rightEdge)
            {
                PlatformSpeed = Vector3.right * speed * Time.fixedDeltaTime;
                transform.position += PlatformSpeed;
            }
            else
            {
                movingLeft = true;
            }
        }

        // Y-Axis movement
        if (movingUp)
        {
            if (transform.position.y < topEdge)
            {
                PlatformSpeed += Vector3.up * speed * Time.fixedDeltaTime;
                transform.position += PlatformSpeed;
            }
            else
            {
                movingUp = false;
            }
        }
        else
        {
            if (transform.position.y > bottomEdge)
            {
                PlatformSpeed += Vector3.down * speed * Time.fixedDeltaTime;
                transform.position += PlatformSpeed;
            }
            else
            {
                movingUp = true;
            }
        }
    }
}
