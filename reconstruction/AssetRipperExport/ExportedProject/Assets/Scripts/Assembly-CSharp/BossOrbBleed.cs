using Assets.Scripts.Actors.Enemies;
using Assets.Scripts.Actors;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Game.Combat;
using Assets.Scripts.Utility;
using UnityEngine;
using UnityEngine.UIElements.Experimental;

public class BossOrbBleed : MonoBehaviour
{
	public Rigidbody rb;

	public SphereCollider collider;

	public GameObject explosion;

	public GameObject trail;

	public RandomSfx randomSfx;

	private Enemy boss;

	private float acceleration = 90f;

	private bool isFired;

	private float speed = 20f;

	private float moveAtTime;

	private float destroyAtTime;

	private Vector3 offset;

	private Vector3 velocity;

	private float moveTimer;

	private float moveOverSeconds = 1.5f;

	public float spinSpeed = 90f;

	private float currentAngle;

	protected void Start()
	{
		trail.transform.SetParent(null);
	}

	public void Set(Enemy boss, int currentPhase, int numOrbs, int orbIndex)
	{
		this.boss = boss;
		acceleration = 3000f + currentPhase * 300f;
		destroyAtTime = MyTime.time + 8f + currentPhase;
		moveAtTime = MyTime.time + moveOverSeconds + 1.5f + orbIndex * 0.5f;
		if (numOrbs >= 2)
		{
			float angle = 360f / numOrbs * orbIndex * Mathf.Deg2Rad;
			offset = new Vector3(Mathf.Cos(angle) * 15f, 0f, Mathf.Sin(angle) * 15f) + Vector3.down * 15f;
		}
	}

	private void FixedUpdate()
	{
		if (isFired) { Movement(); return; }
		FloatMovement();
		if (MyTime.time > moveAtTime) ShootOrb();
	}

	private void Update()
	{
		trail.transform.position = rb.position;
		if (MyTime.time > destroyAtTime) Explode();
	}

	private void ShootOrb()
	{
		isFired = true;
		randomSfx.Play();
		rb.drag = 0.7f;
	}

	private void Movement()
	{
		Vector3 direction = (MyPlayer.Instance.transform.position - transform.position).normalized;
		rb.AddForce(direction * acceleration * MyTime.fixedDeltaTime);
	}

	private void FloatMovement()
	{
		if (moveTimer < 1f) moveTimer = Mathf.Clamp01(moveTimer + MyTime.fixedDeltaTime / moveOverSeconds);
		float move = Easing.InOutCirc(moveTimer);
		currentAngle += spinSpeed * MyTime.fixedDeltaTime;
		Vector3 rotatedOffset = Quaternion.Euler(0f, currentAngle, 0f) * offset;
		rb.MovePosition(boss.GetCenterPosition() + (rotatedOffset + Vector3.up * (boss.GetHeight() * 0.5f)) * move);
	}

	private void OnCollisionEnter(Collision collision)
	{
		if (collision.gameObject.layer != LayerMask.NameToLayer("Player")) return;
		Explode();
		Collider[] players = Physics.OverlapSphere(rb.transform.position, collider.radius * transform.localScale.x * 1.2f, GameManager.Instance.whatIsPlayer);
		if (collision.gameObject.layer == LayerMask.NameToLayer("Player") || players.Length > 0)
		{
			Vector3 direction = (MyPlayer.Instance.transform.position - transform.position).normalized;
			MyPlayer.Instance.inventory.playerHealth.DamagePlayerExternal(GetDamage(), 20f, direction, false, "Enemy",
				DcFlags.BypassEvade | DcFlags.BossDamage | DcFlags.FinalBossDamage);
			MyPlayer.Instance.inventory.statusEffects.BleedPlayer(5f);
		}
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
