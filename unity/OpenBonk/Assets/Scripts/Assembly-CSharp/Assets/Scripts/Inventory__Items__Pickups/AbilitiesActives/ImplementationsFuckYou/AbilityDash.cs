using UnityEngine;
using Assets.Scripts.Utility;

namespace Assets.Scripts.Inventory__Items__Pickups.AbilitiesActives.ImplementationsFuckYou
{
	public class AbilityDash : ActiveAbility
	{
		private float dashDuration = 0.2f;

		private float dashOverAtTime;

		private bool isDashing;

		private Vector3 dashDir;

		private Vector3 preDashVel;

		private float dashSpeed = 40f;

		private float dashSpeedToUse;

		public override void Init()
		{
		}

		public override void Cleanup()
		{
		}

		public override void UseImplementation()
		{
			isDashing = true;
			dashOverAtTime = MyTime.time + dashDuration;
			PlayerMovement movement = GameManager.Instance.GetPlayerMovement();
			movement.isDashing = true;
			movement.GetRb().useGravity = false;
			dashDir = movement.GetWishDir();
			if (movement.IsTouchingGround()) dashDir = Vector3.ProjectOnPlane(dashDir, movement.DashSurfaceNormal).normalized;
			preDashVel = movement.GetVelocity();
			preDashVel.y = 0f;
			dashSpeedToUse = dashSpeed;
			if (preDashVel.magnitude > dashSpeed) dashSpeedToUse = preDashVel.magnitude + dashSpeed * 0.5f;
			Physics.IgnoreLayerCollision(LayerMask.NameToLayer("Player"), LayerMask.NameToLayer("Enemy"), true);
		}

		private void DashFinished()
		{
			isDashing = false;
			PlayerMovement movement = GameManager.Instance.GetPlayerMovement();
			movement.isDashing = false;
			movement.GetRb().useGravity = false;
			movement.GetRb().velocity = dashDir * preDashVel.magnitude;
			Physics.IgnoreLayerCollision(LayerMask.NameToLayer("Player"), LayerMask.NameToLayer("Enemy"), false);
		}

		public override void Tick()
		{
			if (!isDashing) return;
			GameManager.Instance.GetPlayerMovement().GetRb().velocity = dashDir * dashSpeedToUse;
			if (MyTime.time >= dashOverAtTime) DashFinished();
		}

		public override float GetCooldown()
		{
			return 1.5f;
		}
	}
}
