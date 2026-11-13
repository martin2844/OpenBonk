using System.Collections.Generic;
using Assets.Scripts.Actors;
using Assets.Scripts.Actors.Enemies;
using Assets.Scripts.Inventory.Stats;
using Assets.Scripts.Game.Combat.EnemyDebuffs;
using Assets.Scripts.Game.Combat.EnemyDebuffs.Implementations;
using Assets.Scripts.Utility;
using UnityEngine;

namespace Actors.Enemies
{
	public class EnemyMovementRb : MonoBehaviour
	{
		public enum State
		{
			Normal = 0,
			Sucked = 1,
			Charmed = 2
		}

		public Enemy enemy;

		public Rigidbody rb;

		private float nextStepTime;

		private Vector3 offsetBias;

		private Vector3 desiredVelocity;

		private Quaternion desiredRotation;

		private float knockbackResetSpeed = 2.5f;

		private Vector3 knockbackVelocity;

		private float randomOffset;

		private float randomGroundedCheckOffset;

		public State state;

		private float flyingKnockupVel;

		private bool canRotate = true;

		private string ignoreTag = "Ignore";

		private bool isClimbing;

		private float nextClimbCheckTime;

		private float nextGroundedCheckTime;

		private float groundCheckInterval = 0.25f;

		private bool dashing;

		private float dashStopTime;

		private Vector3 dashDirection;

		private float dashSpeed;

		private bool isDashingWall;

		private HashSet<EDebuff> debuffs = new HashSet<EDebuff>();

		private Vector3 baseVelocity;

		private const float baseRotationSpeed = 10f;

		private float rotationSpeed = 10f;

		private Vector3 flyingOffset = Vector3.up * 1.6f;

		public float distanceToTarget;

		private bool isStationary;

		private float nextGetSpeedTime;

		private float getSpeedCooldown = 2f;

		private float storedSpeed;

		private static float knockbackConstant = 10f;

		private const float maxKnockback = 5f;

		private const float maxBossKnockback = 2.25f;

		private float maxKnockbackVelSqrBoss = 10.125f;

		private bool isBoss;

		private float knockbackResistance;

		private float lastFoundKnockbackResTime;

		private Transform suckTarget;

		private float totalSuckTime;

		private float totalSuckTimeMax = 15f;

		private float nextCheckDamageTime;

		public bool grounded { get; private set; } = true;

		private void Awake()
		{
			MyTime.A_TimeScaleChange += TimescaleVelocity;
		}

		private void OnDestroy()
		{
			MyTime.A_TimeScaleChange -= TimescaleVelocity;
		}

		public void Init()
		{
			nextGetSpeedTime = 0f;
			grounded = true;
			state = State.Normal;
			totalSuckTime = 0f;
			if (enemy.IsImportantEnemy()) offsetBias = Vector3.zero;
			else
			{
				Vector3 direction = Random.insideUnitSphere;
				direction.y = 0f;
				offsetBias = direction.normalized * Random.Range(0.5f, 1f);
			}
			knockbackVelocity = Vector3.zero;
			rb.useGravity = !enemy.enemyData.isFlying;
			randomOffset = Random.Range(0f, 1f);
			randomGroundedCheckOffset = Random.Range(0f, 0.35f);
			nextGroundedCheckTime = MyTime.time + randomGroundedCheckOffset;
			distanceToTarget = 100f;
			dashing = false;
			debuffs.Clear();
			isBoss = enemy.IsBoss();
			float speed = enemy.GetSpeed();
			maxKnockbackVelSqrBoss = speed * speed + 5.0625f;
			totalSuckTimeMax = enemy.enemyData.maxSuckTime;
		}

		public void MyFixedUpdate()
		{
			if (state == State.Sucked)
			{
				totalSuckTime += MyTime.fixedDeltaTime;
				if (totalSuckTime > totalSuckTimeMax) state = State.Normal;
			}
			if (!enemy.CanMove()) return;
			if (MyTime.time >= nextStepTime) FindNextPosition();
			float vertical = enemy.enemyData.isFlying ? desiredVelocity.y : rb.velocity.y;
			if (enemy.enemyData.isFlying && flyingKnockupVel > 0f)
			{
				vertical += flyingKnockupVel;
				flyingKnockupVel += Physics.gravity.y * MyTime.fixedDeltaTime;
			}
			float climbVelocity = TryClimbWall();
			if (isClimbing) vertical = climbVelocity;
			CheckGrounded();
			if (knockbackVelocity != Vector3.zero)
			{
				knockbackVelocity = Vector3.Lerp(knockbackVelocity, Vector3.zero, MyTime.fixedDeltaTime * knockbackResetSpeed);
				if (knockbackVelocity.sqrMagnitude <= 1f) knockbackVelocity = Vector3.zero;
			}
			rb.velocity = new Vector3(desiredVelocity.x + knockbackVelocity.x, vertical, desiredVelocity.z + knockbackVelocity.z);
			if ((state == State.Normal || state == State.Charmed) && canRotate &&
				Mathf.Abs(desiredRotation.eulerAngles.y - rb.rotation.eulerAngles.y) > 8f)
				rb.MoveRotation(Quaternion.Lerp(rb.rotation, desiredRotation, Time.deltaTime * rotationSpeed));
		}

