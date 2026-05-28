using UnityEngine;

[CreateAssetMenu(menuName = "Player Data")] //Create a new playerData object by right clicking in the Project Menu then Create/Player/Player Data and drag onto the player
public class PlayerData : ScriptableObject
{
    [Header("Gravity")]
    [HideInInspector] public float gravityStrength; //Downwards force (gravity) needed for the desired jumpHeight and jumpTimeToApex.
    [HideInInspector] public float gravityScale; //Strength of the player's gravity as a multiplier of gravity (set in ProjectSettings/Physics2D).
                                                 //Also the value the player's rigidbody2D.gravityScale is set to.
    [Space(5)]
    public float fallGravityMult; //Multiplier to the player's gravityScale when falling.
    public float maxFallSpeed; //Maximum fall speed (terminal velocity) of the player when falling.
    [Space(5)]
    public float fastFallGravityMult; //Larger multiplier to the player's gravityScale when they are falling and a downwards input is pressed.
                                      //Seen in games such as Celeste, lets the player fall extra fast if they wish.
    public float maxFastFallSpeed; //Maximum fall speed(terminal velocity) of the player when performing a faster fall.

    [Space(20)]

    [Header("Run")]
    public float runMaxSpeed; //Target speed we want the player to reach.
    public float runAcceleration; //The speed at which our player accelerates to max speed, can be set to runMaxSpeed for instant acceleration down to 0 for none at all
    [HideInInspector] public float runAccelAmount; //The actual force (multiplied with speedDiff) applied to the player.
    public float runDecceleration; //The speed at which our player decelerates from their current speed, can be set to runMaxSpeed for instant deceleration down to 0 for none at all
    [HideInInspector] public float runDeccelAmount; //Actual force (multiplied with speedDiff) applied to the player .
    [Space(5)]
    [Range(0f, 1)] public float accelInAir; //Multipliers applied to acceleration rate when airborne.
    [Range(0f, 1)] public float deccelInAir;
    [Space(5)]
    public bool doConserveMomentum = true;

    [Space(20)]

    [Header("Jump")]
    public float jumpHeight; //Height of the player's jump
    public float jumpTimeToApex; //Time between applying the jump force and reaching the desired jump height. These values also control the player's gravity and jump force.
    [HideInInspector] public float jumpForce; //The actual force applied (upwards) to the player when they jump.

    [Header("Both Jumps")]
    public float jumpCutGravityMult; //Multiplier to increase gravity if the player releases the jump button while still jumping
    [Range(0f, 1)] public float jumpHangGravityMult; //Reduces gravity while close to the apex (desired max height) of the jump
    public float jumpHangTimeThreshold; //Speeds (close to 0) where the player will experience extra "jump hang". The player's velocity.y is closest to 0 at the jump's apex (think of the gradient of a parabola or quadratic function)
    [Space(0.5f)]
    public float jumpHangAccelerationMult;
    public float jumpHangMaxSpeedMult;

    [Header("Air Attack")]
    public bool enableAirAttackFloat = true;
    [Range(0f, 1f)] public float airAttackGravityMult = 0.25f;
    public float airAttackFloatDuration = 0.18f;
    public float airAttackMaxFallSpeed = 2.5f;
    public float airAttackStartFallSpeed = 0.5f;
    public float airAttackMaxUpwardSpeed = 0f;
    public float airAttackRestartGraceTime = 0.2f;
    [Range(0f, 1f)] public float airAttackGraceGravityMult = 0.5f;
    public float airAttackGraceMaxFallSpeed = 4f;

    [Header("Extra Jump")]
    public int extraJumpCount = 1; //How many extra jumps the player can perform in air
    [Range(0.5f, 1.5f)] public float extraJumpForceMultiplier = 1f; //Multiplier for extra jump force compared to normal jump

    [Header("Wall Jump")]
    public Vector2 wallJumpForce; //The actual force (this time set by us) applied to the player when wall jumping.
    [Space(5)]
    [Range(0f, 1f)] public float wallJumpRunLerp; //Reduces the effect of player's movement while wall jumping.
    [Range(0f, 1.5f)] public float wallJumpTime; //Time after wall jumping the player's movement is slowed for.
    public bool doTurnOnWallJump; //Player will rotate to face wall jumping direction

