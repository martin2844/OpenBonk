using UnityEngine;

namespace Assets.Scripts.Inventory__Items__Pickups.Items.ItemProjectiles
{
	public class ItemProjectileOrb : ItemProjectile
	{
		private float startTime;

		private float hoverTime;

		private float offsetTime;

		private float spinSpeed;

		private float currentAngle;

		private float moveTimer;

		private Vector3 offset;

		private Transform orbitTarget;

		private Vector3 defaultScale;

		private bool fired;

		public GameObject fireSfx;

		private Vector3 movementDirection;

		protected override void Init()
		{
		}

		private void FireOrb()
		{
		}

		protected override void Step()
		{
		}

		protected override Vector3 GetMovementDirection()
		{
			return default(Vector3);
		}

		protected void StepHoverMovement()
		{
		}

		protected override void ProjectileDone()
		{
		}
	}
}
