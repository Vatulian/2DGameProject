using System.Collections;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class ColossalBossMovingAttackHitbox : ColossalBossAttackHitbox
{
    [Header("Ground Slam States")]
    [Tooltip("Hitbox position at the beginning of Attack.")]
    [SerializeField] private Vector3 rightFacingStartLocalPosition = Vector3.zero;
    [Tooltip("Hitbox position when EnableHitbox is fired by the animation.")]
    [SerializeField] private Vector3 rightFacingPrepLocalPosition = Vector3.zero;
    [Tooltip("Hitbox position when SpawnShockwaves is fired by the animation.")]
    [SerializeField] private Vector3 rightFacingImpactLocalPosition = new Vector3(0f, -1f, 0f);
    [Tooltip("How quickly the disabled debug hitbox moves from START to PREP after Attack begins.")]
    [SerializeField] private float startToPrepDuration = 0.45f;
    [Tooltip("How quickly the hitbox travels from PREP to IMPACT after EnableHitbox.")]
    [SerializeField] private float prepToImpactDuration = 0.12f;

    [Header("Debug")]
    [Tooltip("Draws START, PREP and IMPACT boxes in the Scene view for attack tuning.")]
    [SerializeField] private bool showStateGizmos = true;
    [SerializeField] private Color startGizmoColor = new Color(0.2f, 0.75f, 1f, 0.9f);
    [SerializeField] private Color prepGizmoColor = new Color(0.25f, 1f, 0.35f, 0.9f);
    [SerializeField] private Color impactGizmoColor = new Color(1f, 0.35f, 0.15f, 0.9f);
    [SerializeField] private Color currentGizmoColor = new Color(1f, 1f, 0.1f, 1f);

    public override void Begin(Transform attackOwner, int attackFacing)
    {
        base.Begin(attackOwner, attackFacing);

        if (startToPrepDuration > 0f)
            SetTimedRoutine(MoveToPrep(startToPrepDuration));
        else
            ApplyFacingToLocalPosition(rightFacingPrepLocalPosition);
    }

    public override void EnableTimed(float duration)
    {
        StopTimedRoutine();
        ApplyFacingToLocalPosition(rightFacingPrepLocalPosition);
        SetActive(true);
        SetTimedRoutine(TimedGroundSlam(duration));
    }

    public override void EnableHitbox()
    {
        StopTimedRoutine();
        ApplyFacingToLocalPosition(rightFacingPrepLocalPosition);
        SetActive(true);

        if (prepToImpactDuration > 0f)
            SetTimedRoutine(MoveToImpact(prepToImpactDuration, true));
        else
            MoveToImpactPosition();
    }

    public override void MoveToImpactPosition()
    {
        StopTimedRoutine();
        ApplyFacingToLocalPosition(rightFacingImpactLocalPosition);
    }

    protected override void ResetLocalPosition()
    {
        ApplyFacingToLocalPosition(rightFacingStartLocalPosition);
    }

    private IEnumerator MoveToImpact(float duration, bool clearRoutineOnComplete)
    {
        Vector3 start = transform.localPosition;
        Vector3 end = rightFacingImpactLocalPosition;
        end.x = Mathf.Abs(end.x) * Facing;

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / duration);
            transform.localPosition = Vector3.LerpUnclamped(start, end, t);
            yield return null;
        }

        if (clearRoutineOnComplete)
            ClearTimedRoutine();

        ApplyFacingToLocalPosition(rightFacingImpactLocalPosition);
    }

    private IEnumerator MoveToPrep(float duration)
    {
        Vector3 start = transform.localPosition;
        Vector3 end = rightFacingPrepLocalPosition;
        end.x = Mathf.Abs(end.x) * Facing;

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / duration);
            transform.localPosition = Vector3.LerpUnclamped(start, end, t);
            yield return null;
        }

        ClearTimedRoutine();
        ApplyFacingToLocalPosition(rightFacingPrepLocalPosition);
    }

    private IEnumerator TimedGroundSlam(float duration)
    {
        if (prepToImpactDuration > 0f)
            yield return MoveToImpact(prepToImpactDuration, false);
        else
            ApplyFacingToLocalPosition(rightFacingImpactLocalPosition);

        if (duration > prepToImpactDuration)
            yield return new WaitForSeconds(duration - prepToImpactDuration);

        ClearTimedRoutine();
        SetActive(false);
        ResetLocalPosition();
    }

    protected override void OnValidate()
    {
        base.OnValidate();
        startToPrepDuration = Mathf.Max(0f, startToPrepDuration);
        prepToImpactDuration = Mathf.Max(0f, prepToImpactDuration);
    }

    private void OnDrawGizmos()
    {
        if (!showStateGizmos)
            return;

        DrawMotionState(rightFacingStartLocalPosition, startGizmoColor, "START");
        DrawMotionState(rightFacingPrepLocalPosition, prepGizmoColor, "PREP");
        DrawMotionState(rightFacingImpactLocalPosition, impactGizmoColor, "IMPACT");
        DrawCurrentState();

        Gizmos.color = Color.white;
        Gizmos.DrawLine(GetStateWorldCenter(rightFacingStartLocalPosition), GetStateWorldCenter(rightFacingPrepLocalPosition));
        Gizmos.DrawLine(GetStateWorldCenter(rightFacingPrepLocalPosition), GetStateWorldCenter(rightFacingImpactLocalPosition));
    }

    private void DrawMotionState(Vector3 rightFacingPosition, Color color, string label)
    {
        Vector3 center = GetStateWorldCenter(rightFacingPosition);
        Gizmos.color = color;
        Gizmos.DrawWireCube(center, GetColliderWorldSize());

#if UNITY_EDITOR
        Handles.color = color;
        Handles.Label(center, label);
#endif
    }

    private void DrawCurrentState()
    {
        Gizmos.color = currentGizmoColor;
        Gizmos.DrawWireCube(GetWorldCenter(), GetColliderWorldSize());

#if UNITY_EDITOR
        Handles.color = currentGizmoColor;
        Handles.Label(GetWorldCenter(), "CURRENT");
#endif
    }

    private Vector3 GetStateWorldCenter(Vector3 rightFacingPosition)
    {
        int gizmoFacing = transform.localPosition.x < 0f ? -1 : 1;
        Vector3 localPosition = rightFacingPosition;
        localPosition.x = Mathf.Abs(localPosition.x) * gizmoFacing;

        Vector3 offset = GetColliderOffset();
        Transform parent = transform.parent;
        return parent != null ? parent.TransformPoint(localPosition + offset) : transform.TransformPoint(offset);
    }
}
