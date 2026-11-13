using Assets.Scripts.Utility;
using System;
using Assets.Scripts.Movement;
using Assets.Scripts.Player.Movement;
using Assets.Scripts.Inventory.Stats;
using Assets.Scripts.Inventory__Items__Pickups.AbilitiesPassive;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Menu.Shop;
using Assets.Scripts.MapGeneration;
using Assets.Scripts.Saves___Serialization.Progression.Stats;
using UnityEngine;
using UnityEngine.Splines;

public class PlayerMovement : MonoBehaviour
{
	public enum CrouchState
	{
		None = 0,
		Crouching = 1,
		Sliding = 2
	}

	public Transform playerCam;

	public Transform orientation;

	public Transform feet;

	public Transform head;

	public LayerMask whatIsGround;

	public LayerMask whatIsGroundOnly;

	private Rigidbody rb;

	internal bool HasRigidbody => rb != null;

	private float currentMoveSpeed;

	private float currentMaxSpeed;

	private float counterMovement = 0.25f;

	private float defaultCounterMovement = 0.25f;

	private float threshold = 0.01f;

	private int readyToCounterX;

	private int readyToCounterY;

	private float slowDownSpeed = 0.01f;

	private float pushMultiplier = 1f;

	private float pushResetSpeed = 1f;

	private float resetPushCounter;

	private float resetPushCounterValue = 25f;

	private float maxSlopeAngle = 45f;

	private float slideAngle = 30f;

	private float minBreakFallAngle = 12f;

	private float maxBreakFallAngle = 85f;

	private int surfaceDelay = 5;

	private int groundCancel;

	private int surfCancel;

	private int climbCancel;

	private Vector3 ladderNormal;

	private Transform ladder;

	private float ladderSpeed = 11f;

	private bool onLadderLastFrame;

	private float ladderRefreshTime = 0.1f;

	private Vector3 ladderWishDir;

	private Vector3 ladderWallVec;

	public bool onRamp;

	public bool onLadder;

	private bool pushed;

	public bool grounded;

	private bool isUnderwater;

	private bool onGround;

	private bool surfing;

	private bool cancellingGrounded;

	private bool cancellingSurf;

	private Vector3 playerScale;

	private Vector3 crouchScale;

	private float slideCounterMovement = 0.1f;

	public const float crouchRatio = 1f;

	private float playerHeight;

	private float slideThresholdSpeed = 8f;

	private bool readyToCrouch = true;

	private bool readyToSlide = true;

	private bool justLanded;

	private bool justUncrouched;

	private bool readyToJump = true;

	private int aerialJumps = 1;

	private float x;

	private float y;

	private float mouseDeltaX;

	private float mouseDeltaY;

	private bool jumping;

	private bool crouching;

	private Vector3 normalVector;

	private CapsuleCollider playerCollider;

	private float fallSpeed;

	private Vector3 lastVelocity;

	private int ladderRefreshCount;

	private int ladderRefreshCountMax = 5;

	private int resetJumpCounter = 12;

	private int jumpCounterResetTime = 12;

	private int crouchCooldownCounter;

	private int crouchCooldownCounterMax = 20;

	private int slideCooldownCounter;

	private int slideCooldownCounterMax = 25;

	private int justLandedCounter;

	private int justLandedCounterMax = 15;

	private int justUncrouchedCounter;

	private int justUncrouchedCounterMax = 5;

	private Vector3 headHeight;

	private Vector3 crouchHeadHeight;

	private Vector3 feetHeight;

	public static Action<PlayerMovement> A_Jumped;

	public static Action<PlayerMovement> A_Crouched;

	public static Action<PlayerMovement> A_MovementState;

	public static PlayerMovement Instance;

	public PlayerMovementValues movementValues;

	private ECharacter currentCharacter;

	private EMovementState lastMovementState;

	public InputState inputState;

	private bool frozen;

	public bool isDashing;

	private float leftGroundAtTime;

	private Rail rail;

	private float railSpeed;

	private float progress;

	private float railDirectionMultiplier;

	private float canJumpOffRailTime;

	private float canJumpOffRailAtTime;

	private Vector3 railOffsetPosition;

	private Vector3 railOffsetPositionStart;

	private float railOffsetPositionTimer;

	private float railLerpTime = 0.15f;

	public static Action<bool> A_ToggleGrind;

	public PlayerSfxs playerSfx;

	public static Action A_StartedWallClimb;

	private float minWallClimbSpeed = 10f;

	private float wallClimbCooldown = 0.4f;

	private float canWallClimbAtTime;

	private float wallrunAttachAngle = 60f;

	private bool isNoclipping;

	private FrictionModifier.EFrictionSurface surface;

	private GameObject groundedObject;

	private float unstuckForce = 2000f;

	private float stuckTimer;

	private float stuckTimerMax = 1f;

	private float lastFallSpeed;

	private float lastTouchedWallTime;

	public static Action<Vector3> CameraBob;

	public static Action<float> A_Landed;

	public static Action<Vector3, float> A_LandedSmoke;

	public static Action<float> Shake;

	private bool wallClimbing;

	public static Action<bool> A_Grounded;

	public static Action<PlayerMovement> A_SlideStart;

	public static Action<PlayerMovement> A_Wallrun;

	private float jumpAnimationCooldownSlide = 0.4f;

	private int usedJumps;

	private float jumpedTime;

	private float landedAtTime;

	private float landingJumpCooldownPegeMode = 0.15f;

	private int climbCancelTicks = 5;

	private float avgVelocity;

	private Vector3 standingFeetOffset;

	private Vector3 crouchingFeetOffset;

	private float lastTouchedTornadoTime;

	public CrouchState crouchState { get; private set; }

	public float playerRadius { get; private set; }

	public Vector3 wallNormal { get; private set; }

