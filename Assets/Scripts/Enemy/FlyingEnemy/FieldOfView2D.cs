using UnityEngine;

public class FieldOfView2D : MonoBehaviour
{
    [Header("FOV Settings")]
    public float viewDistance = 7f;
    [Range(0f, 180f)] public float viewAngle = 120f;

    [Tooltip("Görüşü kapatan layer'lar (Ground vb.)")]
    public LayerMask obstacleLayers;

    [Header("Gizmos")]
    public bool drawGizmos = true;
    public Color coneColor = Color.yellow;
    public Color rayColor = Color.red;

    // Debug için son baktığı hedef
    [HideInInspector] public Vector3 debugTarget;

    /// <summary>
    /// Bu noktayı görebiliyor mu? (Root’un baktığı yöne göre)
    /// </summary>
    public bool CanSeePoint(Vector3 targetPos)
    {
        debugTarget = targetPos;

        Vector2 origin = transform.position;
        Vector2 toTarget = targetPos - (Vector3)origin;
        float dist = toTarget.magnitude;
        if (dist <= 0.001f || dist > viewDistance) return false;

        Vector2 dir = toTarget / dist;

        // Sağ yönü "ileri" kabul ediyoruz
        float angle = Vector2.Angle(transform.right, dir);
        if (angle > viewAngle * 0.5f) return false;

        // Arada engel var mı?
        if (Physics2D.Raycast(origin, dir, dist, obstacleLayers))
            return false;

        return true;
    }

    private void OnDrawGizmos()
    {
        if (!drawGizmos) return;

        Vector3 origin = transform.position;
        Vector3 rightDir = Quaternion.Euler(0, 0, viewAngle * 0.5f) * transform.right;
        Vector3 leftDir = Quaternion.Euler(0, 0, -viewAngle * 0.5f) * transform.right;

        Gizmos.color = coneColor;
        Gizmos.DrawLine(origin, origin + rightDir * viewDistance);
        Gizmos.DrawLine(origin, origin + leftDir * viewDistance);

        if (debugTarget != Vector3.zero)
        {
            Gizmos.color = rayColor;
            Gizmos.DrawLine(origin, debugTarget);
        }
    }
}