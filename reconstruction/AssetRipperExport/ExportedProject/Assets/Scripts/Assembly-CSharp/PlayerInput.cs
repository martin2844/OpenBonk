using Assets.Scripts.Inventory__Items__Pickups.Interactables;
using Assets.Scripts.Inventory__Items__Pickups;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Utility;
using Assets.Scripts.Saves___Serialization.Progression.Challenges;
using Assets.Scripts.Player.Movement;
using UnityEngine;

public class PlayerInput : MonoBehaviour
{
	public DetectInteractables detectInteractables;

	private float moveHorizontal;

	private float moveVertical;

	private bool jumping;

	private bool interacting;

	private bool sliding;

	private bool aiming;

	private bool holdingJump;

	private bool holdingWallrun;

	public Vector3 cameraRotation;

	private Vector3 desiredCameraRotation;

	private float cameraSmoothingMin = 0.25f;

	private float cameraSmoothingMax = 0.75f;

	private bool hasWallrunActionBound;

	private string stringMouseX = "Mouse X";

	private string stringMouseY = "Mouse Y";

	private int jumpBufferTicks = 4;

	private int currentJumpBufferTick;

	private void Awake()
	{
		PlayerHealth.A_Died += OnPlayerDied;
		KeyListener.A_MapChanged += OnInputMappingChanged;
	}

	private void OnDestroy()
	{
		PlayerHealth.A_Died -= OnPlayerDied;
		KeyListener.A_MapChanged -= OnInputMappingChanged;
	}

	private void Start()
	{
		hasWallrunActionBound = MyInputManager.IsActionBound(MyInputManager.Wallrun);
	}

	private void Update()
	{
		if (!CanInput() || MyTime.paused || MyTime.unpauseTick >= MyTime.tick || IsConsoleOpen()) return;
		MovementInput();
		RotationInput();
	}

	public static bool IsConsoleOpen()
	{
		return Assets.Scripts.UI.Debug.DebugConsole.Instance != null && Assets.Scripts.UI.Debug.DebugConsole.Instance.IsActive();
	}

	public Vector3 GetWishDir()
	{
		Quaternion rotation = Quaternion.Euler(0f, cameraRotation.y, 0f);
		return (rotation * Vector3.forward * moveVertical + rotation * Vector3.right * moveHorizontal).normalized;
	}

	// Preserve analog magnitude for presentation; movement direction remains normalized.
	internal float MovementInputMagnitude => Mathf.Clamp01(new Vector2(moveHorizontal, moveVertical).magnitude);

	private void TestInput()
	{
	}

	private void MovementInput()
	{
		moveHorizontal = MyInputManager.GetAxis(MyInputManager.MoveHorizontal);
		moveVertical = MyInputManager.GetAxis(MyInputManager.MoveVertical);
		if (ChallengesTracker.HasChallengeModifier("inverted_controls"))
		{
			moveHorizontal = -moveHorizontal;
			moveVertical = -moveVertical;
		}
		if (MyInputManager.GetButtonDown(MyInputManager.Jump) ||
			(MyInputManager.GetButtonDown(MyInputManager.JumpBhop) && MyPlayer.Instance.playerMovement.CanBhopJump()))
		{
			jumping = true;
			currentJumpBufferTick = 0;
		}
		holdingJump = MyInputManager.GetButton(MyInputManager.Jump);
		var controls = SaveManager.Instance.config.cfControlSettings;
		if (controls.hold_crouch == 1) sliding = MyInputManager.GetButton(MyInputManager.Slide);
		else if (MyInputManager.GetButtonDown(MyInputManager.Slide)) sliding = !sliding;
		if (MyInputManager.GetButtonDown(MyInputManager.Interact)) interacting = true;
		if (!MyPlayer.Instance.inventory.weaponInventory.HasAimableWeapon()) aiming = false;
		else if (controls.hold_aim == 1) aiming = MyInputManager.GetButton(MyInputManager.Aim);
		else if (MyInputManager.GetButtonDown(MyInputManager.Aim)) aiming = !aiming;
		holdingWallrun = !hasWallrunActionBound || MyInputManager.GetButton(MyInputManager.Wallrun);
	}

	private void AbilityInput()
	{
	}

	public void SetSpawnDirection(Vector3 direction, float pitch = 0f)
	{
		float yaw = Quaternion.LookRotation(direction).eulerAngles.y;
		desiredCameraRotation = new Vector3(pitch, yaw, 0f);
		cameraRotation = desiredCameraRotation;
	}

	public bool IsHoldingJump()
	{
		return MyInputManager.GetButton(MyInputManager.Jump);
	}

