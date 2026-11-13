using UnityEngine;

public class RotateObjectRandomly : MonoBehaviour
{
	public float minSpeed;

	public float maxSpeed;

	private float speed;

	private Vector3 rotation;

	private void Start()
	{
		FindNewRotation();
	}

	public void FindNewRotation()
	{
		transform.rotation = Quaternion.identity;
		speed = Random.Range(minSpeed, maxSpeed);
		rotation = new Vector3(Random.Range(-1f, 1f), Random.Range(-1f, 1f), Random.Range(-1f, 1f));
	}

	private void Update()
	{
		transform.Rotate(rotation * speed * Assets.Scripts.Utility.MyTime.deltaTime);
	}
}
