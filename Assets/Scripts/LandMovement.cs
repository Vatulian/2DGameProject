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
    public Vector3 PlatformSpeed {get; set;}

    private void Awake()
    {
        
        leftEdge = transform.position.x - horizontalDistance;
        rightEdge = transform.position.x + horizontalDistance;
        bottomEdge = transform.position.y - verticalDistance;
        topEdge = transform.position.y + verticalDistance;
    }

    private void FixedUpdate()
    {
        
        if (movingLeft)
        {
            if (transform.position.x > leftEdge)
            {
                PlatformSpeed = Vector3.left * speed;
                transform.position += PlatformSpeed;
            }
            else
                movingLeft = false;
        }
        else
        {
            if (transform.position.x < rightEdge)
            {
                PlatformSpeed = Vector3.right * speed;
                transform.position += PlatformSpeed;
            }
                
            else
                movingLeft = true;
        }

        
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

}
