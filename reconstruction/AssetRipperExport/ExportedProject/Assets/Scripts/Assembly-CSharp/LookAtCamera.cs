using UnityEngine;

public class LookAtCamera : MonoBehaviour
{
	public bool invert;

	public bool xzOnly;

	private Transform target;

	private void Update()
	{
		if (target == null)
		{
			if (PlayerCamera.Instance != null) target = PlayerCamera.Instance.transform;
			return;
		}
		Vector3 direction = target.position - transform.position;
		if (xzOnly)
		{
			direction.y = 0f;
			if (direction.sqrMagnitude > 0f) transform.LookAt(transform.position + direction);
		}
		else
		{
			if (invert) direction = -direction;
			if (direction.sqrMagnitude > 0f) transform.rotation = Quaternion.LookRotation(direction);
		}
	}
}
