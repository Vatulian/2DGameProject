using UnityEngine;

[RequireComponent(typeof(PlayerMana))]
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

    [Header("Mana Reward")]
    [SerializeField, Min(0)] private int successfulParryManaReward = 20;

    private Animator anim;
    private PlayerAnimationController animationController;
    private PlayerMovement movement;
    private PlayerMeleeAttack meleeAttack;
    private PlayerSpecialMove specialMove;
    private PlayerMana playerMana;
    private Health health;
    private float activeTimer;
    private float cooldownTimer;

    public bool IsParryActive => activeTimer > 0f;

    public static bool TryParryHit(Collider2D playerHit, Vector3 attackerPosition, bool canBeParried = true, Component parrySource = null)
    {
        if (!canBeParried || playerHit == null)
            return false;

        PlayerParry parry = playerHit.GetComponent<PlayerParry>() ?? playerHit.GetComponentInParent<PlayerParry>();
        if (parry == null || !parry.TryParry(attackerPosition))
            return false;

        NotifyParryReceiver(parrySource, parry, attackerPosition);
        return true;
    }

    private static void NotifyParryReceiver(Component parrySource, PlayerParry parry, Vector3 attackerPosition)
    {
        if (parrySource == null)
            return;

        MonoBehaviour[] behaviours = parrySource.GetComponentsInParent<MonoBehaviour>();
        for (int i = 0; i < behaviours.Length; i++)
        {
            if (behaviours[i] is IParryReceiver receiver)
            {
                receiver.OnParried(parry, attackerPosition);
                return;
            }
        }
    }

    private void Awake()
    {
        if (!anim)
            anim = GetComponent<Animator>() ?? GetComponentInChildren<Animator>();

        animationController = GetComponentInChildren<PlayerAnimationController>();
        movement = GetComponent<PlayerMovement>();
        meleeAttack = GetComponent<PlayerMeleeAttack>();
        specialMove = GetComponent<PlayerSpecialMove>();
        playerMana = GetComponent<PlayerMana>();
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

        if (movement != null && (movement.IsDashing || movement.IsClimbing))
            return false;

        if (specialMove != null && specialMove.IsActive)
            return false;

        meleeAttack?.TryCancelForParry();

        activeTimer = activeTime;
        cooldownTimer = cooldownTime;

        if (lockMovementTime > 0f)
            movement?.LockHorizontalMovement(lockMovementTime);

        PlayParryState(parryPoseStateName, activeTime);

        if (SoundManager.instance && parryStartSound)
            SoundManager.instance.PlaySound(parryStartSound);

        return true;
    }

    public bool TryParry(Vector3 attackerPosition)
    {
        if (!IsParryActive)
            return false;

        activeTimer = 0f;
        cooldownTimer = 0f;
        FaceAttacker(attackerPosition);
        movement?.LockHorizontalMovement(successAnimationLockTime);

        PlayParryState(parrySuccessStateName, successAnimationLockTime);

        if (SoundManager.instance && parrySuccessSound)
            SoundManager.instance.PlaySound(parrySuccessSound);

        if (successfulParryManaReward > 0)
            playerMana?.RestoreMana(successfulParryManaReward);

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
