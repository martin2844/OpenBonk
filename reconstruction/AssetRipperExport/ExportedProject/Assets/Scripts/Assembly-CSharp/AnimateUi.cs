using UnityEngine;

public class AnimateUi : MonoBehaviour
{
	private float fps = 7f;

	private float nextUpdateTime;

	public float rotationAmount = 4f;

	public float rotationSpeed = 3f;

	public float scaleAmount = 0.05f;

	public float scaleSpeed = 6f;

	public bool animateScale = true;

	private float defaultZRot;

	private void Awake()
	{
		defaultZRot = transform.localEulerAngles.z;
	}

	private void Update()
	{
		if (Time.time < nextUpdateTime) return;
		nextUpdateTime = Time.time + 1f / fps;
		if (animateScale) transform.localScale = Vector3.one * (1f + Mathf.Sin(Time.time * scaleSpeed) * scaleAmount);
		transform.localEulerAngles = new Vector3(0f, 0f, defaultZRot + Mathf.Sin(Time.time * rotationSpeed) * rotationAmount);
	}
}
