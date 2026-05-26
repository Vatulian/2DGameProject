using UnityEngine;

public class LandMovement : MonoBehaviour
{
    private const float MinPathDistance = 0.001f;
    private const float MinSpeed = 0.01f;

    [Header("Point Movement")]
    [SerializeField] private Transform pointA;
    [SerializeField] private Transform pointB;
    [SerializeField, Min(MinSpeed)] private float speed = 3f;

    [Header("Fallback Movement")]
    [SerializeField] private float horizontalDistance;
    [SerializeField] private float verticalDistance;

    private Rigidbody2D rb;
    private Vector3 initialPosition;
    private Vector3 startPoint;
    private Vector3 endPoint;
    private bool hasPath;
    private float pathProgress;
    private int direction = 1;

    public Vector3 PlatformSpeed { get; private set; } // We just made it readable.

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        initialPosition = transform.position;
        hasPath = TryBuildPath(out startPoint, out endPoint);
        pathProgress = GetProgressClosestToCurrentPosition();
    }

    private void FixedUpdate()
    {
        PlatformSpeed = Vector3.zero;

        if (!hasPath)
        {
            return;
        }

        float pathDistance = Vector3.Distance(startPoint, endPoint);
        if (pathDistance <= MinPathDistance)
        {
            return;
        }

        float tripDuration = pathDistance / Mathf.Max(speed, MinSpeed);
        pathProgress += direction * Time.fixedDeltaTime / tripDuration;

        if (pathProgress >= 1f)
        {
            pathProgress = 1f;
            direction = -1;
        }
        else if (pathProgress <= 0f)
        {
            pathProgress = 0f;
            direction = 1;
        }

        float easedProgress = EaseInOutSine(pathProgress);
        Vector3 nextPosition = Vector3.Lerp(startPoint, endPoint, easedProgress);
        MovePlatform(nextPosition);
    }

    private bool TryBuildPath(out Vector3 pathStart, out Vector3 pathEnd)
    {
        if (pointA != null && pointB != null)
        {
            pathStart = pointA.position;
            pathEnd = pointB.position;
            return true;
        }

        Vector3 offset = new Vector3(horizontalDistance, verticalDistance, 0f);
        if (offset.sqrMagnitude <= MinPathDistance * MinPathDistance)
        {
            pathStart = initialPosition;
            pathEnd = initialPosition;
            return false;
        }

        pathStart = initialPosition - offset;
        pathEnd = initialPosition + offset;
        return true;
    }

    private float GetProgressClosestToCurrentPosition()
    {
        if (!hasPath)
        {
            return 0f;
        }

        Vector3 path = endPoint - startPoint;
        float pathLengthSqr = path.sqrMagnitude;
        if (pathLengthSqr <= MinPathDistance * MinPathDistance)
        {
            return 0f;
        }

        float positionOnPath = Mathf.Clamp01(Vector3.Dot(transform.position - startPoint, path) / pathLengthSqr);
        return InverseEaseInOutSine(positionOnPath);
    }

    private void MovePlatform(Vector3 nextPosition)
    {
        Vector3 currentPosition = transform.position;
        nextPosition.z = currentPosition.z;
        PlatformSpeed = nextPosition - currentPosition;

        if (rb != null)
        {
            rb.MovePosition((Vector2)nextPosition);
        }
        else
        {
            transform.position = nextPosition;
        }
    }

    private float EaseInOutSine(float value)
    {
        return 0.5f - 0.5f * Mathf.Cos(Mathf.Clamp01(value) * Mathf.PI);
    }

    private float InverseEaseInOutSine(float value)
    {
        return Mathf.Acos(1f - 2f * Mathf.Clamp01(value)) / Mathf.PI;
    }

    private void OnDrawGizmosSelected()
    {
        Vector3 center = Application.isPlaying ? initialPosition : transform.position;
        Vector3 startPoint;
        Vector3 endPoint;

        if (pointA != null && pointB != null)
        {
            startPoint = pointA.position;
            endPoint = pointB.position;
        }
        else
        {
            Vector3 offset = new Vector3(horizontalDistance, verticalDistance, 0f);
            startPoint = center - offset;
            endPoint = center + offset;
        }

        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(startPoint, endPoint);
        Gizmos.DrawWireSphere(startPoint, 0.15f);
        Gizmos.DrawWireSphere(endPoint, 0.15f);
    }
}
