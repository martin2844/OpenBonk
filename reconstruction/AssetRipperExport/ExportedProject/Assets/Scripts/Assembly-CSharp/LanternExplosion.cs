using MilkShake;
using UnityEngine;
using Assets.Scripts.Utility;
using UnityEngine.UIElements.Experimental;

public class LanternExplosion : MonoBehaviour
{
	private Material material;

	public ShakePreset shakePreset;

	public GameObject explosionFx;

	public float sizeMultiplier = 1f;

	public float lifeTime = 1f;

	public bool disableOnFinish;

	private Color startColor;

	private float startAlpha;

	private Vector3 desiredSize;

	private Vector3 startSize;

	private float myTime;

	private void Awake()
	{
		var renderer = GetComponent<Renderer>();
		if (renderer != null) { material = renderer.material; startColor = material.GetColor("_MainColor"); startAlpha = startColor.a; }
		if (PlayerCamera.Instance != null && PlayerCamera.Instance.shaker != null && shakePreset != null) PlayerCamera.Instance.shaker.Shake(shakePreset);
		if (explosionFx != null) explosionFx.transform.SetParent(null);
	}

	public void SetRadius(float radius)
	{
		desiredSize = Vector3.one * radius;
	}

	private void OnEnable()
	{
		transform.localScale = startSize;
		myTime = 0f;
		if (material != null) material.SetColor("_MainColor", startColor);
		if (explosionFx != null) { explosionFx.SetActive(true); explosionFx.transform.position = transform.position; }
	}

	private void OnDestroy()
	{
		if (material != null) Destroy(material);
	}

	private void FixedUpdate()
	{
		myTime += MyTime.fixedDeltaTime;
		float progress = Easing.InOutCirc(myTime / lifeTime);
		transform.localScale = Vector3.Lerp(startSize, desiredSize, progress);
		if (progress >= .8f && material != null)
		{
			Color color = material.GetColor("_MainColor");
			color.a = Mathf.Lerp(startAlpha, 0f, (progress - .8f) / .2f);
			material.SetColor("_MainColor", color);
		}
		if (myTime < lifeTime) return;
		if (disableOnFinish) { if (explosionFx != null) explosionFx.SetActive(false); gameObject.SetActive(false); }
		else Destroy(gameObject);
	}
}
