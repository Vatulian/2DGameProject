using System.Collections;
using UnityEngine;

public class PlayerMeleeAttack : MonoBehaviour
{
    [System.Serializable]
    public class AttackPhase
    {
        [System.Serializable]
        public class HitWindow
        {
            public int damage = 1;
            public Vector2 size = new Vector2(1.2f, 0.8f);
            public Vector2 offset = new Vector2(0.8f, 0f);
            public PlayerMeleeHitboxAnchor anchor = PlayerMeleeHitboxAnchor.Root;
            public bool clearPreviousHits = true;
        }

        public string animatorStateName = "Swordmaster_Slash_1";
        [Range(0f, 0.08f)] public float transitionDuration = 0.03f;
        [Range(0.5f, 3f)] public float animationSpeed = 1.5f;
        public int damage = 1;
        public Vector2 hitboxSize = new Vector2(1.2f, 0.8f);
        public Vector2 hitboxOffset = new Vector2(0.8f, 0f);
        public PlayerMeleeHitboxAnchor hitboxAnchor = PlayerMeleeHitboxAnchor.Root;
        public HitWindow[] hitWindows;
        public float forwardLungeSpeed = 0f;
        public float forwardLungeDuration = 0f;
        public bool allowJumpCancel = true;
        public bool allowDashCancel = true;
        public bool allowParryCancel = true;
        public AudioClip attackSound;
    }

    [Header("Input")]
    [SerializeField] private KeyCode attackKey = KeyCode.Mouse1;

    [Header("Combo")]
    [SerializeField] private AttackPhase[] phases;
    [SerializeField] private float comboResetDelay = 0.75f;
    [SerializeField] private float inputQueueMemory = 0.4f;

    [Header("Hitbox")]
    [SerializeField] private PlayerMeleeHitbox meleeHitbox;
    [SerializeField] private bool logHitboxEvents;

    private PlayerAnimationController animationController;
    private PlayerMovement playerMovement;
    private Health health;

    private int currentPhaseIndex = -1;
    private float comboResetTimer;
    private float queuedInputTimer;
    private int queuedAttackCount;
    private Coroutine lungeCoroutine;

    public bool IsAttacking => currentPhaseIndex >= 0;
    public int CurrentPhase => currentPhaseIndex;

    private void Awake()
    {
        animationController = GetComponent<PlayerAnimationController>();
        playerMovement = GetComponent<PlayerMovement>();
        health = GetComponent<Health>();

        if (!meleeHitbox)
            meleeHitbox = GetComponentInChildren<PlayerMeleeHitbox>();
    }

    private void OnDisable()
    {
        ResetCombo(health == null || !health.IsDead);
    }

    private void Update()
    {
        if (health != null && health.IsDead)
            return;

        HandleCancelInputs();

        if (queuedInputTimer > 0f)
        {
            queuedInputTimer -= Time.deltaTime;
            if (queuedInputTimer <= 0f)
                queuedAttackCount = 0;
        }

        if (IsAttacking && comboResetTimer > 0f)
        {
            comboResetTimer -= Time.deltaTime;
            if (comboResetTimer <= 0f)
                ResetCombo();
        }

        if (Input.GetKeyDown(attackKey))
            HandleAttackInput();
    }

    private void HandleCancelInputs()
    {
        if (!IsAttacking || currentPhaseIndex < 0 || currentPhaseIndex >= phases.Length)
            return;

        AttackPhase phase = phases[currentPhaseIndex];

        if (phase.allowJumpCancel && playerMovement != null && playerMovement.WasJumpPressedThisFrame())
        {
            ResetCombo();
            return;
        }

        if (phase.allowDashCancel && playerMovement != null && playerMovement.WasDashPressedThisFrame())
            ResetCombo();
    }

    private void HandleAttackInput()
    {
        if (health != null && health.IsDead)
            return;

        if (!IsAttacking)
        {
            if (playerMovement == null || !playerMovement.canAttack())
                return;

            StartPhase(0);
            return;
        }

        if (!HasNextPhase())
            return;

        int remainingPhases = phases.Length - (currentPhaseIndex + 1);
        queuedAttackCount = Mathf.Clamp(queuedAttackCount + 1, 0, remainingPhases);
        queuedInputTimer = inputQueueMemory;
    }