    [Header("Wall Settings")]
    [Range(0f, 0.3f)] public float wallJumpInputLockTime = 0.15f; //Short delay before allowing flip/input influence after wall jump
    [Range(0f, 0.3f)] public float wallJumpCooldown = 0.08f;
    [Range(0f, 0.5f)] public float sameWallJumpLockTime = 0.25f;
    [Range(0f, 10f)] public float wallJumpMaxUpwardCarrySpeed = 0f;
    [Range(0f, 60f)] public float wallJumpMaxHorizontalSpeed = 25f;
    [Range(0f, 60f)] public float wallJumpMaxVerticalSpeed = 18f;
    [Range(0f, 0.5f)] public float wallClingTime = 0.15f; //How long the player sticks to the wall before starting to slide

    [Space(20)]

    [Header("Slide")]
    public float slideSpeed; //Use a negative value for downward wall slide, e.g. -3
    public float slideAccel;
    [Range(0f, 0.5f)] public float wallSlideReleaseGraceTime = 0.12f; //Keeps wall slide/jump/dash available briefly after letting go of the wall input

    [Header("Assists")]
    [Range(0.01f, 0.5f)] public float coyoteTime; //Grace period after falling off a platform, where you can still jump
    [Range(0f, 0.5f)] public float wallCoyoteTime = 0.05f; //Grace period after leaving a wall, where a wall jump/slide can still register
    [Range(0.01f, 0.5f)] public float jumpInputBufferTime; //Grace period after pressing jump where a jump will be automatically performed once the requirements (eg. being grounded) are met.

    [Space(20)]

    [Header("Dash")]
    public int dashAmount;
    public float dashSpeed;
    public float dashSleepTime; //Duration for which the game freezes when we press dash but before we read directional input and apply a force
    [Space(5)]
    public float dashAttackTime;
    [Space(5)]
    public float dashEndTime; //Time after you finish the inital drag phase, smoothing the transition back to idle (or any standard state)
    public Vector2 dashEndSpeed; //Slows down player, makes dash feel more responsive
    [Space(5)]
    public float dashRefillTime;
    [Space(5)]
    [Range(0.01f, 0.5f)] public float dashInputBufferTime;

    //Unity Callback, called when the inspector updates
    private void OnValidate()
    {
        //Calculate gravity strength using the formula (gravity = 2 * jumpHeight / timeToJumpApex^2) 
        gravityStrength = -(2 * jumpHeight) / (jumpTimeToApex * jumpTimeToApex);

        //Calculate the rigidbody's gravity scale (ie: gravity strength relative to unity's gravity value, see project settings/Physics2D)
        gravityScale = gravityStrength / Physics2D.gravity.y;

        //Calculate are run acceleration & deceleration forces using formula: amount = ((1 / Time.fixedDeltaTime) * acceleration) / runMaxSpeed
        runAccelAmount = (50 * runAcceleration) / runMaxSpeed;
        runDeccelAmount = (50 * runDecceleration) / runMaxSpeed;

        //Calculate jumpForce using the formula (initialJumpVelocity = gravity * timeToJumpApex)
        jumpForce = Mathf.Abs(gravityStrength) * jumpTimeToApex;

        #region Variable Ranges
        runAcceleration = Mathf.Clamp(runAcceleration, 0.01f, runMaxSpeed);
        runDecceleration = Mathf.Clamp(runDecceleration, 0.01f, runMaxSpeed);
        extraJumpCount = Mathf.Max(0, extraJumpCount);
        airAttackFloatDuration = Mathf.Max(0f, airAttackFloatDuration);
        airAttackMaxFallSpeed = Mathf.Max(0f, airAttackMaxFallSpeed);
        airAttackStartFallSpeed = Mathf.Max(0f, airAttackStartFallSpeed);
        airAttackMaxUpwardSpeed = Mathf.Max(0f, airAttackMaxUpwardSpeed);
        airAttackRestartGraceTime = Mathf.Max(0f, airAttackRestartGraceTime);
        airAttackGraceMaxFallSpeed = Mathf.Max(0f, airAttackGraceMaxFallSpeed);
        wallJumpCooldown = Mathf.Max(0f, wallJumpCooldown);
        sameWallJumpLockTime = Mathf.Max(0f, sameWallJumpLockTime);
        wallJumpMaxUpwardCarrySpeed = Mathf.Max(0f, wallJumpMaxUpwardCarrySpeed);
        wallJumpMaxHorizontalSpeed = Mathf.Max(0f, wallJumpMaxHorizontalSpeed);
        wallJumpMaxVerticalSpeed = Mathf.Max(0f, wallJumpMaxVerticalSpeed);
        #endregion
    }
}
