using System.Collections;
using UnityEngine;
using UnityEngine.Serialization;

public class PlayerMovement : MonoBehaviour
{
    
    public PlayerData Data;

    #region COMPONENTS
    public Rigidbody2D RB { get; private set; }
    
    //public PlayerAnimator AnimHandler { get; private set; }
    #endregion

    #region STATE PARAMETERS
    //Variables control the various actions the player can perform at any time.
    //These are fields which can are public allowing for other sctipts to read them
    //but can only be privately written to.
    public bool IsFacingRight { get; private set; }
    public bool IsJumping { get; private set; }
    public bool IsWallJumping { get; private set; }
    public bool IsDashing { get; private set; }
    public bool IsSliding { get; private set; }

    //Timers (also all fields, could be private and a method returning a bool could be used)
    public float LastOnGroundTime { get; private set; }
    public float LastOnWallTime { get; private set; }
    public float LastOnWallRightTime { get; private set; }
    public float LastOnWallLeftTime { get; private set; }

    //Jump
    private bool _isJumpCut;
    private bool _isJumpFalling;
    private bool _jumpPressedThisFrame;

    //Wall Jump
    private float _wallJumpStartTime;
    private float _lastWallJumpTime = float.NegativeInfinity;
    private float _sameWallJumpLockedUntil;
    private int _lastWallJumpDir;
    private int _lastWallJumpWallDir;
    private bool _releasedLastWallJumpWall = true;
    private bool _isTouchingWallRight;
    private bool _isTouchingWallLeft;

    //Dash
    private int _dashesLeft;
    private bool _dashRefilling;
    private Vector2 _lastDashDir;
    private bool _isDashAttacking;

    //Extra Jump
    private int _extraJumpsLeft;

    [Header("Wall Runtime State")]
    private float _wallClingStartTime;
    private bool _wasOnWall;

    #endregion

    #region INPUT PARAMETERS
    private Vector2 _moveInput;

    public float LastPressedJumpTime { get; private set; }
    public float LastPressedDashTime { get; private set; }
    #endregion

    #region CHECK PARAMETERS
    //Set all of these up in the inspector
    [Header("Checks")]
    [SerializeField] private Transform _groundCheckPoint;
    //Size of groundCheck depends on the size of your character generally you want them slightly small than width (for ground) and height (for the wall check)
    [SerializeField] private Vector2 _groundCheckSize = new Vector2(0.49f, 0.03f);
    [Space(5)]
    [FormerlySerializedAs("_frontWallCheckPoint")]
    [SerializeField] private Transform _rightWallCheckPoint;
    [FormerlySerializedAs("_backWallCheckPoint")]
    [SerializeField] private Transform _leftWallCheckPoint;
    [SerializeField] private Vector2 _wallCheckSize = new Vector2(0.5f, 1f);
    [SerializeField] private string oneWayPlatformTag = "OneWayPlatform";
    [SerializeField] private float dropThroughPlatformDuration = 0.25f;
    [SerializeField] private float dropThroughPushSpeed = 2f;
    #endregion

    #region LAYERS & TAGS
    [Header("Layers & Tags")]
    [SerializeField] private LayerMask _groundLayer;
    [SerializeField] private LayerMask _wallLayer;

    //Animation
    private PlayerAnimationController animationController;
    private float externalRunMultiplier = 1f;
    private float forcedHorizontalVelocity;
    private float forcedHorizontalVelocityTimer;
    private float airAttackFloatTimer;
    private float airAttackRestartGraceTimer;
    private Health health;
    private Collider2D[] playerColliders;
    private Coroutine dropThroughCoroutine;
    private LedgeClimb ledgeClimb;

    #endregion

    private void Awake()
    {
        RB = GetComponent<Rigidbody2D>();
        //AnimHandler = GetComponent<PlayerAnimator>();
        animationController = GetComponentInChildren<PlayerAnimationController>();
        health = GetComponent<Health>();
        playerColliders = GetComponentsInChildren<Collider2D>();
        ledgeClimb = GetComponent<LedgeClimb>();

    }

    private void Start()
    {
        SetGravityScale(Data.gravityScale);
        Vector3 rootScale = transform.localScale;
        rootScale.x = Mathf.Abs(rootScale.x);
        transform.localScale = rootScale;

        IsFacingRight = true;
        animationController?.SetFacing(IsFacingRight);
        _dashesLeft = Data.dashAmount;
        _extraJumpsLeft = Data.extraJumpCount;
    }