		private float TryClimbWall()
		{
			if (!isClimbing)
			{
				if (nextClimbCheckTime > MyTime.time) return 0f;
				nextClimbCheckTime = MyTime.time + 1f + randomOffset;
			}
			if (enemy == null || enemy.collider == null || GameManager.Instance == null)
			{
				isClimbing = false;
				return 0f;
			}
			Vector3 direction = GetTargetPosition() - enemy.GetCenterPosition();
			direction.y = 0f;
			float radius = enemy.collider.radius;
			RaycastHit hit;
			bool found;
			if (enemy.IsImportantEnemy())
			{
				float castRadius = radius * .75f;
				found = Physics.SphereCast(enemy.GetBottomPosition() + Vector3.up * (castRadius + .1f), castRadius, direction, out hit, radius + 1f, GameManager.Instance.whatIsGround);
			}
			else
			{
				found = Physics.Raycast(enemy.GetBottomPosition() + Vector3.up * .1f, direction, out hit, radius + 1f, GameManager.Instance.whatIsGroundAndObjects);
			}
			isClimbing = found && hit.collider != null && !hit.collider.CompareTag(ignoreTag);
			return isClimbing ? 8f : 0f;
		}

		private void CheckGrounded()
		{
			if (nextGroundedCheckTime > MyTime.time) return;
			if (enemy == null || enemy.enemyData == null || enemy.enemyData.isFlying)
			{
				grounded = false;
				return;
			}
			nextGroundedCheckTime = MyTime.time + randomGroundedCheckOffset + groundCheckInterval;
			grounded = GameManager.Instance != null && Physics.Raycast(enemy.GetGroundCheckPosition(), Vector3.down, 1.5f, GameManager.Instance.whatIsGroundAndObjects);
		}

		private Vector3 GetTargetPosition()
		{
			if (state == State.Sucked) return suckTarget.position;
			if (enemy.IsRunningFromPlayer()) return transform.position * 2f - enemy.target.position;
			return enemy.state == EEnemyState.FollowTarget ? enemy.followTarget.position : enemy.target.position;
		}

		public void DashStart(Vector3 dir, float dashTime, float dashSpeed)
		{
			if (rb == null) return;
			rb.isKinematic = false;
			this.dashSpeed = dashSpeed;
			dashDirection = new Vector3(dir.x, 0f, dir.z).normalized;
			dashing = true;
			dashStopTime = MyTime.time + dashTime;
			if (dir != Vector3.zero) rb.MoveRotation(Quaternion.LookRotation(dir));
			isDashingWall = false;
		}

		public void SetDashDirection(Vector3 dir)
		{
			if (rb != null && dir != Vector3.zero) rb.MoveRotation(Quaternion.LookRotation(dir));
		}

		private void Dashing()
		{
			if (rb == null || enemy == null || enemy.collider == null || GameManager.Instance == null) return;
			float vertical = rb.velocity.y;
			float radius = enemy.collider.radius;
			float castRadius = radius * .75f;
			Vector3 targetDirection = GetTargetPosition() - enemy.GetCenterPosition();
			targetDirection.y = 0f;
			if (Physics.SphereCast(enemy.GetBottomPosition() + Vector3.up * (castRadius + .1f), castRadius,
				targetDirection.normalized, out _, radius + 1f, GameManager.Instance.whatIsGround))
			{
				isDashingWall = true;
				vertical = 15f;
			}
			else
			{
				if (isDashingWall) vertical = 10f;
				isDashingWall = false;
				if (Physics.Raycast(transform.position, Vector3.down, out RaycastHit hit, 1000f, GameManager.Instance.whatIsGround))
					dashDirection = Vector3.ProjectOnPlane(dashDirection, hit.normal).normalized;
			}
			rb.velocity = new Vector3(dashDirection.x * dashSpeed, vertical, dashDirection.z * dashSpeed);
		}

		public void StopDash()
		{
			dashing = false;
		}

		public void StopMovement()
		{
			rb.isKinematic = true;
		}

		public void StartMovement()
		{
			rb.isKinematic = false;
		}

		private void SetVelocity(Vector3 vel)
		{
			if (enemy.debuffs.ContainsKey(EDebuff.Stun)) desiredVelocity = Vector3.zero;
			else if (enemy.debuffs.ContainsKey(EDebuff.Freeze)) desiredVelocity = vel * DebuffIce.slowMultiplier;
			else desiredVelocity = vel;
			baseVelocity = desiredVelocity;
			TimescaleVelocity();
		}

