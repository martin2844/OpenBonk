using UnityEngine;
using Assets.Scripts.Managers;
using Assets.Scripts.MapGeneration;
using Assets.Scripts.Utility;

public class QuestMonke : MonoBehaviour
{
	public Animator animator;

	public Rigidbody rb;

	private Vector3 runDir;

	private float speed = 12f;

	public GameObject dissapearFx;

	private float aliveTime = 120f;

	private float deadTime;

	private bool isWalking;

	private Vector3 savedVelocity;

	private float stopMusicAtTime;

	private bool isClimbingWall;

	private float climbSpeed = 8f;

	private void Start()
	{
		runDir = transform.forward;
		deadTime = MyTime.time + aliveTime;
		stopMusicAtTime = MyTime.time + 10f;
		if (MusicController.Instance != null) MusicController.Instance.RefreshFilter();
	}

	private void OnDestroy()
	{
		if (MusicController.Instance != null) MusicController.Instance.RefreshFilter();
	}

	private void Update()
	{
		if (animator.GetCurrentAnimatorStateInfo(0).IsName("Walking"))
		{
			if (!isWalking) { isWalking = true; rb.isKinematic = true; }
		}
		else isWalking = false;
	}

	private void FixedUpdate()
	{
		if (!isWalking) return;
		if (MyTime.paused)
		{
			if (!rb.isKinematic) { rb.isKinematic = true; savedVelocity = rb.velocity; }
			return;
		}
		if (rb.isKinematic) { rb.isKinematic = false; rb.velocity = savedVelocity; }
		TryClimb();
		rb.velocity = new Vector3(runDir.x * speed, rb.velocity.y, runDir.z * speed);
		if (isClimbingWall)
		{
			Vector3 up = (Vector3.up - runDir.normalized).normalized;
			Quaternion target = Quaternion.FromToRotation(transform.up, up) * transform.rotation;
			rb.MoveRotation(Quaternion.Slerp(rb.rotation, target, Time.fixedDeltaTime * 10f));
		}
		else if (Physics.Raycast(transform.position, Vector3.down, out RaycastHit hit, 2f))
		{
			Vector3 forward = Vector3.ProjectOnPlane(runDir, hit.normal).normalized;
			if (forward.sqrMagnitude > 0.001f)
				rb.MoveRotation(Quaternion.Slerp(rb.rotation, Quaternion.LookRotation(forward, hit.normal), Time.fixedDeltaTime * 10f));
		}
		CheckDead();
		isClimbingWall = false;
	}

	private void TryClimb()
	{
		if (GameManager.Instance == null) return;
		if (Physics.SphereCast(transform.position + Vector3.up * .5f, .3f, runDir.normalized, out _, 2.5f, GameManager.Instance.whatIsGroundAndObjects))
		{
			Vector3 velocity = rb.velocity;
			velocity.y = climbSpeed;
			rb.velocity = velocity;
			isClimbingWall = true;
		}
	}

	private void CheckDead()
	{
		if (transform.position.y < MapInfo.DespawnEnemyHeight()) Die();
		if (MyTime.time > deadTime) Die();
	}

	private void Die()
	{
		if (dissapearFx != null)
		{
			dissapearFx.SetActive(true);
			dissapearFx.transform.SetParent(null);
		}
		Destroy(gameObject);
	}
}
