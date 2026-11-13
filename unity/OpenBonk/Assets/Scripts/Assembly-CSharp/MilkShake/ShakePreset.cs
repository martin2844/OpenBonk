using UnityEngine;

namespace MilkShake
{
	[CreateAssetMenu(fileName = "New Shake Preset", menuName = "MilkShake/Shake Preset")]
	public class ShakePreset : ScriptableObject, IShakeParameters
	{
		[Header("Shake Type")]
		[SerializeField]
		private ShakeType shakeType;

		[Header("Shake Strength")]
		[SerializeField]
		private float strength;

		[SerializeField]
		private float roughness;

		[Header("Fade")]
		[SerializeField]
		private float fadeIn;

		[SerializeField]
		private float fadeOut;

		[Header("Shake Influence")]
		[SerializeField]
		private Vector3 positionInfluence;

		[SerializeField]
		private Vector3 rotationInfluence;

		public ShakeType ShakeType
		{
			get => shakeType;
			set => shakeType = value;
		}

		public float Strength
		{
			get => strength;
			set => strength = value;
		}

		public float Roughness
		{
			get => roughness;
			set => roughness = value;
		}

		public float FadeIn
		{
			get => fadeIn;
			set => fadeIn = value;
		}

		public float FadeOut
		{
			get => fadeOut;
			set => fadeOut = value;
		}

		public Vector3 PositionInfluence
		{
			get => positionInfluence;
			set => positionInfluence = value;
		}

		public Vector3 RotationInfluence
		{
			get => rotationInfluence;
			set => rotationInfluence = value;
		}
	}
}
