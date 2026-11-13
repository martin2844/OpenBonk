using Assets.Scripts.Utility;
using UnityEngine;

public class FloatingObject : MonoBehaviour
{
	private Vector3 defaultPosition;
	public bool useMeTime = true;
	public float heightOffset = .2f;
	public float floatSpeed = 1f;
	public float rotationSpeed = 10f;

	private void Awake()
	{
		defaultPosition = transform.position;
	}

	private void Update()
	{
		float delta = useMeTime ? MyTime.deltaTime : Time.deltaTime;
		transform.Rotate(Vector3.up, rotationSpeed * delta);
		transform.position = defaultPosition + Vector3.up * (Mathf.Sin(delta * floatSpeed) * heightOffset);
	}
}
