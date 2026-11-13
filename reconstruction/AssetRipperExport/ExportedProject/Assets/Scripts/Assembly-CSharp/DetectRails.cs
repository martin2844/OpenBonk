using Assets.Scripts.Actors;
using Assets.Scripts.Inventory__Items__Pickups;
using UnityEngine;

public class DetectRails : MonoBehaviour
{
	public LayerMask whatIsRails;

	private PlayerMovement playerMovement;

	private Collider[] buffer = new Collider[10];

	private string railTag = "Rail";

	private void Awake()
	{
		playerMovement = GetComponent<PlayerMovement>();
		PlayerHealth.A_TakeDamage += OnDamage;
	}

	private void OnDestroy()
	{
		PlayerHealth.A_TakeDamage -= OnDamage;
	}

	private void OnDamage(PlayerHealth arg1, DamageContainer arg2, bool arg3)
	{
		playerMovement.StopRail();
	}

	private void FixedUpdate()
	{
		if (playerMovement == null || playerMovement.GetRb() == null || !playerMovement.CanStartGrind()) return;
		float radius = playerMovement.playerRadius * 1.75f;
		Vector3 center = playerMovement.GetRbFeetPosition() + Vector3.up * (radius * 0.5f);
		int count = Physics.OverlapSphereNonAlloc(center, radius, buffer, whatIsRails);
		for (int i = 0; i < count; i++)
		{
			if (!buffer[i].CompareTag(railTag)) continue;
			Rail candidate = buffer[i].GetComponent<Rail>();
			if (candidate != null && !candidate.IsOnCooldown()) { playerMovement.StartRail(candidate); return; }
		}
	}
}
