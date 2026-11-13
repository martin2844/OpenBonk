using Assets.Scripts.Inventory__Items__Pickups.Pickups;
using UnityEngine;
using Assets.Scripts.Utility;

public class PickupOrb : MonoBehaviour
{
	public GameObject hitEffect;

	public TrailRenderer trail;

	public ParticleSystem[] particleSystems;

	public Rigidbody rb;

	public EPickup ePickup;

	private float timeoutAtTime;

	private bool isDone;

	private void Awake()
	{
		timeoutAtTime = MyTime.time + 10f;
	}

	private void Update()
	{
		if (!isDone && MyTime.time >= timeoutAtTime) Timeout();
	}

	public void Set(EPickup ePickup)
	{
		this.ePickup = ePickup;
		Color color = MyColorUtility.PickupToColor(ePickup);
		if (particleSystems != null)
			foreach (ParticleSystem ps in particleSystems)
				if (ps != null) { var main = ps.main; main.startColor = color; }
		if (trail != null) { trail.startColor = color; trail.endColor = trail.startColor; }
		Vector3 random = Random.insideUnitSphere;
		Vector3 direction = (Vector3.up + new Vector3(random.x, 0f, random.z) * 0.2f).normalized;
		if (rb != null) rb.AddForce(direction * 15f, ForceMode.Impulse);
	}

	private void OnCollisionEnter(Collision collision)
	{
		if (isDone) return;
		isDone = true;
		if (trail != null) trail.emitting = false;
		if (particleSystems != null) foreach (var ps in particleSystems) if (ps != null) { ps.Clear(); ps.Stop(); }
		if (PickupManager.Instance != null) PickupManager.Instance.SpawnPickup(ePickup, transform.position, 1, true, 0.2f);
		if (hitEffect != null) hitEffect.SetActive(true);
		Destroy(gameObject, 1f);
	}

	private void Timeout()
	{
		isDone = true;
		if (trail != null) trail.emitting = false;
		if (particleSystems != null) foreach (var ps in particleSystems) if (ps != null) { ps.Clear(); ps.Stop(); }
		Destroy(gameObject, 0.5f);
	}
}