	public Vector3 lastGroundedPosition { get; private set; }

	public bool isTouchingTornado { get; private set; }

	private void UpdateTickRate(int tickRate)
	{
		float ratio = tickRate * 0.02f;
		jumpCounterResetTime = Mathf.RoundToInt(10f * ratio);
		surfaceDelay = Mathf.RoundToInt(5f * ratio);
		pushResetSpeed = ratio;
		crouchCooldownCounterMax = Mathf.RoundToInt(20f * ratio);
		slideCooldownCounterMax = Mathf.RoundToInt(25f * ratio);
		justLandedCounterMax = Mathf.RoundToInt(15f * ratio);
	}

	private void Awake()
	{
		Water.A_PlayerEnterWater += OnWaterEnter;
		Water.A_PlayerExitWater += OnWaterExit;
		Lava.A_PlayerEnterWater += OnLavaEnter;
		Lava.A_PlayerExitWater += OnLavaExit;
	}

	private void OnDestroy()
	{
		Water.A_PlayerEnterWater -= OnWaterEnter;
		Water.A_PlayerExitWater -= OnWaterExit;
		Lava.A_PlayerEnterWater -= OnLavaEnter;
		Lava.A_PlayerExitWater -= OnLavaExit;
	}

	private void OnLavaEnter()
	{
		isUnderwater = true;
		Vector3 velocity = rb.velocity;
		if (velocity.y < 0f)
		{
			velocity.y *= 0.9f;
			rb.velocity = velocity;
		}
	}

	private void OnLavaExit()
	{
		isUnderwater = false;
	}

	private void OnWaterEnter(Water water)
	{
		isUnderwater = true;
		Vector3 velocity = rb.velocity;
		if (velocity.y < 0f)
		{
			velocity.y *= 0.75f;
			rb.velocity = velocity;
		}
	}

	private void OnWaterExit(Water water)
	{
		isUnderwater = false;
	}

	private bool IsInitialized()
	{
		return Instance != null;
	}

	private void Initialize()
	{
		if (IsInitialized()) return;
		Instance = this;
		movementValues = new PlayerMovementValues();
		UpdateTickRate(Mathf.RoundToInt(1f / Time.fixedDeltaTime));
		rb = GetComponent<Rigidbody>();
		playerScale = transform.localScale;
		crouchScale = playerScale;
		playerCollider = GetComponent<CapsuleCollider>();
		playerHeight = playerCollider.height;
		playerRadius = playerCollider.radius;
		rb.sleepThreshold = 0f;
		inputState = default(InputState);
		headHeight = head.position - rb.position;
		crouchHeadHeight = headHeight;
		feetHeight = rb.position - feet.position;
		standingFeetOffset = rb.position - feet.position;
		crouchingFeetOffset = standingFeetOffset;
	}

	public void ResetState(ECharacter character, Vector3 dir)
	{
		Initialize();
		currentCharacter = character;
		surface = FrictionModifier.EFrictionSurface.Normal;
		isUnderwater = false;
		rb.velocity = Vector3.zero;
		rb.angularVelocity = Vector3.zero;
		crouchState = CrouchState.None;
		transform.localScale = playerScale;
		fallSpeed = 0f;
		resetJumpCounter = jumpCounterResetTime;
		readyToJump = true;
		pushMultiplier = 1f;
		resetPushCounter = resetPushCounterValue;
		slideCooldownCounter = slideCooldownCounterMax;
		readyToCrouch = true;
		readyToSlide = true;
		crouchCooldownCounter = crouchCooldownCounterMax;
		justLandedCounter = justLandedCounterMax;
		justUncrouchedCounter = justUncrouchedCounterMax;
		onLadder = false;
		pushed = false;
		onLadderLastFrame = false;
		climbCancel = climbCancelTicks;
		inputState = default(InputState);
		x = 0f;
		y = 0f;
		jumping = false;
		crouching = false;
		movementValues.CreateMovement(rb, character);
		orientation.rotation = Quaternion.LookRotation(dir);
	}

	public void SetBetterInput(InputState input)
	{
		inputState = input;
		x = input.moveHorizontal;
		y = input.moveVertical;
		jumping = input.jumping;
		crouching = input.crouching;
	}

	public void Freeze(bool b)
	{
		frozen = b;
		rb.isKinematic = b;
	}

