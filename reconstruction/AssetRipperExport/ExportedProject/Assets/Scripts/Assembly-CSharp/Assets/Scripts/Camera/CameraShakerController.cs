using Assets.Scripts.Actors;
using Assets.Scripts.Inventory__Items__Pickups;
using Assets.Scripts.Inventory__Items__Pickups.Items.ItemImplementations;
using Assets.Scripts.Utility;
using MilkShake;
using UnityEngine;

namespace Assets.Scripts.Camera
{
	public class CameraShakerController : MonoBehaviour
	{
		public Shaker shaker;

		public ShakePreset damageShake;

		public ShakePreset objectImpactShake;

		public ShakePreset grindShake;

		public ShakePreset pylonSpawnShake;

		private float damageShakeCooldown = .25f;

		private float damageNextShakeReadyTime;

		private ShakeInstance grindShakeInstance;

		private void Awake()
		{
			PlayerHealth.A_TakeDamage += OnDamage;
			SmokeAndShakeObject.A_Impact += OnObjectImpact;
			PlayerMovement.A_ToggleGrind += OnGrindToggle;
			FinalFightController.A_PylonsStarted += OnPylonsStarted;
			CurrentSettings.A_SettingUpdated += OnSettingUpdated;
		}

		private void OnDestroy()
		{
			PlayerHealth.A_TakeDamage -= OnDamage;
			SmokeAndShakeObject.A_Impact -= OnObjectImpact;
			PlayerMovement.A_ToggleGrind -= OnGrindToggle;
			FinalFightController.A_PylonsStarted -= OnPylonsStarted;
			CurrentSettings.A_SettingUpdated -= OnSettingUpdated;
		}

		private void Start()
		{
			if (shaker != null && SaveManager.Instance?.config?.cfGameSettings != null)
				shaker.enabled = SaveManager.Instance.config.cfGameSettings.camera_shake == 1;
		}

		private void OnSettingUpdated(string setting, object oldValue, object newValue)
		{
			if (setting == "camera_shake" && shaker != null && newValue is int value) shaker.enabled = value == 1;
		}

		private void OnDamage(PlayerHealth ph, DamageContainer dc, bool shieldDamage)
		{
			if (dc == null || dc.damage <= 1f || dc.damageSource == ItemKevin.damageSource || dc.damageSource == "HealthRegen" || MyTime.time < damageNextShakeReadyTime) return;
			damageNextShakeReadyTime = MyTime.time + damageShakeCooldown;
			if (shaker != null && damageShake != null) shaker.Shake(damageShake);
		}

		private void OnObjectImpact(float shakeMultiplier)
		{
			if (shaker == null || objectImpactShake == null) return;
			objectImpactShake.Strength = shakeMultiplier * 2f;
			shaker.Shake(objectImpactShake);
		}

		private void OnGrindToggle(bool isGrinding)
		{
			if (isGrinding) { if (shaker != null && grindShake != null) grindShakeInstance = shaker.Shake(grindShake); }
			else grindShakeInstance?.Stop(.1f, true);
		}

		private void OnPylonsStarted()
		{
			if (shaker != null && pylonSpawnShake != null) shaker.Shake(pylonSpawnShake);
		}
	}
}
