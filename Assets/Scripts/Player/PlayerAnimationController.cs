using UnityEngine;

[DefaultExecutionOrder(50)]
public class PlayerAnimationController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Animator anim;
    [SerializeField] private Transform visualRoot;
    [SerializeField] private Rigidbody2D rb;
    [SerializeField] private PlayerMovement movement;
    [SerializeField] private PlayerMeleeAttack meleeAttack;
    [SerializeField] private Health health;

    [Header("Locomotion States")]
    [SerializeField] private string idleStateName = "Swordmaster_Idle";
    [SerializeField] private string runStateName = "Swordmaster_Run";
    [SerializeField] private string jumpStateName = "Swordmaster_Jump";
    [SerializeField] private string fallStateName = "Swordmaster_Fall";
    [SerializeField] private string dashStateName = "Swordmaster_Dash";
    [SerializeField] private string wallSlideStateName = "Swordmaster_Wall_Slide";
    [SerializeField, Range(0f, 0.12f)] private float locomotionTransitionDuration = 0.04f;
    [SerializeField] private float runVelocityThreshold = 0.05f;
    [SerializeField] private float fallVelocityThreshold = -0.05f;

    private static readonly int RunHash = Animator.StringToHash("Run");
    private static readonly int GroundedHash = Animator.StringToHash("Grounded");
    private static readonly int JumpHash = Animator.StringToHash("Jump");
    private static readonly int AttackHash = Animator.StringToHash("Attack");
    private static readonly int HurtHash = Animator.StringToHash("Hurt");
    private static readonly int DieHash = Animator.StringToHash("Die");
    private float defaultAnimatorSpeed = 1f;
    private float defaultVisualScaleX = 1f;
    private string currentLocomotionStateName;
    private float locomotionLockedUntil;

    private void Awake()
    {
        if (!anim) anim = GetComponent<Animator>() ?? GetComponentInChildren<Animator>() ?? GetComponentInParent<Animator>();
        if (!visualRoot) visualRoot = anim != null ? anim.transform : transform;
        if (!rb) rb = GetComponentInParent<Rigidbody2D>();
        if (!movement) movement = GetComponentInParent<PlayerMovement>();
        if (!meleeAttack) meleeAttack = GetComponentInParent<PlayerMeleeAttack>();
        if (!health) health = GetComponentInParent<Health>();
        if (anim) defaultAnimatorSpeed = anim.speed;
        if (visualRoot) defaultVisualScaleX = Mathf.Abs(visualRoot.localScale.x);
        if (Mathf.Approximately(defaultVisualScaleX, 0f)) defaultVisualScaleX = 1f;
    }

    private void OnEnable()
    {
        if (health == null)
            health = GetComponentInParent<Health>();

        if (health == null)
            return;

        health.OnDamaged += HandleDamaged;
        health.OnDeath += HandleDeath;
        health.OnRespawned += HandleRespawned;
    }

    private void OnDisable()
    {
        if (health == null)
            return;

        health.OnDamaged -= HandleDamaged;
        health.OnDeath -= HandleDeath;
        health.OnRespawned -= HandleRespawned;
    }

    private void Update()
    {
        if (!anim || !movement)
            return;

        if (health != null && health.IsDead)
            return;

        if (movement.IsClimbing)
        {
            ClearAirborneTriggers();
            anim.SetBool(RunHash, false);
            anim.SetBool(GroundedHash, true);
            return;
        }

        bool isGrounded = IsGroundedForAnimation();
        bool isRunning = IsRunningForAnimation(isGrounded);

        anim.SetBool(RunHash, isRunning);
        anim.SetBool(GroundedHash, isGrounded);

        if (IsLocomotionLocked())
            return;

        CrossFadeLocomotionState(GetLocomotionStateName(isGrounded, isRunning), locomotionTransitionDuration);
    }

    public void PlayJump()
    {
        if (!anim) return;
        anim.ResetTrigger(HurtHash);
        anim.SetTrigger(JumpHash);
        CrossFadeLocomotionState(jumpStateName, locomotionTransitionDuration, true);
    }

    public void PlayAttack()
    {
        if (!anim) return;
        LockLocomotion(0.25f);
        anim.SetTrigger(AttackHash);
    }

    public void PlayAttackState(string stateName, float transitionDuration = 0.05f)
    {
        if (!anim || string.IsNullOrWhiteSpace(stateName))
            return;

        anim.ResetTrigger(HurtHash);
        anim.ResetTrigger(DieHash);
        LockLocomotion(0.12f);
        currentLocomotionStateName = null;
        anim.CrossFadeInFixedTime(stateName, Mathf.Clamp(transitionDuration, 0f, 0.08f));
    }

    public void PlayLockedState(string stateName, float transitionDuration, float lockDuration)
    {
        if (!anim || string.IsNullOrWhiteSpace(stateName))
            return;

        anim.ResetTrigger(HurtHash);
        anim.ResetTrigger(DieHash);
        ClearAirborneTriggers();
        LockLocomotion(lockDuration);
        currentLocomotionStateName = null;
        anim.CrossFadeInFixedTime(stateName, Mathf.Clamp(transitionDuration, 0f, 0.12f));
    }

    public void PlayLockedStateImmediate(string stateName, float lockDuration)
    {
        if (!anim || string.IsNullOrWhiteSpace(stateName))
            return;

        anim.ResetTrigger(HurtHash);
        anim.ResetTrigger(DieHash);
        ClearAirborneTriggers();
        LockLocomotion(lockDuration);
        currentLocomotionStateName = null;
        anim.Play(stateName, 0, 0f);
        anim.Update(0f);
    }

    public void PlayHurt()
    {
        if (!anim) return;
        LockLocomotion(0.25f);
        currentLocomotionStateName = null;
        anim.ResetTrigger(DieHash);
        anim.SetTrigger(HurtHash);
    }

    public void PlayDeath()
    {
        if (!anim) return;
        ResetAnimatorSpeed();
        anim.ResetTrigger(AttackHash);
        anim.ResetTrigger(JumpHash);
        anim.SetBool(GroundedHash, true);
        anim.ResetTrigger(HurtHash);
        anim.SetTrigger(DieHash);
        LockLocomotion(999f);
        currentLocomotionStateName = null;
        anim.CrossFadeInFixedTime("Swordmaster_death", 0.02f);
    }

    public void PlayRespawn()
    {
        if (!anim) return;
        ResetAnimatorSpeed();
        anim.Rebind();
        anim.Update(0f);
        anim.ResetTrigger(DieHash);
        locomotionLockedUntil = 0f;
        currentLocomotionStateName = null;
        anim.Play(idleStateName, 0, 0f);
    }

    public void ReturnToLocomotionState(float transitionDuration = 0.05f)
    {
        if (!anim || !anim.isActiveAndEnabled || !gameObject.activeInHierarchy)
            return;

        string stateName = idleStateName;

        if (movement != null)
        {
            bool isGrounded = IsGroundedForAnimation();

            bool isRunning = IsRunningForAnimation(isGrounded);
            stateName = GetLocomotionStateName(isGrounded, isRunning);
        }

        locomotionLockedUntil = 0f;
        CrossFadeLocomotionState(stateName, transitionDuration, true);
    }

    public void ReturnToIdleState(float transitionDuration = 0.05f)
    {
        if (!anim || !anim.isActiveAndEnabled || !gameObject.activeInHierarchy)
            return;

        locomotionLockedUntil = 0f;
        CrossFadeLocomotionState(idleStateName, transitionDuration, true);
    }

    public void ReturnToIdleAfterClimb(float transitionDuration = 0.05f)
    {
        if (!anim || !anim.isActiveAndEnabled || !gameObject.activeInHierarchy)
            return;

        ClearAirborneTriggers();
        anim.SetBool(RunHash, false);
        anim.SetBool(GroundedHash, true);
        locomotionLockedUntil = 0f;
        CrossFadeLocomotionState(idleStateName, transitionDuration, true);
    }

    public void SetAnimatorSpeed(float speed)
    {
        if (!anim)
            return;

        anim.speed = Mathf.Max(0.1f, speed);
    }

    public void ResetAnimatorSpeed()
    {
        if (!anim)
            return;

        anim.speed = defaultAnimatorSpeed;
    }

    public void SetFacing(bool isFacingRight)
    {
        if (!visualRoot)
            return;

        Vector3 scale = visualRoot.localScale;
        float magnitude = Mathf.Abs(scale.x);
        if (Mathf.Approximately(magnitude, 0f))
            magnitude = defaultVisualScaleX;

        scale.x = isFacingRight ? magnitude : -magnitude;
        visualRoot.localScale = scale;
    }

    private void HandleDamaged(float remainingHp)
    {
        if (remainingHp <= 0f)
            return;

        PlayHurt();
    }

    private void HandleDeath()
    {
        PlayDeath();
    }

    private void HandleRespawned()
    {
        PlayRespawn();
    }

    private bool IsLocomotionLocked()
    {
        return Time.time < locomotionLockedUntil || (meleeAttack != null && meleeAttack.IsAttacking);
    }

    private void LockLocomotion(float duration)
    {
        locomotionLockedUntil = Mathf.Max(locomotionLockedUntil, Time.time + duration);
    }

    private void ClearAirborneTriggers()
    {
        anim.ResetTrigger(JumpHash);
    }

    private bool IsGroundedForAnimation()
    {
        return movement != null
               && !movement.IsJumping
               && !movement.IsWallJumping
               && !movement.IsDashing
               && !movement.IsSliding
               && movement.LastOnGroundTime > 0f;
    }

    private bool IsRunningForAnimation(bool isGrounded)
    {
        if (isGrounded && movement != null)
            return Mathf.Abs(movement.HorizontalInput) > 0.1f;

        return Mathf.Abs(rb != null ? rb.velocity.x : 0f) > runVelocityThreshold;
    }

    private string GetLocomotionStateName(bool isGrounded, bool isRunning)
    {
        if (movement != null)
        {
            if (movement.IsDashing && !string.IsNullOrWhiteSpace(dashStateName))
                return dashStateName;

            if (movement.IsSliding && !string.IsNullOrWhiteSpace(wallSlideStateName))
                return wallSlideStateName;
        }

        if (!isGrounded)
        {
            float verticalVelocity = rb != null ? rb.velocity.y : 0f;
            return verticalVelocity <= fallVelocityThreshold ? fallStateName : jumpStateName;
        }

        return isRunning ? runStateName : idleStateName;
    }

    private void CrossFadeLocomotionState(string stateName, float transitionDuration, bool force = false)
    {
        if (!anim || !anim.isActiveAndEnabled || !gameObject.activeInHierarchy || string.IsNullOrWhiteSpace(stateName))
            return;

        if (!force && currentLocomotionStateName == stateName)
            return;

        currentLocomotionStateName = stateName;
        if (transitionDuration <= 0f)
        {
            anim.Play(stateName, 0, 0f);
            anim.Update(0f);
            return;
        }

        anim.CrossFadeInFixedTime(stateName, Mathf.Clamp(transitionDuration, 0f, 0.12f));
    }

}