	public void MovementTick()
	{
		if (MyTime.paused) return;
		if (frozen) { x = 0f; y = 0f; jumping = false; return; }
		orientation.transform.localRotation = Quaternion.Euler(0f, inputState.rotationHorizontal, 0f);
		if (isNoclipping) { NoclipMovement(); return; }
		UpdateCooldowns();
		if (crouching && crouchState == CrouchState.None) StartCrouch();
		else if (!crouching && crouchState != CrouchState.None && CanStopCrouching())
		{
			if (crouchState == CrouchState.Sliding) { readyToSlide = false; slideCooldownCounter = 0; }
			crouchState = CrouchState.None;
			justUncrouched = true;
			justUncrouchedCounter = 0;
		}
		currentMaxSpeed = movementValues.GetMaxSpeed();
		if (crouchState == CrouchState.Crouching) currentMaxSpeed *= 0.5f;
		fallSpeed = rb.velocity.y;
		lastVelocity = rb.velocity;
		currentMoveSpeed = movementValues.GetMoveSpeed(surface, grounded) * pushMultiplier;
		Vector3 localVelocity = orientation.InverseTransformVector(rb.velocity);
		if (isDashing) return;
		if (rail != null) { RailMovement(); return; }
		if (isUnderwater)
			rb.AddForce(Vector3.up * rb.mass * -Physics.gravity.y * (inputState.holdingJump ? 1.9125f : 0.85f));
		if (rb.drag > 0f) rb.drag = 0f;
		if (!grounded && !rb.isKinematic && !isUnderwater) rb.AddForce(Vector3.down * movementValues.extraGravity);
		CounterMovement(x, y, new Vector2(localVelocity.x, localVelocity.z));
		UpdateCrouchState();
		RampMovement(new Vector2(localVelocity.x, localVelocity.z));
		if (jumping) Jump();
		if (!onLadder && onLadderLastFrame) onLadder = true;
		if (TryLadderMovement()) return;
		if (crouchState == CrouchState.Sliding && grounded && readyToJump && rb.velocity.y < 0f)
			rb.AddForce(Vector3.down * 50f * rb.mass);
		float moveX = x, moveY = y;
		if ((x > 0f && localVelocity.x > currentMaxSpeed) || (x < 0f && localVelocity.x < -currentMaxSpeed)) moveX = 0f;
		if ((y > 0f && localVelocity.z > currentMaxSpeed) || (y < 0f && localVelocity.z < -currentMaxSpeed)) moveY = 0f;
		bool againstVertical = (y > 0f && localVelocity.z < -threshold) || (y < 0f && localVelocity.z > threshold);
		float forwardMultiplier = 1f, sideMultiplier = 1f;
		if (grounded && crouchState == CrouchState.Sliding)
		{
			forwardMultiplier = againstVertical ? 0.25f : 0f;
			sideMultiplier = 0.15f;
		}
		else if (!grounded)
		{
			forwardMultiplier = againstVertical ? Mathf.Max(Mathf.Abs(localVelocity.z) * 0.025f, 0.5f) : 0.45f;
			sideMultiplier = 0.45f;
		}
		rb.AddForce(orientation.forward * moveY * currentMoveSpeed * 0.02f * forwardMultiplier);
		rb.AddForce(orientation.right * moveX * currentMoveSpeed * 0.02f * sideMultiplier);
		if (!grounded && GetAirDeceleration() > 0f)
		{
			if (moveX != 0f) rb.AddForce(orientation.forward * localVelocity.z * currentMoveSpeed * -0.02f * GetAirDeceleration());
			if (moveY != 0f) rb.AddForce(orientation.right * localVelocity.x * currentMoveSpeed * -0.02f * GetAirDeceleration());
		}
		EMovementState state = GetMovementState();
		if (state != lastMovementState) { lastMovementState = state; A_MovementState?.Invoke(this); }
		avgVelocity = (avgVelocity + rb.velocity.magnitude) * 0.5f;
		VerifyPosition();
		CheckStuck();
		CheckFallDamageBug();
	}

	private float GetAirDeceleration()
	{
		return GameManager.Instance != null && GameManager.Instance.isCrypt ? 0f : movementValues.airDeceleration;
	}

	public void StartRail(Rail rail)
	{
		this.rail = rail;
		canJumpOffRailTime = MyTime.time + Time.fixedDeltaTime;
		railSpeed = Mathf.Max(rb.velocity.magnitude, currentMaxSpeed * 1.5f);
		progress = FindClosestPointOnSpline(rail.splineContainer, transform.position);
		Vector3 tangent = rail.splineContainer.EvaluateTangent(progress);
		railDirectionMultiplier = Vector3.Dot(tangent, rb.velocity) < 0f ? -1f : 1f;
		railOffsetPosition = rb.position - GetRailPosition();
		railOffsetPositionStart = railOffsetPosition;
		railOffsetPositionTimer = 0f;
		rb.isKinematic = true;
		playerSfx.StartGrind();
		A_ToggleGrind?.Invoke(true);
	}

	public void StopRail()
	{
		if (rail == null) return;
		rb.isKinematic = false;
		rail.Cooldown(playerCollider);
		StopGrounded();
		float sample = progress >= 1f ? 0.99f : progress <= 0f ? 0.01f : progress;
		Vector3 tangent = rail.splineContainer.EvaluateTangent(sample);
		rb.velocity = (tangent * railDirectionMultiplier).normalized * railSpeed;
		rb.velocity += Vector3.up * 5f;
		rail = null;
		playerSfx.StopGrind();
		A_ToggleGrind?.Invoke(false);
	}

	private void RailMovement()
	{
		progress = Mathf.Clamp01(progress + railDirectionMultiplier * (railSpeed / rail.splineContainer.CalculateLength()) * Time.fixedDeltaTime);
		if ((progress >= 1f && railDirectionMultiplier > 0f) || (progress <= 0f && railDirectionMultiplier < 0f)) { StopRail(); return; }
		if (railOffsetPositionTimer < 1f)
		{
			railOffsetPositionTimer = Mathf.Clamp01(railOffsetPositionTimer + MyTime.fixedDeltaTime / railLerpTime);
			railOffsetPosition = Vector3.Lerp(railOffsetPositionStart, Vector3.zero, railOffsetPositionTimer);
		}
		rb.MovePosition(GetRailPosition() + railOffsetPosition);
		if (MyTime.time > canJumpOffRailTime && jumping) Jump();
	}

	private Vector3 GetRailPosition()
	{
		return (Vector3)rail.splineContainer.EvaluatePosition(progress) + Vector3.up * feetHeight.y;
	}

	public bool CanStartGrind()
	{
		return rail == null;
	}

	public bool IsGrinding()
	{
		return rail != null;
	}

	public Vector3 GetGrindNormal()
	{
		return rail.splineContainer.EvaluateUpVector(progress);
	}

	public Vector3 GetGrindDirection()
	{
		return rail.splineContainer.EvaluateTangent(progress) * railDirectionMultiplier;
	}

	private float FindClosestPointOnSpline(SplineContainer spline, Vector3 position)
	{
		float closest = 0f, distance = float.MaxValue;
		for (int i = 0; i <= 100; i++)
		{
			float candidate = i / 100f;
			float candidateDistance = Vector3.Distance(position, spline.EvaluatePosition(candidate));
			if (candidateDistance < distance) { distance = candidateDistance; closest = candidate; }
		}
		return closest;
	}