		private void TimescaleVelocity()
		{
			desiredVelocity = baseVelocity * MyTime.timeScale;
			rotationSpeed = 10f * MyTime.timeScale;
		}

		public void FindNextPosition()
		{
			Vector3 targetPosition = GetTargetPosition();
			distanceToTarget = Vector3.Distance(targetPosition, transform.position);
			nextStepTime = GetNextStepTime(distanceToTarget);
			groundCheckInterval = GetNextGroundCheckOffset(distanceToTarget);
			float bias = distanceToTarget < 20f ? 0f : Mathf.Min(distanceToTarget * 0.4f, 50f);
			targetPosition = GetTargetPosition() + offsetBias * bias;
			Vector3 direction;
			if (enemy.enemyData.isFlying)
			{
				targetPosition += flyingOffset;
				direction = (targetPosition + Vector3.up - rb.position).normalized;
			}
			else
			{
				direction = targetPosition - rb.position;
				direction.y = 0f;
				direction.Normalize();
			}
			SetVelocity(direction * GetSpeed());
			Vector3 facing = targetPosition - enemy.transform.position;
			facing.y = 0f;
			desiredRotation = facing == Vector3.zero ? Quaternion.identity : Quaternion.LookRotation(facing);
			canRotate = !enemy.debuffs.ContainsKey(EDebuff.Stun);
		}

		public void SetDesiredRotation(Vector3 targetPos)
		{
			Vector3 direction = targetPos - enemy.GetCenterPosition();
			direction.y = 0f;
			desiredRotation = Quaternion.LookRotation(direction);
			rb.MoveRotation(desiredRotation);
		}

		private float GetSpeed()
		{
			if (MyTime.time >= nextGetSpeedTime)
			{
				nextGetSpeedTime = MyTime.time + getSpeedCooldown;
				storedSpeed = enemy.GetSpeed();
			}
			return state == State.Sucked ? Mathf.Min(storedSpeed, 20f) : storedSpeed;
		}

		public void MyUpdate()
		{
		}

		public void Pause(bool isPaused)
		{
			rb.constraints = isPaused ? RigidbodyConstraints.FreezeAll : RigidbodyConstraints.FreezeRotation;
		}

		public void KnockUp(float knockbackForce)
		{
			if (rb.isKinematic || enemy.enemyData.nukeProtection) return;
			if (knockbackForce + 1f - EnemyStats.GetKnockbackResistance(enemy) * 0.5f <= 0f) return;
			float velocity = Mathf.Min(20f, knockbackForce * knockbackConstant * 0.5f);
			if (enemy.enemyData.isFlying) flyingKnockupVel = velocity;
			else if (!rb.isKinematic) rb.velocity = new Vector3(rb.velocity.x, velocity, rb.velocity.z);
		}

		public void Knockback(DamageContainer dc)
		{
			if (!rb.isKinematic && dc.knockback > 0f) Knockback(dc.direction, dc.knockback);
		}

		public void Knockback(Vector3 dir, float knockback)
		{
			float force = knockback + 1f;
			if (force <= 0f) return;
			if (MyTime.time > lastFoundKnockbackResTime)
			{
				// The shipped implementation stores fixedDeltaTime here, rather than time.
				lastFoundKnockbackResTime = MyTime.fixedDeltaTime;
				knockbackResistance = EnemyStats.GetKnockbackResistance(enemy);
			}
			force -= knockbackResistance;
			force = Mathf.Min(force, isBoss ? maxBossKnockback : maxKnockback);
			if (force <= 0f || (isBoss && knockbackVelocity.sqrMagnitude > maxKnockbackVelSqrBoss)) return;
			knockbackVelocity += dir.normalized * force * knockbackConstant;
		}

		public void Suck(Transform target)
		{
			if (state == State.Sucked || totalSuckTime >= totalSuckTimeMax) return;
			suckTarget = target;
			state = State.Sucked;
			FindNextPosition();
		}

		public void StopSuck()
		{
			state = State.Normal;
		}

		private bool CanFindNextPosition()
		{
			return MyTime.time >= nextStepTime;
		}

		private float GetNextStepTime(float distanceToTarget)
		{
			if (distanceToTarget < 20f || MyTime.finalSwarmTimer > 60f) return MyTime.time;
			if (distanceToTarget < 40f) return MyTime.time + 0.2f;
			if (distanceToTarget < 80f) return MyTime.time + 0.5f;
			if (distanceToTarget < 150f) return MyTime.time + 1f;
			return MyTime.time + (distanceToTarget < 300f ? 3f : 5f);
		}

		private float GetNextGroundCheckOffset(float distanceToTarget)
		{
			if (distanceToTarget < 20f) return 0.5f;
			if (distanceToTarget < 40f) return 0.8f;
			if (distanceToTarget < 80f) return 1.5f;
			if (distanceToTarget < 150f) return 2.5f;
			if (distanceToTarget < 300f) return 4f;
			return MyTime.time + 5f;
		}
	}
}
