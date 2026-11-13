using UnityEngine;

public class FloatingObject2 : MonoBehaviour
{
	private Vector3 defaultPosition;

	public float heightOffset = .2f;

	public float rotationSpeed = 10f;

	public int moveSpeed = 1;

	private void Start()
	{
		defaultPosition = transform.position;
	}

	private void Update()
	{
		transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime, Space.Self);
		float phase = transform.eulerAngles.y * Mathf.Deg2Rad * moveSpeed;
		transform.position = defaultPosition + Vector3.up * Mathf.Sin(phase) * heightOffset;
	}
}