    private void StartPhase(int phaseIndex)
    {
        if (phases == null || phases.Length == 0 || phaseIndex < 0 || phaseIndex >= phases.Length)
            return;

        currentPhaseIndex = phaseIndex;
        comboResetTimer = comboResetDelay;

        AttackPhase phase = phases[currentPhaseIndex];
        playerMovement?.SetExternalRunMultiplier(0f);
        meleeHitbox?.Configure(phase.damage, phase.hitboxSize, phase.hitboxOffset, phase.hitboxAnchor);

        if (animationController != null)
        {
            animationController.SetAnimatorSpeed(phase.animationSpeed);
            animationController.PlayAttackState(phase.animatorStateName, phase.transitionDuration);
        }

        if (SoundManager.instance && phase.attackSound)
            SoundManager.instance.PlaySound(phase.attackSound);

        StartLunge(phase);
    }

    private bool HasNextPhase()
    {
        return phases != null && currentPhaseIndex + 1 < phases.Length;
    }

    public void OpenComboWindow()
    {
        // Legacy event kept for compatibility with existing clips.
    }

    public void CloseComboWindow()
    {
        // Legacy event kept for compatibility with existing clips.
    }

    public void EnableHitbox()
    {
        if (!IsAttacking)
            return;

        if (logHitboxEvents)
            Debug.Log($"[PlayerMeleeAttack] EnableHitbox phase {currentPhaseIndex}", this);

        meleeHitbox?.BeginSwing();
    }

    public void EnableHitboxWindow(int windowIndex)
    {
        if (!IsAttacking || meleeHitbox == null || !TryGetCurrentPhase(out AttackPhase phase))
            return;

        if (phase.hitWindows == null || windowIndex < 0 || windowIndex >= phase.hitWindows.Length)
        {
            EnableHitbox();
            return;
        }

        AttackPhase.HitWindow window = phase.hitWindows[windowIndex];
        if (logHitboxEvents)
            Debug.Log($"[PlayerMeleeAttack] EnableHitboxWindow {windowIndex} phase {currentPhaseIndex}", this);

        meleeHitbox.Configure(window.damage, window.size, window.offset, window.anchor);
        meleeHitbox.BeginSwing(window.clearPreviousHits);
    }

    public void DisableHitbox()
    {
        if (!IsAttacking)
        {
            meleeHitbox?.EndSwing();
            return;
        }

        if (logHitboxEvents)
            Debug.Log($"[PlayerMeleeAttack] DisableHitbox phase {currentPhaseIndex}", this);

        meleeHitbox?.EndSwing();
    }

    public void CompleteAttackPhase()
    {
        if (!IsAttacking)
            return;

        DisableHitbox();

        if (queuedAttackCount > 0 && HasNextPhase())
        {
            queuedAttackCount--;
            StartPhase(currentPhaseIndex + 1);
            return;
        }

        ResetCombo();
    }

    public void ResetCombo()
    {
        ResetCombo(true);
    }

    public void ResetCombo(bool returnToLocomotion)
    {
        comboResetTimer = 0f;
        queuedInputTimer = 0f;
        queuedAttackCount = 0;
        currentPhaseIndex = -1;
        StopLunge();
        meleeHitbox?.EndSwing();
        playerMovement?.ClearForcedHorizontalVelocity();
        playerMovement?.ResetExternalRunMultiplier();
        animationController?.ResetAnimatorSpeed();
        if (returnToLocomotion)
            animationController?.ReturnToLocomotionState();
    }

    public bool TryCancelForParry()
    {
        if (!IsAttacking || currentPhaseIndex < 0 || currentPhaseIndex >= phases.Length)
            return false;

        if (!phases[currentPhaseIndex].allowParryCancel)
            return false;

        ResetCombo();
        return true;
    }

    private void StartLunge(AttackPhase phase)
    {
        StopLunge();

        if (playerMovement == null || phase.forwardLungeSpeed <= 0f || phase.forwardLungeDuration <= 0f)
            return;

        lungeCoroutine = StartCoroutine(PerformLunge(phase.forwardLungeSpeed, phase.forwardLungeDuration));
    }

    private bool TryGetCurrentPhase(out AttackPhase phase)
    {
        phase = null;

        if (phases == null || currentPhaseIndex < 0 || currentPhaseIndex >= phases.Length)
            return false;

        phase = phases[currentPhaseIndex];
        return true;
    }

    private void StopLunge()
    {
        if (lungeCoroutine != null)
        {
            StopCoroutine(lungeCoroutine);
            lungeCoroutine = null;
        }
    }

    private IEnumerator PerformLunge(float speed, float duration)
    {
        if (playerMovement == null || !IsAttacking)
            yield break;

        float facing = playerMovement.IsFacingRight ? 1f : -1f;
        playerMovement.ForceHorizontalVelocity(facing * speed, duration);
        yield return new WaitForSeconds(duration);
        lungeCoroutine = null;
    }
}

