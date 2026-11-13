using UnityEngine;

public class CameraFollowBean : MonoBehaviour
{
	public Transform target;

	public Rigidbody[] rbs;

	public float speed;

	public float rotationSpeed;

	public Camera camera;

	private bool falling;

	private float upOffset;

	private float rightOffset;

	public Animator animator;

	public float force;

	public float rotationForce;

	public GameObject animation;

	public GameObject ragdoll;

	private void Update()
	{
	}
}
