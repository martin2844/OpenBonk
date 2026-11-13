using System;
using UnityEngine;

namespace MilkShake
{
	[Serializable]
	public class ShakeInstance
	{
		public ShakeParameters ShakeParameters;

		public float StrengthScale;

		public float RoughnessScale;

		public bool RemoveWhenStopped;

		private int baseSeed;

		private float seed1;

		private float seed2;

		private float seed3;

		private float noiseTimer;

		private float fadeTimer;

		private float fadeInTime;

		private float fadeOutTime;

		private float pauseTimer;

		private float pauseFadeTime;

		private int lastUpdatedFrame;

		public ShakeState State { get; private set; }

		public bool IsPaused { get; private set; }

		public bool IsFinished => State == ShakeState.Stopped && RemoveWhenStopped;

		public float CurrentStrength => ShakeParameters.Strength * fadeTimer * StrengthScale;

		public float CurrentRoughness => ShakeParameters.Roughness * fadeTimer * RoughnessScale;

		public ShakeInstance(int? seed = null)
		{
			baseSeed = seed ?? UnityEngine.Random.Range(-10000, 10000);
			seed1 = baseSeed * .5f;
			seed2 = baseSeed / 3f;
			seed3 = baseSeed * .25f;
			noiseTimer = baseSeed;
			StrengthScale = RoughnessScale = 1f;
		}

		public ShakeInstance(IShakeParameters shakeData, int? seed = null) : this(seed)
		{
			ShakeParameters = new ShakeParameters(shakeData);
			fadeInTime = shakeData.FadeIn;
			fadeOutTime = shakeData.FadeOut;
			State = ShakeState.FadingIn;
		}

		public ShakeResult UpdateShake(float deltaTime)
		{
			var result = new ShakeResult { PositionShake = getPositionShake(), RotationShake = getRotationShake() };
			if (Time.frameCount == lastUpdatedFrame) return result;
			if (pauseFadeTime > 0f) pauseTimer += (IsPaused ? 1f : -1f) * deltaTime / pauseFadeTime;
			pauseTimer = Mathf.Clamp01(pauseTimer);
			noiseTimer += (1f - pauseTimer) * deltaTime * CurrentRoughness;
			if (State == ShakeState.FadingIn) fadeTimer = fadeInTime > 0f ? fadeTimer + deltaTime / fadeInTime : 1f;
			else if (State == ShakeState.FadingOut) fadeTimer = fadeOutTime > 0f ? fadeTimer - deltaTime / fadeOutTime : 0f;
			fadeTimer = Mathf.Clamp01(fadeTimer);
			if (fadeTimer == 0f) State = ShakeState.Stopped;
			else if (fadeTimer == 1f)
			{
				if (ShakeParameters.ShakeType == ShakeType.OneShot) Stop(ShakeParameters.FadeOut, true);
				else if (ShakeParameters.ShakeType == ShakeType.Sustained) State = ShakeState.Sustained;
			}
			lastUpdatedFrame = Time.frameCount;
			return result;
		}

		public void Start(float fadeTime)
		{
			fadeInTime = fadeTime;
			State = ShakeState.FadingIn;
		}

		public void Stop(float fadeTime, bool removeWhenStopped)
		{
			fadeOutTime = fadeTime;
			RemoveWhenStopped = removeWhenStopped;
			State = ShakeState.FadingOut;
		}

		public void Pause(float fadeTime)
		{
			IsPaused = true;
			pauseFadeTime = fadeTime;
			if (fadeTime <= 0f) pauseTimer = 1f;
		}

		public void Resume(float fadeTime)
		{
			IsPaused = false;
			pauseFadeTime = fadeTime;
			if (fadeTime <= 0f) pauseTimer = 0f;
		}

		public void TogglePaused(float fadeTime)
		{
			if (IsPaused) Resume(fadeTime); else Pause(fadeTime);
		}

		private Vector3 getPositionShake()
		{
			var noise = new Vector3(getNoise(noiseTimer + seed1, baseSeed), getNoise(baseSeed, noiseTimer), getNoise(noiseTimer + seed3, baseSeed + noiseTimer));
			return Vector3.Scale(noise, ShakeParameters.PositionInfluence) * CurrentStrength;
		}

		private Vector3 getRotationShake()
		{
			var noise = new Vector3(getNoise(noiseTimer - baseSeed, seed3), getNoise(baseSeed, noiseTimer + seed2), getNoise(baseSeed + noiseTimer, noiseTimer + seed1));
			return Vector3.Scale(noise, ShakeParameters.RotationInfluence) * CurrentStrength;
		}

		private float getNoise(float x, float y)
		{
			return (Mathf.PerlinNoise(x, y) - .5f) * 2f;
		}
	}
}
