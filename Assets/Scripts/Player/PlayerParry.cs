using UnityEngine;

public class PlayerParry : MonoBehaviour
{
    [Header("Input")]
    [SerializeField] private KeyCode parryKey = KeyCode.Q;

    [Header("Timing")]
    [SerializeField] private float activeTime = 0.22f;
    [SerializeField] private float cooldownTime = 0.8f;
    [SerializeField] private float lockMovementTime = 0.12f;

    [Header("Feedback")]
    [SerializeField] private string parryPoseStateName = "Swordmaster_Block_Pose";
    [SerializeField] private string parrySuccessStateName = "Swordmaster_Block";
    [SerializeField, Range(0f, 0.12f)] private float animationTransitionTime = 0.03f;
    [SerializeField] private float successAnimationLockTime = 0.45f;
    [SerializeField] private AudioClip parryStartSound;
    [SerializeField] private AudioClip parrySuccessSound;

    private Animator anim;
    private PlayerAnimationController animationController;
    private PlayerMovement movement;
    private PlayerMeleeAttack meleeAttack;
    private Health health;
    private float activeTimer;
    private float cooldownTimer;

    public bool IsParryActive => activeTimer > 0f;
    public bool IsOnCooldown => cooldownTimer > 0f;

    private void Awake()
    {
        if (!anim)
            anim = GetComponent<Animator>();

        animationController = GetComponent<PlayerAnimationController>();
        movement = GetComponent<PlayerMovement>();
        meleeAttack = GetComponent<PlayerMeleeAttack>();
        health = GetComponent<Health>();
    }

    private void Update()
    {
        if (activeTimer > 0f)
            activeTimer -= Time.deltaTime;

        if (cooldownTimer > 0f)
            cooldownTimer -= Time.deltaTime;

        if (health != null && health.IsDead)
            return;

        if (Input.GetKeyDown(parryKey))
            TryStartParry();
    }

    public bool TryStartParry()
    {
        if (cooldownTimer > 0f || activeTimer > 0f)
            return false;

        if (movement != null && movement.IsDashing)
            return false;

        meleeAttack?.TryCancelForParry();

        activeTimer = activeTime;
        cooldownTimer = cooldownTime;

        if (lockMovementTime > 0f)
            movement?.SetExternalRunMultiplier(0f);

        PlayParryState(parryPoseStateName, activeTime);

        if (SoundManager.instance && parryStartSound)
            SoundManager.instance.PlaySound(parryStartSound);

        if (lockMovementTime > 0f)
            Invoke(nameof(ReleaseMovementLock), lockMovementTime);

        return true;
    }

    public bool TryParry(Vector3 attackerPosition)
    {
        if (!IsParryActive)
            return false;

        activeTimer = 0f;
        FaceAttacker(attackerPosition);

        PlayParryState(parrySuccessStateName, successAnimationLockTime);

        if (SoundManager.instance && parrySuccessSound)
            SoundManager.instance.PlaySound(parrySuccessSound);

        ReleaseMovementLock();
        return true;
    }

    private void FaceAttacker(Vector3 attackerPosition)
    {
        if (movement == null)
            return;

        float direction = attackerPosition.x - transform.position.x;
        if (!Mathf.Approximately(direction, 0f))
            movement.CheckDirectionToFace(direction > 0f);
    }

    private void ReleaseMovementLock()
    {
        movement?.ResetExternalRunMultiplier();
    }

    private void PlayParryState(string stateName, float lockDuration)
    {
        if (string.IsNullOrWhiteSpace(stateName))
            return;

        if (animationController != null)
        {
            animationController.PlayLockedState(stateName, animationTransitionTime, lockDuration);
            return;
        }

        if (anim != null)
            anim.CrossFadeInFixedTime(stateName, animationTransitionTime);
    }
}
