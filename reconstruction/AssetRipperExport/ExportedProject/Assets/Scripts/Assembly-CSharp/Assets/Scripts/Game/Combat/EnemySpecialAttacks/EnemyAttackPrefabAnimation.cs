using UnityEngine;
using Assets.Scripts.Utility;

namespace Assets.Scripts.Game.Combat.EnemySpecialAttacks
{
	public class EnemyAttackPrefabAnimation : MonoBehaviour
	{
		public AnimationCurve animationCurveScale;

		public float animationDuration = 1f;

		private Vector3 defaultSize;

		private float animationTime;

		private void Awake()
		{
			defaultSize = transform.localScale;
		}

		private void Update()
		{
			animationTime += MyTime.deltaTime / animationDuration;
			if (animationCurveScale != null)
				transform.localScale = defaultSize * animationCurveScale.Evaluate(Mathf.Clamp01(animationTime));
		}
	}
}
