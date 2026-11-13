using Assets.Scripts.Inventory__Items__Pickups.Pickups;
using Assets.Scripts.Utility;
using UnityEngine;
using UnityEngine.UIElements.Experimental;

public class Shield : MonoBehaviour
{
	private Vector3 defaultScale;

	private float destroyTimer;

	private Renderer renderer;

	private Material shieldMaterial;

	public AudioSource sfx;

	private float defaultVolume;

	private float scaleTime = 0.3f;

	private void Awake()
	{
		renderer = GetComponent<Renderer>();
		if (renderer != null) shieldMaterial = renderer.sharedMaterial;
		destroyTimer = PowerupConstants.GetShieldTime();
		defaultScale = transform.localScale;
		transform.localScale = Vector3.zero;
		if (sfx != null) defaultVolume = sfx.volume;
		Pickup.A_PickupTriggered += OnPickup;
		UpdateAlpha(1f);
	}

	private void OnDestroy()
	{
		Pickup.A_PickupTriggered -= OnPickup;
	}

	private void OnPickup(Pickup pickup)
	{
		if (pickup != null && pickup.ePickup == EPickup.Shield)
			destroyTimer = PowerupConstants.GetShieldTime();
	}

	private void Update()
	{
		if (destroyTimer > scaleTime)
		{
			transform.localScale = Vector3.Lerp(transform.localScale, defaultScale, MyTime.deltaTime / scaleTime);
		}
		else
		{
			float progress = 1f - destroyTimer / scaleTime;
			transform.localScale = Vector3.Lerp(defaultScale, Vector3.zero, Easing.InOutCirc(progress));
			if (sfx != null) sfx.volume = defaultVolume * (1f - progress);
		}
		destroyTimer -= MyTime.deltaTime;
		if (destroyTimer <= 0f)
			Destroy(gameObject);
		else if (destroyTimer <= 3f)
			UpdateAlpha(Mathf.Lerp(0.1f, 1f, (Mathf.Sin(MyTime.time * 3f * Mathf.PI * 2f) + 1f) * 0.5f));
	}

	private void UpdateAlpha(float alpha)
	{
		if (shieldMaterial == null) return;
		Color color = shieldMaterial.GetColor("_MainColor");
		color.a = alpha;
		shieldMaterial.SetColor("_MainColor", color);
	}
}
