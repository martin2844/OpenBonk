using UnityEngine;

public class AlertIcon : MonoBehaviour
{
	private float fps = 7f;

	private float nextUpdateTime;

	private float rotationAmount = 4f;

	private float rotationSpeed = 3f;

	private float scaleAmount = 0.05f;

	private float scaleSpeed = 6f;

	private float defaultZRot;

	private void Awake()
	{
		defaultZRot = transform.localEulerAngles.z;
	}

	private void Update()
	{
		if (Time.time < nextUpdateTime) return;
		nextUpdateTime = Time.time + 1f / fps;
		transform.localScale = Vector3.one * (1f + Mathf.Sin(Time.time * scaleSpeed) * scaleAmount);
		transform.localEulerAngles = new Vector3(0f, 0f, defaultZRot + Mathf.Sin(Time.time * rotationSpeed) * rotationAmount);
	}
}
