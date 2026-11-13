using Assets.Scripts.Actors;
using Assets.Scripts.Inventory__Items__Pickups;
using UnityEngine;

public class ControllerShaker : MonoBehaviour
{
#if UNITY_EDITOR
	internal static System.Action<int, float, float> TestShake;
	internal static System.Action TestStop;
#endif
	private void Awake()
	{
		PlayerHealth.A_TakeDamage += OnPlayerTakeDamage;
		PlayerMovement.A_Landed += OnPlayerLanded;
	}

	private void OnDestroy()
	{
		PlayerHealth.A_TakeDamage -= OnPlayerTakeDamage;
		PlayerMovement.A_Landed -= OnPlayerLanded;
	}

	private void OnPlayerTakeDamage(PlayerHealth ph, DamageContainer dc, bool isShield)
	{
		if (ph.IsDead()) Shake(1, 0.8f, 1f);
		else Shake(2, 0.2f, 0.15f);
	}

	private void OnPlayerLanded(float speed)
	{
		if (speed > 25f) Shake(1, 0.08f, 0.15f);
	}

	public static void Shake(int motor, float intensity, float duration)
	{
		if (!CanShake()) return;
#if UNITY_EDITOR
		if (TestShake != null) { TestShake(motor, intensity, duration); return; }
#endif
#if UNITY_ANDROID && !UNITY_EDITOR
		AndroidControllerVibration.Shake(intensity, duration);
#else
		var player = MyInputManager.GetPlayer();
		var joystick = player.controllers.GetLastActiveController() as Rewired.Joystick;
		if (joystick == null || joystick.vibrationMotorCount <= 0) return;
		if (motor > 0 && joystick.vibrationMotorCount <= 1) motor = 0;
		player.SetVibration(motor, intensity, duration);
#endif
	}

	public static void StopShakes()
	{
		if (!CanShake()) return;
#if UNITY_EDITOR
		if (TestStop != null) { TestStop(); return; }
#endif
#if UNITY_ANDROID && !UNITY_EDITOR
		AndroidControllerVibration.Stop();
#else
		MyInputManager.GetPlayer().StopVibration();
#endif
	}

	private static bool CanShake()
	{
		if (GetGlobalIntensity() <= 0f) return false;
#if UNITY_EDITOR
		if (TestShake != null) return true;
#endif
#if UNITY_ANDROID && !UNITY_EDITOR
		return AndroidControllerVibration.Available();
#else
		var player = MyInputManager.GetPlayer();
		var joystick = player != null ? player.controllers.GetLastActiveController() as Rewired.Joystick : null;
		return joystick != null && joystick.supportsVibration;
#endif
	}

	private static float GetGlobalIntensity()
	{
		var save = SaveManager.Instance;
		return save != null && save.config != null && save.config.cfControlSettings != null
			? save.config.cfControlSettings.controller_vibration : 0f;
	}
}