	private void WallClimbing()
	{
		if (climbCancel >= climbCancelTicks) A_StartedWallClimb?.Invoke();
		climbCancel = 0;
		float speed = GetWallClimbSpeed();
		if (rb.velocity.y < speed)
		{
			Vector3 velocity = rb.velocity;
			velocity.y = speed;
			rb.velocity = velocity - wallNormal * 4f;
		}
		if (!inputState.holdingWallrun) StopWallClimbing();
	}

	private bool CanWallClimb(Vector3 normal, Collision other)
	{
		if (canWallClimbAtTime > MyTime.time || other.gameObject.CompareTag("Ignore")) return false;
		wallNormal = normal;
		return MyPlayer.Instance != null && MyPlayer.Instance.inventory != null &&
			MyPlayer.Instance.inventory.HasPassive(EPassive.WallClimb) && CheckIfPlayerWantsToWallClimb(normal);
	}

	private void StopWallClimbing()
	{
		canWallClimbAtTime = MyTime.time + wallClimbCooldown;
	}

	public bool IsWallClimbing()
	{
		return climbCancel < climbCancelTicks;
	}

	private bool CheckIfPlayerWantsToWallClimb(Vector3 normal)
	{
		return Mathf.Abs(RelativeToLook(normal)) > wallrunAttachAngle && inputState.holdingWallrun;
	}

	private float RelativeToLook(Vector3 normal)
	{
		Vector3 input = InputVectorRelativeToLook();
		Vector3 horizontalVelocity = new Vector3(rb.velocity.x, 0f, rb.velocity.z);
		if (input.magnitude > 0.1f && horizontalVelocity.magnitude > 3f)
			input += horizontalVelocity.normalized * 0.2f;
		return Vector3.SignedAngle(input, normal, Vector3.up);
	}

	private Vector3 InputVectorRelativeToLook()
	{
		return orientation.forward * y + orientation.right * x;
	}

	private float GetWallClimbSpeed()
	{
		return Mathf.Max(minWallClimbSpeed, currentMaxSpeed * 1.5f);
	}

	private void VerifyPosition()
	{
		if (grounded)
		{
			if (groundedObject != null && !groundedObject.CompareTag("Ignore"))
				lastGroundedPosition = rb.position;
			return;
		}

		if (rb.position.y < MapInfo.mapBoundsLower.y - 10f)
		{
			rb.MovePosition(lastGroundedPosition);
			rb.velocity = Vector3.zero;
			CheckLanding(lastGroundedPosition - Vector3.up * playerHeight, Vector3.up);
		}
	}

	public void TeleportPlayerBackToBounds(Vector3 pos)
	{
		rb.MovePosition(pos + Vector3.up * 4f);
		rb.velocity = Vector3.zero;
		CheckLanding(pos - Vector3.up * playerHeight, Vector3.up);
		usedJumps = 0;
		aerialJumps = PlayerStats.HasStats() ? Mathf.FloorToInt(PlayerStats.GetStat(EStat.ExtraJumps)) : 0;
	}

	public void TeleportPlayer(Vector3 pos)
	{
		rb.MovePosition(pos + Vector3.up * (playerHeight * 0.5f));
		rb.velocity = Vector3.zero;
	}

	private void Update()
	{
	}

	private void NoclipMovement()
	{
	}

	public Vector2 GetLocalVelocity()
	{
		Vector3 velocity = orientation.InverseTransformVector(rb.velocity);
		return new Vector2(velocity.x, velocity.z);
	}

	public static Vector3 AirAccelerate(Vector3 velocity, Vector3 wishdir, float wishspeed, float accel, float airCap, float deltaTime, float gainMultiplier = 1f)
	{
		float additionalSpeed = (Mathf.Min(airCap, wishspeed) - Vector3.Dot(velocity, wishdir)) * gainMultiplier;
		if (additionalSpeed <= 0f) return Vector3.zero;
		float acceleration = Mathf.Min(additionalSpeed, wishspeed * accel * deltaTime);
		return wishdir * acceleration;
	}

	public Vector3 GetWishDir()
	{
		return (orientation.forward * y + orientation.right * x).normalized;
	}

	public Vector2 VelRelativeToLook()
	{
		Vector3 localVelocity = orientation.InverseTransformVector(rb.velocity);
		return new Vector2(localVelocity.x, localVelocity.z);
	}

	private void OnCollisionEnter(Collision other)
	{
		ContactPoint contact = other.contacts[0];
		if ((whatIsGround.value & (1 << other.gameObject.layer)) == 0 || !IsFloor(contact.normal)) return;
		FrictionModifier friction = other.gameObject.GetComponent<FrictionModifier>();
		surface = friction != null ? friction.frictionSurface : FrictionModifier.EFrictionSurface.Normal;
		if (!grounded)
		{
			onRamp = Vector3.Angle(Vector3.up, contact.normal) > 1f;
			groundedObject = other.gameObject;
			StartGrounded();
			normalVector = contact.normal;
			if (crouchState != CrouchState.None && onRamp && GetSpeedHorizontal() > slideThresholdSpeed && crouchState != CrouchState.Sliding)
				StartSlide();
			justLanded = true;
			justLandedCounter = 0;
			landedAtTime = MyTime.time;
		}
		CheckLanding(contact.point, contact.normal);
		fallSpeed = 0f;
	}

	private void CheckLanding(Vector3 point, Vector3 normal)
	{
		if (leftGroundAtTime + 0.55f > MyTime.time) return;
		float impact = Vector3.Dot(lastVelocity, normal);
		if (impact < -3f)
		{
			A_Landed?.Invoke(Mathf.Abs(fallSpeed));
			CameraBob?.Invoke(Vector3.up * fallSpeed);
		}
		if (impact < -12f) A_LandedSmoke?.Invoke(point, fallSpeed);
	}