    private void Update()
    {
        if (health != null && health.IsDead)
            return;

        if (IsClimbing)
            return;

        if (forcedHorizontalVelocityTimer > 0f)
            forcedHorizontalVelocityTimer -= Time.deltaTime;

        if (airAttackFloatTimer > 0f)
            airAttackFloatTimer -= Time.deltaTime;

        if (airAttackRestartGraceTimer > 0f)
            airAttackRestartGraceTimer -= Time.deltaTime;

        #region TIMERS
        LastOnGroundTime -= Time.deltaTime;
        LastOnWallTime -= Time.deltaTime;
        LastOnWallRightTime -= Time.deltaTime;
        LastOnWallLeftTime -= Time.deltaTime;

        LastPressedJumpTime -= Time.deltaTime;
        LastPressedDashTime -= Time.deltaTime;
        #endregion

        #region INPUT HANDLER
        _moveInput.x = Input.GetAxisRaw("Horizontal");
        _moveInput.y = Input.GetAxisRaw("Vertical");

        bool canFlip = !IsWallJumping || Time.time - _wallJumpStartTime > Data.wallJumpInputLockTime;

        if (_moveInput.x != 0 && canFlip)
            CheckDirectionToFace(_moveInput.x > 0);

        if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.C) || Input.GetKeyDown(KeyCode.J))
        {
            if (!TryDropThroughPlatform())
                OnJumpInput();
        }

        if (Input.GetKeyUp(KeyCode.Space) || Input.GetKeyUp(KeyCode.C) || Input.GetKeyUp(KeyCode.J))
        {
            OnJumpUpInput();
        }

        if (Input.GetKeyDown(KeyCode.X) || Input.GetKeyDown(KeyCode.LeftShift) || Input.GetKeyDown(KeyCode.K))
        {
            OnDashInput();
        }
        #endregion

        #region COLLISION CHECKS
        if (!IsDashing)
        {
            bool rightWallHit = HasWallContact(_rightWallCheckPoint.position);
            bool leftWallHit = HasWallContact(_leftWallCheckPoint.position);

            _isTouchingWallRight = rightWallHit;
            _isTouchingWallLeft = leftWallHit;
            UpdateLastWallJumpWallRelease();

            //Ground Check
            if (Physics2D.OverlapBox(_groundCheckPoint.position, _groundCheckSize, 0, _groundLayer)) //checks if set box overlaps with ground
            {
                if (LastOnGroundTime < -0.1f)
                {
                    //AnimHandler.justLanded = true;
                }

                LastOnGroundTime = Data.coyoteTime; //if so sets the lastGrounded to coyoteTime
            }

            //Right Wall Check
            if (_isTouchingWallRight && !IsWallJumping)
                LastOnWallRightTime = Data.wallCoyoteTime;

            //Left Wall Check
            if (_isTouchingWallLeft && !IsWallJumping)
                LastOnWallLeftTime = Data.wallCoyoteTime;

            //Two checks needed for both left and right walls since whenever the play turns the wall checkPoints swap sides
            LastOnWallTime = Mathf.Max(LastOnWallLeftTime, LastOnWallRightTime);
        }
        #endregion

        #region WALL CLING TRACKING
        bool onWallNow = LastOnWallTime > 0 && LastOnGroundTime <= 0;

        if (onWallNow && !_wasOnWall)
        {
            _wallClingStartTime = Time.time;
        }

        _wasOnWall = onWallNow;
        #endregion

        #region JUMP CHECKS
        if (IsJumping && RB.velocity.y < 0)
        {
            IsJumping = false;

            _isJumpFalling = true;
        }

        if (IsWallJumping && Time.time - _wallJumpStartTime > Data.wallJumpTime)
        {
            IsWallJumping = false;
        }

        if (LastOnGroundTime > 0 && !IsJumping && !IsWallJumping)
        {
                _isJumpCut = false;

                _isJumpFalling = false;
                _extraJumpsLeft = Data.extraJumpCount;
                _lastWallJumpWallDir = 0;
                _releasedLastWallJumpWall = true;
            }

        if (!IsDashing)
        {
            //Jump
            if (CanJump() && LastPressedJumpTime > 0)
            {
                IsJumping = true;
                IsWallJumping = false;
                _isJumpCut = false;
                _isJumpFalling = false;
                Jump();
                animationController?.PlayJump();

                //AnimHandler.startedJumping = true;
            }
            //WALL JUMP
            else if (CanWallJump())
            {
                IsWallJumping = true;
                IsJumping = false;
                _isJumpCut = false;
                _isJumpFalling = false;

                _wallJumpStartTime = Time.time;
                GetWallJumpDirections(out int wallJumpDir, out int wallDir);
                _lastWallJumpDir = wallJumpDir;
                _lastWallJumpWallDir = wallDir;
                _lastWallJumpTime = Time.time;
                _sameWallJumpLockedUntil = Time.time + Data.sameWallJumpLockTime;
                _releasedLastWallJumpWall = false;

                if (Data.doTurnOnWallJump)
                    CheckDirectionToFace(_lastWallJumpDir > 0);

                WallJump(_lastWallJumpDir);

            }
            //DOUBLE JUMP
            else if (CanExtraJump() && LastPressedJumpTime > 0)
            {
                IsJumping = true;
                IsWallJumping = false;
                _isJumpCut = false;
                _isJumpFalling = false;

                ExtraJump();
                animationController?.PlayJump();
            }
        }
        #endregion

        #region DASH CHECKS
        if (CanDash() && LastPressedDashTime > 0)
        {
            //Freeze game for split second. Adds juiciness and a bit of forgiveness over directional input
            Sleep(Data.dashSleepTime);

            //Dash always goes to the facing direction
            _lastDashDir = IsFacingRight ? Vector2.right : Vector2.left;

            IsDashing = true;
            IsJumping = false;
            IsWallJumping = false;
            _isJumpCut = false;

            StartCoroutine(nameof(StartDash), _lastDashDir);
        }
        #endregion

        #region SLIDE CHECKS
        IsSliding = CanSlide();
        #endregion

        #region GRAVITY
        if (!_isDashAttacking)
        {
            if (ShouldApplyAirAttackFloat())
            {
                SetGravityScale(Data.gravityScale * Data.airAttackGravityMult);
                RB.velocity = new Vector2(
                    RB.velocity.x,
                    Mathf.Clamp(RB.velocity.y, -Data.airAttackMaxFallSpeed, Data.airAttackMaxUpwardSpeed));
            }
            else if (ShouldApplyAirAttackGraceFloat())
            {
                SetGravityScale(Data.gravityScale * Data.airAttackGraceGravityMult);
                RB.velocity = new Vector2(RB.velocity.x, Mathf.Max(RB.velocity.y, -Data.airAttackGraceMaxFallSpeed));
            }
            //Higher gravity if we've released the jump input or are falling
            else if (IsSliding)
            {
                SetGravityScale(0);
            }
            else if (RB.velocity.y < 0 && _moveInput.y < 0)
            {
                //Much higher gravity if holding down
                SetGravityScale(Data.gravityScale * Data.fastFallGravityMult);
                //Caps maximum fall speed, so when falling over large distances we don't accelerate to insanely high speeds
                RB.velocity = new Vector2(RB.velocity.x, Mathf.Max(RB.velocity.y, -Data.maxFastFallSpeed));
            }
            else if (_isJumpCut)
            {
                //Higher gravity if jump button released
                SetGravityScale(Data.gravityScale * Data.jumpCutGravityMult);
                RB.velocity = new Vector2(RB.velocity.x, Mathf.Max(RB.velocity.y, -Data.maxFallSpeed));
            }
            else if ((IsJumping || _isJumpFalling) && Mathf.Abs(RB.velocity.y) < Data.jumpHangTimeThreshold)
            {
                SetGravityScale(Data.gravityScale * Data.jumpHangGravityMult);
            }
            else if (RB.velocity.y < 0)
            {
                //Higher gravity if falling
                SetGravityScale(Data.gravityScale * Data.fallGravityMult);
                //Caps maximum fall speed, so when falling over large distances we don't accelerate to insanely high speeds
                RB.velocity = new Vector2(RB.velocity.x, Mathf.Max(RB.velocity.y, -Data.maxFallSpeed));
            }
            else
            {
                //Default gravity if standing on a platform or moving upwards
                SetGravityScale(Data.gravityScale);
            }
        }
        else
        {
            //No gravity when dashing (returns to normal once initial dashAttack phase over)
            SetGravityScale(0);
        }
        #endregion
    }

    private void FixedUpdate()
    {
        if (health != null && health.IsDead)
        {
            RB.velocity = Vector2.zero;
            return;
        }

        if (IsClimbing)
        {
            RB.velocity = Vector2.zero;
            return;
        }

        if (forcedHorizontalVelocityTimer > 0f)
        {
            RB.velocity = new Vector2(forcedHorizontalVelocity, RB.velocity.y);
        }

        //Handle Run
        if (!IsDashing && forcedHorizontalVelocityTimer <= 0f)
        {
            if (IsWallJumping)
                Run(Data.wallJumpRunLerp);
            else
                Run(1);
        }
        else if (_isDashAttacking)
        {
            Run(Data.dashEndRunLerp);
        }

        //Handle Slide
        if (IsSliding)
            Slide();
    }

    #region INPUT CALLBACKS
    //Methods which whandle input detected in Update()
    public void OnJumpInput()
    {
        LastPressedJumpTime = Data.jumpInputBufferTime;
        _jumpPressedThisFrame = true;
    }

    public void OnJumpUpInput()
    {
        if (CanJumpCut() || CanWallJumpCut())
            _isJumpCut = true;
    }

    public void OnDashInput()
    {
        LastPressedDashTime = Data.dashInputBufferTime;
    }
    #endregion

    #region GENERAL METHODS
    public void SetGravityScale(float scale)
    {
        RB.gravityScale = scale;
    }

    private void Sleep(float duration)
    {
        //Method used so we don't need to call StartCoroutine everywhere
        //nameof() notation means we don't need to input a string directly.
        //Removes chance of spelling mistakes and will improve error messages if any
        StartCoroutine(nameof(PerformSleep), duration);
    }

    private IEnumerator PerformSleep(float duration)
    {
        Time.timeScale = 0;
        yield return new WaitForSecondsRealtime(duration); //Must be Realtime since timeScale with be 0 
        Time.timeScale = 1;
    }
    #endregion

    //MOVEMENT METHODS
    #region RUN METHODS
    private void Run(float lerpAmount)
    {
        //Calculate the direction we want to move in and our desired velocity
        float targetSpeed = _moveInput.x * Data.runMaxSpeed * externalRunMultiplier;
        //We can reduce are control using Lerp() this smooths changes to are direction and speed
        targetSpeed = Mathf.Lerp(RB.velocity.x, targetSpeed, lerpAmount);

        #region Calculate AccelRate
        float accelRate;

        //Gets an acceleration value based on if we are accelerating (includes turning) 
        //or trying to decelerate (stop). As well as applying a multiplier if we're air borne.
        if (LastOnGroundTime > 0)
            accelRate = (Mathf.Abs(targetSpeed) > 0.01f) ? Data.runAccelAmount : Data.runDeccelAmount;
        else
            accelRate = (Mathf.Abs(targetSpeed) > 0.01f) ? Data.runAccelAmount * Data.accelInAir : Data.runDeccelAmount * Data.deccelInAir;
        #endregion

        #region Add Bonus Jump Apex Acceleration
        //Increase are acceleration and maxSpeed when at the apex of their jump, makes the jump feel a bit more bouncy, responsive and natural
        if ((IsJumping || _isJumpFalling) && Mathf.Abs(RB.velocity.y) < Data.jumpHangTimeThreshold)
        {
            accelRate *= Data.jumpHangAccelerationMult;
            targetSpeed *= Data.jumpHangMaxSpeedMult;
        }
        #endregion

        #region Conserve Momentum
        //We won't slow the player down if they are moving in their desired direction but at a greater speed than their maxSpeed
        if (Data.doConserveMomentum && Mathf.Abs(RB.velocity.x) > Mathf.Abs(targetSpeed) && Mathf.Sign(RB.velocity.x) == Mathf.Sign(targetSpeed) && Mathf.Abs(targetSpeed) > 0.01f && LastOnGroundTime < 0)
        {
            //Prevent any deceleration from happening, or in other words conserve are current momentum
            //You could experiment with allowing for the player to slightly increae their speed whilst in this "state"
            accelRate = 0;
        }
        #endregion

        //Calculate difference between current velocity and desired velocity
        float speedDif = targetSpeed - RB.velocity.x;
        //Calculate force along x-axis to apply to thr player

        float movement = speedDif * accelRate;

        //Convert this to a vector and apply to rigidbody
        RB.AddForce(movement * Vector2.right, ForceMode2D.Force);

        /*
		 * For those interested here is what AddForce() will do
		 * RB.velocity = new Vector2(RB.velocity.x + (Time.fixedDeltaTime  * speedDif * accelRate) / RB.mass, RB.velocity.y);
		 * Time.fixedDeltaTime is by default in Unity 0.02 seconds equal to 50 FixedUpdate() calls per second
		*/
    }

    private void Turn()
    {
        IsFacingRight = !IsFacingRight;
        animationController?.SetFacing(IsFacingRight);
    }
    #endregion

    #region JUMP METHODS
    private void Jump()
    {
        //Ensures we can't call Jump multiple times from one press
        LastPressedJumpTime = 0;
        LastOnGroundTime = 0;

        #region Perform Jump
        //We increase the force applied if we are falling
        //This means we'll always feel like we jump the same amount 
        //(setting the player's Y velocity to 0 beforehand will likely work the same, but I find this more elegant :D)
        float force = Data.jumpForce;
        if (RB.velocity.y < 0)
            force -= RB.velocity.y;

        RB.AddForce(Vector2.up * force, ForceMode2D.Impulse);
        #endregion
    }

    private void ExtraJump()
    {
        //Ensures we can't call extra jump multiple times from one press
        LastPressedJumpTime = 0;
        _extraJumpsLeft--;

        #region Perform Extra Jump
        //Double jump daha stabil olsun diye mevcut düşüş/yükseliş hızını sıfırlıyoruz
        RB.velocity = new Vector2(RB.velocity.x, 0f);

        float force = Data.jumpForce * Data.extraJumpForceMultiplier;
        RB.AddForce(Vector2.up * force, ForceMode2D.Impulse);
        #endregion
    }

    private void WallJump(int dir)
    {
        //Ensures we can't call Wall Jump multiple times from one press
        LastPressedJumpTime = 0;
        LastOnGroundTime = 0;
        LastOnWallRightTime = 0;
        LastOnWallLeftTime = 0;
        LastOnWallTime = 0;

        //Wall jump yaptıktan sonra extra jump hakkını kapat
        _extraJumpsLeft = 0;

        #region Perform Wall Jump
        Vector2 force = new Vector2(Data.wallJumpForce.x, Data.wallJumpForce.y);
        force.x *= dir; //apply force in opposite direction of wall

        // Reset carry momentum before the impulse so repeated wall jumps cannot stack force.
        float carriedVerticalVelocity = Mathf.Min(RB.velocity.y, Data.wallJumpMaxUpwardCarrySpeed);
        RB.velocity = new Vector2(0f, carriedVerticalVelocity);

        if (RB.velocity.y < 0) //checks whether player is falling, if so we subtract the velocity.y (counteracting force of gravity). This ensures the player always reaches our desired jump force or greater
            force.y -= RB.velocity.y;

        //Unlike in the run we want to use the Impulse mode.
        //The default mode will apply are force instantly ignoring masss
        RB.AddForce(force, ForceMode2D.Impulse);
        ClampWallJumpVelocity();
        #endregion
    }

    private void ClampWallJumpVelocity()
    {
        float horizontalLimit = Data.wallJumpMaxHorizontalSpeed > 0f
            ? Data.wallJumpMaxHorizontalSpeed
            : Mathf.Abs(Data.wallJumpForce.x);
        float verticalLimit = Data.wallJumpMaxVerticalSpeed > 0f
            ? Data.wallJumpMaxVerticalSpeed
            : Data.wallJumpForce.y;

        RB.velocity = new Vector2(
            Mathf.Clamp(RB.velocity.x, -horizontalLimit, horizontalLimit),
            Mathf.Min(RB.velocity.y, verticalLimit));
    }
    #endregion

    #region DASH METHODS
    //Dash Coroutine
    private IEnumerator StartDash(Vector2 dir)
    {
        //Overall this method of dashing aims to mimic Celeste, if you're looking for
        // a more physics-based approach try a method similar to that used in the jump

        LastOnGroundTime = 0;
        LastPressedDashTime = 0;

        float startTime = Time.time;

        _dashesLeft--;
        _isDashAttacking = true;
        health?.SetEnemyCollisionIgnored(true);

        SetGravityScale(0);

        //We keep the player's velocity at the dash speed during the "attack" phase (in celeste the first 0.15s)
        while (Time.time - startTime <= Data.dashAttackTime)
        {
            RB.velocity = dir.normalized * Data.dashSpeed;
            //Pauses the loop until the next frame, creating something of a Update loop. 
            //This is a cleaner implementation opposed to multiple timers and this coroutine approach is actually what is used in Celeste :D
            yield return null;
        }

        startTime = Time.time;

        _isDashAttacking = false;

        //Begins the "end" of our dash where we return some control to the player but still limit run acceleration (see Update() and Run())
        SetGravityScale(Data.gravityScale);
        RB.velocity = Data.dashEndSpeed * dir.normalized;

        while (Time.time - startTime <= Data.dashEndTime)
        {
            yield return null;
        }

        //Dash over
        health?.SetEnemyCollisionIgnored(false);
        IsDashing = false;
    }

    //Short period before the player is able to dash again
    private IEnumerator RefillDash(int amount)
    {
        //SHoet cooldown, so we can't constantly dash along the ground, again this is the implementation in Celeste, feel free to change it up
        _dashRefilling = true;
        yield return new WaitForSeconds(Data.dashRefillTime);
        _dashRefilling = false;
        _dashesLeft = Mathf.Min(Data.dashAmount, _dashesLeft + 1);
    }
    #endregion

    #region OTHER MOVEMENT METHODS
    private void Slide()
    {
        float timeSinceWallTouch = Time.time - _wallClingStartTime;

        //Duvara ilk temas edildiğinde kısa süre yapışık kal
        if (timeSinceWallTouch < Data.wallClingTime)
        {
            RB.velocity = new Vector2(RB.velocity.x, 0f);
            return;
        }

        //We remove the remaining upwards Impulse to prevent upwards sliding
        if (RB.velocity.y > 0)
        {
            RB.velocity = new Vector2(RB.velocity.x, 0f);
        }

        //Works the same as the Run but only in the y-axis
        //THis seems to work fine, buit maybe you'll find a better way to implement a slide into this system
        float speedDif = Data.slideSpeed - RB.velocity.y;
        float movement = speedDif * Data.slideAccel;
        //So, we clamp the movement here to prevent any over corrections (these aren't noticeable in the Run)
        //The force applied can't be greater than the (negative) speedDifference * by how many times a second FixedUpdate() is called. For more info research how force are applied to rigidbodies.
        movement = Mathf.Clamp(movement, -Mathf.Abs(speedDif) * (1 / Time.fixedDeltaTime), Mathf.Abs(speedDif) * (1 / Time.fixedDeltaTime));

        RB.AddForce(movement * Vector2.up);
    }

    #endregion


    #region CHECK METHODS
    public void CheckDirectionToFace(bool isMovingRight)
    {
        if (isMovingRight != IsFacingRight)
            Turn();
    }

    private bool CanJump()
    {
        return LastOnGroundTime > 0 && !IsJumping;
    }

    private bool CanExtraJump()
    {
        return _extraJumpsLeft > 0 && LastOnGroundTime <= 0 && LastOnWallTime <= 0 && !IsWallJumping;
    }

    private bool CanWallJump()
    {
        if (!_jumpPressedThisFrame || LastOnGroundTime > 0)
            return false;

        if (Time.time - _lastWallJumpTime < Data.wallJumpCooldown)
            return false;

        if (!GetWallJumpDirections(out _, out int wallDir))
            return false;

        if (wallDir == _lastWallJumpWallDir && Time.time < _sameWallJumpLockedUntil)
            return false;

        if (wallDir == _lastWallJumpWallDir && !_releasedLastWallJumpWall)
            return false;

        return !IsWallJumping || wallDir != _lastWallJumpWallDir;
    }

    private bool CanJumpCut()
    {
        return IsJumping && RB.velocity.y > 0;
    }

    private bool CanWallJumpCut()
    {
        return IsWallJumping && RB.velocity.y > 0;
    }

    private bool CanDash()
    {
        if (!IsDashing && _dashesLeft < Data.dashAmount && CanRefillDash() && !_dashRefilling)
        {
            StartCoroutine(nameof(RefillDash), 1);
        }

        return _dashesLeft > 0;
    }

    private bool CanRefillDash()
    {
        return LastOnGroundTime > 0 || CanRefillDashFromWall();
    }

    private bool CanRefillDashFromWall()
    {
        return LastOnWallTime > 0
            && LastOnGroundTime <= 0
            && !IsWallJumping
            && IsPressingIntoWall();
    }

    public bool CanSlide()
    {
        return LastOnWallTime > 0
            && IsPressingIntoWall()
            && !IsJumping
            && !IsWallJumping
            && !IsDashing
            && LastOnGroundTime <= 0;
    }

    private bool IsPressingIntoWall()
    {
        int wallDirection = GetCurrentWallDirection();

        if (wallDirection == 0)
            return false;

        return Mathf.Sign(_moveInput.x) == wallDirection && Mathf.Abs(_moveInput.x) > 0.1f;
    }

    private int GetCurrentWallDirection()
    {
        if (_isTouchingWallRight || LastOnWallRightTime > 0)
            return 1;

        if (_isTouchingWallLeft || LastOnWallLeftTime > 0)
            return -1;

        return 0;
    }

    private bool GetWallJumpDirections(out int jumpDir, out int wallDir)
    {
        wallDir = GetWallJumpWallDirection();
        jumpDir = wallDir != 0 ? -wallDir : 0;
        return wallDir != 0;
    }

    private int GetWallJumpWallDirection()
    {
        if (_isTouchingWallRight)
            return 1;

        if (_isTouchingWallLeft)
            return -1;

        if (LastOnWallRightTime > LastOnWallLeftTime && LastOnWallRightTime > 0)
            return 1;

        if (LastOnWallLeftTime > 0)
            return -1;

        return 0;
    }

    private void UpdateLastWallJumpWallRelease()
    {
        if (_lastWallJumpWallDir == 0 || _releasedLastWallJumpWall)
            return;

        if (!IsTouchingWallDirection(_lastWallJumpWallDir))
            _releasedLastWallJumpWall = true;
    }

    private bool IsTouchingWallDirection(int wallDir)
    {
        if (wallDir > 0)
            return _isTouchingWallRight;

        if (wallDir < 0)
            return _isTouchingWallLeft;

        return false;
    }

    private bool HasWallContact(Vector2 checkPosition)
    {
        Collider2D[] hits = Physics2D.OverlapBoxAll(checkPosition, _wallCheckSize, 0, _wallLayer);

        for (int i = 0; i < hits.Length; i++)
        {
            Collider2D hit = hits[i];
            if (hit != null && !ShouldIgnoreWallContact(hit))
                return true;
        }

        return false;
    }

    private bool ShouldIgnoreWallContact(Collider2D hit)
    {
        if (hit.attachedRigidbody == RB || hit.transform.IsChildOf(transform))
            return true;

        return IsDropThroughPlatform(hit);
    }

    private bool TryDropThroughPlatform()
    {
        if (!IsPressingDown() || _groundCheckPoint == null)
            return false;

        Collider2D platform = FindDropThroughPlatformBelow();
        if (platform == null)
            return false;

        if (dropThroughCoroutine != null)
            StopCoroutine(dropThroughCoroutine);

        dropThroughCoroutine = StartCoroutine(DisablePlatformCollision(platform));
        LastPressedJumpTime = 0f;
        LastOnGroundTime = 0f;
        RB.velocity = new Vector2(RB.velocity.x, Mathf.Min(RB.velocity.y, -dropThroughPushSpeed));
        return true;
    }

    private Collider2D FindDropThroughPlatformBelow()
    {
        Collider2D[] hits = Physics2D.OverlapBoxAll(_groundCheckPoint.position, _groundCheckSize, 0, _groundLayer);

        for (int i = 0; i < hits.Length; i++)
        {
            Collider2D hit = hits[i];
            if (hit != null && IsDropThroughPlatform(hit))
                return hit;
        }

        return null;
    }

    private bool IsDropThroughPlatform(Collider2D hit)
    {
        if (hit == null)
            return false;

        if (!string.IsNullOrEmpty(oneWayPlatformTag) && hit.gameObject.tag == oneWayPlatformTag)
            return true;

        return hit.GetComponent<PlatformEffector2D>() != null
            || hit.GetComponentInParent<PlatformEffector2D>() != null;
    }

    private bool IsPressingDown()
    {
        return _moveInput.y < -0.5f || Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow);
    }

    private IEnumerator DisablePlatformCollision(Collider2D platform)
    {
        if (playerColliders == null || playerColliders.Length == 0)
            playerColliders = GetComponentsInChildren<Collider2D>();

        SetPlatformCollisionIgnored(platform, true);
        yield return new WaitForSeconds(dropThroughPlatformDuration);
        SetPlatformCollisionIgnored(platform, false);
        dropThroughCoroutine = null;
    }

    private void SetPlatformCollisionIgnored(Collider2D platform, bool ignored)
    {
        if (platform == null || playerColliders == null)
            return;

        for (int i = 0; i < playerColliders.Length; i++)
        {
            Collider2D playerCollider = playerColliders[i];
            if (playerCollider != null && playerCollider.enabled && !playerCollider.isTrigger)
                Physics2D.IgnoreCollision(playerCollider, platform, ignored);
        }
    }
    #endregion


    #region EDITOR METHODS
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(_groundCheckPoint.position, _groundCheckSize);
        Gizmos.color = Color.blue;
        Gizmos.DrawWireCube(_rightWallCheckPoint.position, _wallCheckSize);
        Gizmos.color = Color.blue;
        Gizmos.DrawWireCube(_leftWallCheckPoint.position, _wallCheckSize);
    }
    #endregion

    public bool canAttack()
    {
        return (health == null || !health.IsDead) && !IsClimbing && !IsDashing && !IsWallJumping && !IsSliding;
    }

    public void SetExternalRunMultiplier(float multiplier)
    {
        externalRunMultiplier = Mathf.Clamp01(multiplier);
    }

    public void ResetExternalRunMultiplier()
    {
        externalRunMultiplier = 1f;
    }

    public void ForceHorizontalVelocity(float velocity, float duration)
    {
        forcedHorizontalVelocity = velocity;
        forcedHorizontalVelocityTimer = Mathf.Max(0f, duration);
    }

    public void ApplyKnockbackFrom(Vector3 sourcePosition, float horizontalSpeed, float duration, float upwardVelocity)
    {
        if (health != null && health.IsDead)
            return;

        if (ledgeClimb != null && ledgeClimb.IsClimbing)
            ledgeClimb.CancelClimb();

        if (IsDashing)
        {
            StopCoroutine(nameof(StartDash));
            health?.SetEnemyCollisionIgnored(false);
            IsDashing = false;
            _isDashAttacking = false;
            SetGravityScale(Data.gravityScale);
        }

        float direction = transform.position.x >= sourcePosition.x ? 1f : -1f;
        ForceHorizontalVelocity(direction * Mathf.Abs(horizontalSpeed), duration);

        if (RB != null)
            RB.velocity = new Vector2(RB.velocity.x, Mathf.Max(RB.velocity.y, upwardVelocity));
    }

    public void ClearForcedHorizontalVelocity()
    {
        forcedHorizontalVelocity = 0f;
        forcedHorizontalVelocityTimer = 0f;
    }

    public bool IsGrounded()
    {
        return LastOnGroundTime > 0f;
    }

    public bool IsAirborne()
    {
        return LastOnGroundTime <= 0f && !IsClimbing && !IsDashing && !IsSliding;
    }

    public bool IsClimbing => ledgeClimb != null && ledgeClimb.IsClimbing;

    public void ApplyAirAttackFloat()
    {
        if (Data == null || !Data.enableAirAttackFloat || !IsAirborne())
            return;

        airAttackRestartGraceTimer = 0f;
        airAttackFloatTimer = Mathf.Max(airAttackFloatTimer, Data.airAttackFloatDuration);

        float verticalVelocity = Mathf.Min(RB.velocity.y, Data.airAttackMaxUpwardSpeed);
        verticalVelocity = Mathf.Max(verticalVelocity, -Data.airAttackStartFallSpeed);
        RB.velocity = new Vector2(RB.velocity.x, verticalVelocity);
    }

    public void ClearAirAttackFloat()
    {
        ClearAirAttackFloat(false);
    }

    public void ClearAirAttackFloat(bool allowRestartGrace)
    {
        airAttackFloatTimer = 0f;

        if (allowRestartGrace && Data != null && Data.enableAirAttackFloat && IsAirborne())
            airAttackRestartGraceTimer = Data.airAttackRestartGraceTime;
        else
            airAttackRestartGraceTimer = 0f;
    }

    public bool IsInAirAttackRestartGrace()
    {
        return airAttackRestartGraceTimer > 0f && IsAirborne();
    }

    private bool ShouldApplyAirAttackFloat()
    {
        return Data != null
            && Data.enableAirAttackFloat
            && airAttackFloatTimer > 0f
            && LastOnGroundTime <= 0f
            && !IsSliding
            && !IsDashing;
    }

    private bool ShouldApplyAirAttackGraceFloat()
    {
        return Data != null
            && Data.enableAirAttackFloat
            && airAttackRestartGraceTimer > 0f
            && LastOnGroundTime <= 0f
            && !IsSliding
            && !IsDashing;
    }

    public bool WasJumpPressedThisFrame()
    {
        return Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.C) || Input.GetKeyDown(KeyCode.J);
    }

    public bool WasDashPressedThisFrame()
    {
        return Input.GetKeyDown(KeyCode.X) || Input.GetKeyDown(KeyCode.LeftShift) || Input.GetKeyDown(KeyCode.K);
    }

    private void LateUpdate()
    {
        _jumpPressedThisFrame = false;
    }
}