	public void RotationInput()
	{
#if UNITY_ANDROID && !UNITY_EDITOR
		ManualRotation();
		PlayerCamera.Instance.CameraInput(cameraRotation);
#else
		if (Cursor.lockState != CursorLockMode.None)
		{
			ManualRotation();
			PlayerCamera.Instance.CameraInput(cameraRotation);
		}
		else PlayerCamera.Instance.MovePositionOnly();
#endif
	}

	private void AutoRotation()
	{
	}

	private void ManualRotation()
	{
		var controls = SaveManager.Instance.config.cfControlSettings;
		float mouseX = Input.GetAxis(stringMouseX);
		float mouseY = Input.GetAxis(stringMouseY);
		float lookX = MyInputManager.GetAxis(MyInputManager.LookHorizontal);
		float lookY = MyInputManager.GetAxis(MyInputManager.LookVertical);
		float keyX = 0f, keyY = 0f;
		if (controls.rotate_camera_with_arrow_keys == 1)
		{
			keyX = Input.GetKey(KeyCode.LeftArrow) ? -3f : Input.GetKey(KeyCode.RightArrow) ? 3f : 0f;
			keyY = Input.GetKey(KeyCode.UpArrow) ? 3f : Input.GetKey(KeyCode.DownArrow) ? -3f : 0f;
		}
		ApplyRotationDelta(mouseX, mouseY, lookX, lookY, keyX, keyY, Time.deltaTime);
	}

	private void ApplyRotationDelta(float mouseX, float mouseY, float lookX, float lookY, float keyX, float keyY, float deltaTime)
	{
		var controls = SaveManager.Instance.config.cfControlSettings;
		float horizontal = mouseX * controls.sensitivity + lookX * controls.controller_sensitivity * deltaTime * 50f + keyX;
		float vertical = lookY * controls.controller_sensitivity * deltaTime * 50f - mouseY * controls.sensitivity + keyY;
		if (controls.inverted_horizontal_axis == 1) horizontal = -horizontal;
		if (controls.inverted_vertical_axis == 1) vertical = -vertical;
		if (ChallengesTracker.HasChallengeModifier("inverted_controls")) { horizontal = -horizontal; vertical = -vertical; }
		desiredCameraRotation = new Vector3(Mathf.Clamp(desiredCameraRotation.x + vertical, -40f, 75f), desiredCameraRotation.y + horizontal, 0f);
		float smoothing = GetCameraSmoothing();
		if (smoothing > 0f)
		{
			float factor = cameraSmoothingMin + (cameraSmoothingMax - cameraSmoothingMin) * (1f - smoothing);
			cameraRotation = Vector3.Lerp(cameraRotation, desiredCameraRotation, deltaTime * factor * 20f);
		}
		else cameraRotation = desiredCameraRotation;
	}

	private float GetCameraSmoothing()
	{
		return SaveManager.Instance.config.cfControlSettings.look_smoothing;
	}

	private void FixedUpdate()
	{
		if (GameManager.Instance == null || !GameManager.Instance.IsPlaying() ||
			MyPlayer.Instance == null || MyPlayer.Instance.playerMovement == null || MyTime.paused) return;
		MyPlayer player = MyPlayer.Instance;
		if (!player.IsDead() && !IsMovementDisabled())
			player.playerMovement.SetBetterInput(GetInputState());
		player.playerMovement.MovementTick();
		if (interacting) detectInteractables.TryInteract();
		if (currentJumpBufferTick < jumpBufferTicks && ++currentJumpBufferTick >= jumpBufferTicks) jumping = false;
		interacting = false;
	}

	private void OnPlayerDied()
	{
		MyPlayer.Instance.playerMovement.SetBetterInput(default(InputState));
	}

	private InputState GetInputState()
	{
		return new InputState(moveHorizontal, moveVertical, cameraRotation.y, cameraRotation.x,
			jumping, sliding, holdingJump, holdingWallrun);
	}

	private bool CanInput()
	{
		return GameManager.Instance != null && GameManager.Instance.GetPlayer() != null &&
			PlayerCamera.Instance != null && PlayerCamera.Instance.GetCameraState() == PlayerCamera.ECameraState.Player3rd;
	}

	private bool IsMovementDisabled()
	{
		return ChallengesTracker.HasChallengeModifier("no_movement");
	}

	public bool IsAiming()
	{
		return aiming;
	}

	private void OnInputMappingChanged()
	{
		hasWallrunActionBound = MyInputManager.IsActionBound(MyInputManager.Wallrun);
	}
}
