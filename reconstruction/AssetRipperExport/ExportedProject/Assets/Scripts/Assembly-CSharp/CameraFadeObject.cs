using UnityEngine;
using Assets.Scripts.Utility;

public class CameraFadeObject : MonoBehaviour
{
	public Renderer renderer;

	private Material defaultMaterial;

	public Material fadeMaterial;

	private bool fading;

	private float stopFadeTime;

	private void Start()
	{
		if (renderer != null) defaultMaterial = renderer.sharedMaterial;
		PlayerCamera.A_CameraFadeObjectEnter += OnFadeObject;
	}

	private void OnDestroy()
	{
		PlayerCamera.A_CameraFadeObjectEnter -= OnFadeObject;
	}

	private void OnFadeObject(GameObject go)
	{
		if (go != gameObject) return;
		if (!fading && renderer != null) renderer.sharedMaterial = fadeMaterial;
		fading = true;
		stopFadeTime = MyTime.time + .08f;
	}

	private void Update()
	{
		if (fading && MyTime.time >= stopFadeTime) StopFade();
	}

	private void StopFade()
	{
		fading = false;
		if (renderer != null) renderer.sharedMaterial = defaultMaterial;
	}
}
