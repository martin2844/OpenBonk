using UnityEngine;

public class SpinAroundCharacter : MonoBehaviour
{
	public Transform camera;

	public Transform target;

	public float distanceFromTarget = 10f;

	public float cameraHeight = 5f;

	public Vector3 targetOffset;

	public float rotationSpeed = .5f;

	private float currentAngle;

	private void Update()
	{
		if (target == null || camera == null) return;
		transform.position = target.position;
		float desiredAngle = 60f - Mathf.Abs(Mathf.Repeat(Time.time * rotationSpeed, 240f) - 120f);
		currentAngle = Mathf.Lerp(currentAngle, desiredAngle, Time.deltaTime * .5f);
		transform.rotation = Quaternion.Euler(0f, currentAngle, 0f);
		camera.localPosition = new Vector3(0f, cameraHeight, -distanceFromTarget);
		camera.LookAt(target.position + targetOffset);
	}
}
