using UnityEngine;
using Assets.Scripts.Utility;

public class RotateObject : MonoBehaviour
{
	public bool useMeTime = true;

	public float speed;

	public Vector3 axis = Vector3.up;

	private void Update()
	{
		if (useMeTime) transform.Rotate(axis, speed * MyTime.deltaTime);
		else transform.Rotate(axis, speed * Time.deltaTime, Space.World);
	}
}
