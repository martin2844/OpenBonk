using Assets.Scripts.Actors;
using Assets.Scripts.Inventory__Items__Pickups;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Assets.Scripts.Utility;
using Assets.Scripts.Actors.Player;

public class HealthUI : MonoBehaviour
{
	public Transform healthBar;

	public Transform shieldBar;

	public Transform overhealBar;

	public CanvasGroup canvasGroup;

	private Vector3 defaultPosition;

	public TextMeshProUGUI t_hp;

	public TextMeshProUGUI t_shield;

	public bool followPlayer;

	private void Awake()
	{
		defaultPosition = transform.localPosition;
		MyPlayer.A_PlayerInventoryInitialized += OnPlayerInventoryInit;
		PlayerHealth.A_TakeDamage += OnDamageTaken;
		PlayerHealth.A_Heal += OnHeal;
		PlayerHealth.A_MaxValuesChanged += OnMaxValuesChanged;
		PlayerHealth.A_OverhealUpdate += OnOverhealChanged;
		CurrentSettings.A_SettingUpdated += OnSettingUpdated;
		SpawnPlayerPortal.A_PortalOpen += OnPortalOpen;
		SpawnPlayerPortal.A_PortalClosed += OnPortalClose;
	}

	private void OnDestroy()
	{
		MyPlayer.A_PlayerInventoryInitialized -= OnPlayerInventoryInit;
		PlayerHealth.A_TakeDamage -= OnDamageTaken;
		PlayerHealth.A_Heal -= OnHeal;
		PlayerHealth.A_MaxValuesChanged -= OnMaxValuesChanged;
		PlayerHealth.A_OverhealUpdate -= OnOverhealChanged;
		CurrentSettings.A_SettingUpdated -= OnSettingUpdated;
		SpawnPlayerPortal.A_PortalOpen -= OnPortalOpen;
		SpawnPlayerPortal.A_PortalClosed -= OnPortalClose;
	}

	private void OnPlayerInventoryInit(PlayerInventory inventory)
	{
		UpdateBars(inventory.playerHealth);
	}

	private void Start()
	{
		if (SaveManager.Instance?.config?.cfGameSettings != null)
			SetHealthBarColor((EHpBarColor)SaveManager.Instance.config.cfGameSettings.hp_bar_color);
		if (GameManager.Instance != null && GameManager.Instance.GetPlayerInventory() != null)
			UpdateBars(GameManager.Instance.GetPlayerInventory().playerHealth);
		RefreshHud();
	}

	private void Update()
	{
		if (!followPlayer || GameManager.Instance == null || PlayerCamera.Instance == null) return;
		var player = GameManager.Instance.GetPlayer();
		if (player == null) return;
		Transform cameraTransform = PlayerCamera.Instance.transform;
		Vector3 forward = cameraTransform.forward;
		if (forward.y >= 0f) return;
		float angle = Vector3.Angle(forward, Vector3.up);
		Vector3 direction = cameraTransform.position - player.transform.position;
		direction.y = 0f;
		transform.localPosition = defaultPosition + direction.normalized * (2f * (1f + (180f - angle) / -90f));
	}

	private void UpdateBars(PlayerHealth ph)
	{
		if (ph == null) return;
		UpdateHealthBar(healthBar, ph.hp, ph.maxHp);
		UpdateHealthBar(shieldBar, ph.shield, ph.maxShield);
		UpdateHealthBar(overhealBar, ph.overheal, ph.maxHp + ph.maxOverheal);
		if (shieldBar != null && shieldBar.parent != null) shieldBar.parent.gameObject.SetActive(ph.maxShield > 0f);
		if (t_hp != null) t_hp.text = string.Format("{0:N0}/{1:N0}", ph.GetCombinedHp(), ph.GetCombinedMaxHp());
		if (t_shield != null) t_shield.text = string.Format("{0:N0}/{1:N0}", ph.shield, ph.maxShield);
	}

	private void UpdateHealthBar(Transform bar, float value, float max)
	{
		if (bar != null) bar.localScale = new Vector3(max > 0f ? Mathf.Clamp01(value / max) : 0f, 1f, 1f);
	}

	private void OnPortalOpen()
	{
		if (followPlayer) gameObject.SetActive(false);
	}

	private void OnPortalClose()
	{
		if (followPlayer) gameObject.SetActive(true);
	}

	private void OnHeal(PlayerHealth ph, float amount, bool isShield)
	{
		UpdateBars(ph);
	}

	private void OnDamageTaken(PlayerHealth ph, DamageContainer dc, bool shieldDamage)
	{
		UpdateBars(ph);
	}

	private void OnMaxValuesChanged(PlayerHealth ph)
	{
		UpdateBars(ph);
	}

	private void OnOverhealChanged(PlayerHealth ph)
	{
		UpdateBars(ph);
	}

	private void OnSettingUpdated(string settingName, object oldValue, object newValue)
	{
		if (settingName == "show_hud") RefreshHud();
		else if (settingName == "hp_bar_color") SetHealthBarColor((EHpBarColor)(int)newValue);
	}

	private void RefreshHud()
	{
		if (canvasGroup != null && SaveManager.Instance?.config?.cfGameSettings != null)
			canvasGroup.alpha = SaveManager.Instance.config.cfGameSettings.show_hud == 1 ? 1f : 0f;
	}

	private void SetHealthBarColor(EHpBarColor color)
	{
		var image = healthBar != null ? healthBar.GetComponent<RawImage>() : null;
		if (image == null) return;
		var value = MyColorUtility.GetHealthBarColor(color);
		value.a = 0.75f;
		image.color = value;
	}
}
