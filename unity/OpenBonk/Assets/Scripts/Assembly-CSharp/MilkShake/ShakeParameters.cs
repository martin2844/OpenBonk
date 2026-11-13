using System;
using UnityEngine;

namespace MilkShake
{
	[Serializable]
	public class ShakeParameters : IShakeParameters
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

		public ShakeParameters()
		{
		}

		public ShakeParameters(IShakeParameters original)
		{
			ShakeType = original.ShakeType;
			Strength = original.Strength;
			Roughness = original.Roughness;
			FadeIn = original.FadeIn;
			FadeOut = original.FadeOut;
			PositionInfluence = original.PositionInfluence;
			RotationInfluence = original.RotationInfluence;
		}
	}
}
