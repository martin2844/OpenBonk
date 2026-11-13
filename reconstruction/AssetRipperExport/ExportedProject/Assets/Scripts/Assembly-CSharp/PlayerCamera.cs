using System;
using MilkShake;
using UnityEngine;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Managers;
using Assets.Scripts.Utility;

public class PlayerCamera : MonoBehaviour
{
	public enum ECameraState
	{
		Portal = 0,
		Player3rd = 1,
		Player1st = 2,
		Death = 3
	}

	public Vector3 offset3rdPerson = new Vector3(0f, 0.5f, 9.5f);

	private float currentBob;

	private float desiredBob;

	public Shaker shaker;

	private ECameraState cameraState;

	public CameraOutlines cameraOutlines;

	public Camera camera;

	private bool inited;

	public static PlayerCamera Instance;

	private float defaultZ;

	private Transform portal;

	public bool isPortalCameraFocusingPlayer;

	private float currentZ;

	private float maxExtraZoomoutDistance = 15f;

	public bool useCenter;

	public Transform testingTarget;

	public Camera deathCamera;

	public RenderTexture deathRenderTexture;

	private float deathOffset = 4f;

	public static Action<GameObject> A_CameraFadeObjectEnter;

	private float cameraRadius = 1f;

	public float testDist;

	private void Awake()
	{
#if UNITY_ANDROID && !UNITY_EDITOR
		if (SaveManager.Instance != null && SaveManager.Instance.config != null) TryInit();
#else
		TryInit();
#endif
	}

	public void TryInit()
	{
		if (inited) return;
		inited = true;
		if (Instance != null) { Destroy(gameObject); return; }
		Instance = this;
		defaultZ = offset3rdPerson.z;
		currentZ = defaultZ;
		PlayerMovement.A_Landed += OnPlayerLanded;
		CurrentSettings.A_SettingUpdated += OnSettingUpdated;
		float fov = (int)SaveManager.Instance.config.cfVideoSettings.fov;
		camera.fieldOfView = fov;
		deathCamera.fieldOfView = fov;
#if UNITY_ANDROID && !UNITY_EDITOR
		// Water and lava use the scene depth for their shore and intersection colors.
		camera.depthTextureMode |= DepthTextureMode.Depth;
#endif
		UpdateZoom();
	}

	private void OnDestroy()
	{
		if (Instance != this) return;
		PlayerMovement.A_Landed -= OnPlayerLanded;
		CurrentSettings.A_SettingUpdated -= OnSettingUpdated;
	}

	private void Start()
	{
		AdjustCameraFar();
	}

	private void AdjustCameraFar()
	{
		if (camera != null && MapController.currentStage != null)
			camera.farClipPlane = MapController.currentStage.farClipPlane;
	}

	public ECameraState GetCameraState()
	{
		return cameraState;
	}

	private void Update()
	{
		UpdateBob();
#if UNITY_ANDROID && !UNITY_EDITOR
		if (AndroidGameplayBootstrap.IsRunning) return;
#endif
		if (cameraState == ECameraState.Portal) PortalCamera();
		else if (cameraState == ECameraState.Death) DeathCam();
	}

	private void UpdateBob()
	{
		desiredBob = Mathf.Lerp(desiredBob, 0f, Time.deltaTime * 7f);
		currentBob = Mathf.Lerp(currentBob, desiredBob, Time.deltaTime * 12f);
	}

	public void SetCameraState(ECameraState state)
	{
		cameraState = state;
	}

	public void CameraInput(Vector3 playerRotation)
	{
		if (cameraState == ECameraState.Player3rd) PlayerCam(playerRotation);
	}

	public void StartPortalCamera(Transform portal)
	{
		isPortalCameraFocusingPlayer = false;
		this.portal = portal;
		transform.position = portal.position + portal.forward * 0.25f;
		transform.LookAt(portal);
		Vector3 direction = -portal.forward;
		MyPlayer.Instance.playerInput.SetSpawnDirection(new Vector3(direction.x, 0f, direction.z));
	}

	public void StopPortalCamera()
	{
		cameraState = ECameraState.Player3rd;
	}

	private void PortalCamera()
	{
		if (!isPortalCameraFocusingPlayer && portal != null)
		{
			Vector3 target = portal.position + GetPortalOffsetPosition(portal.forward);
			transform.position = Vector3.Lerp(transform.position, target, Time.deltaTime * 0.9f);
			transform.LookAt(portal);
		}
		else
		{
			transform.position = Vector3.Lerp(transform.position, GetCameraPosition(), Time.deltaTime * 5f);
			transform.rotation = Quaternion.Lerp(transform.rotation, Quaternion.Euler(MyPlayer.Instance.playerInput.cameraRotation), Time.deltaTime * 3f);
		}
	}

	public Vector3 GetPortalOffsetPosition(Vector3 portalForward)
	{
		return portalForward * (offset3rdPerson.z + 3f) + Vector3.up * offset3rdPerson.y;
	}