	private void CheckStuck()
	{
		if (rb.velocity.magnitude > 0.5f || IsTouchingGround() || rb.isKinematic || !rb.useGravity)
		{
			stuckTimer = 0f;
			return;
		}
		stuckTimer += MyTime.fixedDeltaTime;
		if (stuckTimer > stuckTimerMax)
		{
			stuckTimer = 0f;
			rb.AddForce(Vector3.up * unstuckForce * rb.mass);
		}
	}

	private void CheckFallDamageBug()
	{
		if (lastFallSpeed < -25f && fallSpeed > -5f &&
			!(crouchState != CrouchState.None && onRamp) &&
			MyTime.time - lastTouchedWallTime < 0.1f)
		{
			var health = MyPlayer.Instance != null && MyPlayer.Instance.inventory != null
				? MyPlayer.Instance.inventory.playerHealth : null;
			if (health != null && MyTime.time - health.fallDamageTakenAtTime > 0.15f)
				MyStats.AddValue(EMyStat.wallhugs, 1f);
		}
		lastFallSpeed = fallSpeed;
	}

	public bool CanTakeFallDamage(Vector3 normal)
	{
		return !IsBreakingFall(normal) && !surfing;
	}

	private void OnCollisionStay(Collision other)
	{
		if ((whatIsGround.value & (1 << other.gameObject.layer)) == 0) return;
		foreach (ContactPoint contact in other.contacts)
		{
			Vector3 contactNormal = contact.normal;
			Vector3 normal = new Vector3(contactNormal.x, Mathf.Abs(contactNormal.y), contactNormal.z);
			if (IsWall(normal)) lastTouchedWallTime = Assets.Scripts.Utility.MyTime.time;
			if (IsFloor(normal))
			{
				if (contact.point.y > transform.position.y) continue;
				onRamp = Vector3.Angle(normal, Vector3.up) > 1f;
				groundedObject = other.gameObject;
				StartGrounded();
				normalVector = normal;
				if (crouchState != CrouchState.None && GetSpeedHorizontal() > slideThresholdSpeed && crouchState != CrouchState.Sliding)
					StartSlide();
			}
			else if (CanWallClimb(normal, other) && (IsWall(normal) || IsSurf(normal)))
			{
				WallClimbing();
			}
			else if (IsSurf(normal))
			{
				if (crouchState != CrouchState.None && crouchState != CrouchState.Sliding && Mathf.Abs(fallSpeed) > 4f)
					StartSlide();
				surfing = true;
				cancellingSurf = false;
				normalVector = normal;
				surfCancel = 0;
			}
		}
	}

	private void StartGrounded()
	{
		grounded = true;
		groundCancel = 0;
		cancellingGrounded = false;
		ResetAerialJumps();
		A_Grounded?.Invoke(true);
	}

	private void UpdateCooldowns()
	{
		if (!readyToJump && ++resetJumpCounter >= jumpCounterResetTime) readyToJump = true;
		if (pushMultiplier < 1f) pushMultiplier = Mathf.Min(1f, pushMultiplier + pushResetSpeed * 0.02f);
		if (pushed && ++resetPushCounter >= resetPushCounterValue) pushed = false;
		if ((onLadder || onLadderLastFrame) && ++ladderRefreshCount >= ladderRefreshCountMax)
		{
			onLadder = false;
			onLadderLastFrame = false;
		}
		if (!readyToSlide && ++slideCooldownCounter >= slideCooldownCounterMax) readyToSlide = true;
		if (!readyToCrouch && ++crouchCooldownCounter >= crouchCooldownCounterMax) readyToCrouch = true;
		if (justLanded && ++justLandedCounter >= justLandedCounterMax) justLanded = false;
		if (justUncrouched && ++justUncrouchedCounter >= justUncrouchedCounterMax) justUncrouched = false;
		if (isTouchingTornado && Assets.Scripts.Utility.MyTime.time >= lastTouchedTornadoTime + 3f)
			isTouchingTornado = false;
		UpdateCollisionChecks();
	}

	private void CheckInput()
	{
	}

	private void StartCrouch()
	{
		if (UseLimitedMovement() || crouchState != CrouchState.None || !readyToCrouch) return;
		readyToCrouch = false;
		crouchState = CrouchState.Crouching;
		crouchCooldownCounter = 0;
		A_Crouched?.Invoke(this);
		if ((rb.velocity.magnitude > slideThresholdSpeed || IsSlideable(normalVector)) && (grounded || surfing)) StartSlide();
	}

	private void StopCrouch()
	{
		if (!CanStopCrouching()) return;
		if (crouchState == CrouchState.Sliding) StopSlide();
		crouchState = CrouchState.None;
		justUncrouched = true;
		justUncrouchedCounter = 0;
	}

	private bool CanStopCrouching()
	{
		Vector3 headOffset = crouchState == CrouchState.None ? headHeight : crouchHeadHeight;
		Ray ray = new Ray(rb.position + headOffset, Vector3.up);
		float radius = playerCollider.bounds.size.x * 0.5f * 0.9f;
		float distance = headHeight.y - crouchHeadHeight.y;
		foreach (RaycastHit hit in Physics.SphereCastAll(ray, radius, distance, whatIsGround))
		{
			if (hit.transform.root.gameObject.CompareTag("Water") || hit.transform.CompareTag("MainCamera")) continue;
			return false;
		}
		return true;
	}

	public bool IsCrouching()
	{
		return crouchState != CrouchState.None;
	}

	private bool UseLimitedMovement()
	{
		return SaveManager.Instance.config.cfGameSettings.pege_mode == 1;
	}

