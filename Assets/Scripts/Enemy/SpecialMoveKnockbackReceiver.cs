using UnityEngine;

public class SpecialMoveKnockbackReceiver : MonoBehaviour
{
    private Rigidbody2D body;
    private Vector3 startPosition;
    private Vector3 targetPosition;
    private float duration;
    private float elapsed;
    private bool active;

    public void Apply(Vector3 sourcePosition, float horizontalDistance, float upwardDistance, float knockbackDuration)
    {
        if (horizontalDistance <= 0f || knockbackDuration <= 0f)
            return;

        if (body == null)
            body = GetComponent<Rigidbody2D>() ?? GetComponentInParent<Rigidbody2D>();

        float direction = transform.position.x >= sourcePosition.x ? 1f : -1f;
        float verticalDistance = body != null && body.bodyType == RigidbodyType2D.Kinematic
            ? 0f
            : upwardDistance;

        startPosition = transform.position;
        targetPosition = startPosition + new Vector3(direction * horizontalDistance, verticalDistance, 0f);
        duration = knockbackDuration;
        elapsed = 0f;
        active = true;
    }

    private void LateUpdate()
    {
        if (!active)
            return;

        elapsed += Time.deltaTime;
        float t = Mathf.Clamp01(elapsed / duration);
        float eased = 1f - (1f - t) * (1f - t);
        transform.position = Vector3.Lerp(startPosition, targetPosition, eased);

        if (t >= 1f)
            active = false;
    }
}