	private void PlayerCam(Vector3 playerRotation)
	{
		transform.rotation = Quaternion.Euler(playerRotation);
		transform.position = GetCameraPosition();
	}

	public void MovePositionOnly()
	{
		transform.position = GetCameraPosition();
	}

	private void DeathCam()
	{
		var player = MyPlayer.Instance;
		if (player == null || player.playerRenderer == null) return;
		Vector3 target = player.transform.position + player.playerRenderer.transform.up * .55f;
		Vector3 direction = target - transform.position;
		if (direction.sqrMagnitude > 0f)
			transform.rotation = Quaternion.Lerp(transform.rotation, Quaternion.LookRotation(direction, player.transform.up), Time.deltaTime);
		transform.position = Vector3.Lerp(transform.position, target + Vector3.up * 4.5f, Time.deltaTime * .5f);
	}

	private Vector3 GetPlayerHeadPosition()
	{
		if (useCenter) return MyPlayer.Instance.transform.position;
		return GameManager.Instance.GetPlayer().head.position + Vector3.down * 0.5f + Vector3.up * offset3rdPerson.y;
	}

	private Vector3 GetCameraPosition()
	{
		Vector3 headPosition = GetPlayerHeadPosition();
		float targetDistance = Get3rdPersonOffset();
		Vector3 targetPosition = headPosition - transform.forward * targetDistance;
		Vector3 direction = (targetPosition - headPosition).normalized;
		float distance = Vector3.Distance(headPosition, targetPosition) - cameraRadius;
		Ray ray = new Ray(headPosition - transform.forward * cameraRadius, direction);
		RaycastHit[] hits = Physics.SphereCastAll(ray, cameraRadius, distance, GameManager.Instance.whatIsCameraCollision);
		float nearest = float.MaxValue;
		foreach (RaycastHit hit in hits)
		{
			Collider collider = hit.collider;
			if (collider.CompareTag("CameraFade") || collider.CompareTag("CameraIgnore") ||
				collider.CompareTag("Ignore") || collider.isTrigger || collider.CompareTag("Interactable")) continue;
			if (hit.distance < nearest) { nearest = hit.distance; targetDistance = hit.distance; }
		}
		CheckFadeObjects(ray, distance, 6f);
		currentZ = Mathf.Lerp(currentZ, targetDistance, Time.deltaTime * 30f);
		return headPosition + Get3rdPersonOffset(currentZ) + GetBobOffset();
	}

	private void CheckFadeObjects(Ray ray, float distance, float radius)
	{
		ray.origin += ray.direction * (radius + 0.25f);
		foreach (RaycastHit hit in Physics.SphereCastAll(ray, radius, distance + radius, GameManager.Instance.whatIsCameraCollision))
			if (hit.collider.CompareTag("CameraFade")) A_CameraFadeObjectEnter?.Invoke(hit.collider.gameObject);
	}

	public void DeathCamera()
	{
		deathRenderTexture = new RenderTexture(Screen.width, Screen.height, 24);
		deathRenderTexture.useMipMap = false;
		deathRenderTexture.autoGenerateMips = false;
		deathRenderTexture.format = RenderTextureFormat.ARGB32;
		deathCamera.targetTexture = deathRenderTexture;
		deathCamera.gameObject.SetActive(true);
		cameraState = ECameraState.Death;
	}

	public void HideDeathCamera()
	{
		deathCamera.gameObject.SetActive(false);
	}

	private void OnPlayerLanded(float fallSpeed)
	{
		if (fallSpeed >= 6f) desiredBob = 2f;
	}

	private void BobOnce(float strength = 0.5f)
	{
		desiredBob = strength;
	}

	private Vector3 GetBobOffset()
	{
		return Vector3.down * currentBob;
	}

	private float Get3rdPersonOffset()
	{
		float t = Mathf.Clamp(MyTime.finalSwarmTimer / 1320f, 0f, 1.5f);
		if (MapController.IsFinalBossStage() && !GameManager.Instance.IsFinalBossDead()) t *= 0.5f;
		float fov = camera.fieldOfView;
		float fovOffset = fov < 90f ? (fov - 90f) / -5f : fov > 90f ? (fov - 90f) / -10f : 0f;
		return offset3rdPerson.z + t * maxExtraZoomoutDistance + fovOffset;
	}

	private Vector3 Get3rdPersonOffset(float zValue)
	{
		return Vector3.up - transform.forward * zValue;
	}

	private void OnSettingUpdated(string name, object oldValue, object newValue)
	{
		if (name == "fov") SetFov((int)(float)newValue);
		if (name == "camera_distance") UpdateZoom();
	}

	public void SetFov(int fov)
	{
		camera.fieldOfView = fov;
		deathCamera.fieldOfView = fov;
	}

	public void SetZoom(float ratio)
	{
		UpdateZoom();
	}

	private void UpdateZoom()
	{
		if (SaveManager.Instance == null) return;
		currentZ = defaultZ + SaveManager.Instance.config.cfVideoSettings.camera_distance;
		offset3rdPerson.z = currentZ;
	}
}