	private void StartSlide()
	{
		if (UseLimitedMovement() || !readyToSlide) return;
		Vector3 velocity = rb.velocity;
		if (!(justLanded && Mathf.Abs(Vector3.Angle(Vector3.up, normalVector)) < 2f))
		{
			Vector3 horizontalVelocity = new Vector3(velocity.x, 0f, velocity.z).normalized;
			Vector3 direction;
			if (Mathf.Abs(x) < 0.05f && Mathf.Abs(y) < 0.05f) direction = horizontalVelocity;
			else direction = ((orientation.right * x + orientation.forward * y).normalized + horizontalVelocity).normalized;
			float extraForce = Mathf.Clamp(Mathf.Abs(fallSpeed) - 10f, 0f, 2147483648f) * 1000f;
			if (velocity.y > 0f) extraForce *= 0.5f;
			rb.AddForce(direction * (movementValues.slideForce + extraForce));
		}
		crouchState = CrouchState.Sliding;
		readyToSlide = false;
		A_SlideStart?.Invoke(this);
	}

	private void StopSlide()
	{
		readyToSlide = false;
		slideCooldownCounter = 0;
	}

	public bool IsSliding()
	{
		return crouchState == CrouchState.Sliding;
	}

	public bool IsSlidingAnimation()
	{
		return crouchState != CrouchState.None && MyTime.time > jumpedTime + jumpAnimationCooldownSlide && resetJumpCounter > 3;
	}

	private void UpdateCrouchState()
	{
		if (crouchState != CrouchState.None && rb.velocity.magnitude < slideThresholdSpeed)
		{
			if (crouchState == CrouchState.Sliding) StopSlide();
			crouchState = CrouchState.Crouching;
		}
	}

	private void WaterMovement()
	{
	}

	private bool IsUnderwater()
	{
		return isUnderwater;
	}

	private void RampMovement(Vector2 mag)
	{
		if (!grounded || !onRamp || surfing || crouching || jumping ||
			resetJumpCounter < jumpCounterResetTime || Mathf.Abs(x) >= 0.05f ||
			Mathf.Abs(y) >= 0.05f || pushed || !HasFooting())
		{
			rb.useGravity = true;
			return;
		}

		rb.useGravity = false;
		Vector3 velocity = rb.velocity;
		if (velocity.y > 0f)
			rb.velocity = new Vector3(velocity.x, 0f, velocity.z);
		else if (mag.magnitude < 1f)
			rb.velocity = Vector3.zero;
	}

	private bool TryLadderMovement()
	{
		if (!onLadder || ladder == null) return false;
		if (IsTouchingGround() && feet.position.y > ladder.position.y && Mathf.Abs(rb.velocity.y) < 0.1f)
		{
			onLadderLastFrame = true;
			return false;
		}
		LadderMovementTick(x, y);
		onLadderLastFrame = false;
		return true;
	}

	public void RefreshLadder(Transform ladder)
	{
		onLadder = true;
		ladderNormal = ladder.forward;
		this.ladder = ladder;
		ladderWishDir = orientation.right * x + orientation.forward * y;
		if (ladder.position.y > feet.position.y && grounded && Vector3.Angle(ladderWishDir, ladderNormal) < 50f)
		{
			StopLadder();
			return;
		}
		if (resetJumpCounter <= 1)
		{
			StopLadder();
			return;
		}
		ladderRefreshCount = 0;
	}

	private void StopLadder()
	{
		onLadder = false;
		onLadderLastFrame = false;
	}

	private void LadderMovementTick(float x, float y)
	{
		ladderWishDir = orientation.right * x + orientation.forward * y;
		ladderWallVec = Quaternion.AngleAxis(90f, ladder.right) * ladderWishDir;
		rb.velocity = ladderWallVec * ladderSpeed * (crouchState == CrouchState.None ? 1f : 0.5f);
		if (grounded && Vector3.Angle(ladderWishDir, ladderNormal) < 50f) onLadder = false;
	}

	public void JumpPad(JumpPad pad)
	{
		pushed = true;
		resetPushCounter = 0f;
		rb.useGravity = true;
		Vector3 velocity = rb.velocity;
		if (velocity.y < 0f) velocity.y = 0f;
		rb.velocity = velocity + pad.GetForce();
	}

	public void RocketJump(Vector3 pushForce)
	{
		PushPlayer(pushForce);
	}

	public void PushPlayer(Vector3 pushForce)
	{
		rb.velocity += pushForce;
		pushed = true;
		resetPushCounter = 0f;
		rb.useGravity = true;
		pushMultiplier = 0f;
		Shake?.Invoke(1f);
	}

	public void PushPlayerButKeepMovement()
	{
		pushed = true;
		resetPushCounter = 0f;
		rb.useGravity = true;
	}

	public void BouncePlayer(Vector3 pushForce)
	{
		rb.velocity = pushForce;
		rb.useGravity = true;
		Shake?.Invoke(1f);
	}

	public bool RecentlyJumped()
	{
		return resetJumpCounter < 4;
	}

	public bool IsTouchingGround()
	{
		float distance = playerHeight * 0.5f + playerRadius;
		return Physics.SphereCast(transform.position, playerRadius, Vector3.down, out RaycastHit hit, distance + 0.1f, whatIsGroundOnly)
			|| Physics.Raycast(transform.position, Vector3.down, distance, whatIsGround);
	}

	public bool HasFooting()
	{
		return Physics.Raycast(feet.position, Vector3.down, 1f, whatIsGround);
	}

	private bool AreFeetTouchingFloor()
	{
		return Physics.Raycast(feet.position, Vector3.down, 0.1f, whatIsGround);
	}

	private void SetKinematic(bool b)
	{
		if (b) rb.velocity = Vector3.zero;
		rb.useGravity = !b;
	}

