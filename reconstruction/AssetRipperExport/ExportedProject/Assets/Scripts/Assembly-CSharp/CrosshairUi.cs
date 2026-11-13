using Assets.Scripts.Inventory__Items__Pickups.Weapons;
using Assets.Scripts.Inventory__Items__Pickups.AbilitiesPassive.Implementations;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Managers;
using Assets.Scripts.Movement;
using Assets.Scripts.Utility;
using UnityEngine;
using UnityEngine.UI;

public class CrosshairUi : MonoBehaviour
{
	public Image crosshairOuter;

	public RawImage crosshairInner;

	public RectTransform parentCrosshair;

	public CanvasGroup group;

	public Color hoveringEnemyColor;

	public Color hoveringMarkedEnemyColor;

	public float movingSize = 50f;

	public float airborneSize = 65f;

	private float globalSizeMultiplier = 1f;

	private float defaultSize = 40f;

	private float desiredSize;

	private bool hoveringEnemy;

	private bool isVisible;

	private static float yMin = .05f;

	private static float yMax = .5f;

	private void Awake()
	{
		CurrentSettings.A_SettingUpdated += OnSettingUpdated;
		WeaponInventory.A_WeaponAdded += OnWeaponAdded;
		WeaponInventory.A_WeaponRemoved += OnWeaponRemoved;
	}

	private void OnDestroy()
	{
		CurrentSettings.A_SettingUpdated -= OnSettingUpdated;
		WeaponInventory.A_WeaponAdded -= OnWeaponAdded;
		WeaponInventory.A_WeaponRemoved -= OnWeaponRemoved;
	}

	private void Start()
	{
		desiredSize = defaultSize;
		RefreshAlpha();
		RefreshSize();
		CheckVisible();
	}

	private void CheckVisible()
	{
		var player = MyPlayer.Instance;
		var weapons = player != null && player.inventory != null ? player.inventory.weaponInventory : null;
		if (weapons == null || weapons.weapons == null) return;
		bool visible = weapons.HasAimableWeapon() && player.playerInput != null && player.playerInput.IsAiming();
		if (visible == isVisible) return;
		isVisible = visible;
		if (crosshairInner != null) crosshairInner.gameObject.SetActive(visible);
		if (crosshairOuter != null) crosshairOuter.gameObject.SetActive(visible);
	}

	private void Update()
	{
		CheckVisible();
		if (MyTime.paused || !isVisible) return;
		if (parentCrosshair != null) parentCrosshair.position = GetCrosshairUiPosition();
		FindDesiredSize();
		if (crosshairOuter != null)
		{
			float size = Mathf.Lerp(crosshairOuter.rectTransform.sizeDelta.x, desiredSize * globalSizeMultiplier, Time.deltaTime * 6f);
			crosshairOuter.rectTransform.sizeDelta = new Vector2(size, size);
		}
	}

	private void FixedUpdate()
	{
		var camera = Camera.main;
		if (camera == null || GameManager.Instance == null) return;
		bool hit = Physics.Raycast(camera.ScreenPointToRay(GetCrosshairRaycastPosition()), out RaycastHit result, 999f, GameManager.Instance.whatIsEnemy);
		if (hit && !hoveringEnemy)
		{
			hoveringEnemy = true;
			Color color = hoveringEnemyColor;
			if (EnemyManager.Instance != null && EnemyManager.Instance.GetEnemy(result.collider, out var enemy) && enemy != null && PassiveAbilityBullseye.IsMarkedEnemy(enemy)) color = hoveringMarkedEnemyColor;
			if (crosshairOuter != null) crosshairOuter.color = color;
			if (crosshairInner != null) crosshairInner.color = color;
		}
		else if (!hit && hoveringEnemy)
		{
			hoveringEnemy = false;
			if (crosshairOuter != null) crosshairOuter.color = Color.white;
			if (crosshairInner != null) crosshairInner.color = Color.white;
		}
	}

	private void FindDesiredSize()
	{
		var movement = MyPlayer.Instance != null ? MyPlayer.Instance.playerMovement : null;
		if (movement == null) return;
		desiredSize = movement.GetMovementState() == EMovementState.Airborne ? airborneSize : movement.GetSpeed() > 5f ? movingSize : defaultSize;
	}

	public static Vector3 GetCrosshairRaycastPosition()
	{
		float height = SaveManager.Instance?.config?.cfGameSettings != null ? SaveManager.Instance.config.cfGameSettings.crosshair_height : .5f;
		return new Vector3(Screen.width * .5f, Screen.height * .5f * (1f + Mathf.Lerp(yMin, yMax, height)), 0f);
	}

	public static Vector3 GetCrosshairUiPosition()
	{
		return GetCrosshairRaycastPosition();
	}

	private void OnSettingUpdated(string settingName, object oldValue, object newValue)
	{
		if (settingName == "crosshair_size") RefreshSize();
		else if (settingName == "crosshair_alpha") RefreshAlpha();
	}

	private void OnWeaponAdded(WeaponBase weapon)
	{
		if (weapon != null) CheckVisible();
	}

	private void OnWeaponRemoved(WeaponBase weapon)
	{
		if (weapon != null) CheckVisible();
	}

	private void RefreshSize()
	{
		if (parentCrosshair != null && SaveManager.Instance?.config?.cfGameSettings != null)
			parentCrosshair.localScale = Vector3.one * SaveManager.Instance.config.cfGameSettings.crosshair_size;
	}

	private void RefreshAlpha()
	{
		if (group != null && SaveManager.Instance?.config?.cfGameSettings != null)
			group.alpha = SaveManager.Instance.config.cfGameSettings.crosshair_alpha;
	}
}
