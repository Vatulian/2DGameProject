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

    private void Awake()
    {
        
        leftEdge = transform.position.x - horizontalDistance;
        rightEdge = transform.position.x + horizontalDistance;
        bottomEdge = transform.position.y - verticalDistance;
        topEdge = transform.position.y + verticalDistance;
    }

    private void Update()
    {
        
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