	private bool IsBreakingFall(Vector3 normal)
	{
		float angle = Vector3.Angle(Vector3.up, normal);
		return crouchState != CrouchState.None && angle > minBreakFallAngle && angle < maxBreakFallAngle;
	}

	private bool CanJump(bool ignoreAerialJumps = false)
	{
		if (UseLimitedMovement() && landedAtTime + landingJumpCooldownPegeMode > MyTime.time) return false;
		if (!readyToJump) return false;
		return (!ignoreAerialJumps && aerialJumps > 0) || grounded || rail != null ||
			climbCancel < climbCancelTicks || onLadder;
	}

	private void ResetAerialJumps()
	{
		usedJumps = 0;
		aerialJumps = PlayerStats.HasStats() ? Mathf.FloorToInt(PlayerStats.GetStat(EStat.ExtraJumps)) : 0;
	}

	public bool CanBhopJump()
	{
		return CanJump(true);
	}

	public void Jump()
	{
		if (!CanJump()) return;
		rb.useGravity = true;
		readyToJump = false;
		resetJumpCounter = 0;
		usedJumps++;
		if ((!grounded || surfing) && rail == null && climbCancel >= climbCancelTicks) aerialJumps--;
		float force = GetJumpForce();
		if (onLadder)
		{
			rb.AddForce(ladderNormal * force * 2f, ForceMode.Impulse);
			onLadder = false;
		}
		else if (onRamp && rail == null)
		{
			if (force > 0f)
			{
				rb.AddForce(Vector3.up * force * 1.5f, ForceMode.Impulse);
				float direction = Vector3.Dot(Vector3.Cross(Vector3.right, normalVector).normalized, rb.velocity.normalized);
				if (direction > 0f) rb.AddForce(Vector3.up * force * 0.25f, ForceMode.Impulse);
				else if (direction < 0f) rb.AddForce(normalVector * force * 0.5f, ForceMode.Impulse);
			}
		}
		else
		{
			if (rail != null) StopRail();
			if (rb.velocity.y < 0f) rb.velocity = new Vector3(rb.velocity.x, 0f, rb.velocity.z);
			rb.AddForce(Vector3.up * force * 1.5f, ForceMode.Impulse);
			rb.AddForce(normalVector * force * 0.5f, ForceMode.Impulse);
		}
		if (climbCancel < climbCancelTicks)
		{
			canWallClimbAtTime = MyTime.time + wallClimbCooldown;
			rb.AddForce(wallNormal * force * 1.5f, ForceMode.Impulse);
		}
		if (currentCharacter == ECharacter.Ninja || currentCharacter == ECharacter.Monke)
		{
			Vector3 localVelocity = orientation.InverseTransformVector(rb.velocity);
			bool opposing = (localVelocity.x < -threshold && x > 0f) || (localVelocity.x > threshold && x < 0f) ||
				(localVelocity.z < -threshold && y > 0f) || (localVelocity.z > threshold && y < 0f);
			if (opposing) rb.velocity = GetWishDir() * rb.velocity.magnitude;
			else if (currentCharacter == ECharacter.Monke)
			{
				float maxSpeed = movementValues.GetMaxSpeed();
				float boost = GetSpeedHorizontal() > maxSpeed ? maxSpeed - GetSpeedHorizontal() + 10f : 10f;
				Vector3 wish = GetWishDir();
				rb.velocity += new Vector3(wish.x, 0f, wish.z).normalized * Mathf.Max(5f, boost);
			}
		}
		jumpedTime = MyTime.time;
		A_Jumped?.Invoke(this);
	}

	private float GetJumpForce()
	{
		return PlayerStats.GetStat(EStat.JumpHeight) * rb.mass * (isUnderwater ? 1.4f : 1f);
	}

	private void CounterMovement(float x, float y, Vector2 mag)
	{
		if (Mathf.Abs(x) < 0.05f && Mathf.Abs(y) < 0.05f && rb.velocity.magnitude < 1f &&
			grounded && !jumping && !pushed && resetJumpCounter >= 3 && HasFooting())
		{
			rb.velocity = Vector3.zero;
			groundCancel = 0;
			if (!rb.isKinematic)
			{
				rb.velocity = Vector3.zero;
				rb.useGravity = false;
			}
			return;
		}
		if (rb.isKinematic) rb.useGravity = true;
		if (!grounded || !readyToJump || pushed) return;
		counterMovement = defaultCounterMovement * movementValues.GetCounterMovementMultiplier(surface);
		if (crouchState == CrouchState.Sliding)
		{
			rb.AddForce(-rb.velocity.normalized * currentMoveSpeed * 0.02f * slideCounterMovement);
			return;
		}
		if (justLandedCounter < 3) return;
		Vector3 velocity = rb.velocity;
		float horizontalSpeed = Mathf.Sqrt(velocity.x * velocity.x + velocity.z * velocity.z);
		if (horizontalSpeed > currentMaxSpeed)
		{
			slowDownSpeed = pushMultiplier < 1f ? 0.01f : 0.1f;
			float force = Mathf.Clamp((horizontalSpeed - currentMaxSpeed) / Time.fixedDeltaTime * rb.mass,
				0f, slowDownSpeed * currentMoveSpeed);
			force *= PlayerMovementValues.GetSlowdownMultiplier(surface, currentCharacter);
			Vector3 direction = velocity.normalized;
			rb.AddForce(new Vector3(-direction.x * force, 0f, -direction.z * force));
		}
		float factor = currentMoveSpeed * 0.02f * counterMovement;
		if (Mathf.Abs(mag.x) > threshold && Mathf.Abs(x) < 0.05f && readyToCounterX >= 2)
			rb.AddForce(orientation.right * (-mag.x * factor));
		if (Mathf.Abs(mag.y) > threshold && Mathf.Abs(y) < 0.05f && readyToCounterY >= 2)
			rb.AddForce(orientation.forward * (-mag.y * factor));
		if (IsHoldingAgainstHorizontalVel(mag)) rb.AddForce(orientation.right * (-mag.x * factor * 2f));
		if (IsHoldingAgainstVerticalVel(mag)) rb.AddForce(orientation.forward * (-mag.y * factor * 2f));
		readyToCounterX = Mathf.Abs(x) < 0.05f ? readyToCounterX + 1 : 0;
		readyToCounterY = Mathf.Abs(y) < 0.05f ? readyToCounterY + 1 : 0;
	}

