using Assets.Scripts.Inventory__Items__Pickups.Pickups;
using Assets.Scripts.Inventory__Items__Pickups.Stats;
using UnityEngine;
using Assets.Scripts.Utility;

public class StatusEffectPickup : MonoBehaviour
{
	public EPickup ePickup;

	public EStatusEffect statusEffect;

	public GameObject sparksEffect;

	public GameObject pickupImpactEffect;

	public bool rotateToPlayerVelocity;

	public bool useFeetPosition = true;

	private float timeLeft;

	public void Set()
	{
		if (HasStatusEffect())
		{
			if (pickupImpactEffect != null) pickupImpactEffect.transform.SetParent(null);
			DestroySelf();
			return;
		}
		var player = GameManager.Instance.GetPlayer();
		var movement = GameManager.Instance.GetPlayerMovement();
		if (player != null && movement != null)
		{
			transform.position = useFeetPosition ? movement.GetRbFeetPosition() : movement.transform.position;
			transform.SetParent(player.transform);
			if (sparksEffect != null) { sparksEffect.transform.SetParent(player.transform); sparksEffect.transform.localPosition = Vector3.zero; }
			if (pickupImpactEffect != null) pickupImpactEffect.transform.position = player.transform.position;
		}
		Pickup.A_PickupTriggered += OnPickupTriggered;
		timeLeft = PowerupConstants.GetTime(statusEffect);
	}

	private void OnPickupTriggered(Pickup pickup)
	{
		if (pickup.ePickup == ePickup) timeLeft = PowerupConstants.GetTime(statusEffect);
	}

	private void Update()
	{
		timeLeft -= MyTime.deltaTime;
		if (timeLeft <= 0f) { DestroySelf(); return; }
		if (!rotateToPlayerVelocity || GameManager.Instance == null || GameManager.Instance.GetPlayer() == null) return;
		var movement = GameManager.Instance.GetPlayerMovement();
		if (movement == null) return;
		Vector3 velocity = movement.GetVelocity();
		velocity.y = 0f;
		if (velocity.sqrMagnitude >= 1e-10f) transform.rotation = Quaternion.LookRotation(velocity);
	}

	private bool HasStatusEffect()
	{
		if (GameManager.Instance == null) return true;
		var effects = GameManager.Instance.GetPlayerInventory()?.statusEffects;
		if (effects == null || !effects.HasStatusEffect(statusEffect)) return false;
		return effects.statusEffects[statusEffect].addedTime < MyTime.time;
	}

	private void DestroySelf()
	{
		Destroy(gameObject);
	}

	private void OnDestroy()
	{
		Pickup.A_PickupTriggered -= OnPickupTriggered;
		if (sparksEffect != null) Destroy(sparksEffect);
	}
}
