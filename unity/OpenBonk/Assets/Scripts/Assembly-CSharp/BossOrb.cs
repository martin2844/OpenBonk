using Assets.Scripts.Actors.Enemies;
using Assets.Scripts.Actors;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Game.Combat;
using Assets.Scripts.Utility;
using UnityEngine;
using UnityEngine.UIElements.Experimental;

public class BossOrb : MonoBehaviour
{
	public Rigidbody rb;

	public SphereCollider collider;

	public GameObject explosion;

	public GameObject trail;

	public RandomSfx randomSfx;

	private Vector3 offset;

	private Enemy boss;

	private int iterationsBeforeExplode;

	private float overshootDistance = 1.5f;

	private float moveInterval = 1.5f;

	private float nextMoveTime;

	private float moveTimer = 1f;

	private float moveOverSeconds = 0.5f;

	private float maxMoveDistance = 30f;

	private Vector3 fromPosition;

	private Vector3 toPosition;

	private bool isFired;

	public float spinSpeed = 90f;

	private float currentAngle;

	private float moveDist;

	private Vector3 moveDirection;

	private float moveSpeed;

	private int numMoves;

	protected void Start()
	{
		trail.transform.SetParent(null);
	}

	public void Set(float startDelay, int currentPhase, Enemy ignoreCollisionEnemy, int numOrbs, int orbIndex)
	{
		boss = ignoreCollisionEnemy;
		if (boss != null) Physics.IgnoreCollision(GetComponent<Collider>(), boss.collider);
		nextMoveTime = MyTime.time + moveOverSeconds + 1.5f + orbIndex * 0.5f;
		moveInterval = 1.5f - currentPhase * 0.1f;
		overshootDistance = currentPhase + 0.5f;
		maxMoveDistance = 30f + currentPhase * 10f;
		iterationsBeforeExplode = 6 + currentPhase;
		if (numOrbs >= 2)
		{
			float angle = 360f / numOrbs * orbIndex * Mathf.Deg2Rad;
			offset = new Vector3(Mathf.Cos(angle) * 15f, 0f, Mathf.Sin(angle) * 15f) + Vector3.down * 20f;
		}
	}

	private void FixedUpdate()
	{
		if (isFired)
		{
			UpdateMoving();
			if (MyTime.time >= nextMoveTime) StartMoving();
		}
		else
		{
			FloatMovement();
			if (MyTime.time > nextMoveTime) isFired = true;
		}
	}

	private void Update()
	{
		trail.transform.position = transform.position;
	}

	private void FloatMovement()
	{
		if (moveTimer < 1f) moveTimer = Mathf.Clamp01(moveTimer + MyTime.fixedDeltaTime / moveOverSeconds);
		float move = Easing.InOutCirc(moveTimer);
		currentAngle += spinSpeed * MyTime.fixedDeltaTime;
		Vector3 rotatedOffset = Quaternion.Euler(0f, currentAngle, 0f) * offset;
		rb.MovePosition(boss.GetCenterPosition() + (rotatedOffset + Vector3.up * (boss.GetHeight() * 0.5f)) * move);
	}

	private void UpdateMoving()
	{
		if (moveTimer >= 1f) return;
		float oldProgress = Easing.InOutQuad(moveTimer);
		moveTimer += MyTime.fixedDeltaTime / moveOverSeconds;
		if (moveTimer <= 0f || MyTime.fixedDeltaTime <= 0f) return;
		float distance = (Easing.InOutQuad(moveTimer) - oldProgress) * moveDist;
		Vector3 direction = (toPosition - fromPosition).normalized;
		if (Physics.SphereCast(rb.position, collider.radius, direction, out _, distance, GameManager.Instance.whatIsGroundAndObjects)) direction = Vector3.up;
		rb.velocity = direction * (distance / MyTime.fixedDeltaTime);
		if (moveTimer >= 1f)
		{
			rb.velocity = Vector3.zero;
			if (numMoves >= iterationsBeforeExplode) Explode();
		}
	}

	private void StartMoving()
	{
		nextMoveTime = MyTime.time + moveInterval;
		moveTimer = 0f;
		fromPosition = rb.position;
		toPosition = MyPlayer.Instance.transform.position;
		moveDist = Vector3.Distance(fromPosition, toPosition);
		if (moveDist > maxMoveDistance)
		{
			toPosition = fromPosition + (toPosition - fromPosition).normalized * maxMoveDistance;
			moveDist = maxMoveDistance;
		}
		moveDist += overshootDistance;
		randomSfx.Play();
		numMoves++;
	}

	private void OnCollisionEnter(Collision collision)
	{
		if (collision.gameObject.layer != LayerMask.NameToLayer("Player")) return;
		MyPlayer.Instance.inventory.statusEffects.FreezePlayer(1f);
		Vector3 direction = (MyPlayer.Instance.transform.position - transform.position).normalized;
		MyPlayer.Instance.inventory.playerHealth.DamagePlayerExternal(GetDamage(), 20f, direction, false, "Enemy",
			DcFlags.BypassEvade | DcFlags.BossDamage | DcFlags.FinalBossDamage);
		Explode();
	}

	private void Explode()
	{
		explosion.SetActive(true);
		explosion.transform.SetParent(null);
		Destroy(gameObject);
		Destroy(trail);
	}

	private float GetDamage()
	{
		return (1f + CombatScaling.GetDamageMultiplierAddition(out _, out _, out _)) * 18f;
	}
}