	private bool IsHoldingAgainstHorizontalVel(Vector2 vel)
	{
		return (vel.x < -threshold && x > 0f) || (vel.x > threshold && x < 0f);
	}

	private bool IsHoldingAgainstVerticalVel(Vector2 vel)
	{
		return (vel.y < -threshold && y > 0f) || (vel.y > threshold && y < 0f);
	}

	private bool IsFloor(Vector3 v)
	{
		return Vector3.Angle(Vector3.up, v) < maxSlopeAngle;
	}

	private bool IsSlideable(Vector3 v)
	{
		return Vector3.Angle(Vector3.up, v) > slideAngle;
	}

	private bool IsSurf(Vector3 v)
	{
		float angle = Vector3.Angle(Vector3.up, v);
		return angle > maxSlopeAngle && angle < 89f;
	}

	private bool IsWall(Vector3 v)
	{
		return Mathf.Abs(90f - Vector3.Angle(Vector3.up, v)) < 0.1f;
	}

	private bool IsRoof(Vector3 v)
	{
		return Mathf.Abs(v.y + 1f) < 0.075f;
	}

	private void UpdateCollisionChecks()
	{
		if (!cancellingGrounded) cancellingGrounded = true;
		else if (++groundCancel > surfaceDelay)
		{
			if (grounded) leftGroundAtTime = Assets.Scripts.Utility.MyTime.time;
			grounded = false;
			groundedObject = null;
			A_Grounded?.Invoke(false);
		}
		if (climbCancel < climbCancelTicks) climbCancel++;
		if (!cancellingSurf)
		{
			cancellingSurf = true;
			surfCancel = 1;
		}
		else if (++surfCancel > surfaceDelay) surfing = false;
	}

	private void StopGrounded()
	{
		if (grounded) leftGroundAtTime = Assets.Scripts.Utility.MyTime.time;
		grounded = false;
		groundedObject = null;
		A_Grounded?.Invoke(false);
	}

	private void StopSurf()
	{
		surfing = false;
	}

	public Vector3 GetVelocity()
	{
		return rb.velocity;
	}

	public float GetAverageVelocity()
	{
		return avgVelocity;
	}

	public float GetSpeedHorizontal()
	{
		Vector3 velocity = rb.velocity;
		return Mathf.Sqrt(velocity.x * velocity.x + velocity.z * velocity.z);
	}

	public float GetSpeed()
	{
		return rb.velocity.magnitude;
	}

	public float GetFallSpeed()
	{
		return fallSpeed;
	}

	public Collider GetPlayerCollider()
	{
		return playerCollider;
	}

	public Transform GetPlayerCamTransform()
	{
		return playerCam.transform;
	}

	public Rigidbody GetRb()
	{
		return rb;
	}

	internal Vector3 DashSurfaceNormal => normalVector;
	internal float HoarderFallSpeed => fallSpeed;

	public void StopFallVelocity()
	{
		Vector3 velocity = rb.velocity;
		if (velocity.y < 0f) rb.velocity = new Vector3(velocity.x, 0f, velocity.z);
	}

	public Vector3 GetRbFeetPosition()
	{
		Vector3 position = gameObject.activeInHierarchy ? rb.position : transform.position;
		return position - (crouchState == CrouchState.None ? standingFeetOffset : crouchingFeetOffset);
	}

	public Vector3 GetRbHeadPosition()
	{
		return rb.position + (crouchState == CrouchState.None ? headHeight : crouchHeadHeight);
	}

	public Vector3 FeetPositionToRb(Vector3 feetPos, bool wasCrouching = false)
	{
		return feetPos + feetHeight;
	}

	public bool CanFloat()
	{
		return !IsTouchingGround() && rb.velocity.y < -0.5f && resetJumpCounter >= 4 && !jumping;
	}

	public float GetFeetOffset()
	{
		return feetHeight.y;
	}

	public EMovementState GetMovementState()
	{
		if (!grounded)
		{
			if (crouchState == CrouchState.Crouching) return EMovementState.Airborne | EMovementState.Crouching;
			if (crouchState == CrouchState.Sliding) return EMovementState.Airborne | EMovementState.Sliding;
			return EMovementState.Airborne;
		}
		if (crouchState == CrouchState.Crouching) return EMovementState.Crouching;
		if (crouchState == CrouchState.Sliding) return EMovementState.Sliding;
		return inputState.moveHorizontal == 0f && inputState.moveVertical == 0f && GetSpeedHorizontal() < 3f
			? EMovementState.Idle : EMovementState.Walking;
	}

	public float GetPlayerRadius()
	{
		return playerRadius;
	}

	public Vector3 GetNormal()
	{
		return normalVector;
	}

	public bool IsHoldingMovement()
	{
		return x != 0f || y != 0f;
	}

	internal bool RendererSurfing => surfing;
	internal bool RendererJumping => resetJumpCounter < 4;
	internal bool RendererGrindingPose => IsGrinding() || (crouchState != CrouchState.None && MyTime.time > jumpedTime + jumpAnimationCooldownSlide && resetJumpCounter > 3);

	public void TouchingTornado()
	{
		lastTouchedTornadoTime = MyTime.time;
		isTouchingTornado = true;
	}
}
