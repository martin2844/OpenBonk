using System.Collections.Generic;
using Assets.Scripts.Game.Combat.ConstantAttacks;
using Assets.Scripts.Inventory__Items__Pickups.Weapons;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Utility;
using UnityEngine;

public class AuraAttacks : MonoBehaviour
{
	private Dictionary<EWeapon, ConstantAttack> auras = new Dictionary<EWeapon, ConstantAttack>();

	public AegisAttack aegisAttack;

	private void Awake()
	{
		WeaponInventory.A_WeaponAdded += OnWeaponAdded;
		WeaponInventory.A_WeaponRemoved += OnWeaponRemoved;
		WeaponInventory.A_WeaponToggled += OnWeaponToggle;
		MyPlayer.A_PlayerInventoryInitialized += OnInventoryInitialized;
	}

	private void OnDestroy()
	{
		WeaponInventory.A_WeaponAdded -= OnWeaponAdded;
		WeaponInventory.A_WeaponRemoved -= OnWeaponRemoved;
		WeaponInventory.A_WeaponToggled -= OnWeaponToggle;
		MyPlayer.A_PlayerInventoryInitialized -= OnInventoryInitialized;
	}

	private void OnInventoryInitialized(PlayerInventory inventory)
	{
		Refresh();
	}

	private void Start()
	{
		Refresh();
		transform.SetParent(null);
	}

	private void Refresh()
	{
		if (MyPlayer.Instance == null || MyPlayer.Instance.inventory == null) return;
		foreach (WeaponBase weapon in MyPlayer.Instance.inventory.weaponInventory.weapons.Values) OnWeaponAdded(weapon);
	}

	private void OnWeaponAdded(WeaponBase weaponBase)
	{
		if (MyPlayer.Instance.inventory == null || !weaponBase.weaponData.isAura || auras.ContainsKey(weaponBase.weaponData.eWeapon)) return;
		GameObject obj = Instantiate(weaponBase.weaponData.attack);
		obj.transform.SetParent(MyPlayer.Instance.feet);
		obj.transform.localPosition = Vector3.up * .01f;
		ConstantAttack attack = obj.GetComponent<ConstantAttack>();
		auras.Add(weaponBase.weaponData.eWeapon, attack);
		attack.Set(weaponBase);
		if (weaponBase.weaponData.eWeapon == EWeapon.Aegis) aegisAttack = obj.GetComponent<AegisAttack>();
	}

	private void OnWeaponRemoved(WeaponBase weaponBase)
	{
		if (!auras.TryGetValue(weaponBase.weaponData.eWeapon, out ConstantAttack attack)) return;
		Destroy(attack.gameObject);
		auras.Remove(weaponBase.weaponData.eWeapon);
	}

	private void OnWeaponToggle(WeaponBase weaponBase)
	{
		if (weaponBase.enabled) OnWeaponAdded(weaponBase);
		else OnWeaponRemoved(weaponBase);
	}

	// Keep the Aura weapon parallel to the sampled floor even while the
	// character renderer is still smoothing its orientation to a new slope.
	internal static Vector3 GroundAxis(Vector3 origin, Vector3 fallback, LayerMask groundMask)
	{
		if (Physics.Raycast(origin + Vector3.up, Vector3.down, out RaycastHit hit, 51f, groundMask)
			&& !hit.collider.gameObject.CompareTag("CameraIgnore")) return hit.normal;
		return fallback;
	}

	private void LateUpdate()
	{
		var player = MyPlayer.Instance;
		if (player == null || player.playerRenderer == null) return;
		Vector3 rendererAxis = player.playerRenderer.transform.up;
		Vector3 groundAxis = rendererAxis;
		if (auras.ContainsKey(EWeapon.Aura) && player.playerMovement != null && GameManager.Instance != null
			&& !player.playerMovement.IsGrinding() && !player.playerMovement.IsWallClimbing()
			&& player.playerMovement.IsTouchingGround())
			groundAxis = GroundAxis(player.playerRenderer.transform.position, rendererAxis, GameManager.Instance.whatIsGround);
		foreach (var entry in auras)
		{
			ConstantAttack aura = entry.Value;
			if (aura.IsManualRotation()) continue;
			Vector3 axis = entry.Key == EWeapon.Aura ? groundAxis : rendererAxis;
			aura.transform.up = axis;
			aura.transform.Rotate(axis, MyTime.time * 20f * aura.GetAuraRotationSpeed(), Space.World);
		}
	}
}
